// ARQUIVO: Rascunho.Shared/DTOs/BolsistaDTOs.cs
//
// ALTERAÇÃO SPRINT 6:
// SugestaoBalanceamentoResponse recebia apenas TurmaIdHash, sem dados
// legíveis da turma. As telas TurmasObrigatorias.razor e TurmasRecomendadas.razor
// exibiam o hash criptografado (ex: "aBcDeFgH") como título do card, tornando
// impossível para o bolsista identificar qual turma estava sendo exibida.
//
// CAMPOS ADICIONADOS:
//   - RitmoNome   (string)    : nome do ritmo da turma
//   - DiaDaSemana (int)       : dia da semana (0=Dom, 1=Seg ... 6=Sáb)
//   - HorarioInicio (TimeSpan): horário de início da turma
//   - HorarioFim    (TimeSpan): horário de fim da turma
//
// ATENÇÃO: Como SugestaoBalanceamentoResponse é um record no C#, a ordem
// dos parâmetros no construtor importa. Os novos campos foram adicionados
// ao FINAL para não quebrar código existente que usa argumento posicional.

namespace Rascunho.Shared.DTOs;

public record DefinirDiasObrigatoriosRequest(int Dia1, int Dia2);
public record AdicionarHabilidadeRequest(string RitmoIdHash, string PapelDominante, string Nivel);

/// <summary>
/// Resposta do endpoint de análise de balanceamento de turmas.
/// Usada nas telas TurmasObrigatorias (Bolsista) e TurmasRecomendadas (Bolsista).
///
/// SPRINT 6: adicionados RitmoNome, DiaDaSemana, HorarioInicio e HorarioFim
/// para que as telas possam exibir informações legíveis ao bolsista,
/// em vez de mostrar apenas o hash criptografado da turma.
/// </summary>
public record SugestaoBalanceamentoResponse(
    string TurmaIdHash,
    int TotalCondutores,
    int TotalConduzidos,
    string Status,
    int QuantidadeFaltante,
    List<BolsistaSugerido> Sugestoes,

    // ── NOVO Sprint 6 — identificação legível da turma ────────────
    // Sem esses campos, as telas exibiam apenas o hash (ex: "aBcDeFgH")
    // como título do card, impossibilitando o bolsista de identificar a turma.
    string RitmoNome,       // ex: "Forró"
    int DiaDaSemana,        // 0=Dom, 1=Seg, 2=Ter, 3=Qua, 4=Qui, 5=Sex, 6=Sáb
    TimeSpan HorarioInicio, // ex: 18:00:00
    TimeSpan HorarioFim     // ex: 19:00:00
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

// ── Sprint 2: Desempenho de frequência do bolsista ──────────────

/// <summary>
/// Detalha a frequência do bolsista separando dias obrigatórios de dias extras.
/// </summary>
public record DesempenhoResponse(
    string BolsistaIdHash,
    string Nome,
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
