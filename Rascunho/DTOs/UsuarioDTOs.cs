using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs
{
    public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Tipo);
    public record EditarPerfilRequest(string FotoUrl, string NomeSocial, string Biografia);
    public record LoginRequest(string Email, string Senha);
    public record LoginResponse(string Token, string IdHash, string Nome, string Tipo);
    public record ListagemComContagemResponse(int Quantidade, IEnumerable<ObterUsuarioResponse> Usuarios);
    public record ObterUsuarioResponse(string IdHash, string Email, string Nome, string NomeSocial, string Biografia, string FotoUrl, string Tipo, bool Ativo)
    {
        // Método estático para converter Entidade -> DTO aplicando o Hash no ID
        public static ObterUsuarioResponse DeEntidade(Usuario u, IHashids hashids)
        {
            return new ObterUsuarioResponse(
                hashids.Encode(u.Id),
                u.Email,
                u.Nome,
                u.NomeSocial,
                u.Biografia,
                u.FotoUrl,
                u.Tipo,
                u.Ativo
            );
        }
    }
}
