// Localização: Rascunho/Endpoints/RitmoEndpoints.cs
//
// SPRINT 8: Adicionado DELETE /excluir/{idHash}.

using HashidsNet;
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;

namespace Rascunho.Endpoints;

public static class RitmoEndpoints
{
    public static void MapRitmoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ritmos")
            .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // CRIAR
        group.MapPost("/criar", async (CriarRitmoRequest request, RitmoService ritmoService, IHashids hashids) =>
        {
            var response = await ritmoService.CriarRitmoAsync(request);
            return Results.Created($"/api/ritmos/{response.IdHash}", response);
        }).AddEndpointFilter<ValidationFilter<CriarRitmoRequest>>();

        // LISTAR TODOS
        // AllowAnonymous: alunos e visitantes precisam ver o catálogo de ritmos
        group.MapGet("/listar", async (RitmoService ritmoService) =>
        {
            var response = await ritmoService.ListarTodosAsync();
            return Results.Ok(response);
        }).AllowAnonymous();

        // ATUALIZAR
        group.MapPut("/atualizar/{idHash}", async (
            string idHash,
            AtualizarRitmoRequest request,
            RitmoService ritmoService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID inválido." });

            await ritmoService.AtualizarRitmoAsync(decodedIds[0], request);
            return Results.NoContent();
        }).AddEndpointFilter<ValidationFilter<AtualizarRitmoRequest>>();

        // ALTERAR STATUS (ativar / desativar)
        // Regra: não pode desativar ritmo com turmas ativas (RitmoService valida)
        group.MapPut("/{idHash}/status", async (
            string idHash,
            bool status,
            RitmoService ritmoService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID inválido." });

            await ritmoService.AlterarStatusAsync(decodedIds[0], status);
            return Results.NoContent();
        });

        // ══════════════════════════════════════════════════════════
        // EXCLUIR PERMANENTEMENTE (SPRINT 8)
        //
        // DELETE /api/ritmos/excluir/{idHash}
        //
        // Regras (validadas em RitmoService.ExcluirRitmoAsync):
        //   - Bloqueia se existir qualquer turma associada (ativa ou encerrada)
        //   - Remove HabilidadeUsuario vinculados antes de excluir
        //
        // Por que usar /excluir/{id} em vez de DELETE /{id}?
        //   A rota PUT /{idHash}/status já segmenta por idHash diretamente.
        //   Prefixar "excluir" evita ambiguidade com futuras rotas GET /{id}
        //   e torna a intenção explícita na URL — importante em APIs destrutivas.
        // ══════════════════════════════════════════════════════════
        group.MapDelete("/excluir/{idHash}", async (
            string idHash,
            RitmoService ritmoService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID inválido." });

            await ritmoService.ExcluirRitmoAsync(decodedIds[0]);
            return Results.NoContent();
        });
    }
}
