using Rascunho.DTOs;
using Rascunho.Entities;
using Rascunho.Data;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;

namespace Rascunho.Services;

public class UsuarioService
{
    private readonly AppDbContext _context;

    public UsuarioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> CriarUsuarioAsync(CriarUsuarioRequest request)
    {
        bool emailJaExiste = await _context.Usuarios.AnyAsync(u => u.Email == request.Email);
        if (emailJaExiste)
        {
            throw new Exception("Este e-mail já está em uso no sistema.");
        }
        string senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);

        Usuario usuario = request.Tipo switch
        {
            "Aluno" => new Aluno(request.Nome, request.Email, senhaHash),
            "Professor" => new Professor(request.Nome, request.Email, senhaHash),
            "Bolsista" => new Bolsista(request.Nome, request.Email, senhaHash),
            "Gerente" => new Gerente(request.Nome, request.Email, senhaHash),
            "Recepção" => new Recepcao(request.Nome, request.Email, senhaHash),
            "Líder" => new Lider(request.Nome, request.Email, senhaHash),
            _ => throw new ArgumentException("Tipo de usuário inválido")
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }
    public async Task InserirUsuariosEmMassaAsync(IEnumerable<CriarUsuarioRequest> requests)
    {
        var usuariosParaInserir = new List<Usuario>();

        foreach (var request in requests)
        {
            string senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);

            Usuario usuario = request.Tipo switch
            {
                "Aluno" => new Aluno(request.Nome, request.Email, senhaHash),
                "Professor" => new Professor(request.Nome, request.Email, senhaHash),
                "Bolsista" => new Bolsista(request.Nome, request.Email, senhaHash),
                "Gerente" => new Gerente(request.Nome, request.Email, senhaHash),
                "Recepção" => new Recepcao(request.Nome, request.Email, senhaHash),
                "Líder" => new Lider(request.Nome, request.Email, senhaHash),
                _ => throw new ArgumentException($"Tipo de usuário inválido: {request.Tipo}")
            };

            usuariosParaInserir.Add(usuario);
        }

        await _context.BulkInsertAsync(usuariosParaInserir);
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarTodosUsuariosAsync()
    {
        var usuarios = await _context.Usuarios
            .Select(usuario => new ObterUsuarioResponse(
                usuario.Email,
                usuario.Nome,
                usuario.NomeSocial,
                usuario.Biografia,
                usuario.FotoUrl,
                usuario.Tipo,
                usuario.Ativo
            ))
            .ToListAsync();

        return usuarios;
    }
    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosAtivosAsync()
    {
        var usuarios = await _context.Usuarios
            .Where(u => u.Ativo)
            .Select(usuario => new ObterUsuarioResponse(
                usuario.Email,
                usuario.Nome,
                usuario.NomeSocial,
                usuario.Biografia,
                usuario.FotoUrl,
                usuario.Tipo,
                usuario.Ativo
            ))
            .ToListAsync();

        return usuarios;
    }
    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosDesativadosAsync()
    {
        var usuarios = await _context.Usuarios
            .Where(u => !u.Ativo)
            .Select(usuario => new ObterUsuarioResponse(
                usuario.Email,
                usuario.Nome,
                usuario.NomeSocial,
                usuario.Biografia,
                usuario.FotoUrl,
                usuario.Tipo,
                usuario.Ativo
            ))
            .ToListAsync();

        return usuarios;
    }
    public async Task<ObterUsuarioResponse> ObterUsuarioPorIdAsync(Guid id)
    {
        var usuario = await _context.Usuarios
            .Where(u => u.Id == id)
            .Select(u => new ObterUsuarioResponse(
                usuario.Email,
                usuario.Nome,
                usuario.NomeSocial,
                usuario.Biografia,
                usuario.FotoUrl,
                usuario.Tipo,
                usuario.Ativo)
            .FirstOrDefaultAsync();

        if (usuario == null)
            throw new Exception("Usuário não encontrado.");

        return usuario;
    }

    public async Task AtualizarPerfilAsync(Guid id, EditarPerfilRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new Exception("Usuário não encontrado.");

        usuario.EditarPerfil(request.FotoUrl, request.NomeSocial, request.Biografia);

        await _context.SaveChangesAsync();
    }
    public async Task ExcluirUsuarioAsync(Guid id)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new Exception("Usuário não encontrado.");

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
    }
    public async Task AlterarStatusAsync(Guid id, bool ativar)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new Exception("Usuário não encontrado.");

        if (ativar)
            usuario.Ativar();
        else
            usuario.Desativar();

        await _context.SaveChangesAsync();
    }
}