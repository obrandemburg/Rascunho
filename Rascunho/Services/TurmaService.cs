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
    private readonly ListaEsperaService _listaEsperaService;

    public TurmaService(AppDbContext context, IHashids hashids, ListaEsperaService listaEsperaService)
    {
        _context = context;
        _hashids = hashids;
        _listaEsperaService = listaEsperaService;
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

        // Tenta converter de forma segura no início. Se falhar, lança a exceção tratável (422)
        if (!TimeSpan.TryParse(request.HorarioInicio, out var hrInicio))
            throw new RegraNegocioException("Horário de início inválido. Use o formato HH:mm.");

        if (!TimeSpan.TryParse(request.HorarioFim, out var hrFim))
            throw new RegraNegocioException("Horário de término inválido. Use o formato HH:mm.");

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
            hrInicio < t.HorarioFim &&
            hrFim > t.HorarioInicio);
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
                    hrInicio < tp.Turma.HorarioFim &&
                    hrFim > tp.Turma.HorarioInicio);

            if (choqueProfessor)
                throw new RegraNegocioException(
                    $"O professor {professor.Nome} já possui uma turma neste " +
                    "mesmo dia e horário em outra sala.");
        }

        var turma = new Turma(
            ritmoIdReal, salaIdReal, request.DataInicio, diaDaSemanaEnum,
            hrInicio, hrFim, request.Nivel,
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

        await _context.SaveChangesAsync();

        // Se a turma estava lotada e agora tem vagas, notificar o próximo da fila
        bool temFilaAtiva = turma.ListaDeEspera.Any(le =>
            le.Status == StatusListaEspera.Aguardando ||
            le.Status == StatusListaEspera.Notificado);

        if (vagasAnteriores <= 0 && vagasNovas > 0 && temFilaAtiva)
        {
            await _listaEsperaService.NotificarProximoAsync(turmaId);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // MATRICULAR ALUNO (RN-TUR05, RN-TUR06, RN-BOL04)
    //
    // Fluxo completo de matrícula com suporte à confirmação de vaga da fila:
    //   1. Turma com vaga:       → matrícula normal
    //   2. Turma lotada + aluno Notificado + prazo válido:
    //                            → confirma vaga (marca Convertido) + matrícula
    //   3. Turma lotada + aluno Notificado + prazo expirado:
    //                            → expira, notifica próximo, erro 422
    //   4. Turma lotada + aluno Aguardando:
    //                            → erro: já está na fila
    //   5. Turma lotada + aluno sem entrada:
    //                            → entra na fila (ListaEsperaService)
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

        // RN-BOL04 (CORRIGIDO BUG-002): Bolsista não pode se matricular em turmas de
        // dança SOLO ou dança de SALÃO nos seus dias obrigatórios.
        if (aluno.Tipo == "Bolsista")
        {
            var ritmo = await _context.Ritmos.FindAsync(turma.RitmoId);
            bool ehDancaRestrita =
                ritmo?.Modalidade.Equals("Dança solo", StringComparison.OrdinalIgnoreCase) == true ||
                ritmo?.Modalidade.Equals("Dança de salão", StringComparison.OrdinalIgnoreCase) == true;

            if (ehDancaRestrita)
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
                            $"Bolsistas não podem se matricular em dança solo ou dança de salão " +
                            $"nos seus dias obrigatórios. A {nomeDia} é um dos seus dias obrigatórios. " +
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

        // ── Verificação de disponibilidade de vaga ────────────────────────
        if (turma.Matriculas.Count >= turma.LimiteAlunos)
        {
            // Turma lotada: verificar o estado do aluno na fila
            var entradaFila = turma.ListaDeEspera
                .FirstOrDefault(le => le.AlunoId == alunoId &&
                                      (le.Status == StatusListaEspera.Aguardando ||
                                       le.Status == StatusListaEspera.Notificado));

            if (entradaFila == null)
            {
                // Aluno não está na fila → adicionar
                return await _listaEsperaService.EntrarNaFilaAsync(turmaId, alunoId);
            }

            if (entradaFila.Status == StatusListaEspera.Aguardando)
            {
                throw new RegraNegocioException(
                    $"Você já está na fila de espera desta turma (posição {entradaFila.Posicao}).");
            }

            // Status == Notificado: aluno está confirmando a vaga reservada
            if (entradaFila.DataExpiracao < DateTime.UtcNow)
            {
                // Prazo expirado: invalidar e notificar o próximo
                entradaFila.Status = StatusListaEspera.Expirado;
                await _context.SaveChangesAsync();
                await _listaEsperaService.NotificarProximoAsync(turmaId);
                throw new RegraNegocioException(
                    "O prazo para confirmar a vaga expirou. " +
                    "Por favor, entre na fila novamente se ainda tiver interesse.");
            }

            // Prazo válido: confirmar a vaga (marcado como Convertido junto com a Matricula)
            entradaFila.Status = StatusListaEspera.Convertido;
            // SaveChanges será feito junto com a inserção da Matricula abaixo
        }
        else
        {
            // Turma tem vagas — limpar qualquer entrada pendente na fila (edge case: limite aumentado)
            var entradaPendente = turma.ListaDeEspera
                .FirstOrDefault(le => le.AlunoId == alunoId &&
                                      (le.Status == StatusListaEspera.Aguardando ||
                                       le.Status == StatusListaEspera.Notificado));
            if (entradaPendente != null)
                _context.ListasEspera.Remove(entradaPendente);
        }

        // ── Matrícula normal — com rastreamento de desconto bolsista (RN-BOL02) ──
        //
        // Por que salvar o ValorMensalidade aqui?
        // O sistema financeiro (fase 1.2) precisará saber se esta matrícula
        // tinha desconto de bolsista para cobrar o valor correto em cada mês.
        //
        // Fluxo de preço:
        //   Aluno regular  → null (preço padrão a ser definido no financeiro)
        //   Bolsista/Solo  → valorSolo * 0.5 (50% RN-BOL02)
        //   Bolsista/Salão → 0.00m (gratuito, RN-BOL01) — sem matrícula formal

        decimal? valorMensalidade = null;
        string? origemDesconto = null;

        if (aluno.Tipo == "Bolsista")
        {
            var ritmoParaPreco = await _context.Ritmos.FindAsync(turma.RitmoId);
            bool eSSolo = ritmoParaPreco?.Modalidade
                .Equals("Dança solo", StringComparison.OrdinalIgnoreCase) == true;

            if (eSSolo)
            {
                origemDesconto = "Bolsista50%";
                // ValorMensalidade será calculado pelo módulo financeiro (Feature #6)
                // com base na OrigemDesconto e no preço padrão do momento da cobrança
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
    // Remove da matrícula formal (e notifica próximo da fila)
    // ou remove da fila de espera diretamente.
    // ──────────────────────────────────────────────────────────────────────
    public async Task DesmatricularAlunoAsync(int turmaId, int alunoId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        var matricula = turma.Matriculas.FirstOrDefault(m => m.AlunoId == alunoId);
        if (matricula != null)
        {
            _context.Matriculas.Remove(matricula);
            await _context.SaveChangesAsync();

            // Uma vaga foi aberta — notificar o próximo da fila de espera (se houver)
            await _listaEsperaService.NotificarProximoAsync(turmaId);
            return;
        }

        // Aluno não está matriculado formalmente — tentar remover da fila de espera
        // ListaEsperaService.SairDaFilaAsync lança RegraNegocioException se não encontrar
        await _listaEsperaService.SairDaFilaAsync(turmaId, alunoId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // LISTAR ALUNOS DE UMA TURMA (BUG-001)
    //
    // Retorna os alunos formalmente matriculados em uma turma.
    // Usado por:
    //   - Professor (MinhasTurmas.razor) — ver lista da sua própria turma
    //   - Recepção/Gerente (GerenciarTurmas.razor) — ver alunos de qualquer turma
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AlunoMatriculadoResponse>> ListarAlunosDaTurmaAsync(int turmaId)
    {
        var matriculas = await _context.Matriculas
            .Include(m => m.Aluno)
            .Where(m => m.TurmaId == turmaId)
            .OrderBy(m => m.Aluno.Nome)
            .ToListAsync();

        return matriculas.Select(m => new AlunoMatriculadoResponse(
            _hashids.Encode(m.AlunoId),
            m.Aluno?.Nome ?? "Desconhecido",
            m.Aluno?.FotoUrl ?? "",
            m.Papel
        ));
    }

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
    // Retorna o total de alunos afetados para log e futura notificação push.
    // ──────────────────────────────────────────────────────────────────────
    public async Task<int> EncerrarTurmaAsync(int turmaId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        if (!turma.Ativa)
            throw new RegraNegocioException("Esta turma já está encerrada.");

        int totalAlunos = turma.Matriculas.Count;

        turma.Encerrar();

        await _context.SaveChangesAsync();

        return totalAlunos;
    }
}
