using HashidsNet;
using Rascunho.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class AvisoEndpoints
{
    public static void MapAvisoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/avisos");

        // ==========================================
        // ÁREA DE LEITURA (CONSUMO)
        // ==========================================

        // 1. QUADRO GERAL: Disponível para TODOS (Até pessoas sem login visitando o site)
        group.MapGet("/geral", async (AvisoService avisoService) =>
        {
            var response = await avisoService.ListarAvisosAtivosAsync("Geral");
            return Results.Ok(response);
        }).AllowAnonymous();

        // 2. QUADRO DA EQUIPE: Restrito aos funcionários e bolsistas (Alunos NÃO acessam)
        group.MapGet("/equipe", async (AvisoService avisoService) =>
        {
            var response = await avisoService.ListarAvisosAtivosAsync("Equipe");
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Professor", "Bolsista", "Líder", "Recepção", "Gerente"));

        // ==========================================
        // ÁREA ADMINISTRATIVA (CRIAÇÃO E EDIÇÃO)
        // ==========================================

        var adminGroup = group.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 3. CRIAR AVISO
        adminGroup.MapPost("/criar", async (CriarAvisoRequest request, AvisoService avisoService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int autorLogadoId)) return Results.Unauthorized();

            var response = await avisoService.CriarAvisoAsync(request, autorLogadoId);
            return Results.Created($"/api/avisos/{response.IdHash}", response);
        })
        .AddEndpointFilter<ValidationFilter<CriarAvisoRequest>>();

        // 4. ATUALIZAR AVISO
        adminGroup.MapPut("/atualizar/aviso/{idHash}", async (string idHash, AtualizarAvisoRequest request, AvisoService avisoService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await avisoService.AtualizarAvisoAsync(decodedIds[0], request);
            return Results.NoContent();
        })
        .AddEndpointFilter<ValidationFilter<AtualizarAvisoRequest>>();

        // 5. DELETAR AVISO (Caso precisem excluir antes da data de expiração)
        adminGroup.MapDelete("/excluir/aviso/{idHash}", async (string idHash, AvisoService avisoService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await avisoService.ExcluirAvisoAsync(decodedIds[0]);
            return Results.NoContent();
        });
    }
}