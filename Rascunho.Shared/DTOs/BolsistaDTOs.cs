// ARQUIVO: Rascunho.Shared/DTOs/BolsistaDTOs.cs
//
// SPRINT 6: Adicionados RitmoNome, DiaDaSemana, HorarioInicio, HorarioFim
//           ao SugestaoBalanceamentoResponse (cards de turma exibiam hash criptografado).
//
// SPRINT 7: Adicionado FotoUrl ao DesempenhoResponse para que
//           QuadroDesempenho (Gerente) e Desempenho (Bolsista) possam
//           exibir a foto do bolsista nos cards e no cabeçalho.
//
// ATENÇÃO: records posicionais em C# geram construtores com ordem fixa.
// Ao adicionar campos, a ORDEM dos argumentos em BolsistaService.cs
// deve corresponder EXATAMENTE à ordem declarada aqui.

namespace Rascunho.Shared.DTOs;

public record DefinirDiasObrigatoriosRequest(int Dia1, int Dia2);
public record AdicionarHabilidadeRequest(string RitmoIdHash, string PapelDominante, string Nivel);

/// <summary>
/// Resposta do endpoint de análise de balanceamento de turmas.
/// Usada nas telas TurmasObrigatorias (Bolsista) e TurmasRecomendadas (Bolsista).
///
/// SPRINT 6: adicionados RitmoNome, DiaDaSemana, HorarioInicio e HorarioFim
/// para exibir informações legíveis em vez do hash criptografado.
/// </summary>
public record SugestaoBalanceamentoResponse(
    string TurmaIdHash,
    int TotalCondutores,
    int TotalConduzidos,
    string Status,
    int QuantidadeFaltante,
    List<BolsistaSugerido> Sugestoes,
    // ── SPRINT 6 ──────────────────────────────────────────────────
    string RitmoNome,
    int DiaDaSemana,
    TimeSpan HorarioInicio,
    TimeSpan HorarioFim
);

public record BolsistaSugerido(
    string BolsistaIdHash,
    string Nome,
    string PapelDominante,
    string Nivel
);

public record RelatorioHorasBolsistaResponse(
    string BolsistaIdHash,
    string Nome,
    double HorasCumpridasNaSemana,
    double HorasFaltantes,
    bool MetaAtingida
);

/// <summary>
/// Detalha a frequência do bolsista separando dias obrigatórios de dias extras.
///
/// SPRINT 7: adicionado FotoUrl após Nome para exibir a foto do bolsista
/// nos cards do QuadroDesempenho (Gerente) e na tela Desempenho (Bolsista).
/// Posição 3 no construtor — atualizar BolsistaService.MeuDesempenhoAsync na mesma posição.
/// </summary>
public record DesempenhoResponse(
    string BolsistaIdHash,
    string Nome,
    string FotoUrl,                 // ← NOVO Sprint 7 — posição 3
    int? DiaObrigatorio1,
    int? DiaObrigatorio2,
    double FrequenciaObrigatoriaPct,
    string IndicadorSituacao,
    int TotalAulasObrigatorias,
    int TotalPresencasObrigatorias,
    double FrequenciaExtraPct,
    int TotalAulasExtras,
    int TotalPresencasExtras,
    List<HistoricoPresencaItem> Historico
);

public record HistoricoPresencaItem(
    DateOnly DataAula,
    string NomeTurma,
    string NomeProfessor,
    bool Presente,
    bool EhDiaObrigatorio
);
