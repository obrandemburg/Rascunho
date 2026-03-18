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

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public void NotifyUserAuthentication(string token)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
    }

    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }

    // CORREÇÃO: Tradução inteligente das Claims do JWT para o formato nativo do C#
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            // O .NET envia o nome do usuário e a Role com chaves curtas no JSON.
            // Aqui nós mapeamos essas chaves curtas para os Tipos Nativos do C#.
            ExtractAndMapClaim(keyValuePairs, claims, "role", ClaimTypes.Role);
            ExtractAndMapClaim(keyValuePairs, claims, ClaimTypes.Role, ClaimTypes.Role);

            ExtractAndMapClaim(keyValuePairs, claims, "unique_name", ClaimTypes.Name);
            ExtractAndMapClaim(keyValuePairs, claims, "name", ClaimTypes.Name);

            // Adiciona o restante das claims que vierem no token
            foreach (var kvp in keyValuePairs)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
            }
        }
        return claims;
    }

    private void ExtractAndMapClaim(Dictionary<string, object> dict, List<Claim> claims, string jsonKey, string claimType)
    {
        if (dict.TryGetValue(jsonKey, out var value) && value != null)
        {
            var strValue = value.ToString()!.Trim();
            if (strValue.StartsWith("["))
            {
                var parsedValues = JsonSerializer.Deserialize<string[]>(strValue);
                if (parsedValues != null)
                {
                    foreach (var val in parsedValues) claims.Add(new Claim(claimType, val));
                }
            }
            else
            {
                claims.Add(new Claim(claimType, strValue));
            }
            dict.Remove(jsonKey);
        }
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}