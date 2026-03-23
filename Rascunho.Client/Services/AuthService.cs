// ARQUIVO: Rascunho.Client/Services/AuthService.cs
//
// ALTERAÇÃO SPRINT 6:
// Adicionado armazenamento de 'userFotoUrl' no LocalStorage durante o login.
// Isso é necessário para que a página Perfil.razor possa recuperar a URL
// da foto atual do usuário antes de fazer o PUT em /api/usuarios/meu-perfil/atualizar.
//
// Sem isso, o PUT enviava FotoUrl = null, o que apagava a foto de todos
// os usuários que salvavam o perfil.
//
// MUDANÇAS:
// - LoginAsync: adiciona SetItemAsync("userFotoUrl", result.FotoUrl)
// - LogoutAsync: adiciona RemoveItemAsync("userFotoUrl")

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Rascunho.Shared.DTOs;
using Rascunho.Client.Security;
using System.Net.Http.Json;

namespace Rascunho.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(
        HttpClient httpClient,
        AuthenticationStateProvider authStateProvider,
        ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    /// <summary>
    /// Autentica o usuário na API e persiste os dados da sessão no LocalStorage.
    /// 
    /// Dados salvos:
    ///   - authToken  : token JWT completo, usado pelo CustomAuthStateProvider
    ///   - userName   : nome do usuário para exibição no AppBar
    ///   - userType   : role/tipo do usuário (Aluno, Professor, Bolsista, etc.)
    ///   - userFotoUrl: URL pública da foto, usada em Perfil.razor para manter a foto
    ///                  ao editar nome social/biografia (ADICIONADO Sprint 6)
    /// 
    /// Retorna null em caso de sucesso, ou uma mensagem de erro em caso de falha.
    /// </summary>
    public async Task<string?> LoginAsync(LoginRequest loginModel)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result != null && !string.IsNullOrWhiteSpace(result.Token))
            {
                // Persiste os dados essenciais da sessão
                await _localStorage.SetItemAsync("authToken", result.Token);
                await _localStorage.SetItemAsync("userName", result.Nome);
                await _localStorage.SetItemAsync("userType", result.Tipo);

                // NOVO Sprint 6: salva a URL da foto para uso em Perfil.razor.
                // Se o usuário não tem foto (FotoUrl = ""), salva string vazia — nunca nulo.
                // Isso evita que GetItemAsync retorne null no Perfil.razor.
                await _localStorage.SetItemAsync("userFotoUrl", result.FotoUrl ?? "");

                // Notifica o CustomAuthStateProvider para atualizar o estado de auth
                // em todos os componentes que escutam (AuthorizeView, cascading state, etc.)
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);

                return null; // Sucesso — sem mensagem de erro
            }
        }

        return "E-mail ou senha incorretos.";
    }

    /// <summary>
    /// Encerra a sessão do usuário: remove todos os dados do LocalStorage
    /// e notifica o CustomAuthStateProvider para redefinir o estado.
    /// </summary>
    public async Task LogoutAsync()
    {
        // Remove todos os itens relacionados à sessão
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("userName");
        await _localStorage.RemoveItemAsync("userType");
        await _localStorage.RemoveItemAsync("userFotoUrl"); // NOVO Sprint 6

        // Remove o header Authorization do HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = null;

        // Notifica o estado de autenticação como anônimo
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
    }
}
