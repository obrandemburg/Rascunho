// Localização: Rascunho/Entities/Matricula.cs
namespace Rascunho.Entities;

public class Matricula
{
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public int AlunoId { get; set; }
    public Usuario Aluno { get; set; } = null!;

    public string Papel { get; set; } = string.Empty; // "Condutor", "Conduzido", "Ambos"
    public DateTime DataMatricula { get; set; } = DateTime.UtcNow;

    // ── NOVO Sprint 4 ─────────────────────────────────────────────
    // Armazena o valor de mensalidade acordado NO MOMENTO da matrícula.
    // null = valor padrão (a ser definido no módulo financeiro 1.2)
    // valor > 0 = preço customizado (ex: 50% de desconto para bolsista)
    //
    // Por que persistir aqui?
    // O bolsista paga 50% em turmas solo (RN-BOL02). Quando ele
    // é desativado, suas matrículas existentes precisam saber
    // qual era o desconto para o sistema financeiro (fase 1.2)
    // poder ajustar corretamente as cobranças.
    //
    // Exemplo prático:
    //   Bolsista matrícula em "Forró Solo" → ValorMensalidade = 100.00 (50% de 200)
    //   Gerente desativa bolsa             → ValorMensalidade = 200.00 (preço cheio)
    //   Sistema financeiro (1.2) lê este campo para gerar cobrança correta
    public decimal? ValorMensalidade { get; set; } = null;

    // Indica qual desconto estava ativo na matrícula.
    // null = sem desconto (aluno regular)
    // "Bolsista50%" = desconto de bolsista (50%)
    // Preservar este histórico evita ambiguidade quando o desconto é removido
    public string? OrigemDesconto { get; set; } = null;
}