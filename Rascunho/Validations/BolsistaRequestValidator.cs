using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class DefinirDiasObrigatoriosRequestValidator : AbstractValidator<DefinirDiasObrigatoriosRequest>
{
    public DefinirDiasObrigatoriosRequestValidator()
    {
        // DayOfWeek em C# vai de 0 (Domingo) a 6 (Sábado)
        RuleFor(x => x.Dia1)
            .InclusiveBetween(0, 6).WithMessage("O Dia 1 é inválido. Escolha entre 0 (Domingo) e 6 (Sábado).");

        RuleFor(x => x.Dia2)
            .InclusiveBetween(0, 6).WithMessage("O Dia 2 é inválido. Escolha entre 0 (Domingo) e 6 (Sábado).")
            .NotEqual(x => x.Dia1).WithMessage("Os dias obrigatórios devem ser diferentes um do outro.");
    }
}

public class AdicionarHabilidadeRequestValidator : AbstractValidator<AdicionarHabilidadeRequest>
{
    public AdicionarHabilidadeRequestValidator()
    {
        RuleFor(x => x.RitmoIdHash)
            .NotEmpty().WithMessage("O ID do ritmo é obrigatório.");

        RuleFor(x => x.PapelDominante)
            .NotEmpty().WithMessage("O papel dominante é obrigatório.")
            .Must(papel => papel == "Condutor" || papel == "Conduzido" || papel == "Ambos")
            .WithMessage("O papel deve ser 'Condutor', 'Conduzido' ou 'Ambos'.");

        RuleFor(x => x.Nivel)
            .NotEmpty().WithMessage("O nível é obrigatório.")
            .Must(nivel => nivel == "Iniciante" || nivel == "Básico" || nivel == "Intermediário" || nivel == "Avançado" || nivel == "Professor")
            .WithMessage("Nível inválido. Escolha entre Iniciante, Básico, Intermediário, Avançado ou Professor.");
    }
}