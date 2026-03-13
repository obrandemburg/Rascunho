using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs;

public record CriarAvisoRequest(string Titulo, string Mensagem, DateTime DataExpiracao, string TipoVisibilidade);
public record AtualizarAvisoRequest(string Titulo, string Mensagem, DateTime DataExpiracao, string TipoVisibilidade);

public record ObterAvisoResponse(
    string IdHash,
    string Titulo,
    string Mensagem,
    DateTime DataPublicacao,
    DateTime DataExpiracao,
    string TipoVisibilidade,
    string NomeAutor)
{
    public static ObterAvisoResponse DeEntidade(Aviso a, IHashids hashids)
    {
        return new ObterAvisoResponse(
            hashids.Encode(a.Id),
            a.Titulo,
            a.Mensagem,
            a.DataPublicacao,
            a.DataExpiracao,
            a.TipoVisibilidade,
            a.Autor?.Nome ?? "Desconhecido"
        );
    }
}