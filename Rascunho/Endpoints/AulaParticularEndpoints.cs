using HashidsNet;
using Rascunho.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class AulaParticularEndpoints
{
    public static void MapAulaParticularEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/aulas-particulares").RequireAuthorization();

        // 1. SOLICITAR (Aluno pede aula)
        group.MapPost("/solicitar", async (SolicitarAulaParticularRequest request, AulaParticularService service, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoLogadoId)) return Results.Unauthorized();

            var response = await service.SolicitarAulaAsync(alunoLogadoId, request);
            return Results.Created($"/api/aulas-particulares/{response.IdHash}", response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"))
        .AddEndpointFilter<ValidationFilter<SolicitarAulaParticularRequest>>();

        // 2. RESPONDER (Professor aceita ou recusa)
        group.MapPut("/{aulaIdHash}/responder", async (string aulaIdHash, ResponderAulaParticularRequest request, AulaParticularService service, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(aulaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int professorLogadoId)) return Results.Unauthorized();

            await service.ResponderSolicitacaoAsync(professorLogadoId, decodedIds[0], request.Aceitar);
            return Results.Ok(new { Mensagem = request.Aceitar ? "Aula aceita com sucesso!" : "Aula recusada." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Professor"));

        // 3. CANCELAR (Qualquer um pode cancelar, sujeito às regras de 24h)
        group.MapDelete("/{aulaIdHash}/cancelar", async (string aulaIdHash, AulaParticularService service, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(aulaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            await service.CancelarAulaAsync(usuarioLogadoId, roleClaim, decodedIds[0]);
            return Results.Ok(new { Mensagem = "Aula cancelada com sucesso." });
        });

        // 4. LISTAR MINHAS AULAS
        group.MapGet("/minhas-aulas", async (AulaParticularService service, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            var response = await service.ListarMinhasAulasAsync(usuarioLogadoId, roleClaim);
            return Results.Ok(response);
        });
    }
}