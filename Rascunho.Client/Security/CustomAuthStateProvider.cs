// ARQUIVO: Rascunho.Client/Security/CustomAuthStateProvider.cs
//
// ALTERAÇÃO — Segurança de tokens:
//   1. GetAuthenticationStateAsync agora verifica se o token está expirado (claim "exp").
//      Se expirado, limpa o localStorage e retorna usuário anônimo automaticamente.
//   2. TokenEstaExpirado: método auxiliar que lê a claim "exp" e compara com UtcNow.
//   Isso garante que mesmo que o token ainda exista no localStorage, ele não é aceito
//   pelo cliente se a validade já foi ultrapassada.

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Rascunho.Client.Security;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        // VERIFICAÇÃO DE EXPIRAÇÃO: se o token expirou, limpa a sessão e retorna anônimo.
        // Isso impede acesso a partes do sistema com token vencido mesmo antes de
        // uma chamada HTTP ser feita.
        if (TokenEstaExpirado(token))
        {
            await LimparSessaoAsync();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Define o header em TODAS as chamadas subsequentes do HttpClient
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    // ──────────────────────────────────────────────────────────────
    // VERIFICAÇÃO DE EXPIRAÇÃO
    //
    // Lê a claim "exp" do payload JWT (segundos desde epoch Unix) e
    // compara com o horário UTC atual.
    // Retorna true se o token estiver expirado ou malformado.
    // ──────────────────────────────────────────────────────────────
    private static bool TokenEstaExpirado(string token)
    {
        try
        {
            var payload = token.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (dict == null || !dict.TryGetValue("exp", out var expRaw))
                return false; // Sem claim exp → não rejeita (token sem expiração)

            if (!long.TryParse(expRaw?.ToString(), out var expSeconds))
                return false;

            var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
            return DateTime.UtcNow >= expDateTime;
        }
        catch
        {
            return true; // Token malformado → trata como expirado
        }
    }

    // Limpa todos os itens de sessão do LocalStorage
    private async Task LimparSessaoAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("userName");
        await _localStorage.RemoveItemAsync("userType");
        await _localStorage.RemoveItemAsync("userFotoUrl");
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public void NotifyUserAuthentication(string token)
    {
        // Aplica o header imediatamente após o login, sem esperar
        // a próxima chamada de GetAuthenticationStateAsync
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var authenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(authenticatedUser)));
    }

    public void NotifyUserLogout()
    {
        // Remove o header de autorização ao sair
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(anonymousUser)));
    }

    // ──────────────────────────────────────────────────────────────
    // CORREÇÃO: Mapeamento completo e robusto dos claims do JWT
    //
    // O JwtSecurityTokenHandler do .NET serializa ClaimTypes longos
    // para nomes curtos no JWT:
    //   ClaimTypes.Role           → "role"
    //   ClaimTypes.NameIdentifier → "nameid"
    //   ClaimTypes.Name           → "unique_name"
    //   ClaimTypes.Email          → "email"
    //
    // Este parser reconhece AMBOS os formatos (curto e longo) para
    // garantir compatibilidade com qualquer versão do .NET.
    //
    // Por que isso importa?
    //   - [Authorize(Roles = "Gerente")] verifica ClaimTypes.Role
    //   - user.Identity?.Name usa ClaimTypes.Name
    //   - user.FindFirst(ClaimTypes.NameIdentifier) usa o URI longo
    //   Se não mapearmos corretamente, essas verificações falham.
    // ──────────────────────────────────────────────────────────────
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs == null) return claims;

        // ── ROLE: "role" ou o URI longo → ClaimTypes.Role ────────
        // O Blazor usa ClaimTypes.Role para [Authorize(Roles = "X")]
        ExtractAndMapClaim(keyValuePairs, claims, "role", ClaimTypes.Role);
        // Fallback: se o JWT usar o URI longo diretamente
        ExtractAndMapClaim(keyValuePairs, claims, ClaimTypes.Role, ClaimTypes.Role);

        // ── NAMEIDENTIFIER: "nameid" / "sub" → ClaimTypes.NameIdentifier ──
        // O servidor usa FindFirst(ClaimTypes.NameIdentifier) para obter o ID do usuário
        ExtractAndMapClaim(keyValuePairs, claims, "nameid", ClaimTypes.NameIdentifier);
        ExtractAndMapClaim(keyValuePairs, claims, "sub", ClaimTypes.NameIdentifier);
        // Fallback URI longo
        ExtractAndMapClaim(keyValuePairs, claims, ClaimTypes.NameIdentifier, ClaimTypes.NameIdentifier);

        // ── NAME: "unique_name" / "name" → ClaimTypes.Name ───────
        // Usado por user.Identity?.Name na AppBar e telas de saudação
        ExtractAndMapClaim(keyValuePairs, claims, "unique_name", ClaimTypes.Name);
        ExtractAndMapClaim(keyValuePairs, claims, "name", ClaimTypes.Name);
        // Fallback URI longo
        ExtractAndMapClaim(keyValuePairs, claims, ClaimTypes.Name, ClaimTypes.Name);

        // ── EMAIL: "email" → ClaimTypes.Email ─────────────────────
        ExtractAndMapClaim(keyValuePairs, claims, "email", ClaimTypes.Email);
        ExtractAndMapClaim(keyValuePairs, claims, ClaimTypes.Email, ClaimTypes.Email);

        // ── Demais claims (exp, nbf, iss, aud, etc.) ─────────────
        // Adicionados com seus tipos originais para inspeção caso necessário
        foreach (var kvp in keyValuePairs)
        {
            if (kvp.Value != null)
                claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
        }

        return claims;
    }

    // Extrai um claim do dicionário, mapeia para o tipo correto do .NET
    // e REMOVE do dicionário para que não seja adicionado duplicado no loop final
    private void ExtractAndMapClaim(
        Dictionary<string, object> dict,
        List<Claim> claims,
        string jsonKey,
        string claimType)
    {
        if (!dict.TryGetValue(jsonKey, out var value) || value == null)
            return;

        var strValue = value.ToString()!.Trim();

        // Suporta arrays de roles (ex: usuário com múltiplos papéis)
        if (strValue.StartsWith("["))
        {
            var parsedValues = JsonSerializer.Deserialize<string[]>(strValue);
            if (parsedValues != null)
            {
                foreach (var val in parsedValues)
                    claims.Add(new Claim(claimType, val));
            }
        }
        else
        {
            claims.Add(new Claim(claimType, strValue));
        }

        // Remove do dict para não duplicar no loop final
        dict.Remove(jsonKey);
    }

    // Decodifica Base64Url (sem padding) para bytes
    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        // Substitui caracteres Base64Url para Base64 padrão
        base64 = base64.Replace('-', '+').Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
