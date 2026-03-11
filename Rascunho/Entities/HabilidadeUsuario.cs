namespace Rascunho.Entities;

public class HabilidadeUsuario
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int RitmoId { get; set; }
    public Ritmo Ritmo { get; set; } = null!;

    public string PapelDominante { get; set; } = string.Empty; // "Condutor", "Conduzida", "Ambos"
    public string Nivel { get; set; } = string.Empty; // "Iniciante", "Intermediário", "Avançado", "Professor"
}