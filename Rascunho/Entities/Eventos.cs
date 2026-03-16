namespace Rascunho.Entities;

public class Evento
{
    public int Id { get; protected set; }
    public string Nome { get; protected set; } = string.Empty;
    public string Descricao { get; protected set; } = string.Empty;
    public DateTime DataHora { get; protected set; }
    public string Tipo { get; protected set; } = string.Empty; // "Baile" ou "Workshop"
    public int Capacidade { get; protected set; }
    public decimal Preco { get; protected set; }
    public bool Ativo { get; protected set; } = true;

    // Relacionamento 1:N com Ingressos
    public ICollection<Ingresso> Ingressos { get; protected set; } = new List<Ingresso>();

    protected Evento() { }

    public Evento(string nome, string descricao, DateTime dataHora, string tipo, int capacidade, decimal preco)
    {
        Nome = nome;
        Descricao = descricao;
        DataHora = dataHora.ToUniversalTime();
        Tipo = tipo;
        Capacidade = capacidade;
        Preco = preco;
        Ativo = true;
    }
}