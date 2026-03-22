using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class TurmaService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public TurmaService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    // ──────────────────────────────────────────────────────────────────────
    // CRIAR TURMA
    // Valida RN-TUR01 (choque professor), RN-TUR02 (choque sala)
    // CORREÇÃO: Removido professor.Tipo == "Assistente" (tipo inexistente no sistema)
    // ──────────────────────────────────────────────────────────────────────
    public async Task<ObterTurmaResponse> CriarTurmaAsync(CriarTurmaRequest request)
    {
        var salaDecoded = _hashids.Decode(request.SalaIdHash);
        var ritmoDecoded = _hashids.Decode(request.RitmoIdHash);

        if (salaDecoded.Length == 0 || ritmoDecoded.Length == 0)
            throw new RegraNegocioException("ID de Sala ou Ritmo inválido.");

        int salaIdReal = salaDecoded[0];
        int ritmoIdReal = ritmoDecoded[0];

        var professoresIdsReais = new List<int>();
        foreach (var profHash in request.ProfessoresIdsHash)
        {
            var profDecoded = _hashids.Decode(profHash);
            if (profDecoded.Length == 0) throw new RegraNegocioException($"O ID de professor '{profHash}' é inválido.");
            professoresIdsReais.Add(profDecoded[0]);
        }

        var sala = await _context.Salas.FindAsync(salaIdReal) ?? throw new RegraNegocioException("Sala não encontrada.");

        if (request.LimiteAlunos > sala.CapacidadeMaxima)
            throw new RegraNegocioException($"O limite de alunos ({request.LimiteAlunos}) excede a capacidade máxima da {sala.Nome} ({sala.CapacidadeMaxima}).");

        var diaDaSemanaEnum = (DayOfWeek)request.DiaDaSemana;

        // RN-TUR02: Choque de sala no mesmo dia e horário
        bool choqueSala = await _context.Turmas.AnyAsync(t =>
            t.SalaId == salaIdReal &&
            t.DiaDaSemana == diaDaSemanaEnum &&
            t.Ativa &&
            (request.HorarioInicio < t.HorarioFim && request.HorarioFim > t.HorarioInicio));

        if (choqueSala)
            throw new RegraNegocioException("Já existe uma turma ativa nesta sala, neste mesmo dia e horário.");

        foreach (var profId in professoresIdsReais)
        {
            var professor = await _context.Usuarios.FindAsync(profId);

            // CORREÇÃO: Removido 'Assistente' — tipo inexistente no discriminador TPH.
            // No sistema só existem: Aluno, Professor, Bolsista, Gerente, Recepção, Líder.
            // Manter 'Assistente' gerava uma condição que nunca seria verdadeira.
            if (professor == null || professor.Tipo != "Professor")
                throw new RegraNegocioException($"O usuário '{professor?.Nome}' não tem permissão para ser professor da turma. Apenas usuários do tipo 'Professor' podem lecionar.");

            // RN-TUR01: Choque de professor no mesmo dia e horário em outra sala
            bool choqueProfessor = await _context.TurmaProfessores
                .Include(tp => tp.Turma)
                .AnyAsync(tp =>
                    tp.ProfessorId == profId &&
                    tp.Turma.DiaDaSemana == diaDaSemanaEnum &&
                    tp.Turma.Ativa &&
                    (request.HorarioInicio < tp.Turma.HorarioFim && request.HorarioFim > tp.Turma.HorarioInicio));

            if (choqueProfessor)
                throw new RegraNegocioException($"O professor {professor.Nome} já possui uma turma neste mesmo dia e horário em outra sala.");
        }

        var turma = new Turma(ritmoIdReal, salaIdReal, request.DataInicio, diaDaSemanaEnum,
            request.HorarioInicio, request.HorarioFim, request.Nivel,
            request.LimiteAlunos, request.LinkWhatsApp);

        _context.Turmas.Add(turma);

        foreach (var profId in professoresIdsReais)
            _context.TurmaProfessores.Add(new TurmaProfessor { Turma = turma, ProfessorId = profId });

        await _context.SaveChangesAsync();

        // Carrega referências para o Mapper poder acessar nomes
        await _context.Entry(turma).Reference(t => t.Ritmo).LoadAsync();
        await _context.Entry(turma).Reference(t => t.Sala).LoadAsync();
        await _context.Entry(turma).Collection(t => t.Professores).Query().Include(p => p.Professor).LoadAsync();

        return turma.ToResponse(_hashids);
    }

    // ──────────────────────────────────────────────────────────────────────
    // LISTAR TURMAS COM FILTROS OPCIONAIS
    // NOVO: Parâmetro opcional `apenasAtivas` — permite filtrar só turmas ativas
    //       sem quebrar o endpoint GET / existente que não usa este filtro
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterTurmaResponse>> ListarTurmasAsync(
        string? ritmoIdHash,
        string? professorIdHash,
        int? diaDaSemana,
        TimeSpan? horario,
        bool? apenasAtivas = null) // NOVO parâmetro — null = sem filtro (comportamento original)
    {
        var query = _context.Turmas
            .Include(t => t.Ritmo)
            .Include(t => t.Sala)
            .Include(t => t.Matriculas)
            .Include(t => t.Professores).ThenInclude(tp => tp.Professor)
            .AsQueryable();

        // NOVO: Aplica filtro de ativas/inativas somente se o parâmetro for fornecido
        // null = retorna todas (comportamento original do GET /)
        // true  = retorna só as ativas (GET /listar-ativas)
        // false = retorna só as inativas (caso de uso futuro)
        if (apenasAtivas.HasValue)
            query = query.Where(t => t.Ativa == apenasAtivas.Value);

        if (!string.IsNullOrEmpty(ritmoIdHash))
        {
            var ritmoDecoded = _hashids.Decode(ritmoIdHash);
            if (ritmoDecoded.Length > 0)
                query = query.Where(t => t.RitmoId == ritmoDecoded[0]);
        }

        if (!string.IsNullOrEmpty(professorIdHash))
        {
            var profDecoded = _hashids.Decode(professorIdHash);
            if (profDecoded.Length > 0)
                query = query.Where(t => t.Professores.Any(p => p.ProfessorId == profDecoded[0]));
        }

        if (diaDaSemana.HasValue)
        {
            var diaEnum = (DayOfWeek)diaDaSemana.Value;
            query = query.Where(t => t.DiaDaSemana == diaEnum);
        }

        if (horario.HasValue)
            query = query.Where(t => horario.Value >= t.HorarioInicio && horario.Value < t.HorarioFim);

        var turmas = await query.ToListAsync();
        return turmas.Select(t => t.ToResponse(_hashids));
    }

    // ──────────────────────────────────────────────────────────────────────
    // NOVO: LISTAR TURMAS DO USUÁRIO LOGADO
    // Lê a role do JWT para decidir a lógica de filtro:
    //   → Professor: turmas onde ele é o professor vinculado (TurmaProfessor)
    //   → Aluno/Bolsista/Líder: turmas onde ele tem matrícula formal (Matricula)
    //
    // IMPORTANTE sobre Bolsistas: O RN-BOL09 diz que o bolsista NÃO se matricula
    // formalmente em turmas de dança de salão. Por isso, turmas de salão NÃO
    // aparecem aqui para o bolsista. Elas são exibidas em TurmasObrigatorias (Sprint 2).
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterTurmaResponse>> ListarMinhasTurmasAsync(int usuarioId, string role)
    {
        var query = _context.Turmas
            .Include(t => t.Ritmo)
            .Include(t => t.Sala)
            .Include(t => t.Matriculas)
            .Include(t => t.Professores).ThenInclude(tp => tp.Professor)
            .Where(t => t.Ativa)  // Sempre só turmas ativas nas listas pessoais
            .AsQueryable();

        if (role == "Professor")
        {
            // Professor vê as turmas onde ele está vinculado como professor
            // A tabela TurmaProfessor é o N:N entre Turma e Professor
            query = query.Where(t => t.Professores.Any(tp => tp.ProfessorId == usuarioId));
        }
        else
        {
            // Aluno, Bolsista, Líder: veem turmas onde têm Matricula formal
            // A entidade Matricula registra a inscrição formal de um usuário em uma turma
            query = query.Where(t => t.Matriculas.Any(m => m.AlunoId == usuarioId));
        }

        var turmas = await query.ToListAsync();
        return turmas.Select(t => t.ToResponse(_hashids));
    }

    // ──────────────────────────────────────────────────────────────────────
    // TROCAR SALA — sem alterações
    // ──────────────────────────────────────────────────────────────────────
    public async Task TrocarSalaAsync(int turmaId, int novaSalaId, int novoLimiteAlunos)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.ListaDeEspera)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        int totalMatriculados = turma.Matriculas.Count;

        var novaSala = await _context.Salas.FindAsync(novaSalaId)
            ?? throw new RegraNegocioException("Nova sala não encontrada.");

        if (novoLimiteAlunos > novaSala.CapacidadeMaxima)
            throw new RegraNegocioException($"O novo limite ({novoLimiteAlunos}) excede a capacidade da {novaSala.Nome} ({novaSala.CapacidadeMaxima}).");

        if (novoLimiteAlunos < totalMatriculados)
            throw new RegraNegocioException($"Não é possível diminuir o limite para {novoLimiteAlunos}, pois a turma já possui {totalMatriculados} alunos matriculados.");

        bool choqueNovaSala = await _context.Turmas.AnyAsync(t =>
            t.Id != turmaId &&
            t.SalaId == novaSalaId &&
            t.DiaDaSemana == turma.DiaDaSemana &&
            t.Ativa &&
            (turma.HorarioInicio < t.HorarioFim && turma.HorarioFim > t.HorarioInicio));

        if (choqueNovaSala)
            throw new RegraNegocioException("A nova sala já está ocupada por outra turma neste mesmo dia e horário.");

        int vagasAnteriores = turma.LimiteAlunos - totalMatriculados;
        turma.AtualizarSalaELimite(novaSalaId, novoLimiteAlunos);
        int vagasNovas = turma.LimiteAlunos - totalMatriculados;

        if (vagasAnteriores <= 0 && vagasNovas > 0 && turma.ListaDeEspera.Any())
        {
            // TODO Sprint 5: Notificar via Firebase FCM os usuários na fila de espera
        }

        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // MATRICULAR ALUNO
    // NOVO: RN-TUR06 — bloqueia se o aluno já tiver turma no mesmo dia e horário
    //
    // Como funciona a detecção de sobreposição de horário:
    //   Intervalo A = [novaInicio, novaFim]
    //   Intervalo B = [outraInicio, outraFim]
    //   Há sobreposição quando: novaInicio < outraFim E novaFim > outraInicio
    //   
    //   Exemplos:
    //   A: 18h-20h, B: 19h-21h → 18 < 21 ✓ e 20 > 19 ✓ → CONFLITO
    //   A: 18h-20h, B: 20h-22h → 18 < 22 ✓ e 20 > 20 ✗ → SEM CONFLITO (adjacentes)
    //   A: 18h-20h, B: 16h-18h → 18 < 18 ✗ → SEM CONFLITO (adjacentes)
    // ──────────────────────────────────────────────────────────────────────
    public async Task<string> MatricularAlunoAsync(int turmaId, int alunoId, string papel)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.ListaDeEspera)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        var aluno = await _context.Usuarios.FindAsync(alunoId)
            ?? throw new RegraNegocioException("Aluno não encontrado.");

        // RN-TUR05: Aluno já matriculado nesta turma
        if (turma.Matriculas.Any(m => m.AlunoId == alunoId))
            throw new RegraNegocioException("O aluno já está matriculado nesta turma.");

        // Aluno já na fila de espera desta turma
        if (turma.ListaDeEspera.Any(e => e.AlunoId == alunoId))
            throw new RegraNegocioException("O aluno já está na fila de espera desta turma.");

        // RN-TUR06: Verifica se o aluno já tem OUTRA turma com horário conflitante neste dia
        // A query consulta todas as Matriculas do aluno no banco, navegando para a Turma
        // via FK — EF Core converte o m.Turma.X em JOIN automático no SQL gerado
        bool choqueHorario = await _context.Matriculas
            .AnyAsync(m =>
                m.AlunoId == alunoId &&
                m.Turma.Ativa &&
                m.Turma.DiaDaSemana == turma.DiaDaSemana &&
                turma.HorarioInicio < m.Turma.HorarioFim &&     // nova aula começa antes da outra terminar
                turma.HorarioFim > m.Turma.HorarioInicio);       // nova aula termina depois da outra começar

        if (choqueHorario)
        {
            // Traduz o DayOfWeek para português para exibir mensagem legível ao usuário
            var diaTexto = turma.DiaDaSemana switch
            {
                DayOfWeek.Monday => "segunda-feira",
                DayOfWeek.Tuesday => "terça-feira",
                DayOfWeek.Wednesday => "quarta-feira",
                DayOfWeek.Thursday => "quinta-feira",
                DayOfWeek.Friday => "sexta-feira",
                DayOfWeek.Saturday => "sábado",
                DayOfWeek.Sunday => "domingo",
                _ => "neste dia"
            };
            throw new RegraNegocioException(
                $"Você já possui uma turma na {diaTexto} com horário conflitante. " +
                $"Verifique sua grade antes de se matricular.");
        }

        // Turma cheia → entra na fila de espera (tabela Interesses)
        if (turma.Matriculas.Count >= turma.LimiteAlunos)
        {
            _context.Interesses.Add(new Interesse { TurmaId = turmaId, AlunoId = alunoId });
            await _context.SaveChangesAsync();
            return "A turma está cheia. Você foi adicionado à fila de espera.";
        }

        // Matrícula normal
        _context.Matriculas.Add(new Matricula { TurmaId = turmaId, AlunoId = alunoId, Papel = papel });
        await _context.SaveChangesAsync();
        return "Matrícula realizada com sucesso.";
    }

    // ──────────────────────────────────────────────────────────────────────
    // DESMATRICULAR ALUNO — sem alterações
    // ──────────────────────────────────────────────────────────────────────
    public async Task DesmatricularAlunoAsync(int turmaId, int alunoId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.ListaDeEspera)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        var matricula = turma.Matriculas.FirstOrDefault(m => m.AlunoId == alunoId);
        if (matricula != null)
        {
            _context.Matriculas.Remove(matricula);
        }
        else
        {
            var interesse = turma.ListaDeEspera.FirstOrDefault(e => e.AlunoId == alunoId);
            if (interesse != null)
                _context.Interesses.Remove(interesse);
            else
                throw new RegraNegocioException("O aluno não está matriculado nem na fila de espera desta turma.");
        }

        await _context.SaveChangesAsync();
    }
}