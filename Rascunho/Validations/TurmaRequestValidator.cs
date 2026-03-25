// ═══════════════════════════════════════════════════════════════════════════
// ARQUIVO: Rascunho/Validations/TurmaRequestValidator.cs
// ═══════════════════════════════════════════════════════════════════════════
using FluentValidation;
using System;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class CriarTurmaRequestValidator : AbstractValidator<CriarTurmaRequest>
{
    public CriarTurmaRequestValidator()
    {
        // ── RitmoIdHash ──────────────────────────────────────────
        RuleFor(x => x.RitmoIdHash)
            .NotEmpty()
            .WithMessage("O ritmo é obrigatório para criar uma turma.");

        // ── SalaIdHash ───────────────────────────────────────────
        RuleFor(x => x.SalaIdHash)
            .NotEmpty()
            .WithMessage("A sala é obrigatória para criar uma turma.");

        // ── ProfessoresIdsHash ───────────────────────────────────
        RuleFor(x => x.ProfessoresIdsHash)
            .NotNull()
            .WithMessage("A lista de professores é obrigatória.")
            .Must(lista => lista != null && lista.Count > 0)
            .WithMessage("Selecione pelo menos um professor para a turma.");

        RuleForEach(x => x.ProfessoresIdsHash)
            .NotEmpty()
            .WithMessage("É necessário selecionar um professor para criar a turma.");

        // ── DiaDaSemana ──────────────────────────────────────────
        RuleFor(x => x.DiaDaSemana)
            .InclusiveBetween(0, 6)
            .WithMessage("Dia da semana inválido. Use 0 (Domingo) a 6 (Sábado).");

        // ── HorarioInicio ────────────────────────────────────────
        RuleFor(x => x.HorarioInicio)
            .NotEmpty()
            .WithMessage("O horário de início é obrigatório.")
            .Must(BeAValidTimeSpan)
            .WithMessage("O formato do horário de início é inválido (use hh:mm).")
            .Must(NotBeZero)
            .WithMessage("O horário de início não pode ser 00:00.");

        // ── HorarioFim ───────────────────────────────────────────
        RuleFor(x => x.HorarioFim)
            .NotEmpty()
            .WithMessage("O horário de término é obrigatório.")
            .Must(BeAValidTimeSpan)
            .WithMessage("O formato do horário de término é inválido (use hh:mm).")
            .Must((request, horarioFim) => BeGreaterThanInicio(request.HorarioInicio, horarioFim))
            .WithMessage("O horário de término deve ser posterior ao horário de início."); // Correção do parêntese aqui

        // ── Nivel ────────────────────────────────────────────────
        RuleFor(x => x.Nivel)
            .NotEmpty()
            .WithMessage("O nível da turma é obrigatório (ex: Iniciante, Básico, Intermediário, Avançado).");

        // ── LimiteAlunos ─────────────────────────────────────────
        RuleFor(x => x.LimiteAlunos)
            .GreaterThan(0)
            .WithMessage("O limite de alunos deve ser maior que zero.");

        // ── LinkWhatsApp ─────────────────────────────────────────
        RuleFor(x => x.LinkWhatsApp)
            .MaximumLength(500)
            .WithMessage("O link do WhatsApp é muito longo (máximo 500 caracteres).")
            .When(x => !string.IsNullOrEmpty(x.LinkWhatsApp));

    } // <-- FIM DO CONSTRUTOR

    // ─────────────────────────────────────────────────────────────
    // Métodos auxiliares de validação (AGORA FORA DO CONSTRUTOR)
    // ─────────────────────────────────────────────────────────────
    private bool BeAValidTimeSpan(string horario)
    {
        if (string.IsNullOrWhiteSpace(horario)) return false;
        return TimeSpan.TryParse(horario, out _);
    }

    private bool NotBeZero(string horario)
    {
        if (TimeSpan.TryParse(horario, out var ts))
        {
            return ts != TimeSpan.Zero;
        }
        return false;
    }

    private bool BeGreaterThanInicio(string horarioInicioStr, string horarioFimStr)
    {
        if (TimeSpan.TryParse(horarioInicioStr, out var inicio) &&
            TimeSpan.TryParse(horarioFimStr, out var fim))
        {
            return fim > inicio;
        }
        return false;
    }
} // <-- FIM DA CLASSE