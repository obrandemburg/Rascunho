namespace Rascunho.Entities;

public class AulaExperimental
{
    public int Id { get; protected set; }

    public int AlunoId { get; protected set; }
    public Usuario Aluno { get; protected set; } = null!;

    public int TurmaId { get; protected set; }
    public Turma Turma { get; protected set; } = null!;

    public DateTime DataAula { get; protected set; }
    public string Status { get; protected set; } = string.Empty; // "Pendente", "Confirmada", "Cancelada", "Realizada"
    public DateTime DataSolicitacao { get; protected set; }

    protected AulaExperimental() { }

    public AulaExperimental(int alunoId, int turmaId, DateTime dataAula)
    {
        AlunoId = alunoId;
        TurmaId = turmaId;
        DataAula = dataAula.ToUniversalTime();
        Status = "Pendente";
        DataSolicitacao = DateTime.UtcNow;
    }

    public void Confirmar() => Status = "Confirmada";
    public void Cancelar() => Status = "Cancelada";
    public void MarcarComoRealizada() => Status = "Realizada";
}