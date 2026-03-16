using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class CriarAvisoRequestValidator : AbstractValidator<CriarAvisoRequest>
{
    public CriarAvisoRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Mensagem).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DataExpiracao)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("A data de expiração deve ser no futuro.");
        RuleFor(x => x.TipoVisibilidade)
            .Must(tipo => tipo == "Geral" || tipo == "Equipe")
            .WithMessage("O tipo de visibilidade deve ser 'Geral' ou 'Equipe'.");
    }
}

public class AtualizarAvisoRequestValidator : AbstractValidator<AtualizarAvisoRequest>
{
    public AtualizarAvisoRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Mensagem).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DataExpiracao).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.TipoVisibilidade).Must(tipo => tipo == "Geral" || tipo == "Equipe");
    }
}