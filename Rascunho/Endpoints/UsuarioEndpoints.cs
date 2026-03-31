using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using Rascunho.Exceptions;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using Rascunho.Shared.DTOs;
using System.Security.Claims;

namespace Rascunho.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios");

        // 1. CADASTRAR usuário único
        group.MapPost("/cadastrar", async (CriarUsuarioRequest request, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.CriarUsuarioAsync(request);
            return Results.Created($"/api/usuarios/{response.IdHash}", response);
        })
        .AddEndpointFilter<ValidationFilter<CriarUsuarioRequest>>()
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 2. CADASTRO EM MASSA
        group.MapPost("/cadastrar/lista", async (
            List<CriarUsuarioRequest> listaDeUsuarios,
            UsuarioService usuarioService) =>
        {
            await usuarioService.InserirUsuariosEmMassaAsync(listaDeUsuarios);
            return Results.Ok(new
            {
                Mensagem = "Importação em massa concluída com sucesso.",
                QuantidadeInserida = listaDeUsuarios.Count
            });
        })
        .AddEndpointFilter<ValidationFilter<List<CriarUsuarioRequest>>>()
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 3. LISTAR TODOS (com Quantidade + Usuarios — admin)
        group.MapGet("/listar", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarTodosUsuariosAsync();
            return Results.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // LISTAR PAGINADO
        group.MapGet("/listar-paginado", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? nome,
            [FromQuery] string? tipo,
            [FromQuery] string? status,
            UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPaginadoAsync(page, pageSize, nome, tipo, status);
            return Results.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 4. LISTAR ATIVOS
        group.MapGet("/listar/ativos", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosAtivosAsync();
            return Results.Ok(response);
        }).RequireAuthorization();

        // 5. LISTAR DESATIVADOS
        group.MapGet("/listar/desativados", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosDesativadosAsync();
            return Results.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 6. LISTAR POR TIPO (retorna envelope { Quantidade, Usuarios } — rota original)
        group.MapGet("/tipo/{tipo}", async (string tipo, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync(tipo, ativo: null);
            return Results.Ok(response);
        }).RequireAuthorization();

        // 7. LISTAR POR TIPO ATIVOS (envelope — rota original, mantida)
        group.MapGet("/tipo/{tipo}/ativos", async (string tipo, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync(tipo, ativo: true);
            return Results.Ok(response);
        }).RequireAuthorization();

        // ══════════════════════════════════════════════════════════════════
        // NOVO: GET /api/usuarios/tipo/{tipo}/ativos/lista
        //
        // Por que criar uma nova rota e não alterar a existente?
        // A rota /tipo/{tipo}/ativos já existe e retorna { Quantidade, Usuarios }.
        // Alterar ela quebraria qualquer código que já usa o envelope.
        //
        // Esta nova rota /ativos/lista retorna diretamente um array JSON:
        //   [{ idHash, nome, email, tipo, ... }, { ... }]
        //
        // Isso é o que os dropdowns do frontend precisam para funcionar com:
        //   GetFromJsonAsync<List<ItemDto>>("api/usuarios/tipo/Professor/ativos/lista")
        //
        // Atenção: a ordem das rotas importa em Minimal APIs.
        // Esta rota DEVE ser mapeada ANTES de /tipo/{tipo}/ativos para evitar
        // que o segmento "lista" seja interpretado como valor do parâmetro {tipo}.
        // Como estamos usando paths distintos isso não é problema aqui.
        // ══════════════════════════════════════════════════════════════════
        group.MapGet("/tipo/{tipo}/ativos/lista", async (string tipo, UsuarioService usuarioService) =>
        {
            // Chama o novo método que retorna List<> puro (sem envelope)
            var response = await usuarioService.ListarUsuariosPorTipoListaAsync(tipo, ativo: true);
            return Results.Ok(response);
        }).RequireAuthorization();

        // 8. LISTAR POR TIPO DESATIVADOS (envelope — original)
        group.MapGet("/tipo/{tipo}/desativados", async (string tipo, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync(tipo, ativo: false);
            return Results.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 9. OBTER POR ID
        group.MapGet("/obter/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID fornecido é inválido ou malformado." });

            var response = await usuarioService.ObterUsuarioPorIdAsync(decodedIds[0]);
            return Results.Ok(response);
        }).RequireAuthorization();

        // 10. ATUALIZAR MEU PERFIL
        group.MapPut("/meu-perfil/atualizar", async (
            EditarPerfilRequest request,
            UsuarioService usuarioService,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int usuarioLogadoId))
                return Results.Unauthorized();

            await usuarioService.AtualizarPerfilAsync(usuarioLogadoId, request);
            return Results.NoContent();
        })
        .AddEndpointFilter<ValidationFilter<EditarPerfilRequest>>()
        .RequireAuthorization();

        // 11. ALTERAR STATUS (ativar/desativar) — apenas Gerente
        group.MapPut("/status/{idHash}", async (
            string idHash,
            [FromBody] bool status,
            UsuarioService usuarioService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await usuarioService.AlterarStatusAsync(decodedIds[0], status);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Gerente"));

        // 12. EXCLUIR USUÁRIO — apenas Gerente
        group.MapDelete("/deletar/{idHash}", async (
            string idHash,
            UsuarioService usuarioService,
            IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await usuarioService.ExcluirUsuarioAsync(decodedIds[0]);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Gerente"));

        // 13. BUSCAR ALUNOS/BOLSISTAS por nome ou CPF (tela de matrícula)
        group.MapGet("/buscar", async (
            [FromQuery] string? q,
            UsuarioService usuarioService) =>
        {
            var resultados = await usuarioService.BuscarUsuariosAsync(q);
            return Results.Ok(resultados);
        })
        .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 14. ALTERAR SENHA
        group.MapPut("/meu-perfil/alterar-senha", async (
            AlterarSenhaRequest request,
            UsuarioService usuarioService,
            ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int usuarioLogadoId))
                return Results.Unauthorized();

            try
            {
                await usuarioService.AlterarSenhaAsync(usuarioLogadoId, request);
                return Results.NoContent();
            }
            catch (RegraNegocioException ex)
            {
                // Aqui está a mágica! Retornamos um JSON com a propriedade "Erro", 
                // exatamente como o seu ErroGenericoDto espera ler lá no front.
                return Results.UnprocessableEntity(new { Erro = ex.Message });
            }
            catch (Exception)
            {
                // Se der algum erro bizarro de sistema (banco caiu, etc), retorna erro 500 genérico
                return Results.Problem("Erro interno no servidor ao tentar alterar a senha.");
            }
        })
        .AddEndpointFilter<ValidationFilter<AlterarSenhaRequest>>()
        .RequireAuthorization();
    }
}