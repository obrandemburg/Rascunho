using HashidsNet;
using Rascunho.DTOs;
using Rascunho.Services;

namespace Rascunho.Endpoints
{
    public static class UsuarioEndpoints
    {
        public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/usuarios");

            group.MapPost("/cadastrar", async (CriarUsuarioRequest request, UsuarioService usuarioService, IHashids hashids) =>
            {
                var usuario = await usuarioService.CriarUsuarioAsync(request);

                var response = ObterUsuarioResponse.DeEntidade(usuario, hashids);

                return Results.Created($"/api/usuarios/{response.IdHash}", response);
            });

            group.MapPost("/cadastrar/lista", async (List<CriarUsuarioRequest> listaDeUsuarios, UsuarioService usuarioService) =>
            {

                if (listaDeUsuarios == null || listaDeUsuarios.Count == 0)
                {
                    return Results.BadRequest(new { Mensagem = "A lista de usuários não pode estar vazia." });
                }

                await usuarioService.InserirUsuariosEmMassaAsync(listaDeUsuarios);

                return Results.Ok(new
                {
                    Mensagem = "Importação em massa concluída com sucesso.",
                    QuantidadeInserida = listaDeUsuarios.Count
                });
            });

            group.MapGet("/listar", async (UsuarioService usuarioService) =>
            {
                var response = await usuarioService.ListarTodosUsuariosAsync();

                return Results.Ok(response);
            });

            group.MapGet("/listar/ativos", async (UsuarioService usuarioService) =>
            {
                var response = await usuarioService.ListarUsuariosAtivosAsync();
                return Results.Ok(response);
            });

            group.MapGet("/listar/desativados", async (UsuarioService usuarioService) =>
            {
                var response = await usuarioService.ListarUsuariosDesativadosAsync();
                return Results.Ok(response);
            });

            // OBTER POR ID
            group.MapGet("/obter/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
            {
                var decodedIds = hashids.Decode(idHash);
                if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID fornecido é inválido ou malformado." });

                var response = await usuarioService.ObterUsuarioPorIdAsync(decodedIds[0]);
                return Results.Ok(response);
            });

            // ATUALIZAR PERFIL
            group.MapPut("/atualizar/{idHash}", async (string idHash, EditarPerfilRequest request, UsuarioService usuarioService, IHashids hashids) =>
            {
                var decodedIds = hashids.Decode(idHash);
                if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

                await usuarioService.AtualizarPerfilAsync(decodedIds[0], request);
                return Results.NoContent();
            });

            // ALTERAR STATUS
            group.MapPut("/status/{idHash}", async (string idHash, bool status, UsuarioService usuarioService, IHashids hashids) =>
            {
                var decodedIds = hashids.Decode(idHash);
                if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

                await usuarioService.AlterarStatusAsync(decodedIds[0], status);
                return Results.NoContent();
            });

            // EXCLUIR USUÁRIO
            group.MapDelete("/deletar/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
            {
                var decodedIds = hashids.Decode(idHash);
                if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

                await usuarioService.ExcluirUsuarioAsync(decodedIds[0]);
                return Results.NoContent();
            });

            

        }
    }
}
