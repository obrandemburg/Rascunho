using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class CadastroEmMassaValidator : AbstractValidator<List<CriarUsuarioRequest>>
{
    public CadastroEmMassaValidator()
    {
        // Garante que a lista não venha vazia
        RuleFor(lista => lista)
            .NotEmpty().WithMessage("A lista de usuários não pode estar vazia.");

        // Para CADA item da lista, aplique as regras do validador individual
        RuleForEach(lista => lista).SetValidator(new CriarUsuarioRequestValidator());
    }
}