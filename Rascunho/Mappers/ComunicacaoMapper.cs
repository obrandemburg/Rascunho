using HashidsNet;
using Rascunho.DTOs;
using Rascunho.Entities;

namespace Rascunho.Mappers;

public static class ComunicacaoMapper
{
    public static ObterAvisoResponse ToResponse(this Aviso a, IHashids hashids) =>
        new ObterAvisoResponse(
            hashids.Encode(a.Id),
            a.Titulo,
            a.Mensagem,
            a.DataPublicacao,
            a.DataExpiracao,
            a.TipoVisibilidade,
            a.Autor?.Nome ?? "Desconhecido"
        );
}