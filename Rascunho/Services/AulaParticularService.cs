using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;

    public AulaParticularService(AppDbContext context, IHashids hashids, IConfiguration configuration)
    {
        _context = context;
        _hashids = hashids;
        _configuration = configuration;
    }

    // ── SOLICITAR ────────────────────────────────────────────────
    public async Task<ObterAulaParticularResponse> SolicitarAulaAsync(
        int alunoId,
        SolicitarAulaParticularRequest request)
    {
        var profDecoded = _hashids.Decode(request.ProfessorIdHash);
        var ritmoDecoded = _hashids.Decode(request.RitmoIdHash);

        if (profDecoded.Length == 0 || ritmoDecoded.Length == 0)
            throw new RegraNegocioException("IDs inválidos.");

        var professor = await _context.Usuarios.FindAsync(profDecoded[0]);
        if (professor == null || professor.Tipo != "Professor")
            throw new RegraNegocioException("Professor não encontrado ou inválido.");

        // RN-BOL05 (CORRIGIDO BUG-002): Bolsista não pode agendar aulas particulares
        // de NENHUMA modalidade de dança (solo OU salão) nos seus dias obrigatórios.
        // Antes só bloqueava "Dança solo" — agora bloqueia qualquer modalidade.
        var bolsista = await _context.Usuarios
            .OfType<Bolsista>()
            .FirstOrDefaultAsync(b => b.Id == alunoId);

        bool ehBolsista = bolsista != null;

        if (ehBolsista)
        {
            // Verifica dia obrigatório ANTES de checar a modalidade —
            // qualquer aula particular no dia obrigatório é proibida.
            var diaDaAula = request.DataHoraInicio.DayOfWeek;
            bool eDiaObrigatorio =
                (bolsista!.DiaObrigatorio1.HasValue && bolsista.DiaObrigatorio1.Value == diaDaAula) ||
                (bolsista.DiaObrigatorio2.HasValue && bolsista.DiaObrigatorio2.Value == diaDaAula);

            if (eDiaObrigatorio)
                throw new RegraNegocioException(
                    "Bolsistas não podem agendar aulas particulares nos seus dias obrigatórios " +
                    "(nem dança solo, nem dança de salão). [RN-BOL05]");

            // Valida que o ritmo existe (necessário para cálculo de preço abaixo)
            _ = await _context.Ritmos.FindAsync(ritmoDecoded[0])
                ?? throw new RegraNegocioException("Ritmo não encontrado.");
        }

        // RN-AP06: Aluno já tem outra particular aceita no mesmo horário?
        var inicioUtc = DateTime.SpecifyKind(request.DataHoraInicio, DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(request.DataHoraFim, DateTimeKind.Utc);

        bool choqueAluno = await _context.AulasParticulares.AnyAsync(a =>
            a.AlunoId == alunoId &&
            a.Status == "Aceita" &&
            inicioUtc < a.DataHoraFim &&
            fimUtc > a.DataHoraInicio);

        if (choqueAluno)
            throw new RegraNegocioException(
                "Você já possui uma aula particular agendada neste horário.");

        // RN-BOL03: Calcula valor com desconto para bolsistas
        decimal precoPadrao = _configuration.GetValue<decimal>("AulaParticular:PrecoPadrao", 80.00m);
        decimal valorCobrado = ehBolsista ? precoPadrao * 0.5m : precoPadrao;

        var aula = new AulaParticular(
            alunoId, profDecoded[0], ritmoDecoded[0],
            inicioUtc, fimUtc,
            request.Observacao, valorCobrado);

        _context.AulasParticulares.Add(aula);
        await _context.SaveChangesAsync();

        var aulaCompleta = await _context.AulasParticulares
            .Include(a => a.Professor)
            .Include(a => a.Aluno)
            .Include(a => a.Ritmo)
            .FirstAsync(a => a.Id == aula.Id);

        return aulaCompleta.ToResponse(_hashids);
    }

    // ── RESPONDER SOLICITAÇÃO ─────────────────────────────────────
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
            bool choqueAulaParticular = await _context.AulasParticulares.AnyAsync(a =>
                a.ProfessorId == professorLogadoId &&
                a.Status == "Aceita" &&
                aula.DataHoraInicio < a.DataHoraFim &&
                aula.DataHoraFim > a.DataHoraInicio);

            var diaDaSemanaAula = aula.DataHoraInicio.DayOfWeek;
            var horarioInicioAula = aula.DataHoraInicio.TimeOfDay;
            var horarioFimAula = aula.DataHoraFim.TimeOfDay;

            bool choqueTurma = await _context.TurmaProfessores
                .Include(tp => tp.Turma)
                .AnyAsync(tp =>
                    tp.ProfessorId == professorLogadoId &&
                    tp.Turma.Ativa &&
                    tp.Turma.DiaDaSemana == diaDaSemanaAula &&
                    horarioInicioAula < tp.Turma.HorarioFim &&
                    horarioFimAula > tp.Turma.HorarioInicio);

            if (choqueAulaParticular || choqueTurma)
                throw new RegraNegocioException(
                    "Você já tem uma aula ou turma neste horário.");

            aula.Aceitar();
        }

        await _context.SaveChangesAsync();
    }

    // ── CANCELAR ─────────────────────────────────────────────────
    public async Task CancelarAulaAsync(int usuarioLogadoId, string roleLogado, int aulaId)
    {
        var aula = await _context.AulasParticulares.FindAsync(aulaId)
            ?? throw new RegraNegocioException("Aula não encontrada.");

        bool temPermissao = aula.AlunoId == usuarioLogadoId
            || aula.ProfessorId == usuarioLogadoId
            || roleLogado == "Recepção"
            || roleLogado == "Gerente";

        if (!temPermissao)
            throw new RegraNegocioException("Sem permissão para cancelar esta aula.");

        if (aula.Status == "Cancelada" || aula.Status == "Recusada")
            throw new RegraNegocioException("Esta aula já está cancelada ou recusada.");

        var horasParaAula = (aula.DataHoraInicio - DateTime.UtcNow).TotalHours;

        if (aula.AlunoId == usuarioLogadoId && horasParaAula < 12)
            throw new RegraNegocioException(
                "Cancelamento com menos de 12 horas de antecedência não é permitido. " +
                "Entre em contato com o professor ou a recepção. [RN-AP03]");

        aula.Cancelar();
        await _context.SaveChangesAsync();
    }

    // ── REAGENDAR ────────────────────────────────────────────────
    // NOVO Sprint 4
    //
    // Fluxo de reagendamento:
    //   1. Valida que o aluno é o dono da aula
    //   2. Valida status (só Pendente ou Aceita podem ser reagendadas)
    //   3. Aplica regra de 12h para aulas Aceitas (igual ao cancelar)
    //   4. Valida que não há choque para o novo horário
    //   5. Cancela a aula atual
    //   6. Cria nova solicitação com os mesmos professor/ritmo/valor
    //      mas com as novas datas — volta para "Pendente"
    //
    // Por que volta para "Pendente"?
    //   O professor aceitou o horário original, não o novo. Para garantir
    //   que ele está ciente da mudança, precisa aceitar novamente.
    // ────────────────────────────────────────────────────────────
    public async Task<ObterAulaParticularResponse> ReagendarAulaAsync(
        int alunoId,
        int aulaId,
        ReagendarAulaParticularRequest request)
    {
        var aulaAtual = await _context.AulasParticulares
            .FirstOrDefaultAsync(a => a.Id == aulaId)
            ?? throw new RegraNegocioException("Aula não encontrada.");

        if (aulaAtual.AlunoId != alunoId)
            throw new RegraNegocioException("Sem permissão para reagendar esta aula.");

        if (aulaAtual.Status != "Aceita" && aulaAtual.Status != "Pendente")
            throw new RegraNegocioException(
                "Só é possível reagendar aulas com status Pendente ou Aceita.");

        // Aplica regra de 12h apenas para aulas já aceitas pelo professor
        if (aulaAtual.Status == "Aceita")
        {
            var horasParaAula = (aulaAtual.DataHoraInicio - DateTime.UtcNow).TotalHours;
            if (horasParaAula < 12)
                throw new RegraNegocioException(
                    "Não é possível reagendar com menos de 12 horas de antecedência. " +
                    "Entre em contato diretamente com o professor. [RN-AP03]");
        }

        var novoInicioUtc = DateTime.SpecifyKind(request.NovaDataHoraInicio, DateTimeKind.Utc);
        var novoFimUtc = DateTime.SpecifyKind(request.NovaDataHoraFim, DateTimeKind.Utc);

        // Validação das novas datas
        if (novoInicioUtc >= novoFimUtc)
            throw new RegraNegocioException(
                "O horário de início deve ser anterior ao horário de fim.");

        if (novoInicioUtc <= DateTime.UtcNow)
            throw new RegraNegocioException("O novo horário deve ser no futuro.");

        // RN-AP06: Verifica choque no NOVO horário (excluindo a aula atual)
        bool choqueAluno = await _context.AulasParticulares.AnyAsync(a =>
            a.AlunoId == alunoId &&
            a.Id != aulaId &&
            a.Status == "Aceita" &&
            novoInicioUtc < a.DataHoraFim &&
            novoFimUtc > a.DataHoraInicio);

        if (choqueAluno)
            throw new RegraNegocioException(
                "Você já possui uma aula agendada no novo horário solicitado.");

        // Cancela a aula atual
        aulaAtual.Cancelar();

        // Cria nova solicitação com as mesmas características,
        // mas novas datas e mantendo o ValorCobrado original
        var novaAula = new AulaParticular(
            alunoId,
            aulaAtual.ProfessorId,
            aulaAtual.RitmoId,
            novoInicioUtc,
            novoFimUtc,
            aulaAtual.ObservacaoAluno,
            aulaAtual.ValorCobrado);  // Mantém o preço original

        _context.AulasParticulares.Add(novaAula);
        await _context.SaveChangesAsync();

        var aulaCompleta = await _context.AulasParticulares
            .Include(a => a.Professor)
            .Include(a => a.Aluno)
            .Include(a => a.Ritmo)
            .FirstAsync(a => a.Id == novaAula.Id);

        return aulaCompleta.ToResponse(_hashids);
    }

    // ── LISTAR MINHAS AULAS ───────────────────────────────────────
    public async Task<IEnumerable<ObterAulaParticularResponse>> ListarMinhasAulasAsync(
        int usuarioId, string role)
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

        var aulas = await query.OrderByDescending(a => a.DataHoraInicio).ToListAsync();
        return aulas.Select(a => a.ToResponse(_hashids));
    }
}