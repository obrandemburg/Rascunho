using FluentValidation;
using HashidsNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rascunho.Data;
using Rascunho.Endpoints;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Text;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CONFIGURAÇÃO DA AUTENTICAÇÃO E AUTORIZAÇÃO
var jwtKey = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.ASCII.GetBytes(jwtKey!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization(); // Ativa as Roles e Policies

// Serviços
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RitmoService>();
builder.Services.AddScoped<SalaService>();
builder.Services.AddScoped<TurmaService>();

// Tratamento global de exceções
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Chave de encriptação de Id
builder.Services.AddSingleton<IHashids>(new Hashids("PontoDaDanca_Chave_Super_Secreta_2026", minHashLength: 8));

// Validações
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// APLICAÇÃO AUTOMÁTICA DE MIGRATIONS (Ideal para o ambiente de testes na VPS)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Isso garante que o banco seja criado e atualizado assim que a API subir
    db.Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Interface moderna do Scalar
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Ponto da Dança API")
               .WithTheme(ScalarTheme.DeepSpace)
               .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios);
    });
}
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUsuarioEndpoints();
app.MapSalaEndpoints();
app.MapRitmoEndpoints();
app.MapTurmaEndpoints();

app.Run();