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

    public async Task InserirUsuariosEmMassaAsync(List<CriarUsuarioRequest> requests)
    {
        var emails = requests.Select(r => r.Email).ToList();
        var existentes = await _context.Usuarios.Where(u => emails.Contains(u.Email)).Select(u => u.Email).ToListAsync();

        var novosUsuarios = new List<Usuario>();
        foreach (var req in requests)
        {
            if (existentes.Contains(req.Email)) continue;
            string senhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha);
            Usuario? u = req.Tipo switch
            {
                "Aluno" => new Aluno(req.Nome, req.Email, senhaHash),
                "Professor" => new Professor(req.Nome, req.Email, senhaHash),
                "Bolsista" => new Bolsista(req.Nome, req.Email, senhaHash),
                "Líder" => new Lider(req.Nome, req.Email, senhaHash),
                "Recepção" => new Recepcao(req.Nome, req.Email, senhaHash),
                "Gerente" => new Gerente(req.Nome, req.Email, senhaHash),
                _ => null
            };
            if (u != null) novosUsuarios.Add(u);
        }
        if (novosUsuarios.Any())
        {
            _context.Usuarios.AddRange(novosUsuarios);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarTodosUsuariosAsync()
    {
        var usuarios = await _context.Usuarios.ToListAsync();
        return usuarios.Select(u => u.ToResponse(_hashids));
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosAtivosAsync()
    {
        var usuarios = await _context.Usuarios.Where(u => u.Ativo).ToListAsync();
        return usuarios.Select(u => u.ToResponse(_hashids));
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosDesativadosAsync()
    {
        var usuarios = await _context.Usuarios.Where(u => !u.Ativo).ToListAsync();
        return usuarios.Select(u => u.ToResponse(_hashids));
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosPorTipoAsync(string tipo)
    {
        var usuarios = await _context.Usuarios.Where(u => u.Tipo == tipo).ToListAsync();
        return usuarios.Select(u => u.ToResponse(_hashids));
    }

    public async Task<ObterUsuarioResponse> ObterUsuarioPorIdAsync(int id)
    {
        var u = await _context.Usuarios.FindAsync(id) ?? throw new RegraNegocioException("Usuário não encontrado.");
        return u.ToResponse(_hashids);
    }

    public async Task AtualizarPerfilAsync(int id, EditarPerfilRequest request)
    {
        var u = await _context.Usuarios.FindAsync(id) ?? throw new RegraNegocioException("Usuário não encontrado.");
        u.EditarPerfil(request.FotoUrl, request.NomeSocial, request.Biografia);
        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(int id, bool ativo)
    {
        var u = await _context.Usuarios.FindAsync(id) ?? throw new RegraNegocioException("Usuário não encontrado.");
        if (ativo) u.Ativar(); else u.Desativar();
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirUsuarioAsync(int id)
    {
        var u = await _context.Usuarios.FindAsync(id) ?? throw new RegraNegocioException("Usuário não encontrado.");
        _context.Usuarios.Remove(u);
        await _context.SaveChangesAsync();
    }
}