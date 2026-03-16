namespace Rascunho.Shared.DTOs;

public record DefinirDiasObrigatoriosRequest(int Dia1, int Dia2);

public record AdicionarHabilidadeRequest(string RitmoIdHash, string PapelDominante, string Nivel);

// O DTO do nosso relatório de Inteligência
public record SugestaoBalanceamentoResponse(
    string TurmaIdHash,
    int TotalCondutores,
    int TotalConduzidos,
    string Status, // "Balanceada", "Faltam Condutores", "Faltam Conduzidos"
    int QuantidadeFaltante,
    List<BolsistaSugerido> Sugestoes
);

public record BolsistaSugerido(string BolsistaIdHash, string Nome, string PapelDominante, string Nivel);

// DTO para relatório de horas
public record RelatorioHorasBolsistaResponse(
    string BolsistaIdHash,
    string Nome,
    double HorasCumpridasNaSemana,
    double HorasFaltantes,
    bool MetaAtingida
);