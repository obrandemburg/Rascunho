namespace Rascunho.Entities;

public class Sala
{
    public int Id { get; protected set; }
    public string Nome { get; protected set; } = string.Empty;
    public int CapacidadeMaxima { get; protected set; }
    public bool Ativo { get; protected set; } = true;

    protected Sala() { }

    public Sala(string nome, int capacidadeMaxima)
    {
        Nome = nome;
        CapacidadeMaxima = capacidadeMaxima;
        Ativo = true;
    }

    public void Atualizar(string nome, int capacidadeMaxima)
    {
        Nome = nome;
        CapacidadeMaxima = capacidadeMaxima;
    }

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}