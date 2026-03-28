using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Services;
using Rascunho.Shared.DTOs;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class ProfessorEndpoints
{
    public static void MapProfessorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/professores").RequireAuthorization();

        // ══════════════════════════════════════════════════════════
        // 1. PROFESSOR: Ver sua própria disponibilidade
        //    GET /api/professores/minha-disponibilidade
        // ══════════════════════════════════════════════════════════
        group.MapGet("/minha-disponibilidade", async (
            ProfessorDisponibilidadeService service,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int professorId)) return Results.Unauthorized();

            var response = await service.ObterMinhaDisponibilidadeAsync(professorId);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Professor"));

        // ══════════════════════════════════════════════════════════
        // 2. PROFESSOR: Atualizar sua disponibilidade (replace all)
        //    PUT /api/professores/minha-disponibilidade
        // ══════════════════════════════════════════════════════════
        group.MapPut("/minha-disponibilidade", async (
            AtualizarDisponibilidadeRequest request,
            ProfessorDisponibilidadeService service,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int professorId)) return Results.Unauthorized();

            await service.AtualizarDisponibilidadeAsync(professorId, request);
            return Results.Ok(new { Mensagem = "Disponibilidade atualizada com sucesso!" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Professor"));

        // ══════════════════════════════════════════════════════════
        // 3. ALUNO/BOLSISTA: Ver disponibilidade de um professor específico
        //    GET /api/professores/{idHash}/disponibilidade
        //    Retorna apenas slots sem conflito com turmas (RN-AP01)
        // ══════════════════════════════════════════════════════════
        group.MapGet("/{professorIdHash}/disponibilidade", async (
            string professorIdHash,
            ProfessorDisponibilidadeService service,
            IHashids hashids) =>
        {
            var decoded = hashids.Decode(professorIdHash);
            if (decoded.Length == 0)
                return Results.BadRequest(new { erro = "ID de professor inválido." });

            var response = await service.ObterDisponibilidadePorProfessorAsync(decoded[0]);
            return Results.Ok(response);
        });

        // FIX (28/03/2026): endpoint GET /api/turmas/{turmaIdHash}/alunos removido daqui.
        // Existia duplicado em TurmaEndpoints.cs (registrado antes, linha 174 do Program.cs),
        // causando conflito de rota e retorno vazio ao tentar listar alunos da turma.
        // A implementação correta permanece em TurmaEndpoints.cs via TurmaService.ListarAlunosDaTurmaAsync.
    }
}