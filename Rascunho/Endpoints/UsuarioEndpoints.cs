using Rascunho.DTOs;
using Rascunho.Services;

namespace Rascunho.Endpoints
{
    public static class UsuarioEndpoints
    {
        public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/usuarios");

            //Apenas para testes, cadastra usuários no sistema sem necessidade de login
            group.MapPost("/cadastrar", async (CriarUsuarioRequest request, UsuarioService usuarioService) =>
            {
                var usuario = await usuarioService.CriarUsuarioAsync(request);
                return Results.Created($"/api/usuarios/{usuario.Id}", new ObterUsuarioResponse
                    (usuario.Email, usuario.Nome, usuario.NomeSocial, usuario.Biografia, usuario.FotoUrl, usuario.Tipo, usuario.Ativo));
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

            group.MapGet("/{id:guid}", async (Guid id, UsuarioService usuarioService) =>
            {
                var response = await usuarioService.ObterUsuarioPorIdAsync(id);
                return Results.Ok(response);
            });

            group.MapPut("/atualizar/{id:guid}", async (Guid id, EditarPerfilRequest request, UsuarioService usuarioService) =>
            {
                await usuarioService.AtualizarPerfilAsync(id, request);

                return Results.NoContent();
            });



        }
    }
}
