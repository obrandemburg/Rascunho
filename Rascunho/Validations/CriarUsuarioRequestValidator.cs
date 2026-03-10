using FluentValidation;
using Rascunho.DTOs;

namespace Rascunho.Validations;

public class CriarUsuarioRequestValidator : AbstractValidator<CriarUsuarioRequest>
{
    public CriarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .Length(3, 100).WithMessage("O nome deve ter entre 3 e 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O formato do e-mail é inválido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 8 caracteres.");

        // O .Must() permite criar regras customizadas usando código C# normal!
        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("O tipo de usuário é obrigatório.")
            .Must(tipo => ValidarTipoUsuario(tipo))
            .WithMessage("Tipo de usuário inválido. Opções válidas: Aluno, Professor, Bolsista, Gerente, Recepção, Líder.");
    }

    // Função auxiliar para deixar o construtor mais limpo
    private bool ValidarTipoUsuario(string tipo)
    {
        var tiposValidos = new[] { "Aluno", "Professor", "Bolsista", "Gerente", "Recepção", "Líder" };
        return tiposValidos.Contains(tipo);
    }
}