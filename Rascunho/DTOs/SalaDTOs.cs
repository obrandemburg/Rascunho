using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs;

public record CriarSalaRequest(string Nome, int CapacidadeMaxima);
public record AtualizarSalaRequest(string Nome, int CapacidadeMaxima);

public record ObterSalaResponse(string IdHash, string Nome, int CapacidadeMaxima, bool Ativo)
{
    public static ObterSalaResponse DeEntidade(Sala s, IHashids hashids)
    {
        return new ObterSalaResponse(
            hashids.Encode(s.Id),
            s.Nome,
            s.CapacidadeMaxima,
            s.Ativo
        );
    }
}