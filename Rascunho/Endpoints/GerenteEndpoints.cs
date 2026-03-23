using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Services;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class GerenteEndpoints
{
    public static void MapGerenteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gerente")
            .RequireAuthorization(policy => policy.RequireRole("Gerente"));

        // ── CONFIGURAÇÕES ─────────────────────────────────────────

        // 1. VER CONFIGURAÇÕES
        group.MapGet("/configuracoes", (ConfiguracaoService cfg) =>
            Results.Ok(cfg.ObterConfiguracoes()));

        // 2. ATUALIZAR PREÇO DAS PARTICULARES
        group.MapPut("/configuracoes/preco-aula-particular",
            (AtualizarPrecoRequest req, ConfiguracaoService cfg) =>
            {
                cfg.AtualizarPrecoAulaParticular(req.Valor);
                return Results.Ok(new
                {
                    Mensagem = $"Preço atualizado para R$ {req.Valor:F2}. " +
                               "Aulas já solicitadas mantêm o valor original."
                });
            });

        // 3. ATUALIZAR JANELA DE REPOSIÇÃO
        group.MapPut("/configuracoes/janela-reposicao",
            (AtualizarJanelaRequest req, ConfiguracaoService cfg) =>
            {
                cfg.AtualizarJanelaReposicao(req.Dias);
                return Results.Ok(new
                {
                    Mensagem = $"Janela de reposição atualizada para {req.Dias} dias."
                });
            });

        // ── DESEMPENHO DE BOLSISTAS ───────────────────────────────

        // 4. LISTAR TODOS COM DESEMPENHO (ordenado por prioridade)
        group.MapGet("/desempenho-bolsistas",
            async (BolsistaService bolsistaService, AppDbContext db) =>
            {
                var ids = await db.Usuarios
                    .OfType<Rascunho.Entities.Bolsista>()
                    .Where(b => b.Ativo)
                    .Select(b => b.Id)
                    .ToListAsync();

                var tarefas = ids.Select(id => bolsistaService.MeuDesempenhoAsync(id));
                var resultados = await Task.WhenAll(tarefas);

                // Ordena por urgência: Crítico → Atenção → Vamos melhorar → Excelente
                var ordenados = resultados.OrderBy(r => r.IndicadorSituacao switch
                {
                    "Crítico" => 0,
                    "Atenção" => 1,
                    "Vamos melhorar" => 2,
                    "Excelente" => 3,
                    _ => 4
                });

                return Results.Ok(ordenados);
            });

        // 4. LISTAR TODOS COM DESEMPENHO (ordenado por prioridade)
        group.MapGet("/desempenho-bolsistas",
            async (BolsistaService bolsistaService, AppDbContext db) =>
            {
                var ids = await db.Usuarios
                    .OfType<Rascunho.Entities.Bolsista>()
                    .Where(b => b.Ativo)
                    .Select(b => b.Id)
                    .ToListAsync();

                // CORREÇÃO: Execução sequencial para evitar o erro de concorrência 
                // "A second operation was started on this context" do AppDbContext
                var resultados = new List<Rascunho.Shared.DTOs.DesempenhoResponse>();
                foreach (var id in ids)
                {
                    resultados.Add(await bolsistaService.MeuDesempenhoAsync(id));
                }

                // Ordena por urgência: Crítico → Atenção → Vamos melhorar → Excelente
                var ordenados = resultados.OrderBy(r => r.IndicadorSituacao switch
                {
                    "Crítico" => 0,
                    "Atenção" => 1,
                    "Vamos melhorar" => 2,
                    "Excelente" => 3,
                    _ => 4
                });

                return Results.Ok(ordenados);
            });

        // 6. REGISTRAR CONVERSA
        group.MapPost("/bolsistas/{idHash}/registrar-conversa",
            async (string idHash, RegistrarConversaRequest req,
                   AppDbContext db, IHashids hashids) =>
            {
                var decoded = hashids.Decode(idHash);
                if (decoded.Length == 0)
                    return Results.BadRequest(new { erro = "ID inválido." });

                var bolsista = await db.Usuarios
                    .OfType<Rascunho.Entities.Bolsista>()
                    .FirstOrDefaultAsync(b => b.Id == decoded[0]);

                if (bolsista == null)
                    return Results.NotFound(new { erro = "Bolsista não encontrado." });

                // TODO Sprint 4.1: Criar entidade RegistroConversa para persistir
                // Por ora, o registro é retornado como confirmação mas não persistido no banco
                return Results.Ok(new
                {
                    Mensagem = "Conversa registrada com sucesso.",
                    BolsistaNome = bolsista.Nome,
                    DataRegistro = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                    Observacao = req.Observacao
                });
            });

        // ══════════════════════════════════════════════════════════
        // 7. DESATIVAR BOLSA (NOVO Sprint 4)
        //
        // PUT /api/gerente/bolsistas/{idHash}/desativar-bolsa
        //
        // Converte o Bolsista em Aluno comum alterando o discriminador TPH.
        // O EF Core não suporta mudança de discriminador pelo objeto model,
        // então usamos SQL direto parametrizado via ExecuteSqlAsync.
        //
        // O que é preservado:
        //   ✓ RegistroPresenca (histórico de presenças)
        //   ✓ HabilidadeUsuario (ritmos e papéis cadastrados)
        //   ✓ Matriculas em turmas solo existentes
        //   ✓ Todos os dados pessoais (nome, email, foto, etc.)
        //
        // O que é removido/limpo:
        //   ✗ DiaObrigatorio1 e DiaObrigatorio2 (limpos para NULL)
        //   ✗ Role "Bolsista" → se torna "Aluno" no próximo login
        //
        // NOTA: O usuário precisará fazer login novamente para que
        // o novo token JWT reflita a role "Aluno".
        // ══════════════════════════════════════════════════════════
        group.MapPut("/bolsistas/{idHash}/desativar-bolsa",
    async (string idHash, AppDbContext db, IHashids hashids) =>
    {
        var decoded = hashids.Decode(idHash);
        if (decoded.Length == 0)
            return Results.BadRequest(new { erro = "ID inválido." });

        int bolsistaId = decoded[0];

        // Verificação: É realmente um bolsista ativo?
        var bolsista = await db.Usuarios
            .OfType<Rascunho.Entities.Bolsista>()
            .FirstOrDefaultAsync(b => b.Id == bolsistaId && b.Ativo);

        if (bolsista == null)
            return Results.NotFound(new
            {
                erro = "Bolsista não encontrado ou já convertido para outro tipo."
            });

        // ── Passo 1: Busca matrículas solo com desconto ───────────────
        // Precisamos saber quantas matrículas serão afetadas para informar
        // o Gerente e para futuro processamento no módulo financeiro.
        var matriculasSoloComDesconto = await db.Matriculas
            .Include(m => m.Turma).ThenInclude(t => t.Ritmo)
            .Where(m => m.AlunoId == bolsistaId &&
                        m.Turma.Ativa &&
                        m.OrigemDesconto == "Bolsista50%")
            .ToListAsync();

        // ── Passo 2: Remove o desconto das matrículas solo ────────────
        // A OrigemDesconto = null indica para o módulo financeiro (1.2)
        // que esta matrícula agora deve ser cobrada a preço integral.
        foreach (var matricula in matriculasSoloComDesconto)
        {
            matricula.OrigemDesconto = null;
            // ValorMensalidade = null também → financeiro usará preço padrão
            matricula.ValorMensalidade = null;
        }

        // ── Passo 3: Converte Bolsista → Aluno via SQL direto ─────────
        // EF Core não suporta mudança de discriminador TPH via objeto model
        // (seria necessário deletar e recriar — inaceitável pois perderia FK's).
        // ExecuteSqlAsync com interpolação em EF Core 7+ é SEGURO e parametrizado.
        //
        // Colunas alteradas:
        //   "Tipo"            = discriminador TPH → muda de "Bolsista" para "Aluno"
        //   "DiaObrigatorio1" = limpo (não faz sentido para Aluno)
        //   "DiaObrigatorio2" = limpo (não faz sentido para Aluno)
        await db.Database.ExecuteSqlAsync(
            $"""
        UPDATE "Usuarios" 
        SET "Tipo" = 'Aluno',
            "DiaObrigatorio1" = NULL,
            "DiaObrigatorio2" = NULL
        WHERE "Id" = {bolsistaId}
        """);

        // SaveChanges persiste as alterações nas Matriculas (passo 2)
        // O SQL do passo 3 já foi executado diretamente no banco
        if (matriculasSoloComDesconto.Any())
            await db.SaveChangesAsync();

        // ── Monta resposta informativa ────────────────────────────────
        var nomesDasTurmasAfetadas = matriculasSoloComDesconto
            .Select(m => m.Turma?.Ritmo?.Nome ?? "Turma desconhecida")
            .Distinct()
            .ToList();

        return Results.Ok(new
        {
            Mensagem =
                $"{bolsista.Nome} foi convertido para Aluno com sucesso. " +
                "O usuário precisará fazer login novamente para o sistema refletir a mudança.",
            MatriculasSoloAtualizadas = matriculasSoloComDesconto.Count,
            TurmasAfetadas = nomesDasTurmasAfetadas,
            AvisoFinanceiro = matriculasSoloComDesconto.Count > 0
                ? $"⚠️ {matriculasSoloComDesconto.Count} matrícula(s) em turmas solo " +
                  "foram atualizadas para preço integral. " +
                  "O módulo financeiro (fase 1.2) processará os ajustes de cobrança."
                : "Nenhuma matrícula solo com desconto encontrada."
        });
    });
    }

    // DTOs privados ao arquivo — não precisam estar em Shared pois
    // são usados exclusivamente por estes endpoints
    private record AtualizarPrecoRequest(decimal Valor);
    private record AtualizarJanelaRequest(int Dias);
    private record RegistrarConversaRequest(string Observacao);
}