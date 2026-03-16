using HashidsNet;
using Rascunho.Entities;
using Rascunho.Shared.DTOs;

namespace Rascunho.Mappers;

public static class UsuarioMapper
{
    public static ObterUsuarioResponse ToResponse(this Usuario u, IHashids hashids) =>
        new ObterUsuarioResponse(hashids.Encode(u.Id), u.Nome, u.Email, u.Tipo, u.NomeSocial, u.Biografia, u.FotoUrl, u.Ativo);
}