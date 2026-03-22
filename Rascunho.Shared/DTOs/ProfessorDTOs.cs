namespace Rascunho.Shared.DTOs;

// ── Disponibilidade do professor para aulas particulares ──────────

/// <summary>
/// Um slot individual de disponibilidade.
/// DiaDaSemana: 0=Domingo, 1=Segunda ... 6=Sábado
/// </summary>
public record SlotDisponibilidadeRequest(
    int DiaDaSemana,
    TimeSpan HorarioInicio,
    TimeSpan HorarioFim
);

/// <summary>
/// Operação "replace all": substitui TODOS os slots existentes
/// pelos novos enviados. Lista vazia = remove toda a disponibilidade.
/// </summary>
public record AtualizarDisponibilidadeRequest(List<SlotDisponibilidadeRequest> Slots);

/// <summary>Response de um slot já salvo.</summary>
public record ObterDisponibilidadeResponse(
    string IdHash,
    int DiaDaSemana,
    TimeSpan HorarioInicio,
    TimeSpan HorarioFim,
    bool Ativo
);