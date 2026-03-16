using EFCore.BulkExtensions;
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.DTOs;
using Rascunho.Entities;
using Rascunho.Exceptions;
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

    public async Task<Usuario> CriarUsuarioAsync(CriarUsuarioRequest request)
    {
        bool emailJaExiste = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailJaExiste)
        {
            throw new RegraNegocioException("Este e-mail já está em uso no sistema.");
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
            _ => throw new RegraNegocioException("Tipo de usuário inválido")
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }
    public async Task InserirUsuariosEmMassaAsync(IEnumerable<CriarUsuarioRequest> requests)
    {
        var emailsRequisicao = requests.Select(r => r.Email).ToList();

        var emailsDuplicadosNaLista = emailsRequisicao
            .GroupBy(email => email.ToLower())
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToList();

        if (emailsDuplicadosNaLista.Any())
        {
            throw new RegraNegocioException($"A lista enviada contém e-mails duplicados entre si: {string.Join(", ", emailsDuplicadosNaLista)}");
        }

        var emailsJaExistentesNoBanco = await _context.Usuarios
            .Where(u => emailsRequisicao.Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync();

        if (emailsJaExistentesNoBanco.Any())
        {
            throw new RegraNegocioException($"Os seguintes e-mails já estão cadastrados no sistema e não podem ser reinseridos: {string.Join(", ", emailsJaExistentesNoBanco)}");
        }

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
                _ => throw new RegraNegocioException($"Tipo de usuário inválido: {request.Tipo}")
            };

            usuariosParaInserir.Add(usuario);
        }

        await _context.BulkInsertAsync(usuariosParaInserir);
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarTodosUsuariosAsync()
    {
        var usuariosDb = await _context.Usuarios.ToListAsync();

        return usuariosDb.Select(usuario => ObterUsuarioResponse.DeEntidade(usuario, _hashids));
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosAtivosAsync()
    {
        var usuariosDb = await _context.Usuarios
            .Where(u => u.Ativo)
            .ToListAsync();

        return usuariosDb.Select(u => ObterUsuarioResponse.DeEntidade(u, _hashids));
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosDesativadosAsync()
    {
        var usuariosDb = await _context.Usuarios
            .Where(u => !u.Ativo)
            .ToListAsync();

        return usuariosDb.Select(u => ObterUsuarioResponse.DeEntidade(u, _hashids));
    }

    public async Task<ListagemComContagemResponse> ListarUsuariosPorTipoAsync(string tipo, bool? ativo = null)
    {
        // 1. Inicia a query base filtrando pelo tipo (ignorando maiúsculas/minúsculas para evitar erros)
        var query = _context.Usuarios
            .Where(u => u.Tipo.ToLower() == tipo.ToLower());

        // 2. Aplica o filtro de status apenas se ele foi fornecido
        if (ativo.HasValue)
        {
            query = query.Where(u => u.Ativo == ativo.Value);
        }

        // 3. Faz a contagem de forma ultra rápida no banco de dados (gera um SELECT COUNT(*))
        int quantidade = await query.CountAsync();

        // 4. Busca os dados reais no banco
        var usuariosDb = await query.ToListAsync();

        // 5. Converte as entidades para o DTO aplicando o Hashids
        var listaDto = usuariosDb.Select(u => ObterUsuarioResponse.DeEntidade(u, _hashids));

        // 6. Retorna o envelope com a quantidade e a lista
        return new ListagemComContagemResponse(quantidade, listaDto);
    }

    public async Task<ObterUsuarioResponse> ObterUsuarioPorIdAsync(int idReal)
    {
        var usuario = await _context.Usuarios.FindAsync(idReal);
        if (usuario == null || !usuario.Ativo)
            throw new RegraNegocioException("Usuário não encontrado.");

        return ObterUsuarioResponse.DeEntidade(usuario, _hashids);
    }

    public async Task AtualizarPerfilAsync(int id, EditarPerfilRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        usuario.EditarPerfil(request.FotoUrl, request.NomeSocial, request.Biografia);

        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(int id, bool ativar)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        if (ativar)
            usuario.Ativar();
        else
            usuario.Desativar();

        await _context.SaveChangesAsync();
    }

    public async Task ExcluirUsuarioAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
    }
    
}