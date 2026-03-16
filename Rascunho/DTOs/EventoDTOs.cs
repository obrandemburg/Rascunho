using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs;

public record CriarEventoRequest(string Nome, string Descricao, DateTime DataHora, string Tipo, int Capacidade, decimal Preco);

public record ComprarIngressoResponse(string IngressoIdHash, string Mensagem, decimal ValorCobrado);

public record ObterEventoResponse(string IdHash, string Nome, string Descricao, DateTime DataHora, string Tipo, int Capacidade, decimal Preco, int IngressosVendidos)
{
    public static ObterEventoResponse DeEntidade(Evento e, IHashids hashids)
    {
        return new ObterEventoResponse(
            hashids.Encode(e.Id), e.Nome, e.Descricao, e.DataHora, e.Tipo, e.Capacidade, e.Preco, e.Ingressos?.Count ?? 0
        );
    }
}