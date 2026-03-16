using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using Rascunho.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class EventoEndpoints
{
    public static void MapEventoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/eventos").RequireAuthorization();

        // ==========================================
        // ÁREA PÚBLICA / ALUNOS (VITRINE)
        // ==========================================

        // 1. LISTAR EVENTOS FUTUROS (A Vitrine Principal)
        group.MapGet("/futuros", async (EventoService eventoService) =>
        {
            var response = await eventoService.ListarEventosAsync(apenasFuturos: true);
            return Results.Ok(response);
        });

        // 2. LISTAR HISTÓRICO DE EVENTOS
        group.MapGet("/historico", async (EventoService eventoService) =>
        {
            var response = await eventoService.ListarEventosAsync(apenasFuturos: false);
            return Results.Ok(response);
        });

        // 3. COMPRAR INGRESSO (Ação do Aluno/Bolsista)
        group.MapPost("/{eventoIdHash}/comprar", async (string eventoIdHash, EventoService eventoService, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(eventoIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID do evento inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            var response = await eventoService.ComprarIngressoAsync(decodedIds[0], usuarioLogadoId, roleClaim);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"));

        // ==========================================
        // ÁREA ADMINISTRATIVA (RECEPÇÃO E GERÊNCIA)
        // ==========================================

        var adminGroup = group.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 4. CRIAR EVENTO
        adminGroup.MapPost("/criar", async (CriarEventoRequest request, EventoService eventoService) =>
        {
            var response = await eventoService.CriarEventoAsync(request);
            return Results.Created($"/api/eventos/{response.IdHash}", response);
        })
        .AddEndpointFilter<ValidationFilter<CriarEventoRequest>>();
    }
}