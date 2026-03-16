using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class SolicitarAulaParticularRequestValidator : AbstractValidator<SolicitarAulaParticularRequest>
{
    public SolicitarAulaParticularRequestValidator()
    {
        RuleFor(x => x.DataHoraInicio)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("A data da aula deve ser no futuro.");

        RuleFor(x => x.DataHoraFim)
            .GreaterThan(x => x.DataHoraInicio)
            .WithMessage("O horário de fim deve ser maior que o horário de início.");

        RuleFor(x => x.Observacao)
            .MaximumLength(500).WithMessage("A observação deve ter no máximo 500 caracteres.");
    }
}