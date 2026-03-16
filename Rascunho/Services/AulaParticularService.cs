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

    public async Task<ObterAulaParticularResponse> SolicitarAulaAsync(int alunoId, SolicitarAulaParticularRequest request)
    {
        var profDecoded = _hashids.Decode(request.ProfessorIdHash);
        var ritmoDecoded = _hashids.Decode(request.RitmoIdHash);

        if (profDecoded.Length == 0 || ritmoDecoded.Length == 0)
            throw new RegraNegocioException("IDs inválidos.");

        var professor = await _context.Usuarios.FindAsync(profDecoded[0]);
        if (professor == null || professor.Tipo != "Professor")
            throw new RegraNegocioException("Professor não encontrado ou inválido.");

        var aula = new AulaParticular(alunoId, profDecoded[0], ritmoDecoded[0], request.DataHoraInicio, request.DataHoraFim, request.Observacao);

        _context.AulasParticulares.Add(aula);
        await _context.SaveChangesAsync();

        await _context.Entry(aula).Reference(a => a.Professor).LoadAsync();
        await _context.Entry(aula).Reference(a => a.Ritmo).LoadAsync();
        await _context.Entry(aula).Reference(a => a.Aluno).LoadAsync();

        return aula.ToResponse(_hashids);
    }

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
                (aula.DataHoraInicio < a.DataHoraFim && aula.DataHoraFim > a.DataHoraInicio));

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

    public async Task CancelarAulaAsync(int usuarioLogadoId, string roleLogado, int aulaId)
    {
        var aula = await _context.AulasParticulares.FindAsync(aulaId)
            ?? throw new RegraNegocioException("Aula não encontrada.");

        if (aula.AlunoId != usuarioLogadoId && aula.ProfessorId != usuarioLogadoId && roleLogado != "Recepção" && roleLogado != "Gerente")
            throw new RegraNegocioException("Sem permissão para cancelar.");

        if (aula.Status == "Cancelada" || aula.Status == "Recusada")
            throw new RegraNegocioException("Esta aula já está cancelada ou recusada.");

        var horasParaAula = (aula.DataHoraInicio - DateTime.UtcNow).TotalHours;

        if (aula.AlunoId == usuarioLogadoId && horasParaAula < 24)
        {
            throw new RegraNegocioException("O cancelamento deve ser feito com pelo menos 24 horas de antecedência. Entre em contato com a recepção.");
        }

        aula.Cancelar();
        await _context.SaveChangesAsync();
    }

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

        var aulas = await query.OrderByDescending(a => a.DataHoraInicio).ToListAsync();
        return aulas.Select(a => a.ToResponse(_hashids));
    }
}