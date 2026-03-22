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

        // GET /api/bolsistas/turmas-recomendadas (Sprint 1)
        group.MapGet("/turmas-recomendadas", async (BolsistaService bolsistaService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();
            var response = await bolsistaService.TurmasRecomendadasParaBolsistaAsync(bolsistaId);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"));

        // ══════════════════════════════════════════════════════════
        // NOVO Sprint 2: GET /api/bolsistas/meu-desempenho
        //
        // Retorna a análise completa de frequência do bolsista logado:
        // - Percentual nos dias obrigatórios (indicador principal)
        // - Percentual nos dias extras (informativo)
        // - Indicador de situação ("Excelente" | "Vamos melhorar" | "Atenção" | "Crítico")
        // - Histórico aula-a-aula com data, turma, professor e presença
        //
        // O bolsista vê próprio desempenho. Gerente vê de qualquer bolsista
        // via GET /api/bolsistas/{idHash}/relatorio-horas (método existente).
        // ══════════════════════════════════════════════════════════
        group.MapGet("/meu-desempenho", async (BolsistaService bolsistaService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();

            var response = await bolsistaService.MeuDesempenhoAsync(bolsistaId);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"));

        // POST /api/bolsistas/minhas-habilidades
        group.MapPost("/minhas-habilidades", async (AdicionarHabilidadeRequest request, BolsistaService bolsistaService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();
            await bolsistaService.AdicionarHabilidadeAsync(bolsistaId, request);
            return Results.Ok(new { Mensagem = "Habilidade adicionada com sucesso!" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"))
        .AddEndpointFilter<ValidationFilter<AdicionarHabilidadeRequest>>();

        // GET /api/bolsistas/{idHash}/relatorio-horas
        group.MapGet("/{bolsistaIdHash}/relatorio-horas", async (string bolsistaIdHash, BolsistaService bolsistaService, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(bolsistaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idClaim, out int usuarioLogadoId);
            if (roleClaim != "Gerente" && roleClaim != "Recepção" && usuarioLogadoId != decodedIds[0])
                return Results.Forbid();
            var response = await bolsistaService.RelatorioHorasSemanaisAsync(decodedIds[0]);
            return Results.Ok(response);
        });

        // PUT /api/bolsistas/{idHash}/dias-obrigatorios
        group.MapPut("/{bolsistaIdHash}/dias-obrigatorios", async (string bolsistaIdHash, DefinirDiasObrigatoriosRequest request, BolsistaService bolsistaService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(bolsistaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });
            await bolsistaService.DefinirDiasObrigatoriosAsync(decodedIds[0], request.Dia1, request.Dia2);
            return Results.Ok(new { Mensagem = "Dias obrigatórios atualizados." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Gerente"))
        .AddEndpointFilter<ValidationFilter<DefinirDiasObrigatoriosRequest>>();

        // GET /api/bolsistas/analisar-turma/{idHash} (original, para admin)
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