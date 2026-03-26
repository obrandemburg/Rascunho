// Localização: Rascunho/Services/NotificacaoServiceStub.cs
namespace Rascunho.Services;

/// <summary>
/// Implementação provisória do <see cref="INotificacaoService"/>.
/// Registra a intenção de notificação via log enquanto o Feature #4 (FCM) não está implementado.
///
/// Para implementar o Feature #4:
///   1. Criar FirebaseNotificacaoService implementando INotificacaoService
///   2. Em Program.cs, trocar AddScoped&lt;INotificacaoService, NotificacaoServiceStub&gt;
///      por AddScoped&lt;INotificacaoService, FirebaseNotificacaoService&gt;
///   3. Nenhuma outra alteração é necessária — todos os callers usam a interface.
/// </summary>
public class NotificacaoServiceStub : INotificacaoService
{
    private readonly ILogger<NotificacaoServiceStub> _logger;

    public NotificacaoServiceStub(ILogger<NotificacaoServiceStub> logger)
    {
        _logger = logger;
    }

    public Task NotificarVagaDisponivelAsync(int alunoId, string ritmoNome, DateTime dataExpiracao)
    {
        // TODO [Feature #4 — FCM]: Substituir este stub por chamada à Firebase Cloud Messaging API v1.
        // O aluno deve receber push notification com:
        //   Título: "Vaga disponível! 🎉"
        //   Corpo:  "Uma vaga na turma de {ritmoNome} está reservada para você.
        //            Confirme até {dataExpiracao:dd/MM HH:mm}."
        _logger.LogInformation(
            "[ListaEspera] Notificação pendente — Aluno {AlunoId} | Turma: {RitmoNome} | " +
            "Prazo: {DataExpiracao:dd/MM/yyyy HH:mm} UTC. " +
            "Push notification aguarda Feature #4 (FCM).",
            alunoId, ritmoNome, dataExpiracao);

        return Task.CompletedTask;
    }
}
