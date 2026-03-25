using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using Rascunho.Shared.DTOs;

namespace Rascunho.Client.Infraestrutura;

// Herda de DelegatingHandler para entrar no "meio" do pipeline HTTP
public class HttpInterceptorHandler : DelegatingHandler
{
    private readonly IWebAssemblyHostEnvironment _env;
    private readonly ISnackbar _snackbar;

    public HttpInterceptorHandler(IWebAssemblyHostEnvironment env, ISnackbar snackbar)
    {
        _env = env;
        _snackbar = snackbar;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Deixa a requisição ir para o backend e aguarda a resposta
        var response = await base.SendAsync(request, cancellationToken);

        // Se deu sucesso (200, 201, etc), apenas devolve a resposta para a tela e não faz nada.
        if (response.IsSuccessStatusCode)
            return response;

        // --- SE CAIU AQUI, DEU ERRO ---
        var corpo = await response.Content.ReadAsStringAsync(cancellationToken);
        string mensagemUsuario = "Ocorreu um erro inesperado na comunicação com o servidor.";

        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if ((int)response.StatusCode == 400) // Erros do FluentValidation
            {
                var problema = JsonSerializer.Deserialize<ValidationProblemDto>(corpo, jsonOptions);
                var primeiraMensagem = problema?.Errors?.Values.FirstOrDefault()?.FirstOrDefault();
                if (primeiraMensagem != null) mensagemUsuario = primeiraMensagem;

                _snackbar.Add(mensagemUsuario, Severity.Warning);
            }
            else if ((int)response.StatusCode == 422) // Regras de Negócio
            {
                var erroNegocio = JsonSerializer.Deserialize<ErroGenericoDto>(corpo, jsonOptions);
                if (erroNegocio?.Erro != null) mensagemUsuario = erroNegocio.Erro;

                _snackbar.Add(mensagemUsuario, Severity.Error);
            }
            else // Erro 500 ou outros (404, 401, 403)
            {
                var erro500 = JsonSerializer.Deserialize<ErroGenericoDto>(corpo, jsonOptions);

                // Exibe só a mensagem genérica na UI
                _snackbar.Add(erro500?.Erro ?? mensagemUsuario, Severity.Error);

                // LOGA OS DETALHES NO CONSOLE APENAS EM DESENVOLVIMENTO
                //if (_env.IsDevelopment())
                //{
                    Console.WriteLine($"=== ERRO HTTP {(int)response.StatusCode} ===");
                    Console.WriteLine($"URL: {request.RequestUri}");
                    Console.WriteLine($"Detalhes: {erro500?.Detalhes ?? corpo}");
                    Console.WriteLine("===========================");
                //}
            }
        }
        catch
        {
            // Se o JSON vier quebrado ou o backend mandar HTML de erro
            _snackbar.Add(mensagemUsuario, Severity.Error);
            if (_env.IsDevelopment()) Console.WriteLine($"Erro bruto: {corpo}");
        }

        // Retorna a resposta para o componente (caso ele ainda queira checar o StatusCode)
        return response;
    }
}