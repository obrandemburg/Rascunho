// Localização: Rascunho/Program.cs
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("String de conexão não encontrada.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key ausente.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer ausente.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience ausente.");
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ══════════════════════════════════════════════════════════════════
        // CORREÇÃO CRÍTICA: MapInboundClaims e RoleClaimType
        //
        // PROBLEMA: O TokenService gera o JWT com ClaimTypes.Role (URI longo).
        // O JwtSecurityTokenHandler serializa esse claim para o nome curto "role"
        // no payload do JWT. No .NET 8+, o JsonWebTokenHandler (novo padrão)
        // NÃO mapeia automaticamente "role" de volta para ClaimTypes.Role ao
        // ler o token. Isso faz com que:
        //   - RequireRole("Gerente") procura ClaimTypes.Role → não encontra "role"
        //   - Resultado: 403 Forbidden para usuários autenticados com role válido
        //
        // SOLUÇÃO 1: MapInboundClaims = true (restaura comportamento antigo)
        //   → mapeia "role" → ClaimTypes.Role, "unique_name" → ClaimTypes.Name, etc.
        //
        // SOLUÇÃO 2 (mais explícita e robusta): definir RoleClaimType = "role"
        //   → diz ao framework "o claim de role se chama 'role' neste token"
        //
        // Usamos AMBAS as soluções para garantir compatibilidade com .NET 10.
        // ══════════════════════════════════════════════════════════════════
        options.MapInboundClaims = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            // Define explicitamente qual claim carrega o papel (role) do usuário.
            // O TokenService usa ClaimTypes.Role que o JwtSecurityTokenHandler
            // serializa como "role" no JWT. Ao ler, mapeamos "role" de volta.
            // Sem isso, RequireRole() falha com 403 mesmo com token válido.
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,

            // Define qual claim carrega o nome do usuário.
            // ClaimTypes.Name é serializado como "unique_name" no JWT pelo .NET.
            NameClaimType = System.Security.Claims.ClaimTypes.Name,

            // ClockSkew padrão é 5 minutos — mantemos para tolerância de relógio
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

// ── Serviços de domínio ───────────────────────────────────────────
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RitmoService>();
builder.Services.AddScoped<SalaService>();
builder.Services.AddScoped<TurmaService>();
builder.Services.AddScoped<AvisoService>();
builder.Services.AddScoped<ChamadaService>();
builder.Services.AddScoped<AulaParticularService>();
builder.Services.AddScoped<BolsistaService>();
builder.Services.AddScoped<EventoService>();
builder.Services.AddScoped<AulaExperimentalService>();
builder.Services.AddScoped<ProfessorDisponibilidadeService>();
builder.Services.AddScoped<ReposicaoService>();

// ConfiguracaoService é Singleton porque mantém estado em memória
// (alterações feitas pelo Gerente persistem durante a sessão do servidor)
builder.Services.AddSingleton<ConfiguracaoService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var hashidsSalt = builder.Configuration["Hashids:Salt"]
    ?? throw new InvalidOperationException("Hashids:Salt ausente.");
builder.Services.AddSingleton<IHashids>(new Hashids(hashidsSalt, minHashLength: 8));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try { await db.Database.MigrateAsync(); }
    catch (Exception ex) { Console.WriteLine($"Erro migration: {ex.Message}"); }
}

app.UseExceptionHandler();

//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();
app.MapScalarApiReference(options =>
    options.WithTitle("Ponto da Dança API")
           .WithTheme(ScalarTheme.DeepSpace)
           .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios));
//}

app.UseCors("PermitirFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ── Mapeamento de endpoints ───────────────────────────────────────
app.MapAuthEndpoints();
app.MapUsuarioEndpoints();
app.MapSalaEndpoints();
app.MapRitmoEndpoints();
app.MapTurmaEndpoints();
app.MapAvisoEndpoints();
app.MapChamadaEndpoints();
app.MapAulaParticularEndpoints();
app.MapBolsistaEndpoints();
app.MapEventoEndpoints();
app.MapAulaExperimentalEndpoints();
app.MapProfessorEndpoints();
app.MapReposicaoEndpoints();
app.MapGerenteEndpoints();

app.Run();
