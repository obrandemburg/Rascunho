using Microsoft.AspNetCore.Diagnostics;
using Rascunho.Exceptions;

namespace Rascunho.Infra;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Define o status code padrão como 500 (Erro Interno)
        int statusCode = StatusCodes.Status500InternalServerError;
        string mensagem = "Ocorreu um erro interno no servidor.";

        // Aqui você mapeia qual exceção gera qual HTTP Status Code
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
        }

        // Configura a resposta HTTP
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new { erro = mensagem }, cancellationToken);

        // Retorna true para avisar ao .NET que o erro já foi tratado e não deve derrubar a aplicação
        return true;
    }
}