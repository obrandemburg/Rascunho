using FluentValidation;
using Rascunho.DTOs;

namespace Rascunho.Validations;

public class EditarPerfilRequestValidator : AbstractValidator<EditarPerfilRequest>
{
    public EditarPerfilRequestValidator()
    {
        // Só valida o Nome Social se a pessoa tiver enviado um (não é obrigatório)
        RuleFor(x => x.NomeSocial)
            .MaximumLength(100).WithMessage("O nome social não pode passar de 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.NomeSocial));

        RuleFor(x => x.Biografia)
            .MaximumLength(500).WithMessage("A biografia deve ter no máximo 500 caracteres.");
    }
}