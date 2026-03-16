using BCrypt.Net;
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class UsuarioService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public UsuarioService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterUsuarioResponse> CriarUsuarioAsync(CriarUsuarioRequest request)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Email == request.Email))
            throw new RegraNegocioException("Este e-mail já está em uso.");

        string senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);

        Usuario usuario = request.Tipo switch
        {
            "Aluno" => new Aluno(request.Nome, request.Email, senhaHash),
            "Professor" => new Professor(request.Nome, request.Email, senhaHash),
            "Bolsista" => new Bolsista(request.Nome, request.Email, senhaHash),
            "Líder" => new Lider(request.Nome, request.Email, senhaHash),
            "Recepção" => new Recepcao(request.Nome, request.Email, senhaHash),
            "Gerente" => new Gerente(request.Nome, request.Email, senhaHash),
            _ => throw new RegraNegocioException("Tipo de usuário inválido.")
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return usuario.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosAsync()
    {
        var usuarios = await _context.Usuarios.ToListAsync();
        return usuarios.Select(u => u.ToResponse(_hashids));
    }

    public async Task<ObterUsuarioResponse> ObterPerfilAsync(int usuarioId)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        return usuario.ToResponse(_hashids);
    }

    public async Task EditarPerfilAsync(int usuarioId, EditarPerfilRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        // CORREÇÃO: Utilizando a função exata e respeitando a ordem dos parâmetros 
        // public void EditarPerfil(string fotoUrl, string nomeSocial, string biografia)
        usuario.EditarPerfil(request.FotoUrl, request.NomeSocial, request.Biografia);

        await _context.SaveChangesAsync();
    }

    // O método CadastroEmMassaAsync permanece o mesmo que você já tinha implementado
    // ...
}