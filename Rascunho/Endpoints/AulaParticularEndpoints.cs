using HashidsNet;
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class AulaParticularEndpoints
{
    public static void MapAulaParticularEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/aulas-particulares").RequireAuthorization();

        // ══════════════════════════════════════════════════════════════════
        // 1. SOLICITAR AULA PARTICULAR (aluno envia pedido)
        // ══════════════════════════════════════════════════════════════════
        group.MapPost("/solicitar", async (
            SolicitarAulaParticularRequest request,
            AulaParticularService service,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoLogadoId)) return Results.Unauthorized();

            var response = await service.SolicitarAulaAsync(alunoLogadoId, request);
            return Results.Created($"/api/aulas-particulares/{response.IdHash}", response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"))
        .AddEndpointFilter<ValidationFilter<SolicitarAulaParticularRequest>>();

        // ══════════════════════════════════════════════════════════════════
        // 2. RESPONDER SOLICITAÇÃO (professor aceita ou recusa)
        // ══════════════════════════════════════════════════════════════════
        group.MapPut("/{aulaIdHash}/responder", async (
            string aulaIdHash,
            ResponderAulaParticularRequest request,
            AulaParticularService service,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(aulaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int professorLogadoId)) return Results.Unauthorized();

            await service.ResponderSolicitacaoAsync(professorLogadoId, decodedIds[0], request.Aceitar);
            return Results.Ok(new { Mensagem = request.Aceitar ? "Aula aceita com sucesso!" : "Aula recusada." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Professor"));

        // ══════════════════════════════════════════════════════════════════
        // 3. CANCELAR AULA (sujeito às regras de 12h para aluno)
        // ══════════════════════════════════════════════════════════════════
        group.MapDelete("/{aulaIdHash}/cancelar", async (
            string aulaIdHash,
            AulaParticularService service,
            IHashids hashids,
            ClaimsPrincipal user) =>
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

        // ══════════════════════════════════════════════════════════════════
        // 4. LISTAR — rota ORIGINAL /minhas-aulas (mantida para integridade)
        // ══════════════════════════════════════════════════════════════════
        group.MapGet("/minhas-aulas", async (AulaParticularService service, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            var response = await service.ListarMinhasAulasAsync(usuarioLogadoId, roleClaim);
            return Results.Ok(response);
        });

        // ══════════════════════════════════════════════════════════════════
        // NOVO: 5. ALIAS /minhas — corrige divergência com o frontend
        //
        // O problema: AulasParticulares.razor chamava "api/aulas-particulares/minhas"
        // mas o backend só tinha "/minhas-aulas" → erro 404 silencioso em produção.
        //
        // Solução: Ambas as rotas chamam o mesmo ListarMinhasAulasAsync.
        // Mantemos /minhas-aulas para não quebrar integrações futuras
        // e adicionamos /minhas para o frontend atual funcionar.
        // ══════════════════════════════════════════════════════════════════
        group.MapGet("/minhas", async (AulaParticularService service, ClaimsPrincipal user) =>
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