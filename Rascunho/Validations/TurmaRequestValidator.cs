using FluentValidation;
using Rascunho.DTOs;

namespace Rascunho.Validations;

public class MatricularRequestValidator : AbstractValidator<MatricularRequest>
{
    public MatricularRequestValidator()
    {
        RuleFor(x => x.Papel)
            .NotEmpty().WithMessage("O papel na dança é obrigatório.")
            .Must(papel => papel == "Condutor" || papel == "Conduzido" || papel == "Ambos")
            .WithMessage("O papel deve ser 'Condutor', 'Conduzido' ou 'Ambos'.");
    }
}