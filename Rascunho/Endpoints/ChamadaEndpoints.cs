using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using Rascunho.DTOs;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class ChamadaEndpoints
{
    public static void MapChamadaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/turmas/{turmaIdHash}/chamada")
            .RequireAuthorization(policy => policy.RequireRole("Professor", "Assistente", "Recepção", "Gerente"));

        // 1. LER: O Professor entra na aplicação e pede a lista de alunos do dia X
        group.MapGet("/", async (
            string turmaIdHash,
            [FromQuery] DateOnly dataAula,
            ChamadaService chamadaService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID da turma inválido." });

            var response = await chamadaService.ObterListaParaChamadaAsync(decodedIds[0], dataAula);
            return Results.Ok(response);
        });

        // 2. GRAVAR: O Professor aperta o botão "Salvar Chamada"
        group.MapPost("/", async (
            string turmaIdHash,
            RegistrarChamadaRequest request,
            ChamadaService chamadaService,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID da turma inválido." });

            // Identidade e Nível de Acesso para a Validação Contextual
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            await chamadaService.RegistrarChamadaAsync(decodedIds[0], usuarioLogadoId, roleClaim, request);
            return Results.Ok(new { Mensagem = "Chamada registrada com sucesso!" });
        });
    }
}