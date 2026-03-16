using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Shared.DTOs;
using Rascunho.Services;

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
            return Results.Ok(new LoginResponse(token, usuario.Nome, usuario.Tipo));
        });
    }
}