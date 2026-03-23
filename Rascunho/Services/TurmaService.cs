// Localização: Rascunho/Services/TurmaService.cs
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
            if (profDecoded.Length == 0)
                throw new RegraNegocioException($"O ID de professor '{profHash}' é inválido.");
            professoresIdsReais.Add(profDecoded[0]);
        }

        var sala = await _context.Salas.FindAsync(salaIdReal)
            ?? throw new RegraNegocioException("Sala não encontrada.");

        if (request.LimiteAlunos > sala.CapacidadeMaxima)
            throw new RegraNegocioException(
                $"O limite de alunos ({request.LimiteAlunos}) excede a capacidade " +
                $"máxima da {sala.Nome} ({sala.CapacidadeMaxima}).");

        var diaDaSemanaEnum = (DayOfWeek)request.DiaDaSemana;

        // RN-TUR02: Choque de sala no mesmo dia e horário
        bool choqueSala = await _context.Turmas.AnyAsync(t =>
            t.SalaId == salaIdReal &&
            t.DiaDaSemana == diaDaSemanaEnum &&
            t.Ativa &&
            request.HorarioInicio < t.HorarioFim &&
            request.HorarioFim > t.HorarioInicio);

        if (choqueSala)
            throw new RegraNegocioException(
                "Já existe uma turma ativa nesta sala, neste mesmo dia e horário.");

        foreach (var profId in professoresIdsReais)
        {
            var professor = await _context.Usuarios.FindAsync(profId);

            if (professor == null || professor.Tipo != "Professor")
                throw new RegraNegocioException(
                    $"O usuário '{professor?.Nome}' não pode ser professor da turma. " +
                    "Apenas usuários do tipo 'Professor' podem lecionar.");

            // RN-TUR01: Choque de professor no mesmo dia e horário
            bool choqueProfessor = await _context.TurmaProfessores
                .Include(tp => tp.Turma)
                .AnyAsync(tp =>
                    tp.ProfessorId == profId &&
                    tp.Turma.DiaDaSemana == diaDaSemanaEnum &&
                    tp.Turma.Ativa &&
                    request.HorarioInicio < tp.Turma.HorarioFim &&
                    request.HorarioFim > tp.Turma.HorarioInicio);

            if (choqueProfessor)
                throw new RegraNegocioException(
                    $"O professor {professor.Nome} já possui uma turma neste " +
                    "mesmo dia e horário em outra sala.");
        }

        var turma = new Turma(
            ritmoIdReal, salaIdReal, request.DataInicio, diaDaSemanaEnum,
            request.HorarioInicio, request.HorarioFim, request.Nivel,
            request.LimiteAlunos, request.LinkWhatsApp);

        _context.Turmas.Add(turma);

        foreach (var profId in professoresIdsReais)
            _context.TurmaProfessores.Add(new TurmaProfessor { Turma = turma, ProfessorId = profId });

        await _context.SaveChangesAsync();

        // CORREÇÃO: Busca a turma salva recém-criada diretamente do banco 
        // para garantir que todas as navegações (Ritmo, Sala, Professores) existam no EF Core.
        var turmaSalva = await _context.Turmas
            .Include(t => t.Ritmo)
            .Include(t => t.Sala)
            .Include(t => t.Professores).ThenInclude(tp => tp.Professor)
            .FirstAsync(t => t.Id == turma.Id);

        return turmaSalva.ToResponse(_hashids);
    }

    // ──────────────────────────────────────────────────────────────────────
    // LISTAR TURMAS COM FILTROS OPCIONAIS
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterTurmaResponse>> ListarTurmasAsync(
        string? ritmoIdHash,
        string? professorIdHash,
        int? diaDaSemana,
        TimeSpan? horario,
        bool? apenasAtivas = null)
    {
        var query = _context.Turmas
            .Include(t => t.Ritmo)
            .Include(t => t.Sala)
            .Include(t => t.Matriculas)
            .Include(t => t.Professores).ThenInclude(tp => tp.Professor)
            .AsQueryable();

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
    // LISTAR TURMAS DO USUÁRIO LOGADO
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterTurmaResponse>> ListarMinhasTurmasAsync(
        int usuarioId, string role)
    {
        var query = _context.Turmas
            .Include(t => t.Ritmo)
            .Include(t => t.Sala)
            .Include(t => t.Matriculas)
            .Include(t => t.Professores).ThenInclude(tp => tp.Professor)
            .Where(t => t.Ativa)
            .AsQueryable();

        if (role == "Professor")
            query = query.Where(t => t.Professores.Any(tp => tp.ProfessorId == usuarioId));
        else
            query = query.Where(t => t.Matriculas.Any(m => m.AlunoId == usuarioId));

        var turmas = await query.ToListAsync();
        return turmas.Select(t => t.ToResponse(_hashids));
    }

    // ──────────────────────────────────────────────────────────────────────
    // TROCAR SALA
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
            throw new RegraNegocioException(
                $"O novo limite ({novoLimiteAlunos}) excede a capacidade " +
                $"da {novaSala.Nome} ({novaSala.CapacidadeMaxima}).");

        if (novoLimiteAlunos < totalMatriculados)
            throw new RegraNegocioException(
                $"Não é possível diminuir o limite para {novoLimiteAlunos}, " +
                $"pois a turma já possui {totalMatriculados} alunos matriculados.");

        bool choqueNovaSala = await _context.Turmas.AnyAsync(t =>
            t.Id != turmaId &&
            t.SalaId == novaSalaId &&
            t.DiaDaSemana == turma.DiaDaSemana &&
            t.Ativa &&
            turma.HorarioInicio < t.HorarioFim &&
            turma.HorarioFim > t.HorarioInicio);

        if (choqueNovaSala)
            throw new RegraNegocioException(
                "A nova sala já está ocupada por outra turma neste mesmo dia e horário.");

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
    // MATRICULAR ALUNO (RN-TUR05, RN-TUR06, RN-BOL04)
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

        if (turma.ListaDeEspera.Any(e => e.AlunoId == alunoId))
            throw new RegraNegocioException("O aluno já está na fila de espera desta turma.");

        // RN-BOL04: Bolsista não pode matricular em dança SOLO no dia obrigatório
        if (aluno.Tipo == "Bolsista")
        {
            var ritmo = await _context.Ritmos.FindAsync(turma.RitmoId);
            bool ehSolo = ritmo?.Modalidade
                .Equals("Dança solo", StringComparison.OrdinalIgnoreCase) == true;

            if (ehSolo)
            {
                var bolsista = await _context.Usuarios
                    .OfType<Bolsista>()
                    .FirstOrDefaultAsync(b => b.Id == alunoId);

                if (bolsista != null)
                {
                    bool eDiaObrigatorio =
                        (bolsista.DiaObrigatorio1.HasValue &&
                         bolsista.DiaObrigatorio1.Value == turma.DiaDaSemana) ||
                        (bolsista.DiaObrigatorio2.HasValue &&
                         bolsista.DiaObrigatorio2.Value == turma.DiaDaSemana);

                    if (eDiaObrigatorio)
                    {
                        var nomeDia = turma.DiaDaSemana switch
                        {
                            DayOfWeek.Monday => "segunda-feira",
                            DayOfWeek.Tuesday => "terça-feira",
                            DayOfWeek.Wednesday => "quarta-feira",
                            DayOfWeek.Thursday => "quinta-feira",
                            DayOfWeek.Friday => "sexta-feira",
                            DayOfWeek.Saturday => "sábado",
                            _ => "domingo"
                        };
                        throw new RegraNegocioException(
                            $"Bolsistas não podem se matricular em dança solo nos seus dias " +
                            $"obrigatórios. A {nomeDia} é um dos seus dias obrigatórios. " +
                            "Escolha outro dia. [RN-BOL04]");
                    }
                }
            }
        }

        // RN-TUR06: Choque de horário com outra turma
        bool choqueHorario = await _context.Matriculas
            .AnyAsync(m =>
                m.AlunoId == alunoId &&
                m.Turma.Ativa &&
                m.Turma.DiaDaSemana == turma.DiaDaSemana &&
                turma.HorarioInicio < m.Turma.HorarioFim &&
                turma.HorarioFim > m.Turma.HorarioInicio);

        if (choqueHorario)
        {
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
                $"Você já possui uma turma na {diaTexto} com horário conflitante.");
        }

        // Turma cheia → fila de espera
        if (turma.Matriculas.Count >= turma.LimiteAlunos)
        {
            _context.Interesses.Add(new Interesse { TurmaId = turmaId, AlunoId = alunoId });
            await _context.SaveChangesAsync();
            return "A turma está cheia. Você foi adicionado à fila de espera.";
        }

        // ──────────────────────────────────────────────────────────────────────
        // Matrícula normal — com rastreamento de desconto bolsista (RN-BOL02)
        //
        // Por que salvar o ValorMensalidade aqui?
        // O sistema financeiro (fase 1.2) precisará saber se esta matrícula
        // tinha desconto de bolsista para cobrar o valor correto em cada mês.
        //
        // Fluxo de preço:
        //   Aluno regular  → null (preço padrão a ser definido no financeiro)
        //   Bolsista/Solo  → valorSolo * 0.5 (50% RN-BOL02)
        //   Bolsista/Salão → 0.00m (gratuito, RN-BOL01) — sem matrícula formal
        // ──────────────────────────────────────────────────────────────────────

        // Calcula o ValorMensalidade para matrículas de bolsista em turmas solo
        decimal? valorMensalidade = null;
        string? origemDesconto = null;

        if (aluno.Tipo == "Bolsista")
        {
            // Para turmas solo, o bolsista paga 50% (RN-BOL02)
            // Buscamos o ritmo para confirmar a modalidade
            // (pode estar em cache do EF Core se já foi consultado acima no RN-BOL04)
            var ritmoParaPreco = await _context.Ritmos.FindAsync(turma.RitmoId);
            bool eSSolo = ritmoParaPreco?.Modalidade
                .Equals("Dança solo", StringComparison.OrdinalIgnoreCase) == true;

            if (eSSolo)
            {
                // O preço padrão vem do IConfiguration — injetado no construtor
                // Não há IConfiguration aqui, então lemos do appsettings via
                // um valor fixo por enquanto. O módulo financeiro 1.2 substituirá isso.
                // Por ora, usamos null para indicar "50% do valor padrão" via OrigemDesconto
                origemDesconto = "Bolsista50%";
                // ValorMensalidade será calculado pelo módulo financeiro com base na OrigemDesconto
                // e no preço padrão configurado no momento da cobrança
            }
        }

        _context.Matriculas.Add(new Matricula
        {
            TurmaId = turmaId,
            AlunoId = alunoId,
            Papel = papel,
            ValorMensalidade = valorMensalidade,
            OrigemDesconto = origemDesconto
        });

        await _context.SaveChangesAsync();
        return "Matrícula realizada com sucesso.";
    }

    // ──────────────────────────────────────────────────────────────────────
    // DESMATRICULAR ALUNO
    // Remove da matrícula formal ou da fila de espera.
    // ──────────────────────────────────────────────────────────────────────
    public async Task DesmatricularAlunoAsync(int turmaId, int alunoId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.ListaDeEspera)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        // Tenta remover da matrícula formal primeiro
        var matricula = turma.Matriculas.FirstOrDefault(m => m.AlunoId == alunoId);
        if (matricula != null)
        {
            _context.Matriculas.Remove(matricula);
        }
        else
        {
            // Tenta remover da fila de espera
            var interesse = turma.ListaDeEspera.FirstOrDefault(e => e.AlunoId == alunoId);
            if (interesse != null)
                _context.Interesses.Remove(interesse);
            else
                throw new RegraNegocioException(
                    "O aluno não está matriculado nem na fila de espera desta turma.");
        }

        await _context.SaveChangesAsync();
    }
    // Localização: Rascunho/Services/TurmaService.cs
    // ADICIONAR ao final da classe TurmaService, antes do último }

    // Localização: Rascunho/Services/TurmaService.cs
    // SUBSTITUIR o método EncerrarTurmaAsync

    // ──────────────────────────────────────────────────────────────────────
    // ENCERRAR TURMA (RN-TUR04) — SIMPLIFICADO
    //
    // Conforme requisito: apenas marca a turma como inativa.
    // Todos os dados relacionados são PRESERVADOS:
    //   ✓ Matrículas        — histórico de inscrição
    //   ✓ RegistroPresença  — histórico pedagógico
    //   ✓ AulaExperimental  — registros de experimentais
    //   ✓ Reposições        — agendamentos continuam válidos se o aluno conseguir presença
    //   ✓ Lista de espera   — preservada para consulta histórica
    //
    // A turma some automaticamente das telas de:
    //   - QuadroTurmas (filtra por Ativa = true)
    //   - MinhasAulas dos alunos (filtra por Ativa = true)
    //   - Tela do professor (filtra por Ativa = true)
    //
    // Retorna o total de alunos afetados para log e futura notificação push.
    // ──────────────────────────────────────────────────────────────────────
    public async Task<int> EncerrarTurmaAsync(int turmaId)
    {
        // Include(Matriculas) para contar alunos afetados
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        if (!turma.Ativa)
            throw new RegraNegocioException("Esta turma já está encerrada.");

        int totalAlunos = turma.Matriculas.Count;

        // Apenas muda o flag — nenhum dado é deletado ou cancelado
        turma.Encerrar();

        // TODO Sprint 5: Enviar push notification via Firebase FCM para
        // cada aluno em turma.Matriculas informando o encerramento (RN-TUR04)

        await _context.SaveChangesAsync();

        return totalAlunos;
    }
}