using HashidsNet;
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class AulaParticularEndpoints
{
    public static void MapAulaParticularEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/aulas-particulares").RequireAuthorization();

        // 1. SOLICITAR
        group.MapPost("/solicitar", async (
            SolicitarAulaParticularRequest request,
            AulaParticularService service,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoLogadoId)) return Results.Unauthorized();

            var response = await service.SolicitarAulaAsync(alunoLogadoId, request);
            return Results.Created($"/api/aulas-particulares/{response.IdHash}", response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"))
        .AddEndpointFilter<ValidationFilter<SolicitarAulaParticularRequest>>();

        // 2. RESPONDER
        group.MapPut("/{aulaIdHash}/responder", async (
            string aulaIdHash,
            ResponderAulaParticularRequest request,
            AulaParticularService service,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(aulaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int professorLogadoId)) return Results.Unauthorized();

            await service.ResponderSolicitacaoAsync(professorLogadoId, decodedIds[0], request.Aceitar);
            return Results.Ok(new { Mensagem = request.Aceitar ? "Aula aceita!" : "Aula recusada." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Professor"));

        // 3. CANCELAR
        group.MapDelete("/{aulaIdHash}/cancelar", async (
            string aulaIdHash,
            AulaParticularService service,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(aulaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            await service.CancelarAulaAsync(usuarioLogadoId, roleClaim, decodedIds[0]);
            return Results.Ok(new { Mensagem = "Aula cancelada com sucesso." });
        });

        // 4. LISTAR /minhas-aulas (rota original)
        group.MapGet("/minhas-aulas", async (AulaParticularService service, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            var response = await service.ListarMinhasAulasAsync(usuarioLogadoId, roleClaim);
            return Results.Ok(response);
        });

        // 5. LISTAR /minhas (alias para o frontend)
        group.MapGet("/minhas", async (AulaParticularService service, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (!int.TryParse(idClaim, out int usuarioLogadoId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            var response = await service.ListarMinhasAulasAsync(usuarioLogadoId, roleClaim);
            return Results.Ok(response);
        });

        // ══════════════════════════════════════════════════════════
        // 6. OBTER CONFIGURAÇÕES (NOVO - Correção BUG #1)
        //
        // GET /api/aulas-particulares/configuracoes
        //
        // Retorna as configurações necessárias para o frontend:
        // - Preço padrão de aulas particulares
        // - Janela de reposição em dias
        //
        // IMPORTANTE: Este endpoint foi criado para que alunos/bolsistas
        // possam obter as configurações sem erro 403. A rota anterior
        // (/api/gerente/configuracoes) requer role "Gerente" e bloqueava
        // o acesso. Este novo endpoint permite leitura para todos autenticados.
        // ══════════════════════════════════════════════════════════
        group.MapGet("/configuracoes", (ConfiguracaoService cfg) =>
            Results.Ok(new
            {
                PrecoAulaParticular = cfg.ObterPrecoAulaParticular(),
                JanelaReposicaoDias = cfg.ObterJanelaReposicaoDias()
            }))
        .WithName("ObterConfiguracoesAulasParticulares")
        .Produces<object>(StatusCodes.Status200OK);

        // ══════════════════════════════════════════════════════════
        // 7. REAGENDAR (NOVO Sprint 4)
        //
        // PUT /api/aulas-particulares/{idHash}/reagendar
        //
        // Cancela a aula atual e cria uma nova solicitação com o
        // novo horário. A nova aula volta para "Pendente" — o professor
        // precisa aceitar novamente para confirmar o novo horário.
        //
        // Regras aplicadas:
        //   - Aluno deve ser o dono da aula
        //   - Status deve ser Pendente ou Aceita
        //   - Aulas Aceitas: aplica regra de 12h (RN-AP03)
        //   - Valida choque no novo horário (RN-AP06)
        // ══════════════════════════════════════════════════════════
        group.MapPut("/{aulaIdHash}/reagendar", async (
            string aulaIdHash,
            ReagendarAulaParticularRequest request,
            AulaParticularService service,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(aulaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoLogadoId)) return Results.Unauthorized();

            var response = await service.ReagendarAulaAsync(alunoLogadoId, decodedIds[0], request);

            return Results.Created($"/api/aulas-particulares/{response.IdHash}", response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"));
    }
}