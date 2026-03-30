// Localização: Rascunho/Program.cs
using FluentValidation;
using HashidsNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key ausente.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer ausente.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience ausente.");
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // ── Invalidação server-side de tokens após logout ────────────────
        // Após o usuário fazer logout, UltimoLogoutEmUtc é gravado no banco.
        // Aqui verificamos se o token atual foi emitido ANTES desse horário.
        // Se sim, o token é rejeitado com 401, mesmo que ainda não tenha expirado.
        //
        // A claim "iat" (issued-at) é gerada automaticamente pelo JwtSecurityTokenHandler
        // em GerarToken (TokenService.cs) e representa o momento de emissão do JWT.
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                var userIdStr = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int usuarioId))
                {
                    context.Fail("Token sem identificador de usuário válido.");
                    return;
                }

                var usuario = await db.Usuarios.FindAsync(usuarioId);

                // Usuário não encontrado ou desativado → token inválido
                if (usuario == null || !usuario.Ativo)
                {
                    context.Fail("Usuário não encontrado ou inativo.");
                    return;
                }

                // Verifica se o token foi emitido antes do último logout
                if (usuario.UltimoLogoutEmUtc.HasValue)
                {
                    var iatStr = context.Principal?.FindFirst("iat")?.Value;
                    if (iatStr != null && long.TryParse(iatStr, out long iatSeconds))
                    {
                        var emitidoEm = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;

                        // Token emitido em ou antes do logout → revogado
                        if (emitidoEm <= usuario.UltimoLogoutEmUtc.Value)
                        {
                            context.Fail("Token revogado. Faça login novamente.");
                            return;
                        }
                    }
                }
            }
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
builder.Services.AddSingleton<ConfiguracaoService>();

// ── Feature #3: Lista de Espera ───────────────────────────────────
// INotificacaoService: stub provisório até o Feature #4 (FCM) ser implementado.
// Para ativar FCM: trocar NotificacaoServiceStub por FirebaseNotificacaoService.
builder.Services.AddScoped<INotificacaoService, NotificacaoServiceStub>();
builder.Services.AddScoped<ListaEsperaService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var hashidsSalt = builder.Configuration["Hashids:Salt"]
    ?? throw new InvalidOperationException("Hashids:Salt ausente.");
builder.Services.AddSingleton<IHashids>(new Hashids(hashidsSalt, minHashLength: 8));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// ── Migrations automáticas na inicialização com Retry Policy ────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var maxRetries = 5;
    var currentRetry = 0;
    var delay = TimeSpan.FromSeconds(3);

    while (currentRetry < maxRetries)
    {
        try
        {
            Console.WriteLine($"[MIGRATION] Tentando aplicar migrations (Tentativa {currentRetry + 1}/{maxRetries})...");
            await db.Database.MigrateAsync();
            Console.WriteLine("[MIGRATION] Migrations aplicadas com sucesso!");
            break; // Sai do loop se der certo
        }
        catch (Exception ex)
        {
            currentRetry++;
            Console.WriteLine($"[MIGRATION ERROR] Falha ao conectar/migrar: {ex.Message}");

            if (currentRetry >= maxRetries)
            {
                Console.WriteLine("[MIGRATION FATAL] Limite de tentativas atingido. A aplicação pode ficar instável.");
                // Opcional: throw; se você preferir que a API não suba de jeito nenhum sem o banco
            }
            else
            {
                await Task.Delay(delay); // Aguarda antes da próxima tentativa
            }
        }
    }
}

// ── Garante que a pasta de uploads existe ─────────────────────────
// Em Docker: /app/uploads/fotos/ — mapeada para o volume pd_uploads
// Em desenvolvimento: {raiz_projeto}/uploads/fotos/
var pastaUploads = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "fotos");
Directory.CreateDirectory(pastaUploads);

app.UseExceptionHandler();

// ── Documentação — restrita a desenvolvimento ─────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.WithTitle("Ponto da Dança API")
               .WithTheme(ScalarTheme.DeepSpace)
               .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios));
}

// ── Serve arquivos estáticos da pasta uploads ─────────────────────
// Permite acessar fotos diretamente via URL:
//   http://IP:8080/uploads/fotos/uuid.jpg
//
// PhysicalFileProvider: aponta para /app/uploads/ (fora do wwwroot)
// RequestPath "/uploads": prefixo da URL que mapeia para essa pasta
//
// Por que não usar wwwroot?
//   Arquivos em wwwroot são embutidos na imagem Docker no build.
//   Uploads precisam estar em disco persistente (volume) — fora da imagem.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
    RequestPath = "/uploads"
});

app.UseCors("PermitirFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ── Mapeamento de todos os endpoints ─────────────────────────────
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
app.MapUploadEndpoints();

app.Run();
