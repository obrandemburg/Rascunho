using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class AulaParticularService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public AulaParticularService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    // ──────────────────────────────────────────────────────────────────────
    // SOLICITAR AULA PARTICULAR (aluno envia pedido ao professor)
    //
    // CORREÇÕES neste método:
    // 1. RN-AP06: Adicionada verificação de choque de horário para o ALUNO
    //    (antes só era verificado para o professor na aceitação)
    // 2. OTIMIZAÇÃO: Os 3 LoadAsync separados foram substituídos por uma única
    //    query com Include — reduz roundtrips ao banco de 3 para 1
    // ──────────────────────────────────────────────────────────────────────
    public async Task<ObterAulaParticularResponse> SolicitarAulaAsync(int alunoId, SolicitarAulaParticularRequest request)
    {
        var profDecoded = _hashids.Decode(request.ProfessorIdHash);
        var ritmoDecoded = _hashids.Decode(request.RitmoIdHash);

        if (profDecoded.Length == 0 || ritmoDecoded.Length == 0)
            throw new RegraNegocioException("IDs inválidos.");

        var professor = await _context.Usuarios.FindAsync(profDecoded[0]);
        if (professor == null || professor.Tipo != "Professor")
            throw new RegraNegocioException("Professor não encontrado ou inválido.");

        // RN-AP06: Verifica se o ALUNO já tem outra aula particular ACEITA no mesmo horário
        // Antes desta correção, um aluno podia ter múltiplas aulas particulares sobrepostas.
        // Só verificamos aulas com Status == "Aceita" — "Pendente" não bloqueia (pode ser recusada).
        bool choqueAluno = await _context.AulasParticulares.AnyAsync(a =>
            a.AlunoId == alunoId &&
            a.Status == "Aceita" &&
            request.DataHoraInicio < a.DataHoraFim &&   // nova começa antes da existente terminar
            request.DataHoraFim > a.DataHoraInicio);  // nova termina depois da existente começar

        if (choqueAluno)
            throw new RegraNegocioException(
                "Você já possui uma aula particular agendada neste horário. " +
                "Verifique seus agendamentos antes de fazer uma nova solicitação.");

        // Cria a entidade — começa sempre como "Pendente"
        var aula = new AulaParticular(
            alunoId,
            profDecoded[0],
            ritmoDecoded[0],
            request.DataHoraInicio,
            request.DataHoraFim,
            request.Observacao);

        _context.AulasParticulares.Add(aula);
        await _context.SaveChangesAsync();

        // OTIMIZAÇÃO: Antes existiam 3 LoadAsync separados, gerando 3 queries SQL distintas:
        //   await _context.Entry(aula).Reference(a => a.Professor).LoadAsync();  // Query 1
        //   await _context.Entry(aula).Reference(a => a.Ritmo).LoadAsync();      // Query 2
        //   await _context.Entry(aula).Reference(a => a.Aluno).LoadAsync();      // Query 3
        //
        // Agora uma única query com JOINs faz o mesmo trabalho:
        var aulaCompleta = await _context.AulasParticulares
            .Include(a => a.Professor) // JOIN com tabela Usuarios para o professor
            .Include(a => a.Aluno)     // JOIN com tabela Usuarios para o aluno
            .Include(a => a.Ritmo)     // JOIN com tabela Ritmos
            .FirstAsync(a => a.Id == aula.Id);

        return aulaCompleta.ToResponse(_hashids);
    }

    // ──────────────────────────────────────────────────────────────────────
    // RESPONDER SOLICITAÇÃO (professor aceita ou recusa)
    // Sem alterações — lógica já correta
    // ──────────────────────────────────────────────────────────────────────
    public async Task ResponderSolicitacaoAsync(int professorLogadoId, int aulaId, bool aceitar)
    {
        var aula = await _context.AulasParticulares.FindAsync(aulaId)
            ?? throw new RegraNegocioException("Solicitação não encontrada.");

        if (aula.ProfessorId != professorLogadoId)
            throw new RegraNegocioException("Você não tem permissão para responder por outro professor.");

        if (aula.Status != "Pendente")
            throw new RegraNegocioException($"Esta aula já foi {aula.Status}.");

        if (!aceitar)
        {
            aula.Recusar();
        }
        else
        {
            // Verifica choque com outras aulas particulares do professor
            bool choqueAulaParticular = await _context.AulasParticulares.AnyAsync(a =>
                a.ProfessorId == professorLogadoId &&
                a.Status == "Aceita" &&
                (aula.DataHoraInicio < a.DataHoraFim && aula.DataHoraFim > a.DataHoraInicio));

            // Verifica choque com turmas regulares do professor (RN-AP01)
            var diaDaSemanaAula = aula.DataHoraInicio.DayOfWeek;
            var horarioInicioAula = aula.DataHoraInicio.TimeOfDay;
            var horarioFimAula = aula.DataHoraFim.TimeOfDay;

            bool choqueTurma = await _context.TurmaProfessores
                .Include(tp => tp.Turma)
                .AnyAsync(tp =>
                    tp.ProfessorId == professorLogadoId &&
                    tp.Turma.Ativa &&
                    tp.Turma.DiaDaSemana == diaDaSemanaAula &&
                    (horarioInicioAula < tp.Turma.HorarioFim && horarioFimAula > tp.Turma.HorarioInicio));

            if (choqueAulaParticular || choqueTurma)
                throw new RegraNegocioException("Você já tem uma aula particular ou turma marcada para este horário.");

            aula.Aceitar();
        }

        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // CANCELAR AULA PARTICULAR
    //
    // CORREÇÃO: Janela de cancelamento alterada de 24h para 12h.
    //
    // Antes:  if (aula.AlunoId == usuarioLogadoId && horasParaAula < 24)
    // Depois: if (aula.AlunoId == usuarioLogadoId && horasParaAula < 12)
    //
    // Por que essa regra existe:
    // - Com menos de 12h, o professor provavelmente já organizou seu tempo.
    // - Cancelamentos de última hora são prejudiciais para a escola.
    // - Professor e staff (Recepção/Gerente) podem cancelar a qualquer momento (RN-AP04)
    //   pois há sempre um motivo legítimo (doença, emergência, etc.)
    // ──────────────────────────────────────────────────────────────────────
    public async Task CancelarAulaAsync(int usuarioLogadoId, string roleLogado, int aulaId)
    {
        var aula = await _context.AulasParticulares.FindAsync(aulaId)
            ?? throw new RegraNegocioException("Aula não encontrada.");

        // Verifica se o usuário tem permissão para cancelar:
        // - O próprio aluno pode cancelar sua aula
        // - O próprio professor pode cancelar a aula dele
        // - Recepção e Gerente podem cancelar qualquer aula
        bool temPermissao = aula.AlunoId == usuarioLogadoId
            || aula.ProfessorId == usuarioLogadoId
            || roleLogado == "Recepção"
            || roleLogado == "Gerente";

        if (!temPermissao)
            throw new RegraNegocioException("Sem permissão para cancelar esta aula.");

        if (aula.Status == "Cancelada" || aula.Status == "Recusada")
            throw new RegraNegocioException("Esta aula já está cancelada ou recusada.");

        var horasParaAula = (aula.DataHoraInicio - DateTime.UtcNow).TotalHours;

        // CORREÇÃO: Era 24h, agora é 12h conforme RN-AP03 do planejamento.
        // A regra se aplica APENAS para o aluno/bolsista — não para professor nem staff.
        // Professor (RN-AP04) e staff podem cancelar a qualquer momento.
        if (aula.AlunoId == usuarioLogadoId && horasParaAula < 12)
        {
            throw new RegraNegocioException(
                "O cancelamento deve ser feito com pelo menos 12 horas de antecedência. " +
                "Entre em contato diretamente com o professor ou com a recepção.");
        }

        aula.Cancelar();
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // LISTAR MINHAS AULAS (sem alterações)
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterAulaParticularResponse>> ListarMinhasAulasAsync(int usuarioId, string role)
    {
        var query = _context.AulasParticulares
            .Include(a => a.Professor)
            .Include(a => a.Aluno)
            .Include(a => a.Ritmo)
            .AsQueryable();

        if (role == "Aluno" || role == "Bolsista" || role == "Líder")
            query = query.Where(a => a.AlunoId == usuarioId);
        else if (role == "Professor")
            query = query.Where(a => a.ProfessorId == usuarioId);
        // Recepção e Gerente veem todas → sem filtro adicional

        var aulas = await query.OrderByDescending(a => a.DataHoraInicio).ToListAsync();
        return aulas.Select(a => a.ToResponse(_hashids));
    }
}