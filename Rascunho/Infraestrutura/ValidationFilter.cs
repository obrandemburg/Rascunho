using FluentValidation;

namespace Rascunho.Infraestrutura;

// Esta classe genérica <T> pode validar QUALQUER Dto no futuro (Turmas, Salas, etc)
public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Tenta achar o validador para o DTO específico que chegou na requisição
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();

        if (validator is not null)
        {
            // Pega o JSON (DTO) que o usuário enviou
            var entity = context.Arguments.OfType<T>().FirstOrDefault();

            if (entity is not null)
            {
                // Executa as regras do FluentValidation
                var validationResult = await validator.ValidateAsync(entity);

                if (!validationResult.IsValid)
                {
                    // Se falhar, aborta a requisição e retorna os erros estruturados em um status 400
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }
            }
        }

        // Se estiver tudo certo, deixa o fluxo continuar para o seu Endpoint
        return await next(context);
    }
}