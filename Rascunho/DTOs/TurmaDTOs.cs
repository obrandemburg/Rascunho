using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs;

public record CriarTurmaRequest(
    string RitmoIdHash,
    string SalaIdHash,
    DateTime DataInicio,
    int DiaDaSemana, // 0 = Domingo, 1 = Segunda, etc.
    TimeSpan HorarioInicio,
    TimeSpan HorarioFim,
    string Nivel,
    int LimiteAlunos,
    string LinkWhatsApp,
    List<string> ProfessoresIdsHash
);

public record TrocarSalaRequest(
    string NovaSalaIdHash,
    int NovoLimiteAlunos
);

public record MatricularRequest(
    string Papel // Deve ser "Condutor" ou "Conduzido"
);

public record ObterTurmaResponse(
    string IdHash,
    string RitmoNome,
    string SalaNome,
    DateTime DataInicio,
    int DiaDaSemana,
    TimeSpan HorarioInicio,
    TimeSpan HorarioFim,
    string Nivel,
    int LimiteAlunos,
    int QuantidadeMatriculados,
    string LinkWhatsApp,
    bool Ativa,
    List<string> NomesProfessores
)
{
    public static ObterTurmaResponse DeEntidade(Turma t, IHashids hashids)
    {
        return new ObterTurmaResponse(
            hashids.Encode(t.Id),
            t.Ritmo?.Nome ?? "N/A",
            t.Sala?.Nome ?? "N/A",
            t.DataInicio,
            (int)t.DiaDaSemana,
            t.HorarioInicio,
            t.HorarioFim,
            t.Nivel,
            t.LimiteAlunos,
            t.Matriculas?.Count ?? 0,
            t.LinkWhatsApp,
            t.Ativa,
            t.Professores?.Select(p => p.Professor?.Nome ?? "Desconhecido").ToList() ?? new List<string>()
        );
    }
    public record MatricularAdminRequest(string AlunoIdHash, string Papel);
}