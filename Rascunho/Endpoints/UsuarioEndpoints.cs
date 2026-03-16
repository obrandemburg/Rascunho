using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using Rascunho.Shared.DTOs;

namespace Rascunho.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios");

        // 1. CRIAR USUÁRIO
        group.MapPost("/cadastrar", async (CriarUsuarioRequest request, UsuarioService usuarioService) =>
        {
            var response = await usuarioService.CriarUsuarioAsync(request);
            // CORREÇÃO: Removido o ObterUsuarioResponse.DeEntidade
            return Results.Created($"/api/usuarios/{response.IdHash}", response);
        })
        .AddEndpointFilter<ValidationFilter<CriarUsuarioRequest>>();

        // 2. CADASTRO EM MASSA
        group.MapPost("/cadastrar-massa", async (List<CriarUsuarioRequest> requests, UsuarioService usuarioService) =>
        {
            await usuarioService.InserirUsuariosEmMassaAsync(requests);
            return Results.Ok(new { Mensagem = "Usuários processados com sucesso." });
        });

        // 3. LISTAGENS
        group.MapGet("/todos", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarTodosUsuariosAsync();
            return Results.Ok(response);
        });

        group.MapGet("/ativos", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosAtivosAsync();
            return Results.Ok(response);
        });

        group.MapGet("/desativados", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosDesativadosAsync();
            return Results.Ok(response);
        });

        group.MapGet("/alunos", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync("Aluno");
            return Results.Ok(response);
        });

        group.MapGet("/professores", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync("Professor");
            return Results.Ok(response);
        });

        group.MapGet("/bolsistas", async (UsuarioService usuarioService) =>
        {
            var response = await usuarioService.ListarUsuariosPorTipoAsync("Bolsista");
            return Results.Ok(response);
        });

        // 4. OBTER POR ID
        group.MapGet("/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            var response = await usuarioService.ObterUsuarioPorIdAsync(decodedIds[0]);
            return Results.Ok(response);
        });

        // 5. ATUALIZAR PERFIL
        group.MapPut("/{idHash}", async (string idHash, EditarPerfilRequest request, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await usuarioService.AtualizarPerfilAsync(decodedIds[0], request);
            return Results.NoContent();
        })
        .AddEndpointFilter<ValidationFilter<EditarPerfilRequest>>();

        // 6. ALTERAR STATUS (Ativar/Inativar)
        group.MapPut("/{idHash}/status", async (string idHash, [FromBody] bool ativo, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await usuarioService.AlterarStatusAsync(decodedIds[0], ativo);
            return Results.NoContent();
        });

        // 7. EXCLUIR
        group.MapDelete("/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
        {
            var decodedIds = hashids.Decode(idHash);
            if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            await usuarioService.ExcluirUsuarioAsync(decodedIds[0]);
            return Results.NoContent();
        });
    }
}