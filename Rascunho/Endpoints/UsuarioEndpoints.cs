using HashidsNet;
using Rascunho.Shared.DTOs;
using Rascunho.Infraestrutura;
using Rascunho.Services;
using System.Security.Claims;

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
            }).AddEndpointFilter<ValidationFilter<CriarUsuarioRequest>>();

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

            // OBTER POR ID
            group.MapGet("/obter/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
            {
                var decodedIds = hashids.Decode(idHash);
                if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID fornecido é inválido ou malformado." });

                var response = await usuarioService.ObterUsuarioPorIdAsync(decodedIds[0]);
                return Results.Ok(response);
            }).RequireAuthorization();

            // ATUALIZAR PERFIL
            //group.MapPut("/atualizar/{idHash}", async (string idHash, EditarPerfilRequest request, UsuarioService usuarioService, IHashids hashids) =>
            //{
            //    var decodedIds = hashids.Decode(idHash);
            //    if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

            //    await usuarioService.AtualizarPerfilAsync(decodedIds[0], request);
            //    return Results.NoContent();
            //}).AddEndpointFilter<ValidationFilter<EditarPerfilRequest>>()
            //.RequireAuthorization();

            group.MapPut("/meu-perfil/atualizar", async (EditarPerfilRequest request, UsuarioService usuarioService, ClaimsPrincipal user) =>
            {
                // 1. Extrai o ID diretamente do Token JWT de quem fez a requisição
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 2. Valida se o ID existe e tenta convertê-lo para inteiro
                if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int usuarioLogadoId))
                {
                    return Results.Unauthorized(); // Retorna 401 se o token for inválido ou não tiver o ID
                }

                // 3. Chama o serviço passando o ID seguro extraído do token
                await usuarioService.AtualizarPerfilAsync(usuarioLogadoId, request);

                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<EditarPerfilRequest>>()
            .RequireAuthorization();

            // ALTERAR STATUS
            group.MapPut("/status/{idHash}", async (string idHash, bool status, UsuarioService usuarioService, IHashids hashids) =>
            {
                var decodedIds = hashids.Decode(idHash);
                if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

                await usuarioService.AlterarStatusAsync(decodedIds[0], status);
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Gerente"));

            // EXCLUIR USUÁRIO
            group.MapDelete("/deletar/{idHash}", async (string idHash, UsuarioService usuarioService, IHashids hashids) =>
            {
                var decodedIds = hashids.Decode(idHash);
                if (decodedIds.Length == 0) return Results.BadRequest(new { erro = "ID inválido." });

                await usuarioService.ExcluirUsuarioAsync(decodedIds[0]);
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Gerente"));


        }
    }
}
