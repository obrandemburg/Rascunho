using HashidsNet;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using Rascunho.Shared.DTOs;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class ReposicaoEndpoints
{
    public static void MapReposicaoEndpoints(this IEndpointRouteBuilder app)
    {
        // Acesso restrito: apenas Aluno e Bolsista podem agendar reposições
        var group = app.MapGroup("/api/reposicoes")
            .RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista"));

        // ══════════════════════════════════════════════════════════
        // 1. LISTAR FALTAS ELEGÍVEIS (RN-REP01/02)
        //
        // GET /api/reposicoes/elegiveis
        //
        // Retorna todas as faltas dentro da janela configurada (30 dias)
        // com indicação de elegibilidade. O frontend usa esta lista para
        // mostrar ao aluno quais faltas ele pode repor.
        // ══════════════════════════════════════════════════════════
        group.MapGet("/elegiveis", async (
            ReposicaoService reposicaoService,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoId)) return Results.Unauthorized();

            var response = await reposicaoService.ObterFaltasElegiveisAsync(alunoId);
            return Results.Ok(response);
        });

        // ══════════════════════════════════════════════════════════
        // 2. AGENDAR REPOSIÇÃO (RN-REP01/02/03)
        //
        // POST /api/reposicoes/agendar
        //
        // Body: AgendarReposicaoRequest
        // O service valida todas as regras antes de criar o agendamento.
        // ══════════════════════════════════════════════════════════
        group.MapPost("/agendar", async (
            AgendarReposicaoRequest request,
            ReposicaoService reposicaoService,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoId)) return Results.Unauthorized();

            var response = await reposicaoService.AgendarReposicaoAsync(alunoId, request);

            // 201 Created com localização do novo recurso
            return Results.Created($"/api/reposicoes/{response.IdHash}", response);
        });

        // ══════════════════════════════════════════════════════════
        // 3. CANCELAR REPOSIÇÃO (RN-REP04)
        //
        // DELETE /api/reposicoes/{idHash}/cancelar
        //
        // Ao cancelar, a falta volta automaticamente para a lista de elegíveis.
        // Não é um DELETE real — o registro é preservado com Status="Cancelada"
        // para manter o histórico.
        // ══════════════════════════════════════════════════════════
        group.MapDelete("/{idHash}/cancelar", async (
            string idHash,
            ReposicaoService reposicaoService,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID de reposição inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoId)) return Results.Unauthorized();

            await reposicaoService.CancelarReposicaoAsync(alunoId, decodedIds[0]);

            return Results.Ok(new
            {
                Mensagem = "Reposição cancelada. A falta voltou para sua lista de elegíveis."
            });
        });

        // ══════════════════════════════════════════════════════════
        // 4. LISTAR MINHAS REPOSIÇÕES
        //
        // GET /api/reposicoes/minhas
        //
        // Histórico de reposições do aluno (todas, incluindo canceladas/realizadas).
        // ══════════════════════════════════════════════════════════
        group.MapGet("/minhas", async (
            ReposicaoService reposicaoService,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoId)) return Results.Unauthorized();

            var response = await reposicaoService.ListarMinhasReposicoesAsync(alunoId);
            return Results.Ok(response);
        });
    }
}