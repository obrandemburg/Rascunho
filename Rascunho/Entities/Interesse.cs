namespace Rascunho.Entities;

public class Interesse
{
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public int AlunoId { get; set; }
    public Usuario Aluno { get; set; } = null!;

    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
}