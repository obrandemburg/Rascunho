using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rascunho.Client;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Rascunho.Client.Security;
using Rascunho.Client.Services;
using MudBlazor.Services; // 1. Adicionamos a referência do MudBlazor

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Apontamento FIXO para a API em Produção
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://5.161.202.169:8080/") });

// Adicionando LocalStorage e Autorização Core
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

// Injetando nossos serviços customizados
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<AuthService>();

// 2. Devolvendo o registro do MudBlazor para a interface funcionar
builder.Services.AddMudServices();

await builder.Build().RunAsync();