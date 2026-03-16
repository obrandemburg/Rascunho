using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class CriarSalaRequestValidator : AbstractValidator<CriarSalaRequest>
{
    public CriarSalaRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome da sala é obrigatório.")
            .MaximumLength(50).WithMessage("O nome da sala deve ter no máximo 50 caracteres.");

        RuleFor(x => x.CapacidadeMaxima)
            .GreaterThan(0).WithMessage("A capacidade máxima da sala deve ser maior que zero.");
    }
}

public class AtualizarSalaRequestValidator : AbstractValidator<AtualizarSalaRequest>
{
    public AtualizarSalaRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome da sala é obrigatório.")
            .MaximumLength(50).WithMessage("O nome da sala deve ter no máximo 50 caracteres.");

        RuleFor(x => x.CapacidadeMaxima)
            .GreaterThan(0).WithMessage("A capacidade máxima da sala deve ser maior que zero.");
    }
}