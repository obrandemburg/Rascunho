namespace Rascunho.Entities;

/// <summary>
/// Representa o agendamento de uma reposição de falta.
/// Ciclo de vida: Agendada → Realizada (via chamada) | Cancelada (pelo aluno)
/// 
/// Relação com a falta original:
///   A falta é um RegistroPresenca com Presente=false.
///   Identificamos a falta pela chave composta: TurmaOrigemId + AlunoId + DataFalta.
///   Não criamos FK direta para RegistroPresenca porque sua chave é composta
///   e EF Core não suporta FK para chave composta de forma simples.
/// </summary>
public class Reposicao
{
    public int Id { get; protected set; }

    // ── Aluno ────────────────────────────────────────────────────
    public int AlunoId { get; protected set; }
    public Usuario Aluno { get; protected set; } = null!;

    // ── Referência à falta original ──────────────────────────────
    // Turma onde o aluno FALTOU
    public int TurmaOrigemId { get; protected set; }
    public Turma TurmaOrigem { get; protected set; } = null!;

    // Data exata da falta (DateOnly — sem componente de hora)
    public DateOnly DataFalta { get; protected set; }

    // ── Agendamento da reposição ─────────────────────────────────
    // Turma onde o aluno fará a reposição (pode ser diferente da origem,
    // mas deve ser do mesmo Ritmo — validado no service)
    public int TurmaDestinoId { get; protected set; }
    public Turma TurmaDestino { get; protected set; } = null!;

    // Data e hora agendada para a reposição
    public DateTime DataReposicaoAgendada { get; protected set; }

    // ── Estado ───────────────────────────────────────────────────
    // "Agendada" → status inicial
    // "Realizada" → quando a chamada da turma destino registra o aluno presente
    // "Cancelada" → quando o aluno cancela (falta volta a ser elegível)
    public string Status { get; protected set; } = string.Empty;

    public DateTime DataSolicitacao { get; protected set; }

    protected Reposicao() { }

    public Reposicao(int alunoId, int turmaOrigemId, DateOnly dataFalta,
                     int turmaDestinoId, DateTime dataReposicaoAgendada)
    {
        AlunoId = alunoId;
        TurmaOrigemId = turmaOrigemId;
        DataFalta = dataFalta;
        TurmaDestinoId = turmaDestinoId;

        // Garante que a data está em UTC — consistência com o restante do sistema
        DataReposicaoAgendada = dataReposicaoAgendada.ToUniversalTime();

        Status = "Agendada";
        DataSolicitacao = DateTime.UtcNow;
    }

    public void Cancelar() => Status = "Cancelada";

    /// <summary>
    /// Chamado pelo ChamadaService quando o aluno é marcado como presente
    /// na turma destino na data agendada.
    /// </summary>
    public void MarcarRealizada() => Status = "Realizada";
}