using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rascunho.Client;
using MudBlazor.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Rascunho.Client.Security;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// APONTAMENTO FIXO: O Frontend sempre vai buscar dados na API da VPS (Hetzner)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://5.161.202.169:8080/") });

// Adiciona a biblioteca de Design Visual
builder.Services.AddMudServices();

// Adiciona o armazenamento local (para o Token JWT)
builder.Services.AddBlazoredLocalStorage();

// Sistema de Autenticação
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();