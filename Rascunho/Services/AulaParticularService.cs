using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Shared.DTOs;
using Rascunho.Entities;
using Rascunho.Exceptions;
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

        // Verifica se o professor existe
        var professor = await _context.Usuarios.FindAsync(profDecoded[0]);
        if (professor == null || professor.Tipo != "Professor")
            throw new RegraNegocioException("Professor não encontrado ou inválido.");

        var aula = new AulaParticular(alunoId, profDecoded[0], ritmoDecoded[0], request.DataHoraInicio, request.DataHoraFim, request.Observacao);

        _context.AulasParticulares.Add(aula);
        await _context.SaveChangesAsync();

        // Recarrega os dados para exibir nomes no retorno
        await _context.Entry(aula).Reference(a => a.Professor).LoadAsync();
        await _context.Entry(aula).Reference(a => a.Ritmo).LoadAsync();
        await _context.Entry(aula).Reference(a => a.Aluno).LoadAsync();

        return ObterAulaParticularResponse.DeEntidade(aula, _hashids);
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
            // PARADOXO DA ONIPRESENÇA: O professor está livre neste dia e horário?

            // 1. Checa outras aulas particulares aceitas no mesmo horário
            bool choqueAulaParticular = await _context.AulasParticulares.AnyAsync(a =>
                a.ProfessorId == professorLogadoId &&
                a.Status == "Aceita" &&
                (aula.DataHoraInicio < a.DataHoraFim && aula.DataHoraFim > a.DataHoraInicio));

            // 2. Checa as Turmas oficiais da escola (Atenção para o dia da semana e o TimeSpan)
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

        // Pode ser cancelada pelo Aluno, Professor ou Recepção/Gerente
        if (aula.AlunoId != usuarioLogadoId && aula.ProfessorId != usuarioLogadoId && roleLogado != "Recepção" && roleLogado != "Gerente")
            throw new RegraNegocioException("Sem permissão para cancelar.");

        if (aula.Status == "Cancelada" || aula.Status == "Recusada")
            throw new RegraNegocioException("Esta aula já está cancelada ou recusada.");

        // Regra de Prazo de Cancelamento (ex: mínimo de 24 horas de antecedência)
        var horasParaAula = (aula.DataHoraInicio - DateTime.UtcNow).TotalHours;

        // Se for o aluno cancelando faltando menos de 24h, aplicamos uma punição/bloqueio
        if (aula.AlunoId == usuarioLogadoId && horasParaAula < 24)
        {
            throw new RegraNegocioException("O cancelamento deve ser feito com pelo menos 24 horas de antecedência. Entre em contato com a recepção.");
        }

        aula.Cancelar();
        await _context.SaveChangesAsync();
    }

    // Listagens
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
        // Gerente e Recepção veem tudo

        var aulas = await query.OrderByDescending(a => a.DataHoraInicio).ToListAsync();
        return aulas.Select(a => ObterAulaParticularResponse.DeEntidade(a, _hashids));
    }
}