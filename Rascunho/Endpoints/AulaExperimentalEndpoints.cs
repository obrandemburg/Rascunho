using HashidsNet;
using Rascunho.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class AulaExperimentalEndpoints
{
    public static void MapAulaExperimentalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/experimentais").RequireAuthorization();

        // 1. ALUNO: Solicitar Aula
        group.MapPost("/solicitar", async (SolicitarAulaExperimentalRequest request, AulaExperimentalService service, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoId)) return Results.Unauthorized();

            var response = await service.SolicitarAulaAsync(alunoId, request);
            return Results.Created($"/api/experimentais/{response.IdHash}", response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Aluno"))
        .AddEndpointFilter<ValidationFilter<SolicitarAulaExperimentalRequest>>();

        // 2. ADMIN/RECEPÇÃO: Alterar Status (Ex: Aluno chegou na escola, marca como "Realizada")
        group.MapPut("/admin/{idHash}/status", async (string idHash, AlterarStatusExperimentalRequest request, AulaExperimentalService service, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await service.AlterarStatusAsync(decodedIds[0], request.NovoStatus);
            return Results.Ok(new { Mensagem = $"Status alterado para {request.NovoStatus}." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));
    }
}