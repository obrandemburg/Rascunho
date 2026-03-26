// Localização: Rascunho/Services/INotificacaoService.cs
namespace Rascunho.Services;

/// <summary>
/// Contrato para envio de notificações push aos usuários do aplicativo.
/// A implementação completa (Firebase Cloud Messaging) será entregue no Feature #4.
/// </summary>
public interface INotificacaoService
{
    /// <summary>
    /// Notifica um aluno que uma vaga está disponível na turma em que ele estava aguardando.
    /// </summary>
    /// <param name="alunoId">ID interno do aluno a ser notificado.</param>
    /// <param name="ritmoNome">Nome do ritmo da turma (ex: "Forró", "Tango").</param>
    /// <param name="dataExpiracao">Prazo máximo com timezone para o aluno confirmar a vaga.</param>
    Task NotificarVagaDisponivelAsync(int alunoId, string ritmoNome, DateTimeOffset dataExpiracao);
}
