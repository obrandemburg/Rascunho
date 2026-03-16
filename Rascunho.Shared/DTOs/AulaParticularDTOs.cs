using System;

namespace Rascunho.Shared.DTOs;

public record SolicitarAulaParticularRequest(string ProfessorIdHash, string RitmoIdHash, DateTime DataHoraInicio, DateTime DataHoraFim, string Observacao);
public record ResponderAulaParticularRequest(bool Aceitar);
public record ObterAulaParticularResponse(string IdHash, string NomeProfessor, string NomeAluno, string NomeRitmo, DateTime DataHoraInicio, DateTime DataHoraFim, string Status, string Observacao);