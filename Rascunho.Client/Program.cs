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

// 1. Registra o Interceptador na injeção de dependência
builder.Services.AddTransient<HttpInterceptorHandler>();

// 2. Cria uma fábrica de HttpClient apontando para a sua VPS e "pluga" o interceptador nele
builder.Services.AddHttpClient("ApiBackend", client =>
{
    client.BaseAddress = new Uri("http://5.161.202.169:8080/");
})
.AddHttpMessageHandler<HttpInterceptorHandler>();

// 3. Diz ao Blazor: Toda vez que alguma tela pedir um "@inject HttpClient Http", 
// entregue esse cliente "ApiBackend" que nós acabamos de configurar acima.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiBackend"));

// Adicionando LocalStorage e Autorização Core
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

// Injetando nossos serviços customizados
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<AuthService>();

// Devolvendo o registro do MudBlazor para a interface funcionar
builder.Services.AddMudServices();

await builder.Build().RunAsync();