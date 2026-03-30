using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rascunho.Client;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Rascunho.Client.Security;
using Rascunho.Client.Services;
using MudBlazor.Services;
using Rascunho.Client.Infraestrutura;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Registra o Interceptador
// ALTERAÇÃO: mudado para AddScoped para permitir injeção de serviços Scoped
// (ILocalStorageService, AuthenticationStateProvider, NavigationManager)
// necessários para o logout automático em 401.
builder.Services.AddScoped<HttpInterceptorHandler>();

//    ┌─────────────────────────────────────────────────────────────────┐
//    │  Mecanismo de seleção da URL base da API                        │
//    │                                                                 │
//    │  Development (dotnet run / VS local):                           │
//    │    → builder.HostEnvironment.IsDevelopment() == true            │
//    │    → Lê "ApiBaseUrl" de appsettings.Development.json            │
//    │    → Fallback: http://localhost:5132/                           │
//    │                                                                 │
//    │  Production (container publicado):                              │
//    │    → builder.HostEnvironment.IsDevelopment() == false           │
//    │    → Aponta para o próprio domínio seguro carregado pelo Traefik│
//    └─────────────────────────────────────────────────────────────────┘

var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5132/"
    : builder.HostEnvironment.BaseAddress; // Resolve automaticamente para https://pontodadanca...

builder.Services.AddScoped(sp =>
{
    var interceptor = sp.GetRequiredService<HttpInterceptorHandler>();
    interceptor.InnerHandler = new HttpClientHandler();

    return new HttpClient(interceptor)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

// Adicionando LocalStorage e Autorização Core
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

// Injetando nossos serviços customizados
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<AuthService>();

// Devolvendo o registro do MudBlazor para a interface funcionar
builder.Services.AddMudServices();

await builder.Build().RunAsync();