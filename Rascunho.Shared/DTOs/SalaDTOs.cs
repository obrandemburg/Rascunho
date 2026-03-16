namespace Rascunho.Shared.DTOs;

public record CriarSalaRequest(string Nome, int CapacidadeMaxima);
public record AtualizarSalaRequest(string Nome, int CapacidadeMaxima);
public record ObterSalaResponse(string IdHash, string Nome, int CapacidadeMaxima, bool Ativa);