using FluentValidation;
using Rascunho.DTOs;

namespace Rascunho.Validations;

public class CriarRitmoRequestValidator : AbstractValidator<CriarRitmoRequest>
{
    public CriarRitmoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do ritmo é obrigatório.")
            .MaximumLength(100).WithMessage("O nome do ritmo deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.");

        RuleFor(x => x.Modalidade)
            .NotEmpty().WithMessage("A modalidade é obrigatória.")
            .Must(ValidarModalidade)
            .WithMessage("Modalidade inválida. As opções aceitas são: 'Dança de salão' ou 'Dança solo'.");
    }

    private bool ValidarModalidade(string modalidade)
    {
        // Ignora maiúsculas/minúsculas para facilitar a vida do Front-end
        var modalidadesValidas = new[] { "dança de salão", "dança solo" };
        return modalidadesValidas.Contains(modalidade.ToLower());
    }
}

public class AtualizarRitmoRequestValidator : AbstractValidator<AtualizarRitmoRequest>
{
    public AtualizarRitmoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do ritmo é obrigatório.")
            .MaximumLength(100).WithMessage("O nome do ritmo deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.");

        RuleFor(x => x.Modalidade)
            .NotEmpty().WithMessage("A modalidade é obrigatória.")
            .Must(ValidarModalidade)
            .WithMessage("Modalidade inválida. As opções aceitas são: 'Dança de salão' ou 'Dança solo'.");
    }

    private bool ValidarModalidade(string modalidade)
    {
        var modalidadesValidas = new[] { "dança de salão", "dança solo" };
        return modalidadesValidas.Contains(modalidade.ToLower());
    }
}