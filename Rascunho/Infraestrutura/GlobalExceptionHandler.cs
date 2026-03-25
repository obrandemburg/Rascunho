using Microsoft.AspNetCore.Diagnostics;
using Rascunho.Exceptions;

namespace Rascunho.Infraestrutura;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    // Injetamos o Logger pelo construtor
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
            // Mantém a mensagem genérica para o frontend por segurança...
            //mensagem = "Ocorreu um erro interno. Tente novamente mais tarde.";

            mensagem = exception.ToString();
            // ... MAS LOGA O ERRO REAL NO CONSOLE DO BACKEND!
            //_logger.LogError(exception, "ERRO NÃO TRATADO CAPTURADO PELO GLOBAL HANDLER");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { erro = mensagem }, cancellationToken);

        return true;
    }
}