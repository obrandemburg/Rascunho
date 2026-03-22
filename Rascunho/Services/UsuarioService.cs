using BCrypt.Net;
using EFCore.BulkExtensions;
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
        bool emailJaExiste = await _context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (emailJaExiste)
            throw new RegraNegocioException("Este e-mail já está em uso no sistema.");

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

    public async Task InserirUsuariosEmMassaAsync(IEnumerable<CriarUsuarioRequest> requests)
    {
        var emailsRequisicao = requests.Select(r => r.Email).ToList();

        var emailsDuplicadosNaLista = emailsRequisicao
            .GroupBy(email => email.ToLower())
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToList();

        if (emailsDuplicadosNaLista.Any())
            throw new RegraNegocioException($"A lista enviada contém e-mails duplicados entre si: {string.Join(", ", emailsDuplicadosNaLista)}");

        var emailsJaExistentesNoBanco = await _context.Usuarios
            .Where(u => emailsRequisicao.Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync();

        if (emailsJaExistentesNoBanco.Any())
            throw new RegraNegocioException($"Os seguintes e-mails já estão cadastrados e não podem ser reinseridos: {string.Join(", ", emailsJaExistentesNoBanco)}");

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

    // ──────────────────────────────────────────────────────────────────────
    // LISTAR POR TIPO COM ENVELOPE (método original — mantido para compatibilidade)
    // Retorna: { Quantidade: int, Usuarios: List<ObterUsuarioResponse> }
    // Usado por: GET /api/usuarios/tipo/{tipo} e /tipo/{tipo}/ativos
    // ──────────────────────────────────────────────────────────────────────
    public async Task<object> ListarUsuariosPorTipoAsync(string tipo, bool? ativo = null)
    {
        var query = _context.Usuarios.Where(u => u.Tipo.ToLower() == tipo.ToLower());
        if (ativo.HasValue)
            query = query.Where(u => u.Ativo == ativo.Value);

        // Duas queries separadas: Count + ToList
        // Para o MVP com poucos registros, aceitável.
        int quantidade = await query.CountAsync();
        var usuariosDb = await query.ToListAsync();
        var listaDto = usuariosDb.Select(u => u.ToResponse(_hashids));

        return new { Quantidade = quantidade, Usuarios = listaDto };
    }

    // ──────────────────────────────────────────────────────────────────────
    // NOVO: LISTAR POR TIPO COMO LISTA SIMPLES (sem envelope)
    //
    // Por que este novo método?
    // O método anterior retorna { Quantidade, Usuarios } — um objeto anônimo
    // que o Scalar/Swagger exibe bem, mas que o frontend não consegue
    // desserializar diretamente para List<ItemDto>.
    //
    // Quando o GerenciarTurmas.razor faz:
    //   Http.GetFromJsonAsync<List<ItemSimplesDto>>("api/usuarios/tipo/Professor/ativos")
    // ...ele recebe { Quantidade: 5, Usuarios: [...] } e falha silenciosamente
    // porque o JSON não é um array — é um objeto.
    //
    // Este método retorna IEnumerable<ObterUsuarioResponse> puro → array JSON direto.
    // Usado pelo novo endpoint GET /tipo/{tipo}/ativos/lista
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosPorTipoListaAsync(
        string tipo,
        bool? ativo = null)
    {
        var query = _context.Usuarios
            .Where(u => u.Tipo.ToLower() == tipo.ToLower());

        if (ativo.HasValue)
            query = query.Where(u => u.Ativo == ativo.Value);

        var usuarios = await query.ToListAsync();
        return usuarios.Select(u => u.ToResponse(_hashids));
    }

    public async Task<ObterUsuarioResponse> ObterUsuarioPorIdAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null || !usuario.Ativo)
            throw new RegraNegocioException("Usuário não encontrado.");

        return usuario.ToResponse(_hashids);
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

        if (ativar) usuario.Ativar();
        else usuario.Desativar();

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