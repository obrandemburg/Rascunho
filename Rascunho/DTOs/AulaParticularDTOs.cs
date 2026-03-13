using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs;

public record SolicitarAulaParticularRequest(
    string ProfessorIdHash,
    string RitmoIdHash,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    string Observacao
);

public record ResponderAulaParticularRequest(bool Aceitar);

public record ObterAulaParticularResponse(
    string IdHash,
    string NomeProfessor,
    string NomeAluno,
    string NomeRitmo,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    string Status,
    string Observacao
)
{
    public static ObterAulaParticularResponse DeEntidade(AulaParticular a, IHashids hashids)
    {
        return new ObterAulaParticularResponse(
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
}