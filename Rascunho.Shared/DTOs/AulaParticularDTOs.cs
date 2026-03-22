// Localização: Rascunho.Shared/DTOs/AulaParticularDTOs.cs
using System;

namespace Rascunho.Shared.DTOs;

public record SolicitarAulaParticularRequest(
    string ProfessorIdHash,
    string RitmoIdHash,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    string Observacao
);

public record ResponderAulaParticularRequest(bool Aceitar);

// NOVO Sprint 4: usado pelo aluno para propor novo horário.
// O sistema cancela a aula atual e cria uma nova solicitação.
// O professor precisará aceitar novamente o novo horário.
public record ReagendarAulaParticularRequest(
    DateTime NovaDataHoraInicio,
    DateTime NovaDataHoraFim
);

// MODIFICADO Sprint 4: ProfessorIdHash adicionado.
// Necessário para que o frontend busque os slots de disponibilidade
// do professor ao abrir o fluxo de reagendamento.
public record ObterAulaParticularResponse(
    string IdHash,
    string ProfessorIdHash,   // NOVO — hash do professor para buscar disponibilidade
    string NomeProfessor,
    string NomeAluno,
    string NomeRitmo,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    string Status,
    string Observacao,
    decimal ValorCobrado      // Sprint 3 — RN-BOL03
);