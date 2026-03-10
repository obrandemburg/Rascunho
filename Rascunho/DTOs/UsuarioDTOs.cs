namespace Rascunho.DTOs
{
    public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Tipo);
    public record EditarPerfilRequest(string FotoUrl, string NomeSocial, string Biografia);
    public record ObterUsuarioResponse(string Email, string nome, string NomeSocial, string Biografia, string FotoUrl, string Tipo, bool Ativo);
}
