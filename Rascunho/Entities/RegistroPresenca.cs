namespace Rascunho.Entities;

public class RegistroPresenca
{
    public int TurmaId { get; protected set; }
    public Turma Turma { get; protected set; } = null!;

    public int AlunoId { get; protected set; }
    public Usuario Aluno { get; protected set; } = null!;

    public DateOnly DataAula { get; protected set; }
    public bool Presente { get; protected set; }

    // NOVO Sprint 2: Observação opcional do professor (ex: "chegou atrasado", "evolução notável")
    // Nullable string — null significa sem observação registrada
    public string? Observacao { get; protected set; }

    protected RegistroPresenca() { }

    public RegistroPresenca(int turmaId, int alunoId, DateOnly dataAula, bool presente, string? observacao = null)
    {
        TurmaId = turmaId;
        AlunoId = alunoId;
        DataAula = dataAula;
        Presente = presente;
        Observacao = observacao;
    }

    // ATUALIZAÇÃO: Aceita observação opcional
    // Se observacao == null: mantém a observação anterior intacta
    // Se observacao == "":   limpa a observação (professor apagou)
    // Se observacao == "xx": atualiza para o novo texto
    public void AtualizarPresenca(bool presente, string? observacao = null)
    {
        Presente = presente;
        if (observacao != null)
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao;
    }
}