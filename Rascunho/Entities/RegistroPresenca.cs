namespace Rascunho.Entities;

public class RegistroPresenca
{
    public int TurmaId { get; protected set; }
    public Turma Turma { get; protected set; } = null!;

    public int AlunoId { get; protected set; }
    public Usuario Aluno { get; protected set; } = null!;

    // Usamos DateOnly porque a chamada refere-se a um dia específico, não a um segundo específico
    public DateOnly DataAula { get; protected set; }

    public bool Presente { get; protected set; }

    protected RegistroPresenca() { }

    public RegistroPresenca(int turmaId, int alunoId, DateOnly dataAula, bool presente)
    {
        TurmaId = turmaId;
        AlunoId = alunoId;
        DataAula = dataAula;
        Presente = presente;
    }

    public void AtualizarPresenca(bool presente)
    {
        Presente = presente;
    }
}