// Localização: Rascunho/Validations/CriarUsuarioRequestValidator.cs
using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

public class CriarUsuarioRequestValidator : AbstractValidator<CriarUsuarioRequest>
{
    public CriarUsuarioRequestValidator()
    {
        // ── Campos comuns a TODOS os tipos ────────────────────────

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .Length(3, 100).WithMessage("O nome deve ter entre 3 e 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres.");

        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("O tipo de usuário é obrigatório.")
            .Must(tipo => new[] { "Aluno", "Professor", "Bolsista", "Gerente", "Recepção", "Líder" }
                .Contains(tipo))
            .WithMessage("Tipo inválido. Opções: Aluno, Professor, Bolsista, Gerente, Recepção, Líder.");

        RuleFor(x => x.Genero)
            .Must(g => g == null || new[] { "Masculino", "Feminino", "Não informado" }.Contains(g))
            .WithMessage("Gênero inválido. Opções: Masculino, Feminino, Não informado.")
            .When(x => !string.IsNullOrEmpty(x.Genero));

        // DataNascimento: obrigatória para todos, deve ser data passada
        // e o usuário deve ter ao menos 5 anos (evita dados claramente errados)
        RuleFor(x => x.DataNascimento)
            .Must(d => d < DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("A data de nascimento deve ser uma data passada.")
            .Must(d => d < DateOnly.FromDateTime(DateTime.Today.AddYears(-5)))
            .WithMessage("Data de nascimento inválida.");

        // Telefone: opcional, mas se informado deve ter comprimento razoável
        RuleFor(x => x.Telefone)
            .MaximumLength(20).WithMessage("O telefone deve ter no máximo 20 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Telefone));

        // CPF: opcional. Se informado, deve ter exatamente 11 dígitos após
        // remover a formatação, e deve ser matematicamente válido.
        RuleFor(x => x.Cpf)
            .Must(cpf =>
            {
                if (string.IsNullOrWhiteSpace(cpf)) return true; // opcional — vazio é válido
                var digitos = new string(cpf.Where(char.IsDigit).ToArray());
                return digitos.Length == 11 && ValidarCpf(digitos);
            })
            .WithMessage("CPF inválido. Informe os 11 dígitos ou deixe em branco.")
            .When(x => !string.IsNullOrEmpty(x.Cpf));

        // ── Regras específicas por tipo ────────────────────────────

        // PROFESSOR: pelo menos 1 ritmo obrigatório
        RuleFor(x => x.RitmosIdHash)
            .NotNull().WithMessage("Informe pelo menos um ritmo para o professor.")
            .Must(r => r != null && r.Count > 0)
            .WithMessage("O professor deve lecionar pelo menos um ritmo.")
            .When(x => x.Tipo == "Professor");

        // BOLSISTA: papel dominante obrigatório
        RuleFor(x => x.PapelDominante)
            .NotEmpty().WithMessage("O papel dominante é obrigatório para bolsistas.")
            .Must(p => p == "Condutor" || p == "Conduzido" || p == "Ambos")
            .WithMessage("Papel inválido. Use: 'Condutor', 'Conduzido' ou 'Ambos'.")
            .When(x => x.Tipo == "Bolsista");

        // BOLSISTA: dois dias obrigatórios distintos
        RuleFor(x => x.DiaObrigatorio1)
            .NotNull().WithMessage("O Dia Obrigatório 1 é obrigatório para bolsistas.")
            .InclusiveBetween(0, 6).WithMessage("Dia inválido. Use 0 (Domingo) a 6 (Sábado).")
            .When(x => x.Tipo == "Bolsista");

        RuleFor(x => x.DiaObrigatorio2)
            .NotNull().WithMessage("O Dia Obrigatório 2 é obrigatório para bolsistas.")
            .InclusiveBetween(0, 6).WithMessage("Dia inválido. Use 0 (Domingo) a 6 (Sábado).")
            .NotEqual(x => x.DiaObrigatorio1)
            .WithMessage("Os dois dias obrigatórios devem ser diferentes.")
            .When(x => x.Tipo == "Bolsista");

    }

    /// <summary>
    /// Validação matemática do CPF usando o algoritmo oficial da Receita Federal.
    ///
    /// O CPF tem dois dígitos verificadores (os últimos 2 dígitos).
    /// Cada dígito é calculado como:
    ///   1. Multiplica os 9 primeiros dígitos por pesos decrescentes (10, 9, 8 ... 2)
    ///   2. Soma os produtos
    ///   3. Calcula o resto por 11
    ///   4. Se resto < 2, dígito = 0; senão dígito = 11 - resto
    ///
    /// CPFs com todos os dígitos iguais (111.111.111-11) são tecnicamente
    /// válidos matematicamente mas não são emitidos pela Receita — rejeitados aqui.
    /// </summary>
    private static bool ValidarCpf(string cpf)
    {
        // Rejeita sequências repetidas como "00000000000"
        if (cpf.Distinct().Count() == 1) return false;

        // Calcula primeiro dígito verificador
        int soma = 0;
        for (int i = 0; i < 9; i++)
            soma += int.Parse(cpf[i].ToString()) * (10 - i);
        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;
        if (digito1 != int.Parse(cpf[9].ToString())) return false;

        // Calcula segundo dígito verificador
        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += int.Parse(cpf[i].ToString()) * (11 - i);
        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;
        return digito2 == int.Parse(cpf[10].ToString());
    }

}
