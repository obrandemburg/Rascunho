using HashidsNet;
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;

namespace Rascunho.Endpoints;

public static class SalaEndpoints
{
    public static void MapSalaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/salas")
            .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // CRIAR
        group.MapPost("/criar", async (CriarSalaRequest request, SalaService salaService, IHashids hashids) =>
        {
            var response = await salaService.CriarSalaAsync(request);
            return Results.Created($"/api/salas/{response.IdHash}", response);
        }).AddEndpointFilter<ValidationFilter<CriarSalaRequest>>();

        // LISTAR TODAS (Ativas e Inativas)
        group.MapGet("/listar", async (SalaService salaService) =>
        {
            var response = await salaService.ListarTodasAsync();
            return Results.Ok(response);
        }).AllowAnonymous(); // Alunos precisam ver as salas para alugar, então liberamos a leitura

        // LISTAR SOMENTE ATIVAS
        group.MapGet("/listar/ativas", async (SalaService salaService) =>
        {
            var response = await salaService.ListarTodasAsync(ativo: true);
            return Results.Ok(response);
        }).AllowAnonymous();

        // ATUALIZAR
        group.MapPut("/atualizar/{idHash}", async (string idHash, AtualizarSalaRequest request, SalaService salaService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await salaService.AtualizarSalaAsync(decodedIds[0], request);
            return Results.NoContent();
        }).AddEndpointFilter<ValidationFilter<AtualizarSalaRequest>>();

        // ALTERAR STATUS
        group.MapPut("/{idHash}/status", async (string idHash, bool status, SalaService salaService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await salaService.AlterarStatusAsync(decodedIds[0], status);
            return Results.NoContent();
        });
    }
}