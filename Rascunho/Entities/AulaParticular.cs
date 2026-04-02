// Localização: Rascunho/Entities/AulaParticular.cs
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

    public string Status { get; protected set; } = string.Empty;

    // Observação enviada pelo aluno ao solicitar (ex: "preciso melhorar o passo básico")
    public string ObservacaoAluno { get; protected set; } = string.Empty;

    public DateTime DataSolicitacao { get; protected set; }

    // RN-BOL03: Valor calculado no momento da solicitação e persistido.
    // Bolsista paga 50% do preço padrão; Aluno paga valor integral.
    // Persistir garante que mudanças de preço não afetam cobranças históricas.
    public decimal ValorCobrado { get; protected set; }

    protected AulaParticular() { }

    public AulaParticular(
        int alunoId,
        int professorId,
        int ritmoId,
        DateTime dataHoraInicio,
        DateTime dataHoraFim,
        string observacaoAluno,
        decimal valorCobrado)
    {
        AlunoId = alunoId;
        ProfessorId = professorId;
        RitmoId = ritmoId;
        DataHoraInicio = DateTime.SpecifyKind(dataHoraInicio, DateTimeKind.Utc);
        DataHoraFim = DateTime.SpecifyKind(dataHoraFim, DateTimeKind.Utc);
        ObservacaoAluno = observacaoAluno;
        ValorCobrado = valorCobrado;
        Status = "Pendente";
        DataSolicitacao = DateTime.UtcNow;
    }

    public void Aceitar() => Status = "Aceita";
    public void Recusar() => Status = "Recusada";
    public void Cancelar() => Status = "Cancelada";
}