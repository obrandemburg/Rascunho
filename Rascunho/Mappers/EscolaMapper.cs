using System.Linq;
using HashidsNet;
using Rascunho.Entities;
using Rascunho.Shared.DTOs;

namespace Rascunho.Mappers;

public static class EscolaMapper
{
    public static ObterRitmoResponse ToResponse(this Ritmo r, IHashids hashids) =>
        new ObterRitmoResponse(hashids.Encode(r.Id), r.Nome, r.Descricao, r.Modalidade, r.Ativo);

    public static ObterSalaResponse ToResponse(this Sala s, IHashids hashids) =>
        new ObterSalaResponse(hashids.Encode(s.Id), s.Nome, s.CapacidadeMaxima, s.Ativo);

    public static ObterTurmaResponse ToResponse(this Turma t, IHashids hashids) =>
        new ObterTurmaResponse(
            hashids.Encode(t.Id),
            t.Ritmo?.Nome ?? "N/A",
            t.Sala?.Nome ?? "N/A",
            t.DataInicio,
            (int)t.DiaDaSemana,
            t.HorarioInicio.ToString(@"hh\:mm"),
            t.HorarioFim.ToString(@"hh\:mm"),
            t.Nivel,
            t.LimiteAlunos,
            t.Matriculas?.Count ?? 0,
            t.LinkWhatsApp ?? "",
            t.Ativa,
            t.Professores?.Select(p => p.Professor?.Nome ?? "Desconhecido").ToList() ?? new List<string>()
        );

    public static ObterEventoResponse ToResponse(this Evento e, IHashids hashids) =>
        new ObterEventoResponse(
            hashids.Encode(e.Id), e.Nome, e.Descricao, e.DataHora, e.Tipo, e.Capacidade, e.Preco, e.Ingressos?.Count(i => i.Status != "Cancelado") ?? 0
        );
}