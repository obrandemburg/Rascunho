namespace Rascunho.Entities;

public class AulaParticular
{
    public int Id { get; protected set; }

    public int AlunoId { get; protected set; }
    public Usuario Aluno { get; protected set; } = null!;

    public int ProfessorId { get; protected set; }
    public Usuario Professor { get; protected set; } = null!;

    public int RitmoId { get; protected set; }
    public Ritmo Ritmo { get; protected set; } = null!;

    public DateTime DataHoraInicio { get; protected set; }
    public DateTime DataHoraFim { get; protected set; }

    public string Status { get; protected set; } = string.Empty; // Pendente, Aceita, Recusada, Cancelada
    public string ObservacaoAluno { get; protected set; } = string.Empty;
    public DateTime DataSolicitacao { get; protected set; }

    protected AulaParticular() { }

    public AulaParticular(int alunoId, int professorId, int ritmoId, DateTime dataHoraInicio, DateTime dataHoraFim, string observacaoAluno)
    {
        AlunoId = alunoId;
        ProfessorId = professorId;
        RitmoId = ritmoId;
        DataHoraInicio = dataHoraInicio.ToUniversalTime();
        DataHoraFim = dataHoraFim.ToUniversalTime();
        ObservacaoAluno = observacaoAluno;
        Status = "Pendente";
        DataSolicitacao = DateTime.UtcNow;
    }

    public void Aceitar() => Status = "Aceita";
    public void Recusar() => Status = "Recusada";
    public void Cancelar() => Status = "Cancelada";
}