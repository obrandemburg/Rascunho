using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Shared.DTOs;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest request, AppDbContext db, TokenService tokenService, IHashids hashids) =>
        {
            // 1. Busca o usuário pelo e-mail
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            // 2. Se não achar ou se estiver desativado, retorna erro
            if (usuario == null || !usuario.Ativo)
                return Results.BadRequest(new { erro = "E-mail ou senha inválidos." });

            // 3. Valida a senha usando o BCrypt
            bool senhaValida = BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash);
            if (!senhaValida)
                return Results.BadRequest(new { erro = "E-mail ou senha inválidos." });

            // 4. Gera o token
            string token = tokenService.GerarToken(usuario);

            // 5. Retorna os dados + Token
            return Results.Ok(new LoginResponse(token, usuario.Nome, usuario.Tipo, usuario.FotoUrl));
        });

        // ── Logout com invalidação server-side ─────────────────────────────
        // Atualiza UltimoLogoutEmUtc no banco. O middleware de autenticação
        // (OnTokenValidated no Program.cs) rejeita qualquer token cujo "iat"
        // (emitido em) seja anterior ou igual a este valor, invalidando o JWT
        // mesmo antes de ele expirar naturalmente (8 horas).
        group.MapPost("/logout", async (HttpContext httpContext, AppDbContext db) =>
        {
            var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdStr, out int usuarioId))
                return Results.Unauthorized();

            var usuario = await db.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return Results.Unauthorized();

            usuario.RegistrarLogout();
            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization();
    }
}