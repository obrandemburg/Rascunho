// Localização: Rascunho/Entities/Usuario.cs
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

    public DateOnly DataNascimento { get; protected set; }

    // Opcional para todos os perfis
    public string Telefone { get; protected set; } = string.Empty;

    // CPF: opcional (pode ser nulo/vazio).
    // Quando informado, é armazenado SÓ com dígitos (11 chars), sem pontos e traço.
    // Exibição formatada é feita no frontend: "123.456.789-01"
    // Isso garante consistência nas comparações e evita duplicatas de formato.
    public string? Cpf { get; protected set; }
    public string Genero { get; protected set; } = "Não informado";

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
    public void AlterarSenha(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
            throw new ArgumentException("Hash de senha inválido.");

        SenhaHash = novoHash;
    }

    /// <summary>
    /// Define os dados complementares comuns a todos os perfis.
    /// O CPF é automaticamente sanitizado — remove pontos, traço e espaços.
    /// Exemplo: "123.456.789-01" → armazenado como "12345678901"
    /// </summary>
    public void DefinirDadosComplementares(DateOnly dataNascimento, string telefone, string? cpf)
    {
        DataNascimento = dataNascimento;
        Telefone = telefone.Trim();

        // Extrai apenas os dígitos do CPF informado
        // Se vier vazio ou nulo, armazena null (campo opcional)
        Cpf = string.IsNullOrWhiteSpace(cpf)
            ? null
            : new string(cpf.Where(char.IsDigit).ToArray());
    }

    /// <summary>Define ou atualiza a URL pública da foto de perfil.</summary>
    public void DefinirFoto(string fotoUrl) => FotoUrl = fotoUrl;
    public void DefinirGenero(string genero) => Genero = genero;

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}

// ──────────────────────────────────────────────────────────────────
// Subclasses concretas — usadas pelo TPH (Table-Per-Hierarchy).
// Cada uma tem seu próprio construtor mas herda todos os campos
// da classe base. O campo "Tipo" discrimina no banco de dados.
// ──────────────────────────────────────────────────────────────────

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
    // Dias da semana obrigatórios (0=Domingo ... 6=Sábado)
    public DayOfWeek? DiaObrigatorio1 { get; private set; }
    public DayOfWeek? DiaObrigatorio2 { get; private set; }

    // Papel dominante definido no cadastro inicial.
    // "Condutor", "Conduzido" ou "Ambos"
    // Usado pelo algoritmo de balanceamento de turmas.
    public string PapelDominante { get; private set; } = string.Empty;

    public Bolsista(string nome, string email, string senhaHash)
        : base(nome, email, senhaHash, "Bolsista") { }

    public void DefinirDiasObrigatorios(DayOfWeek dia1, DayOfWeek dia2)
    {
        DiaObrigatorio1 = dia1;
        DiaObrigatorio2 = dia2;
    }

    public void DefinirPapelDominante(string papel) => PapelDominante = papel;
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
