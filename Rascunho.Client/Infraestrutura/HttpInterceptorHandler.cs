// ARQUIVO: Rascunho.Client/Infraestrutura/HttpInterceptorHandler.cs
//
// ALTERAÇÃO — Logout automático em 401 e Correção de Loop Infinito (100%):
//   O construtor agora recebe apenas o IServiceProvider.
//   Os demais serviços são instanciados dinamicamente sob demanda para evitar 
//   a Dependência Circular que travava o carregamento do Blazor WebAssembly.

using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Rascunho.Client.Security;
using Rascunho.Shared.DTOs;

namespace Rascunho.Client.Infraestrutura;

public class HttpInterceptorHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;

    public HttpInterceptorHandler(IServiceProvider serviceProvider)
    {
        // Injeta apenas o Provedor de Serviços para quebrar a dependência circular
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Deixa a requisição ir para o backend e aguarda a resposta
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return response;

        // --- SE CAIU AQUI, DEU ERRO ---
        var corpo = await response.Content.ReadAsStringAsync(cancellationToken);

        Console.WriteLine($"[INTERCEPTADOR] Falha HTTP {(int)response.StatusCode} na rota {request.RequestUri}");
        Console.WriteLine($"[INTERCEPTADOR] Resposta: {corpo}");

        // Resolvendo as dependências apenas quando necessárias (Lazy Loading)
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorageService>();
        var authStateProvider = _serviceProvider.GetRequiredService<AuthenticationStateProvider>();
        var navigationManager = _serviceProvider.GetRequiredService<NavigationManager>();
        var snackbar = _serviceProvider.GetRequiredService<ISnackbar>();
        var env = _serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>();

        // ── 401 UNAUTHORIZED: token expirado ou inválido ──────────
        // Remove a sessão do localStorage e redireciona para login.
        if ((int)response.StatusCode == 401)
        {
            await localStorage.RemoveItemAsync("authToken");
            await localStorage.RemoveItemAsync("userName");
            await localStorage.RemoveItemAsync("userType");
            await localStorage.RemoveItemAsync("userFotoUrl");

            ((CustomAuthStateProvider)authStateProvider).NotifyUserLogout();

            snackbar.Add("Sua sessão expirou. Faça login novamente.", Severity.Warning);
            navigationManager.NavigateTo("/login");
            return response;
        }

        string mensagemUsuario = "Ocorreu um erro inesperado.";

        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if ((int)response.StatusCode == 400) // Erros do FluentValidation
            {
                var problema = JsonSerializer.Deserialize<ValidationProblemDto>(corpo, jsonOptions);
                var primeiraMensagem = problema?.Errors?.Values.FirstOrDefault()?.FirstOrDefault();
                if (primeiraMensagem != null) mensagemUsuario = primeiraMensagem;

                snackbar.Add(mensagemUsuario, Severity.Warning);
            }
            else if ((int)response.StatusCode == 422) // Regras de Negócio
            {
                var erroNegocio = JsonSerializer.Deserialize<ErroGenericoDto>(corpo, jsonOptions);
                if (erroNegocio?.Erro != null) mensagemUsuario = erroNegocio.Erro;

                snackbar.Add(mensagemUsuario, Severity.Error);
            }
            else // Erro 500 ou outros (404, 403)
            {
                var erro500 = JsonSerializer.Deserialize<ErroGenericoDto>(corpo, jsonOptions);

                snackbar.Add(erro500?.Erro ?? mensagemUsuario, Severity.Error);

                Console.WriteLine($"=== ERRO HTTP {(int)response.StatusCode} ===");
                Console.WriteLine($"URL: {request.RequestUri}");
                Console.WriteLine($"Detalhes: {erro500?.Detalhes ?? corpo}");
                Console.WriteLine("===========================");
            }
        }
        catch
        {
            // Se o JSON vier quebrado ou o backend mandar HTML de erro
            snackbar.Add(mensagemUsuario, Severity.Error);
            if (env.IsDevelopment()) Console.WriteLine($"Erro bruto: {corpo}");
        }

        // Retorna a resposta para o componente (caso ele ainda queira checar o StatusCode)
        return response;
    }
}