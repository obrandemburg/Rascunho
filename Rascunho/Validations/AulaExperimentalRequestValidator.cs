using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class SolicitarAulaExperimentalRequestValidator : AbstractValidator<SolicitarAulaExperimentalRequest>
{
    public SolicitarAulaExperimentalRequestValidator()
    {
        RuleFor(x => x.TurmaIdHash).NotEmpty().WithMessage("ID da turma é obrigatório.");
        RuleFor(x => x.DataAula).GreaterThan(DateTime.UtcNow).WithMessage("A data da aula deve ser no futuro.");
    }
}