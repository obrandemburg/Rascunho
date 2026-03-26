// Localização sugerida: Rascunho/Validators/AlterarSenhaRequestValidator.cs
using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validators;

public class AlterarSenhaRequestValidator : AbstractValidator<AlterarSenhaRequest>
{
    public AlterarSenhaRequestValidator()
    {
        RuleFor(x => x.SenhaAtual)
            .NotEmpty()
            .WithMessage("A senha atual é obrigatória.");

        RuleFor(x => x.NovaSenha)
            .NotEmpty()
            .WithMessage("A nova senha é obrigatória.")
            .MinimumLength(8)
            .WithMessage("A nova senha deve ter no mínimo 8 caracteres.")
            .NotEqual(x => x.SenhaAtual)
            .WithMessage("A nova senha não pode ser igual à senha atual.");
    }
}