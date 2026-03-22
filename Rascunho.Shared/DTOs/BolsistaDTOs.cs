namespace Rascunho.Shared.DTOs;

public record DefinirDiasObrigatoriosRequest(int Dia1, int Dia2);
public record AdicionarHabilidadeRequest(string RitmoIdHash, string PapelDominante, string Nivel);

public record SugestaoBalanceamentoResponse(
    string TurmaIdHash,
    int TotalCondutores,
    int TotalConduzidos,
    string Status,
    int QuantidadeFaltante,
    List<BolsistaSugerido> Sugestoes
);

public record BolsistaSugerido(string BolsistaIdHash, string Nome, string PapelDominante, string Nivel);

public record RelatorioHorasBolsistaResponse(
    string BolsistaIdHash,
    string Nome,
    double HorasCumpridasNaSemana,
    double HorasFaltantes,
    bool MetaAtingida
);

// ── NOVO Sprint 2: Desempenho de frequência do bolsista ──────────

/// <summary>
/// Detalha a frequência do bolsista separando dias obrigatórios de dias extras.
/// Indicadores de situação (baseado na frequência obrigatória):
///   Excelente    → ≥ 85%
///   Vamos melhorar → 75% a 84%
///   Atenção      → 60% a 74%
///   Crítico      → abaixo de 60%
/// </summary>
public record DesempenhoResponse(
    string BolsistaIdHash,
    string Nome,
    int? DiaObrigatorio1,            // null se não configurado ainda
    int? DiaObrigatorio2,
    double FrequenciaObrigatoriaPct, // percentual só dos dias obrigatórios
    string IndicadorSituacao,        // "Excelente" | "Vamos melhorar" | "Atenção" | "Crítico"
    int TotalAulasObrigatorias,      // total de registros nos dias obrigatórios
    int TotalPresencasObrigatorias,  // quantas foram presença
    double FrequenciaExtraPct,       // percentual nos dias extras (informativo)
    int TotalAulasExtras,
    int TotalPresencasExtras,
    List<HistoricoPresencaItem> Historico
);

/// <summary>
/// Um item do histórico aula-a-aula do bolsista.
/// EhDiaObrigatorio = true → esta aula conta para o indicador de frequência obrigatória.
/// </summary>
public record HistoricoPresencaItem(
    DateOnly DataAula,
    string NomeTurma,
    string NomeProfessor,
    bool Presente,
    bool EhDiaObrigatorio
);