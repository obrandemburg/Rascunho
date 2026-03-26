// Localização: Rascunho/Entities/ListaEspera.cs
namespace Rascunho.Entities;

/// <summary>
/// Status de uma entrada na fila de espera de uma turma.
/// </summary>
public enum StatusListaEspera
{
    /// <summary>Aluno na fila, aguardando abertura de vaga.</summary>
    Aguardando,

    /// <summary>Vaga disponível; aluno foi notificado e tem prazo para confirmar.</summary>
    Notificado,

    /// <summary>O prazo de confirmação encerrou sem resposta do aluno.</summary>
    Expirado,

    /// <summary>Aluno confirmou a vaga e foi matriculado na turma.</summary>
    Convertido
}

/// <summary>
/// Representa a posição de um aluno na fila de espera de uma turma lotada.
/// Substitui o uso improvisado de <see cref="Interesse"/> como fila,
/// adicionando controle de posição, status e prazo de confirmação.
/// </summary>
public class ListaEspera
{
    public int Id { get; set; }

    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public int AlunoId { get; set; }
    public Usuario Aluno { get; set; } = null!;

    /// <summary>Data e hora em que o aluno entrou na fila (com timezone).</summary>
    public DateTimeOffset DataEntrada { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Posição sequencial na fila (1 = primeiro).</summary>
    public int Posicao { get; set; }

    public StatusListaEspera Status { get; set; } = StatusListaEspera.Aguardando;

    /// <summary>Data em que o aluno foi notificado sobre a vaga disponível (com timezone).</summary>
    public DateTimeOffset? DataNotificacao { get; set; }

    /// <summary>Prazo máximo para o aluno confirmar a vaga (com timezone).</summary>
    public DateTimeOffset? DataExpiracao { get; set; }
}
