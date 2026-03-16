using HashidsNet;
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class BolsistaEndpoints
{
    public static void MapBolsistaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolsistas").RequireAuthorization();

        // 1. BOLSISTA: Adicionar suas habilidades (Autoatendimento)
        group.MapPost("/minhas-habilidades", async (AdicionarHabilidadeRequest request, BolsistaService bolsistaService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();

            await bolsistaService.AdicionarHabilidadeAsync(bolsistaId, request);
            return Results.Ok(new { Mensagem = "Habilidade adicionada com sucesso!" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"))
        .AddEndpointFilter<ValidationFilter<AdicionarHabilidadeRequest>>();

        // 2. BOLSISTA / GERENTE: Ver relatório de horas
        group.MapGet("/{bolsistaIdHash}/relatorio-horas", async (string bolsistaIdHash, BolsistaService bolsistaService, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(bolsistaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idClaim, out int usuarioLogadoId);

            // Apenas o próprio bolsista ou um gerente podem ver as horas
            if (roleClaim != "Gerente" && roleClaim != "Recepção" && usuarioLogadoId != decodedIds[0])
                return Results.Forbid();

            var response = await bolsistaService.RelatorioHorasSemanaisAsync(decodedIds[0]);
            return Results.Ok(response);
        });

        // 3. GERENTE: Definir os 2 dias obrigatórios do bolsista
        group.MapPut("/{bolsistaIdHash}/dias-obrigatorios", async (string bolsistaIdHash, DefinirDiasObrigatoriosRequest request, BolsistaService bolsistaService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(bolsistaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await bolsistaService.DefinirDiasObrigatoriosAsync(decodedIds[0], request.Dia1, request.Dia2);
            return Results.Ok(new { Mensagem = "Dias obrigatórios atualizados." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Gerente"))
        .AddEndpointFilter<ValidationFilter<DefinirDiasObrigatoriosRequest>>();

        // 4. AUTOMAÇÃO (A MÁGICA): Analisar e Balancear Turma
        group.MapGet("/analisar-turma/{turmaIdHash}", async (string turmaIdHash, BolsistaService bolsistaService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID da turma inválido." });

            var response = await bolsistaService.AnalisarEBalancearTurmaAsync(decodedIds[0]);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Gerente", "Recepção"));
    }
}