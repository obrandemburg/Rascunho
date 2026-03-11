namespace Rascunho.Entities;

public class TurmaProfessor
{
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public int ProfessorId { get; set; }
    public Usuario Professor { get; set; } = null!;
}