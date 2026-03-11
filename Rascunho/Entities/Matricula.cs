namespace Rascunho.Entities;

public class Matricula
{
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public int AlunoId { get; set; }
    public Usuario Aluno { get; set; } = null!;

    public string Papel { get; set; } = string.Empty; // "Condutor", "Conduzido" (Crucial para bolsistas!)
    public DateTime DataMatricula { get; set; } = DateTime.UtcNow;
}