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
builder.Services.AddTransient<HttpInterceptorHandler>();

// 2. Injeta o HttpClient FORÇANDO ele a passar pelo interceptador
builder.Services.AddScoped(sp =>
{
    var interceptor = sp.GetRequiredService<HttpInterceptorHandler>();
    // Garante que o interceptador tenha um motor base para fazer a requisição na web
    interceptor.InnerHandler = new HttpClientHandler();

    return new HttpClient(interceptor)
    {
        BaseAddress = new Uri("http://5.161.202.169:8080/")
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