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

        // ══════════════════════════════════════════════════════════
        // 4. GET /api/turmas/{idHash}/alunos
        //    Lista os alunos matriculados em uma turma (para o modal "Ver Alunos")
        //    Implementado aqui por conveniência, mas logicamente pertence à turma
        // ══════════════════════════════════════════════════════════
        app.MapGet("/api/turmas/{turmaIdHash}/alunos", async (
            string turmaIdHash,
            Rascunho.Data.AppDbContext db,
            IHashids hashids) =>
        {
            var decoded = hashids.Decode(turmaIdHash);
            if (decoded.Length == 0)
                return Results.BadRequest(new { erro = "ID da turma inválido." });

            var matriculas = await db.Matriculas
                .Include(m => m.Aluno)
                .Where(m => m.TurmaId == decoded[0])
                .OrderBy(m => m.Aluno.Nome)
                .Select(m => new AlunoMatriculadoResponse(
                    hashids.Encode(m.AlunoId),
                    m.Aluno.Nome,
                    m.Aluno.FotoUrl,
                    m.Papel
                ))
                .ToListAsync();

            return Results.Ok(matriculas);
        })
        .RequireAuthorization(policy => policy.RequireRole("Professor", "Recepção", "Gerente"));
    }
}