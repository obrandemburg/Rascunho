using Microsoft.AspNetCore.Diagnostics;
using Rascunho.Exceptions;

namespace Rascunho.Infraestrutura;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode = StatusCodes.Status500InternalServerError;
        string mensagem = "Ocorreu um erro interno no servidor.";
        string? detalhes = null; // NOVA VARIÁVEL

        if (exception is ArgumentException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            mensagem = exception.Message;
        }
        else if (exception is RegraNegocioException)
        {
            statusCode = StatusCodes.Status422UnprocessableEntity;
            mensagem = exception.Message;
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            mensagem = "Ocorreu um erro interno. Tente novamente mais tarde.";

            // Captura o stacktrace completo para enviar ao frontend
            detalhes = exception.ToString();

            _logger.LogError(exception, "ERRO NÃO TRATADO CAPTURADO PELO GLOBAL HANDLER");
        }

        httpContext.Response.StatusCode = statusCode;

        // Agora retornamos um objeto com duas propriedades
        await httpContext.Response.WriteAsJsonAsync(new { erro = mensagem, detalhes = detalhes }, cancellationToken);

        return true;
    }
}