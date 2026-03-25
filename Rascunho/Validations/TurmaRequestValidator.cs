// ═══════════════════════════════════════════════════════════════════════════
// ARQUIVO: Rascunho/Validations/TurmaRequestValidator.cs
// SPRINT 9 — TAREFA S9-01 (NOVO ARQUIVO)
//
// CONTEXTO DO BUG:
// O endpoint POST /api/turmas/criar retornava 500 (Internal Server Error)
// porque NÃO existia este validator. Sem validação, dados inválidos
// (campos null, listas vazias, horários invertidos) chegavam ao
// TurmaService.CriarTurmaAsync e causavam exceções genéricas como
// NullReferenceException — que o GlobalExceptionHandler classificava
// corretamente como 500 (erro interno), não 422 (regra de negócio).
//
// COMO ESTA VALIDAÇÃO RESOLVE O PROBLEMA:
// 1. O ValidationFilter<CriarTurmaRequest> intercepta a requisição
//    ANTES de chegar ao endpoint.
// 2. Se alguma regra falhar, retorna HTTP 400 (Bad Request) com a
//    lista de erros estruturados — o endpoint NUNCA é executado.
// 3. Se todas as regras passarem, os dados chegam ao TurmaService
//    já sanitizados, e qualquer exceção será RegraNegocioException
//    (choque de sala, professor inválido) → capturada pelo
//    GlobalExceptionHandler como 422.
//
// RESULTADO: O endpoint para de retornar 500 e passa a retornar:
//   - 400 para dados inválidos (este validator)
//   - 422 para regras de negócio violadas (GlobalExceptionHandler)
//   - 201 para criação bem-sucedida
// ═══════════════════════════════════════════════════════════════════════════

using FluentValidation;
using Rascunho.Shared.DTOs;

namespace Rascunho.Validations;

/// <summary>
/// Validador do DTO CriarTurmaRequest.
/// Garante que todos os campos obrigatórios estão presentes e íntegros
/// antes de atingir o TurmaService — eliminando NullReferenceExceptions.
///
/// Registrado automaticamente pelo FluentValidation via:
///   builder.Services.AddValidatorsFromAssemblyContaining<Program>();
/// (já presente no Program.cs)
/// </summary>
public class CriarTurmaRequestValidator : AbstractValidator<CriarTurmaRequest>
{
    public CriarTurmaRequestValidator()
    {
        // ── RitmoIdHash ──────────────────────────────────────────
        // Obrigatório: sem ritmo, não há turma.
        // O TurmaService decodifica via _hashids.Decode() — se vier
        // vazio, Decode retorna array vazio e o service lança
        // RegraNegocioException, mas preferimos barrar aqui.
        RuleFor(x => x.RitmoIdHash)
            .NotEmpty()
            .WithMessage("O ritmo é obrigatório para criar uma turma.");

        // ── SalaIdHash ───────────────────────────────────────────
        // Obrigatório: toda turma precisa de uma sala física.
        RuleFor(x => x.SalaIdHash)
            .NotEmpty()
            .WithMessage("A sala é obrigatória para criar uma turma.");

        // ── ProfessoresIdsHash ───────────────────────────────────
        // PONTO CRÍTICO DO BUG: se ProfessoresIdsHash vier null,
        // o foreach no TurmaService causa NullReferenceException.
        //
        // NotNull() valida que a lista existe.
        // Must(count > 0) valida que tem pelo menos 1 professor.
        // Juntas, garantem que o foreach sempre terá dados válidos.
        RuleFor(x => x.ProfessoresIdsHash)
            .NotNull()
            .WithMessage("A lista de professores é obrigatória.")
            .Must(lista => lista != null && lista.Count > 0)
            .WithMessage("Selecione pelo menos um professor para a turma.");
        RuleForEach(x => x.ProfessoresIdsHash)
            .NotEmpty()
            .WithMessage("É necessário selecionar um professor para criar a turma.");

        // ── DiaDaSemana ──────────────────────────────────────────
        // DayOfWeek no .NET: 0 = Domingo, 6 = Sábado.
        // Aceita qualquer dia da semana válido.
        RuleFor(x => x.DiaDaSemana)
            .InclusiveBetween(0, 6)
            .WithMessage("Dia da semana inválido. Use 0 (Domingo) a 6 (Sábado).");

        // ── HorarioInicio ────────────────────────────────────────
        // Deve ser um horário válido (não pode ser TimeSpan.Zero
        // para evitar turmas sem horário definido).
        RuleFor(x => x.HorarioInicio)
            .NotEqual(TimeSpan.Zero)
            .WithMessage("O horário de início é obrigatório.");

        // ── HorarioFim ───────────────────────────────────────────
        // Deve ser posterior ao início. Sem essa regra, o TurmaService
        // criaria turmas com duração zero ou negativa.
        RuleFor(x => x.HorarioFim)
            .GreaterThan(x => x.HorarioInicio)
            .WithMessage("O horário de término deve ser posterior ao horário de início.");

        // ── Nivel ────────────────────────────────────────────────
        // Identifica o nível da turma (Iniciante, Básico, etc.).
        // Não validamos valores específicos aqui para permitir
        // flexibilidade futura — apenas garantimos que não é vazio.
        RuleFor(x => x.Nivel)
            .NotEmpty()
            .WithMessage("O nível da turma é obrigatório (ex: Iniciante, Básico, Intermediário, Avançado).");

        // ── LimiteAlunos ─────────────────────────────────────────
        // Deve ser positivo. O TurmaService valida se excede a
        // capacidade da sala — aqui apenas garantimos > 0.
        RuleFor(x => x.LimiteAlunos)
            .GreaterThan(0)
            .WithMessage("O limite de alunos deve ser maior que zero.");

        // ── LinkWhatsApp ─────────────────────────────────────────
        // Campo opcional — não obrigatório. Se preenchido, apenas
        // valida comprimento máximo para evitar abuse.
        RuleFor(x => x.LinkWhatsApp)
            .MaximumLength(500)
            .WithMessage("O link do WhatsApp é muito longo (máximo 500 caracteres).")
            .When(x => !string.IsNullOrEmpty(x.LinkWhatsApp));
    }
}