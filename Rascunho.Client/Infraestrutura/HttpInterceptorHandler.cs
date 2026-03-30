// ARQUIVO: Rascunho.Client/Infraestrutura/HttpInterceptorHandler.cs
//
// ALTERAÇÃO — Logout automático em 401:
//   Quando o backend retorna HTTP 401 (Unauthorized), o interceptador agora
//   limpa o LocalStorage e redireciona para /login automaticamente.
//   Isso garante que um token expirado — mesmo que não detectado no cliente —
//   resulte em logout ao tentar acessar um endpoint protegido.

using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using Rascunho.Client.Security;
using Rascunho.Shared.DTOs;

namespace Rascunho.Client.Infraestrutura;

// Herda de DelegatingHandler para entrar no "meio" do pipeline HTTP
public class HttpInterceptorHandler : DelegatingHandler
{
    private readonly IWebAssemblyHostEnvironment _env;
    private readonly ISnackbar _snackbar;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;

    public HttpInterceptorHandler(
        IWebAssemblyHostEnvironment env,
        ISnackbar snackbar,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager)
    {
        _env = env;
        _snackbar = snackbar;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
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

        // ── 401 UNAUTHORIZED: token expirado ou inválido ──────────
        // Remove a sessão do localStorage e redireciona para login.
        // Isso garante logout automático quando o token expira no servidor.
        if ((int)response.StatusCode == 401)
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userName");
            await _localStorage.RemoveItemAsync("userType");
            await _localStorage.RemoveItemAsync("userFotoUrl");

            ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();

            _snackbar.Add("Sua sessão expirou. Faça login novamente.", Severity.Warning);
            _navigationManager.NavigateTo("/login");
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

                _snackbar.Add(mensagemUsuario, Severity.Warning);
            }
            else if ((int)response.StatusCode == 422) // Regras de Negócio
            {
                var erroNegocio = JsonSerializer.Deserialize<ErroGenericoDto>(corpo, jsonOptions);
                if (erroNegocio?.Erro != null) mensagemUsuario = erroNegocio.Erro;

                _snackbar.Add(mensagemUsuario, Severity.Error);
            }
            else // Erro 500 ou outros (404, 403)
            {
                var erro500 = JsonSerializer.Deserialize<ErroGenericoDto>(corpo, jsonOptions);

                _snackbar.Add(erro500?.Erro ?? mensagemUsuario, Severity.Error);

                Console.WriteLine($"=== ERRO HTTP {(int)response.StatusCode} ===");
                Console.WriteLine($"URL: {request.RequestUri}");
                Console.WriteLine($"Detalhes: {erro500?.Detalhes ?? corpo}");
                Console.WriteLine("===========================");
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