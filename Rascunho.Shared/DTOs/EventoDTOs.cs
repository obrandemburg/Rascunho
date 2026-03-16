using System;

namespace Rascunho.Shared.DTOs;

public record CriarEventoRequest(string Nome, string Descricao, DateTime DataHora, string Tipo, int Capacidade, decimal Preco);
public record ComprarIngressoResponse(string IngressoIdHash, string Mensagem, decimal ValorCobrado);
public record ObterEventoResponse(string IdHash, string Nome, string Descricao, DateTime DataHora, string Tipo, int Capacidade, decimal Preco, int IngressosVendidos);