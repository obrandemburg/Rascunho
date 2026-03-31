using Microsoft.AspNetCore.Diagnostics;
using Rascunho.Exceptions;

namespace Rascunho.Infraestrutura;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
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
            mensagem = "Ocorreu um erro interno. Tente novamente mais tarde.";

            _logger.LogError(exception, "ERRO NÃO TRATADO CAPTURADO PELO GLOBAL HANDLER");
        }

        httpContext.Response.StatusCode = statusCode;

        if (_env.IsDevelopment())
        {
            await httpContext.Response.WriteAsJsonAsync(new { erro = mensagem, detalhes = exception.ToString() }, cancellationToken);
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync(new { erro = mensagem }, cancellationToken);
        }

        return true;
    }
}