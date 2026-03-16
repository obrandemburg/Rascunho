using HashidsNet;
using Rascunho.Shared.DTOs;
using Rascunho.Entities;

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

    public static ObterAulaParticularResponse ToResponse(this AulaParticular a, IHashids hashids) =>
        new ObterAulaParticularResponse(
            hashids.Encode(a.Id),
            a.Professor?.Nome ?? "",
            a.Aluno?.Nome ?? "",
            a.Ritmo?.Nome ?? "",
            a.DataHoraInicio,
            a.DataHoraFim,
            a.Status,
            a.ObservacaoAluno
        );
}