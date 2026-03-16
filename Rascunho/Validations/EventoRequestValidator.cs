using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class CriarEventoRequestValidator : AbstractValidator<CriarEventoRequest>
{
    public CriarEventoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do evento é obrigatório.")
            .MaximumLength(150).WithMessage("O nome não pode passar de 150 caracteres.");

        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("O tipo de evento é obrigatório.")
            .Must(tipo => tipo == "Baile" || tipo == "Workshop")
            .WithMessage("O tipo do evento deve ser exatamente 'Baile' ou 'Workshop'.");

        RuleFor(x => x.DataHora)
            .GreaterThan(DateTime.UtcNow).WithMessage("A data do evento deve ser no futuro.");

        RuleFor(x => x.Capacidade)
            .GreaterThan(0).WithMessage("A capacidade deve ser maior que zero.");

        RuleFor(x => x.Preco)
            .GreaterThanOrEqualTo(0).WithMessage("O preço não pode ser negativo.");
    }
}