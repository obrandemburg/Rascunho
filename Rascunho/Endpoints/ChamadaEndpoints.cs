using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using Rascunho.Shared.DTOs;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class ChamadaEndpoints
{
    public static void MapChamadaEndpoints(this IEndpointRouteBuilder app)
    {
        // CORREÇÃO: removido "Assistente" da lista de roles — tipo inexistente no sistema
        var group = app.MapGroup("/api/turmas/{turmaIdHash}/chamada")
            .RequireAuthorization(policy => policy.RequireRole("Professor", "Recepção", "Gerente"));

        // ══════════════════════════════════════════════════════════
        // 1. LER: Carrega a lista de alunos para a tela de chamada
        //    MODIFICADO: response agora inclui Extras e Observacao
        // ══════════════════════════════════════════════════════════
        group.MapGet("/", async (
            string turmaIdHash,
            [FromQuery] DateOnly dataAula,
            ChamadaService chamadaService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID da turma inválido." });

            var response = await chamadaService.ObterListaParaChamadaAsync(decodedIds[0], dataAula);
            return Results.Ok(response);
        });

        // ══════════════════════════════════════════════════════════
        // 2. GRAVAR: Professor salva a chamada
        //    MODIFICADO: aceita ExtrasPresencas e Observacao por aluno
        // ══════════════════════════════════════════════════════════
        group.MapPost("/", async (
            string turmaIdHash,
            RegistrarChamadaRequest request,
            ChamadaService chamadaService,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID da turma inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            await chamadaService.RegistrarChamadaAsync(decodedIds[0], usuarioLogadoId, roleClaim, request);
            return Results.Ok(new { Mensagem = "Chamada registrada com sucesso!" });
        });

        // ══════════════════════════════════════════════════════════
        // NOVO Sprint 2: 3. BUSCAR PARTICIPANTES EXTRAS (Seção B)
        //
        // GET /api/turmas/{idHash}/chamada/buscar-extras?termo=João
        //
        // Retorna bolsistas e alunos com experimental para esta turma.
        // O professor digita no campo de busca e os resultados aparecem
        // em tempo real para ele adicionar à chamada.
        //
        // ?termo= : obrigatório, mínimo 2 chars (validado no endpoint)
        // ══════════════════════════════════════════════════════════
        group.MapGet("/buscar-extras", async (
            string turmaIdHash,
            [FromQuery] string termo,
            ChamadaService chamadaService,
            IHashids hashids) =>
        {
            if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
                return Results.BadRequest(new { erro = "O termo de busca deve ter ao menos 2 caracteres." });

            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID da turma inválido." });

            var response = await chamadaService.BuscarParticipantesExtrasAsync(decodedIds[0], termo);
            return Results.Ok(response);
        });
    }
}