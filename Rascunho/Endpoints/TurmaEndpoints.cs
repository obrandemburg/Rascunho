using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class TurmaEndpoints
{
    public static void MapTurmaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/turmas").RequireAuthorization();

        // ══════════════════════════════════════════════════════════════════
        // NOVO: GET /api/turmas/listar-ativas
        // Retorna todas as turmas com Ativa = true, sem autenticação.
        // Usado por:
        //   - QuadroTurmas.razor (tela pública para visitantes)
        //   - GerenciarTurmas.razor (admin lista turmas existentes)
        //
        // AllowAnonymous: sobrescreve o RequireAuthorization do grupo.
        // Qualquer pessoa pode ver as turmas disponíveis — é a "vitrine" da escola.
        // ══════════════════════════════════════════════════════════════════
        group.MapGet("/listar-ativas", async (TurmaService turmaService) =>
        {
            // Chama ListarTurmasAsync com apenasAtivas: true
            // Os demais filtros ficam nulos → sem filtro adicional
            var response = await turmaService.ListarTurmasAsync(
                ritmoIdHash: null,
                professorIdHash: null,
                diaDaSemana: null,
                horario: null,
                apenasAtivas: true);

            return Results.Ok(response);
        })
        .AllowAnonymous(); // Visitantes sem login também podem ver as turmas

        // ══════════════════════════════════════════════════════════════════
        // NOVO: GET /api/turmas/minhas-turmas
        // Retorna as turmas do usuário logado com base na sua role:
        //   → Professor:          turmas onde ele está vinculado como professor
        //   → Aluno/Bolsista/Líder: turmas onde ele tem matrícula formal
        //
        // O userId e a role são lidos diretamente das Claims do JWT —
        // não precisamos passar nada no body ou query string.
        // ══════════════════════════════════════════════════════════════════
        group.MapGet("/minhas-turmas", async (TurmaService turmaService, ClaimsPrincipal user) =>
        {
            // Lê o ID do usuário da Claim "NameIdentifier" (gerada pelo TokenService)
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Lê o tipo/role do usuário da Claim "Role"
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(idClaim, out int usuarioId) || string.IsNullOrEmpty(roleClaim))
                return Results.Unauthorized();

            var response = await turmaService.ListarMinhasTurmasAsync(usuarioId, roleClaim);
            return Results.Ok(response);
        });
        // Autenticação herdada do grupo — qualquer usuário logado pode acessar

        // ══════════════════════════════════════════════════════════════════
        // 1. LISTAR TURMAS com filtros opcionais (rota original — mantida)
        // ══════════════════════════════════════════════════════════════════
        group.MapGet("/", async (
            [FromQuery] string? ritmoIdHash,
            [FromQuery] string? professorIdHash,
            [FromQuery] int? diaDaSemana,
            [FromQuery] TimeSpan? horario,
            TurmaService turmaService) =>
        {
            // Sem o parâmetro apenasAtivas → retorna todas (inclusive inativas)
            // Mantém comportamento original para o painel admin que pode precisar ver inativas
            var response = await turmaService.ListarTurmasAsync(ritmoIdHash, professorIdHash, diaDaSemana, horario);
            return Results.Ok(response);
        })
        .RequireAuthorization();

        // ══════════════════════════════════════════════════════════════════
        // 2. CRIAR TURMA (Recepção e Gerente)
        // ══════════════════════════════════════════════════════════════════
        group.MapPost("/criar", async (CriarTurmaRequest? request, TurmaService turmaService) =>
        {
            if (request is null)
                return Results.BadRequest(new { erro = "O corpo da requisição não pode estar vazio." });

            var response = await turmaService.CriarTurmaAsync(request);
            return Results.Created($"/api/turmas/{response.IdHash}", response);
        })
            .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"))
            .AddEndpointFilter<ValidationFilter<CriarTurmaRequest>>();

        // ══════════════════════════════════════════════════════════════════
        // 3. MATRICULAR ALUNO (auto-serviço do aluno)
        // ══════════════════════════════════════════════════════════════════
        group.MapPost("/{turmaIdHash}/matricular", async (
            string turmaIdHash,
            MatricularRequest request,
            TurmaService turmaService,
            IHashids hashids,
            ClaimsPrincipal user) =>
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

        // ══════════════════════════════════════════════════════════════════
        // 4. DESMATRICULAR (auto-serviço)
        // ══════════════════════════════════════════════════════════════════
        group.MapDelete("/{turmaIdHash}/desmatricular", async (
            string turmaIdHash,
            TurmaService turmaService,
            IHashids hashids,
            ClaimsPrincipal user) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID da turma inválido." });

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int alunoLogadoId)) return Results.Unauthorized();

            await turmaService.DesmatricularAlunoAsync(decodedIds[0], alunoLogadoId);
            return Results.Ok(new { Mensagem = "Você saiu da turma/fila de espera." });
        });

        // ══════════════════════════════════════════════════════════════════
        // 5. ADMIN: Recepção matricula qualquer aluno
        // ══════════════════════════════════════════════════════════════════
        group.MapPost("/{turmaIdHash}/admin/matricular", async (
            string turmaIdHash,
            MatricularAdminRequest request,
            TurmaService turmaService,
            IHashids hashids) =>
        {
            var turmaDecoded = hashids.Decode(turmaIdHash);
            var alunoDecoded = hashids.Decode(request.AlunoIdHash);

            if (turmaDecoded.Length == 0 || alunoDecoded.Length == 0)
                return Results.BadRequest(new { erro = "ID da turma ou do aluno inválido." });

            if (request.Papel != "Condutor" && request.Papel != "Conduzido" && request.Papel != "Ambos")
                return Results.BadRequest(new { erro = "Papel inválido. Escolha 'Condutor', 'Conduzido' ou 'Ambos'." });

            string mensagem = await turmaService.MatricularAlunoAsync(turmaDecoded[0], alunoDecoded[0], request.Papel);
            return Results.Ok(new { Mensagem = mensagem });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // ══════════════════════════════════════════════════════════════════
        // 6. ADMIN: Recepção desmatricula qualquer aluno
        // ══════════════════════════════════════════════════════════════════
        group.MapDelete("/{turmaIdHash}/admin/desmatricular/{alunoIdHash}", async (
            string turmaIdHash,
            string alunoIdHash,
            TurmaService turmaService,
            IHashids hashids) =>
        {
            var turmaDecoded = hashids.Decode(turmaIdHash);
            var alunoDecoded = hashids.Decode(alunoIdHash);

            if (turmaDecoded.Length == 0 || alunoDecoded.Length == 0)
                return Results.BadRequest(new { erro = "IDs inválidos." });

            await turmaService.DesmatricularAlunoAsync(turmaDecoded[0], alunoDecoded[0]);
            return Results.Ok(new { Mensagem = "Aluno desmatriculado com sucesso pela recepção." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // ══════════════════════════════════════════════════════════════════
        // 7. TROCAR SALA
        // ══════════════════════════════════════════════════════════════════
        group.MapPut("/{turmaIdHash}/trocar-sala", async (
            string turmaIdHash,
            TrocarSalaRequest request,
            TurmaService turmaService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            var salaDecoded = hashids.Decode(request.NovaSalaIdHash);

            if (decodedIds.Length == 0 || salaDecoded.Length == 0)
                return Results.BadRequest(new { erro = "IDs inválidos." });

            await turmaService.TrocarSalaAsync(decodedIds[0], salaDecoded[0], request.NovoLimiteAlunos);
            return Results.Ok(new { Mensagem = "Sala trocada com sucesso!" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));
        // Localização: Rascunho/Endpoints/TurmaEndpoints.cs
        // ADICIONAR após o endpoint "7. TROCAR SALA"

        // ══════════════════════════════════════════════════════════════════
        // 8. ENCERRAR TURMA (RN-TUR04)
        //
        // PUT /api/turmas/{turmaIdHash}/encerrar
        //
        // Encerra a turma definitivamente.
        // Cancela experimentais pendentes e reposições com ela como destino.
        // Retorna quantos alunos foram afetados (para log e futura notificação push).
        // ══════════════════════════════════════════════════════════════════
        group.MapPut("/{turmaIdHash}/encerrar", async (
            string turmaIdHash,
            TurmaService turmaService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(turmaIdHash);
            if (decodedIds.Length == 0)
                return Results.BadRequest(new { erro = "ID da turma inválido." });

            int totalAlunos = await turmaService.EncerrarTurmaAsync(decodedIds[0]);

            return Results.Ok(new
            {
                Mensagem = $"Turma encerrada com sucesso. {totalAlunos} aluno(s) afetado(s). " +
                           "Notificação push será enviada na Sprint 5.",
                TotalAlunos = totalAlunos
            });
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));
    }
}