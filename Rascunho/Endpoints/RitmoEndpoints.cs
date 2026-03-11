using HashidsNet;
using Rascunho.DTOs;
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
        group.MapGet("/listar", async (RitmoService ritmoService) =>
        {
            var response = await ritmoService.ListarTodosAsync();
            return Results.Ok(response);
        }).AllowAnonymous(); // Liberado para alunos verem o catálogo de ritmos

        // ATUALIZAR
        group.MapPut("/atualizar/{idHash}", async (string idHash, AtualizarRitmoRequest request, RitmoService ritmoService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await ritmoService.AtualizarRitmoAsync(decodedIds[0], request);
            return Results.NoContent();
        }).AddEndpointFilter<ValidationFilter<AtualizarRitmoRequest>>();

        // ALTERAR STATUS (A inativação de um ritmo esconde ele da criação de turmas, mas professores ainda podem usar para aulas particulares)
        group.MapPut("/{idHash}/status", async (string idHash, bool status, RitmoService ritmoService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await ritmoService.AlterarStatusAsync(decodedIds[0], status);
            return Results.NoContent();
        });
    }
}