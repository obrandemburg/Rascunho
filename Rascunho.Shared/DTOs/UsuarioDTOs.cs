namespace Rascunho.Shared.DTOs;

public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Tipo);
public record EditarPerfilRequest(string NomeSocial, string Biografia, string FotoUrl);
public record ObterUsuarioResponse(string IdHash, string Nome, string Email, string Tipo, string NomeSocial, string Biografia, string FotoUrl, bool Ativo);
public record LoginRequest(string Email, string Senha);
public record LoginResponse(string Token, string Nome, string Tipo);