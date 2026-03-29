using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
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
        // 4. ALUNO/BOLSISTA: Ritmos que um professor ensina
        //    GET /api/professores/{professorIdHash}/ritmos
        //
        //    Retorna os ritmos distintos das turmas ATIVAS associadas
        //    ao professor, mais os ritmos de suas habilidades cadastradas.
        //    Usado pelos filtros de aulas particulares: professor deve ser
        //    selecionado antes dos ritmos disponíveis serem exibidos.
        // ══════════════════════════════════════════════════════════
        group.MapGet("/{professorIdHash}/ritmos", async (
            string professorIdHash,
            AppDbContext db,
            IHashids hashids) =>
        {
            var decoded = hashids.Decode(professorIdHash);
            if (decoded.Length == 0)
                return Results.BadRequest(new { erro = "ID de professor inválido." });

            int professorId = decoded[0];

            // Ritmos das turmas ativas em que o professor está vinculado
            var ritmosTurmas = await db.TurmaProfessores
                .Include(tp => tp.Turma).ThenInclude(t => t.Ritmo)
                .Where(tp => tp.ProfessorId == professorId && tp.Turma.Ativa && tp.Turma.Ritmo != null)
                .Select(tp => tp.Turma.Ritmo!)
                .Distinct()
                .ToListAsync();

            // Ritmos das habilidades cadastradas (HabilidadeUsuario) — caso o professor as possua
            var ritmosHabilidades = await db.Set<Rascunho.Entities.HabilidadeUsuario>()
                .Include(h => h.Ritmo)
                .Where(h => h.UsuarioId == professorId && h.Ritmo != null && h.Ritmo.Ativo)
                .Select(h => h.Ritmo!)
                .Distinct()
                .ToListAsync();

            // União sem duplicatas
            var todosRitmos = ritmosTurmas
                .Union(ritmosHabilidades)
                .Where(r => r.Ativo)
                .OrderBy(r => r.Nome)
                .Select(r => new ObterRitmoResponse(
                    hashids.Encode(r.Id),
                    r.Nome,
                    r.Descricao,
                    r.Modalidade,
                    r.Ativo))
                .ToList();

            return Results.Ok(todosRitmos);
        });

        // FIX (28/03/2026): endpoint GET /api/turmas/{turmaIdHash}/alunos removido daqui.
        // Existia duplicado em TurmaEndpoints.cs (registrado antes, linha 174 do Program.cs),
        // causando conflito de rota e retorno vazio ao tentar listar alunos da turma.
        // A implementação correta permanece em TurmaEndpoints.cs via TurmaService.ListarAlunosDaTurmaAsync.
    }
}