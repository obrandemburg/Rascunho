using HashidsNet;
using Microsoft.AspNetCore.Mvc;
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

        // 1. CADASTRAR
        group.MapPost("/cadastrar", async (CriarUsuarioRequest request, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.CriarUsuarioAsync(request);
            return Results.Created($"/api/usuarios/{response.IdHash}", response);
        }).AddEndpointFilter<ValidationFilter<CriarUsuarioRequest>>();

        // 2. CADASTRO EM MASSA (Restaurado rota e retorno da main)
        group.MapPost("/cadastrar/lista", async (List<CriarUsuarioRequest> listaDeUsuarios, UsuarioService usuarioService) =>
        {
            await usuarioService.InserirUsuariosEmMassaAsync(listaDeUsuarios);
            return Results.Ok(new
            {
                Mensagem = "Importação em massa concluída com sucesso.",
                QuantidadeInserida = listaDeUsuarios.Count
            });
        }).AddEndpointFilter<ValidationFilter<List<CriarUsuarioRequest>>>()
          .RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 3. LISTAGENS (Restauradas rotas da main)
        group.MapGet("/listar", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarTodosUsuariosAsync();
            return Results.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        group.MapGet("/listar/ativos", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosAtivosAsync();
            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapGet("/listar/desativados", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosDesativadosAsync();
            return Results.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 4. LISTAGENS DINÂMICAS POR TIPO (Restauradas da main)
        group.MapGet("/tipo/{tipo}", async (string tipo, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync(tipo, ativo: null);
            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapGet("/tipo/{tipo}/ativos", async (string tipo, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync(tipo, ativo: true);
            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapGet("/tipo/{tipo}/desativados", async (string tipo, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync(tipo, ativo: false);
            return Results.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"));

        // 5. OBTER POR ID (Restaurada rota da main)
        group.MapGet("/obter/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID fornecido é inválido ou malformado." });

            var response = await usuarioService.ObterUsuarioPorIdAsync(decodedIds[0]);
            return Results.Ok(response);
        }).RequireAuthorization();

        // 6. ATUALIZAR MEU PERFIL
        group.MapPut("/meu-perfil/atualizar", async (EditarPerfilRequest request, UsuarioService usuarioService, ClaimsPrincipal user) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int usuarioLogadoId))
            {
                return Results.Unauthorized();
            }

            await usuarioService.AtualizarPerfilAsync(usuarioLogadoId, request);
            return Results.NoContent();
        })
        .AddEndpointFilter<ValidationFilter<EditarPerfilRequest>>()
        .RequireAuthorization();

        // 7. ALTERAR STATUS (Restaurada rota e Roles da main)
        group.MapPut("/status/{idHash}", async (string idHash, [FromBody] bool status, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await usuarioService.AlterarStatusAsync(decodedIds[0], status);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Gerente"));

        // 8. EXCLUIR USUÁRIO (Restaurada rota e Roles da main)
        group.MapDelete("/deletar/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await usuarioService.ExcluirUsuarioAsync(decodedIds[0]);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Gerente"));
    }
}