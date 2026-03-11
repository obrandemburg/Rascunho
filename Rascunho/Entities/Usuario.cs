namespace Rascunho.Entities;
public abstract class Usuario
{
    public int Id { get; protected set; }
    public string Nome { get; protected set; } = string.Empty;
    public string Email { get; protected set; } = string.Empty;
    public string SenhaHash { get; protected set; } = string.Empty;
    public string Tipo { get; protected set; } = string.Empty;
    public string NomeSocial { get; protected set; } = string.Empty;
    public string Biografia { get; protected set; } = string.Empty;
    public string FotoUrl { get; protected set; } = string.Empty;
    public bool Ativo { get; protected set; } = true;
    public DateOnly DataNascimento { get; set; }

    protected Usuario() { }

    protected Usuario(string nome, string email, string senhaHash, string tipo)
    {
        Nome = nome;
        NomeSocial = nome;
        Email = email;
        SenhaHash = senhaHash;
        Tipo = tipo;
        Ativo = true;
    }

    public void EditarPerfil(string fotoUrl, string nomeSocial, string biografia)
    {
        FotoUrl = fotoUrl;
        NomeSocial = nomeSocial;
        Biografia = biografia;
    }
    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}
public class Aluno : Usuario
{
    public Aluno(string nome, string email, string senhaHash)
        : base(nome, email, senhaHash, "Aluno") { }
}

public class Professor : Usuario
{
    public Professor(string nome, string email, string senhaHash)
        : base(nome, email, senhaHash, "Professor") { }
}

public class Bolsista : Usuario
{
    public Bolsista(string nome, string email, string senhaHash)
        : base(nome, email, senhaHash, "Bolsista") { }
}

public class Lider : Usuario
{
    public Lider(string nome, string email, string senhaHash)
        : base(nome, email, senhaHash, "Líder") { }
}

public class Gerente : Usuario
{
    public Gerente(string nome, string email, string senhaHash)
        : base(nome, email, senhaHash, "Gerente") { }
}

public class Recepcao : Usuario
{
    public Recepcao(string nome, string email, string senhaHash)
        : base(nome, email, senhaHash, "Recepção") { }
}
