using HashidsNet;
using Microsoft.AspNetCore.Mvc; // Necessário para o [FromQuery]
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;
using static Rascunho.DTOs.ObterTurmaResponse;

namespace Rascunho.Endpoints;

public static class TurmaEndpoints
{
    public static void MapTurmaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/turmas").RequireAuthorization();

        // 1. LISTAR TURMAS (Com filtros opcionais na Query String)
        // Qualquer usuário logado pode ver o quadro de turmas
        group.MapGet("/", async (
            [FromQuery] string? ritmoIdHash,
            [FromQuery] string? professorIdHash,
            [FromQuery] int? diaDaSemana,
            [FromQuery] TimeSpan? horario,
            TurmaService turmaService) =>
        {
            var response = await turmaService.ListarTurmasAsync(ritmoIdHash, professorIdHash, diaDaSemana, horario);
            return Results.Ok(response);
        })
        .RequireAuthorization();

        // 2. CRIAR TURMA (Apenas Recepção e Gerência)
        group.MapPost("/criar", async (CriarTurmaRequest request, TurmaService turmaService, IHashids hashids) =>
        {
            var turma = await turmaService.CriarTurmaAsync(request);
            return Results.Created($"/api/turmas/{hashids.Encode(turma.Id)}", new { Mensagem = "Turma criada com sucesso!" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // ==========================================
        // ÁREA DO ALUNO (Auto-serviço)
        // ==========================================

        // 3. MATRICULAR ALUNO (O próprio aluno faz pelo App)
        group.MapPost("/{turmaIdHash}/matricular", async (string turmaIdHash, MatricularRequest request, TurmaService turmaService, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID da turma inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoLogadoId)) return Results.Unauthorized();

            string mensagem = await turmaService.MatricularAlunoAsync(decodedIds[0], alunoLogadoId, request.Papel);
            return Results.Ok(new { Mensagem = mensagem });
        })
        .RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"))
        .AddEndpointFilter<ValidationFilter<MatricularRequest>>();

        // 4. DESMATRICULAR (Sair da turma ou desistir da fila de espera)
        group.MapDelete("/{turmaIdHash}/desmatricular", async (string turmaIdHash, TurmaService turmaService, IHashids hashids, ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID da turma inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoLogadoId)) return Results.Unauthorized();

            await turmaService.DesmatricularAlunoAsync(decodedIds[0], alunoLogadoId);
            return Results.Ok(new { Mensagem = "Você saiu da turma/fila de espera." });
        });

        // ==========================================
        // ÁREA ADMINISTRATIVA (Recepção e Gerência)
        // ==========================================

        // 5. RECEPÇÃO MATRICULAR UM ALUNO QUALQUER
        group.MapPost("/{turmaIdHash}/admin/matricular", async (string turmaIdHash, MatricularAdminRequest request, TurmaService turmaService, IHashids hashids) =>
        {
            var turmaDecoded = hashids.Decode(turmaIdHash);
            var alunoDecoded = hashids.Decode(request.AlunoIdHash);

            if (turmaDecoded.Length == 0 || alunoDecoded.Length == 0)
                return Results.BadRequest(new { erro = "ID da turma ou do aluno inválido." });

            if (request.Papel != "Condutor" && request.Papel != "Conduzido" && request.Papel != "Ambos")
                return Results.BadRequest(new { erro = "Papel inválido. Escolha 'Condutor', 'Conduzido' ou 'Ambos'." });

            // Reaproveitamos a MESMA lógica do TurmaService, só mudamos a origem do ID do Aluno!
            string mensagem = await turmaService.MatricularAlunoAsync(turmaDecoded[0], alunoDecoded[0], request.Papel);
            return Results.Ok(new { Mensagem = mensagem });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 6. RECEPÇÃO DESMATRICULAR UM ALUNO QUALQUER
        group.MapDelete("/{turmaIdHash}/admin/desmatricular/{alunoIdHash}", async (string turmaIdHash, string alunoIdHash, TurmaService turmaService, IHashids hashids) =>
        {
            var turmaDecoded = hashids.Decode(turmaIdHash);
            var alunoDecoded = hashids.Decode(alunoIdHash);

            if (turmaDecoded.Length == 0 || alunoDecoded.Length == 0)
                return Results.BadRequest(new { erro = "IDs inválidos." });

            await turmaService.DesmatricularAlunoAsync(turmaDecoded[0], alunoDecoded[0]);
            return Results.Ok(new { Mensagem = "Aluno desmatriculado com sucesso pela recepção." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 7. TROCAR SALA (Apenas Staff)
        group.MapPut("/{turmaIdHash}/trocar-sala", async (string turmaIdHash, TrocarSalaRequest request, TurmaService turmaService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            var salaDecoded = hashids.Decode(request.NovaSalaIdHash);

            if (decodedIds.Length == 0 || salaDecoded.Length == 0)
                return Results.BadRequest(new { erro = "IDs inválidos." });

            await turmaService.TrocarSalaAsync(decodedIds[0], salaDecoded[0], request.NovoLimiteAlunos);
            return Results.Ok(new { Mensagem = "Sala trocada com sucesso!" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));
    }
}