// Localização: Rascunho/Mappers/AulaMapper.cs
using HashidsNet;
using Rascunho.Entities;
using Rascunho.Shared.DTOs;

namespace Rascunho.Mappers;

public static class AulaMapper
{
    public static ObterAulaExperimentalResponse ToResponse(this AulaExperimental a, IHashids hashids) =>
        new ObterAulaExperimentalResponse(
            hashids.Encode(a.Id),
            a.Aluno?.Nome ?? "",
            $"{a.Turma?.Ritmo?.Nome} {a.Turma?.Nivel}",
            a.DataAula,
            a.Status
        );

    // MODIFICADO Sprint 4: ProfessorIdHash adicionado ao response.
    // O frontend usa este hash para buscar a disponibilidade do professor
    // ao reagendar: GET /api/professores/{ProfessorIdHash}/disponibilidade
    public static ObterAulaParticularResponse ToResponse(this AulaParticular a, IHashids hashids) =>
        new ObterAulaParticularResponse(
            hashids.Encode(a.Id),
            hashids.Encode(a.ProfessorId),   // NOVO
            a.Professor?.Nome ?? "",
            a.Aluno?.Nome ?? "",
            a.Ritmo?.Nome ?? "",
            a.DataHoraInicio,
            a.DataHoraFim,
            a.Status,
            a.ObservacaoAluno,
            a.ValorCobrado
        );

    public static ObterReposicaoResponse ToResponse(this Reposicao r, IHashids hashids) =>
        new ObterReposicaoResponse(
            hashids.Encode(r.Id),
            $"{r.TurmaOrigem?.Ritmo?.Nome ?? "?"} — {r.TurmaOrigem?.Nivel ?? "?"}",
            r.DataFalta,
            $"{r.TurmaDestino?.Ritmo?.Nome ?? "?"} — {r.TurmaDestino?.Nivel ?? "?"}",
            r.DataReposicaoAgendada,
            r.Status,
            r.DataSolicitacao
        );
}