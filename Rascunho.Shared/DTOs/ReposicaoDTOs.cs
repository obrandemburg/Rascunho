namespace Rascunho.Shared.DTOs;

/// <summary>
/// Representa uma falta elegível para reposição.
/// Retornada pelo GET /api/reposicoes/elegiveis.
/// </summary>
public record FaltaElegivelResponse(
    string TurmaOrigemIdHash,
    string RitmoNome,
    string NomeTurma,       // ex: "Forró — Iniciante (Terças 18h)"
    DateOnly DataFalta,
    string? MotivoInelegibilidade  // null = elegível; preenchido = por que não pode repor
);

/// <summary>
/// Enviado pelo aluno ao POST /api/reposicoes/agendar.
/// </summary>
public record AgendarReposicaoRequest(
    string TurmaOrigemIdHash,
    DateOnly DataFalta,
    string TurmaDestinoIdHash,
    DateTime DataReposicaoAgendada
);

/// <summary>
/// Retornado após agendar ou ao listar reposições do aluno.
/// </summary>
public record ObterReposicaoResponse(
    string IdHash,
    string TurmaOrigemNome,
    DateOnly DataFalta,
    string TurmaDestinoNome,
    DateTime DataReposicaoAgendada,
    string Status,
    DateTime DataSolicitacao
);