using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Blazored.LocalStorage;
using Rascunho.Client;
using Rascunho.Client.Security;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. INTEGRAÇÃO PRINCIPAL: Apontando para a API de Produção
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://5.161.202.169:8080/")
});

// 2. Registrando o MudBlazor (UI/UX)
builder.Services.AddMudServices();

// 3. REGISTRANDO O LOCAL STORAGE (A SOLUÇÃO DO SEU ERRO ESTÁ AQUI 👇)
builder.Services.AddBlazoredLocalStorage();

// 4. Configuração de Segurança (JWT)
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();