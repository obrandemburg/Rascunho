namespace Rascunho.Entities;

public class Aviso
{
    public int Id { get; protected set; }
    public string Titulo { get; protected set; } = string.Empty;
    public string Mensagem { get; protected set; } = string.Empty;
    public DateTime DataPublicacao { get; protected set; }
    public DateTime DataExpiracao { get; protected set; }

    // "Geral" ou "Equipe"
    public string TipoVisibilidade { get; protected set; } = string.Empty;

    // Relacionamento com quem criou o aviso
    public int AutorId { get; protected set; }
    public Usuario Autor { get; protected set; } = null!;

    protected Aviso() { }

    public Aviso(string titulo, string mensagem, DateTime dataExpiracao, string tipoVisibilidade, int autorId)
    {
        Titulo = titulo;
        Mensagem = mensagem;
        DataPublicacao = DateTime.UtcNow;
        DataExpiracao = dataExpiracao.ToUniversalTime();
        TipoVisibilidade = tipoVisibilidade;
        AutorId = autorId;
    }

    public void Atualizar(string titulo, string mensagem, DateTime dataExpiracao, string tipoVisibilidade)
    {
        Titulo = titulo;
        Mensagem = mensagem;
        DataExpiracao = dataExpiracao.ToUniversalTime();
        TipoVisibilidade = tipoVisibilidade;
    }
}