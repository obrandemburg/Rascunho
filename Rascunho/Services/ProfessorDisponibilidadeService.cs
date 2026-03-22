// ARQUIVO: Rascunho/Services/ProfessorDisponibilidadeService.cs
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;

namespace Rascunho.Services;

public class ProfessorDisponibilidadeService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public ProfessorDisponibilidadeService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    // ──────────────────────────────────────────────────────────────
    // OBTER DISPONIBILIDADE DO PROFESSOR LOGADO
    // Retorna todos os slots ativos, ordenados por dia e horário
    // ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterDisponibilidadeResponse>> ObterMinhaDisponibilidadeAsync(int professorId)
    {
        var slots = await _context.ProfessorDisponibilidades
            .Where(d => d.ProfessorId == professorId)
            .OrderBy(d => d.DiaDaSemana)
            .ThenBy(d => d.HorarioInicio)
            .ToListAsync();

        return slots.Select(d => new ObterDisponibilidadeResponse(
            _hashids.Encode(d.Id),
            (int)d.DiaDaSemana,
            d.HorarioInicio,
            d.HorarioFim,
            d.Ativo
        ));
    }

    // ──────────────────────────────────────────────────────────────
    // ATUALIZAR DISPONIBILIDADE (replace all)
    //
    // Estratégia "replace all":
    //   1. Remove TODOS os slots existentes do professor
    //   2. Insere os novos slots enviados
    //   3. Uma transação implícita via SaveChangesAsync garante atomicidade
    //
    // Por que não CRUD individual?
    //   A tela mostra uma grade semanal. O usuário edita a grade inteira
    //   e clica "Salvar". É mais natural enviar o estado final completo
    //   do que calcular "o que adicionei" e "o que removi" no frontend.
    //
    // Se request.Slots estiver vazio → remove toda a disponibilidade
    // ──────────────────────────────────────────────────────────────
    public async Task AtualizarDisponibilidadeAsync(int professorId, AtualizarDisponibilidadeRequest request)
    {
        // Validação: não pode ter dois slots sobrepostos no mesmo dia
        foreach (var slot in request.Slots)
        {
            var dia = (DayOfWeek)slot.DiaDaSemana;
            bool haChoque = request.Slots.Any(outro =>
                outro != slot &&
                (DayOfWeek)outro.DiaDaSemana == dia &&
                slot.HorarioInicio < outro.HorarioFim &&
                slot.HorarioFim > outro.HorarioInicio);

            if (haChoque)
                throw new RegraNegocioException(
                    $"Há conflito de horário nos slots do dia " +
                    $"{dia switch { DayOfWeek.Monday => "segunda", DayOfWeek.Tuesday => "terça", DayOfWeek.Wednesday => "quarta", DayOfWeek.Thursday => "quinta", DayOfWeek.Friday => "sexta", DayOfWeek.Saturday => "sábado", _ => "domingo" }}-feira.");
        }

        // Remove todos os slots existentes
        var slotsExistentes = await _context.ProfessorDisponibilidades
            .Where(d => d.ProfessorId == professorId)
            .ToListAsync();

        _context.ProfessorDisponibilidades.RemoveRange(slotsExistentes);

        // Insere os novos
        foreach (var slot in request.Slots)
        {
            _context.ProfessorDisponibilidades.Add(new ProfessorDisponibilidade(
                professorId,
                (DayOfWeek)slot.DiaDaSemana,
                slot.HorarioInicio,
                slot.HorarioFim
            ));
        }

        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // OBTER DISPONIBILIDADE DE UM PROFESSOR ESPECÍFICO
    // Usado pelo sistema ao mostrar horários para o aluno solicitar particular
    // Filtra slots que NÃO conflitam com turmas já existentes (RN-AP01)
    // ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterDisponibilidadeResponse>> ObterDisponibilidadePorProfessorAsync(int professorId)
    {
        // Busca os slots de disponibilidade ativos
        var slots = await _context.ProfessorDisponibilidades
            .Where(d => d.ProfessorId == professorId && d.Ativo)
            .OrderBy(d => d.DiaDaSemana)
            .ThenBy(d => d.HorarioInicio)
            .ToListAsync();

        // Busca as turmas ativas do professor (para filtragem RN-AP01)
        var turmasDoProfessor = await _context.TurmaProfessores
            .Include(tp => tp.Turma)
            .Where(tp => tp.ProfessorId == professorId && tp.Turma.Ativa)
            .Select(tp => tp.Turma)
            .ToListAsync();

        // Filtra slots que conflitam com turmas existentes
        // RN-AP01: nunca mostrar horários onde o professor já tem turma
        var slotsDisponiveis = slots.Where(slot =>
            !turmasDoProfessor.Any(turma =>
                turma.DiaDaSemana == slot.DiaDaSemana &&
                slot.HorarioInicio < turma.HorarioFim &&
                slot.HorarioFim > turma.HorarioInicio));

        return slotsDisponiveis.Select(d => new ObterDisponibilidadeResponse(
            _hashids.Encode(d.Id),
            (int)d.DiaDaSemana,
            d.HorarioInicio,
            d.HorarioFim,
            d.Ativo
        ));
    }
}