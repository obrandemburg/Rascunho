namespace Rascunho.Entities;

public class Ritmo
{
    public int Id { get; protected set; }
    public string Nome { get; protected set; } = string.Empty;
    public string Descricao { get; protected set; } = string.Empty;
    public string Modalidade { get; protected set; } = string.Empty;
    public bool Ativo { get; protected set; } = true;

    protected Ritmo() { }

    public Ritmo(string nome, string descricao, string modalidade)
    {
        Nome = nome;
        Descricao = descricao;
        Modalidade = modalidade;
        Ativo = true;
    }

    public void Atualizar(string nome, string descricao, string modalidade)
    {
        Nome = nome;
        Descricao = descricao;
        Modalidade = modalidade;
    }

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}