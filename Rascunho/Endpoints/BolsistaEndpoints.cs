using HashidsNet;
using Microsoft.AspNetCore.Mvc;
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

        // GET /api/bolsistas/turmas-recomendadas
        //
        // BUG-007: Aceita query parameter ?diaDaSemana=N (0=Dom .. 6=Sáb).
        // Padrão: dia da semana atual (sem parâmetro = hoje).
        // Retorna as turmas mais desbalanceadas do dia filtrado,
        // ordenadas do maior desequilíbrio ao menor.
        group.MapGet("/turmas-recomendadas", async (
            [FromQuery] int? diaDaSemana,
            BolsistaService bolsistaService,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();

            DayOfWeek? dia = diaDaSemana.HasValue && diaDaSemana.Value >= 0 && diaDaSemana.Value <= 6
                ? (DayOfWeek)diaDaSemana.Value
                : null;

            var response = await bolsistaService.TurmasRecomendadasParaBolsistaAsync(bolsistaId, dia);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"));

        // ══════════════════════════════════════════════════════════
        // GET /api/bolsistas/meu-desempenho
        //
        // Query parameter "periodo":
        //   - "30dias" (padrão) — últimos 30 dias
        //   - "mes"             — mês corrente
        //   - "semana"          — semana atual
        //   - "2meses"          — últimos 2 meses
        //   - "3meses"          — últimos 3 meses
        //   - ...até "12meses"
        //   - "tudo"            — todo o histórico
        // ══════════════════════════════════════════════════════════
        group.MapGet("/meu-desempenho", async (
            [FromQuery] string periodo = "30dias",
            BolsistaService? bolsistaService = null,
            ClaimsPrincipal? user = null) =>
        {
            if (bolsistaService is null || user is null) return Results.BadRequest();
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();

            // Valores aceitos para evitar injeção de strings inesperadas
            var periodoNormalizado = periodo switch
            {
                "mes"    => "mes",
                "semana" => "semana",
                "tudo"   => "tudo",
                _ when periodo.EndsWith("meses") &&
                       int.TryParse(periodo.Replace("meses", ""), out int n) &&
                       n >= 1 && n <= 12 => periodo,
                _        => "30dias"  // padrão seguro
            };

            var response = await bolsistaService.MeuDesempenhoAsync(bolsistaId, periodoNormalizado);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"));

        // GET /api/bolsistas/minhas-habilidades
        // Implementação da tarefa faltante: listar habilidades cadastradas pelo bolsista.
        group.MapGet("/minhas-habilidades", async (BolsistaService bolsistaService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();
            var habilidades = await bolsistaService.ListarMinhasHabilidadesAsync(bolsistaId);
            return Results.Ok(habilidades);
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

        // DELETE /api/bolsistas/minhas-habilidades/{ritmoIdHash}
        group.MapDelete("/minhas-habilidades/{ritmoIdHash}", async (string ritmoIdHash, BolsistaService bolsistaService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();
            await bolsistaService.RemoverHabilidadeAsync(bolsistaId, ritmoIdHash);
            return Results.Ok(new { Mensagem = "Habilidade removida com sucesso!" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"));

        // GET /api/bolsistas/meu-relatorio-horas
        // Endpoint para o próprio bolsista consultar seu relatório via JWT (sem precisar do hash).
        // NOVO: simplifica o fluxo do frontend que antes precisava buscar o hash do usuário.
        group.MapGet("/meu-relatorio-horas", async (
            BolsistaService bolsistaService,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int bolsistaId)) return Results.Unauthorized();
            var response = await bolsistaService.RelatorioHorasSemanaisAsync(bolsistaId);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Bolsista"));

        // GET /api/bolsistas/{idHash}/relatorio-horas (mantido para compatibilidade — Gerente/Recepção)
        group.MapGet("/{bolsistaIdHash}/relatorio-horas", async (string bolsistaIdHash, BolsistaService bolsistaService, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(bolsistaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int usuarioLogadoId))
                return Results.Unauthorized();
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