namespace Rascunho.Shared.DTOs;

// ── REQUEST: o que o professor envia ao salvar a chamada ──────────
// MODIFICADO: adicionado Observacao (opcional)
public record AlunoPresencaRequest(string AlunoIdHash, bool Presente, string? Observacao);

// MODIFICADO: adicionado ExtrasPresencas (opcional — participantes da Seção B)
public record RegistrarChamadaRequest(
    DateOnly DataAula,
    List<AlunoPresencaRequest> Presencas,              // alunos formalmente matriculados
    List<AlunoPresencaRequest>? ExtrasPresencas         // bolsistas / experimentais adicionados manualmente
);

// ── RESPONSE: o que o professor recebe ao abrir a tela de chamada ─
// MODIFICADO: adicionado Observacao e mudado Nome para ser consistente com API
public record AlunoChamadaResponse(
    string AlunoIdHash,
    string Nome,        // Mudado de NomeAluno para Nome (consistente com entidade)
    string FotoUrl,
    string Papel,
    bool Presente,
    string? Observacao  // NOVO: observação já salva (null se não houver)
);

// MODIFICADO: adicionado Extras (participantes não matriculados já salvos para esta data)
public record ObterChamadaResponse(
    string TurmaIdHash,
    DateOnly DataAula,
    List<AlunoChamadaResponse> Alunos,   // matriculados formalmente
    List<AlunoChamadaResponse> Extras    // NOVO: bolsistas/experimentais já registrados
);

// ── NOVO Sprint 2: resultado da busca de participantes extras (Seção B) ──
// TipoParticipante: "Bolsista" | "Experimental"
public record ParticipanteExtraResponse(
    string UsuarioIdHash,
    string Nome,
    string FotoUrl,
    string TipoParticipante
);