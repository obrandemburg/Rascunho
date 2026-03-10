using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs;

public record CriarRitmoRequest(string Nome, string Descricao, string Modalidade);
public record AtualizarRitmoRequest(string Nome, string Descricao, string Modalidade);

public record ObterRitmoResponse(string IdHash, string Nome, string Descricao, string Modalidade, bool Ativo)
{
    public static ObterRitmoResponse DeEntidade(Ritmo r, IHashids hashids)
    {
        return new ObterRitmoResponse(
            hashids.Encode(r.Id),
            r.Nome,
            r.Descricao,
            r.Modalidade,
            r.Ativo
        );
    }
}