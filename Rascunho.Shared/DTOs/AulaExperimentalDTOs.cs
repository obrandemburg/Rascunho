using System;

namespace Rascunho.Shared.DTOs;

public record SolicitarAulaExperimentalRequest(string TurmaIdHash, DateTime DataAula);
public record AlterarStatusExperimentalRequest(string NovoStatus);
public record ObterAulaExperimentalResponse(string IdHash, string AlunoNome, string TurmaInfo, DateTime DataAula, string Status);