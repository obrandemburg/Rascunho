// Localização: Rascunho.Shared/DTOs/UsuarioDTOs.cs
namespace Rascunho.Shared.DTOs;


public record CriarUsuarioRequest(
    string Nome,
    string Email,
    string Senha,
    string Tipo,
    DateOnly DataNascimento,

    // Opcionais para todos os perfis
    string? Telefone,
    string? FotoUrl,
    string? Cpf,
    string? Genero,

    // Apenas para Professor
    List<string>? RitmosIdHash,

    // Apenas para Bolsista
    string? PapelDominante,
    int? DiaObrigatorio1,
    int? DiaObrigatorio2
);

public record EditarPerfilRequest(string NomeSocial, string Biografia, string FotoUrl);
public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);

public record ObterUsuarioResponse(
    string IdHash,
    string Nome,
    string Email,
    string Tipo,
    string NomeSocial,
    string Biografia,
    string FotoUrl,
    bool Ativo,
    string? Telefone,
    DateOnly DataNascimento,
    string? Cpf,
    string Genero
);
public record BuscarUsuarioResponse(
    string IdHash,
    string Nome,
    string FotoUrl,
    string Tipo,
    string Genero
);

public record LoginRequest(string Email, string Senha);
public record LoginResponse(string Token, string Nome, string Tipo, string FotoUrl);

/// <summary>
/// Retornado pelo POST /api/upload/foto.
/// O frontend salva a Url e a inclui no campo FotoUrl ao criar o usuário.
/// </summary>
public record UploadFotoResponse(
    string Url,           // http://IP:8080/uploads/fotos/uuid.jpg
    string NomeArquivo,
    long TamanhoBytes
);
