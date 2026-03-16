namespace Rascunho.Shared.DTOs;

public record CriarRitmoRequest(string Nome, string Descricao, string Modalidade);
public record AtualizarRitmoRequest(string Nome, string Descricao, string Modalidade);
public record ObterRitmoResponse(string IdHash, string Nome, string Descricao, string Modalidade, bool Ativo);