// Localização: Rascunho.Shared/DTOs/UsuarioDTOs.cs
//
// ATENÇÃO: Este arquivo pertence ao projeto Rascunho.SHARED
// Caminho correto: Rascunho.Shared/DTOs/UsuarioDTOs.cs
// NÃO colocar em Rascunho/DTOs/ — lá não existe e o compilador não o encontra.
namespace Rascunho.Shared.DTOs;

/// <summary>
/// Request de criação de usuário.
///
/// CAMPOS OBRIGATÓRIOS PARA TODOS:
///   Nome, Email, Senha, Tipo, DataNascimento
///
/// CAMPOS OPCIONAIS PARA TODOS:
///   Telefone, FotoUrl, Cpf
///
/// CAMPOS OBRIGATÓRIOS POR TIPO:
///   Professor → RitmosIdHash (pelo menos 1)
///   Bolsista  → PapelDominante + DiaObrigatorio1 + DiaObrigatorio2
/// </summary>
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
    string? Genero,        // NOVO: "Masculino" | "Feminino" | "Não informado"

    // Apenas para Professor
    List<string>? RitmosIdHash,

    // Apenas para Bolsista
    string? PapelDominante,
    int? DiaObrigatorio1,
    int? DiaObrigatorio2
);

public record EditarPerfilRequest(string NomeSocial, string Biografia, string FotoUrl);

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
    string Genero          // NOVO
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
