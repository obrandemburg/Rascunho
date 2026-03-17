using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Rascunho.Shared.DTOs; // Usando os seus DTOs já existentes
using Rascunho.Client.Security;
using System.Net.Http.Json;

namespace Rascunho.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    // Se no seu UsuarioDTOs.cs os nomes forem diferentes (ex: UsuarioLoginRequest), altere nas linhas abaixo:
    public async Task<string?> LoginAsync(LoginRequest loginModel)
    {
        // Apontando para a sua API em produção
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result != null && !string.IsNullOrWhiteSpace(result.Token))
            {
                await _localStorage.SetItemAsync("authToken", result.Token);
                await _localStorage.SetItemAsync("userName", result.Nome);
                await _localStorage.SetItemAsync("userType", result.Tipo);

                // Chama o método atualizado
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
                return null;
            }
        }

        return "E-mail ou senha incorretos.";
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("userName");
        await _localStorage.RemoveItemAsync("userType");

        _httpClient.DefaultRequestHeaders.Authorization = null;

        // Chama o método atualizado que resolve o erro
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
    }
}