using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rascunho.Client; // Ajuste para o namespace do seu projeto
using MudBlazor.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Rascunho.Client.Security;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. PADRÃO: Usando a API que já está em produção na VPS (Hetzner)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://5.161.202.169:8080") });

// 2. PARA TESTES GERAIS: Quando quiser testar com a API rodando no seu próprio Visual Studio
// Descomente a linha abaixo e comente a linha de cima!
// builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5000") });

// 2. Adiciona a biblioteca de Design Visual
builder.Services.AddMudServices();

// 3. Adiciona o armazenamento local (para o Token JWT)
builder.Services.AddBlazoredLocalStorage();

// 4. Sistema de Autenticação (Faremos a classe no próximo passo)
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();