// Localização: Rascunho/Services/UsuarioService.cs
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

    // ──────────────────────────────────────────────────────────────
    // CRIAR USUÁRIO
    //
    // Fluxo:
    //   1. Valida unicidade de e-mail
    //   2. Valida unicidade de CPF (se informado)
    //   3. Cria objeto concreto pelo Tipo
    //   4. Define dados complementares (nascimento, telefone, CPF, foto)
    //   5. Salva no banco
    //   6. Professor → cria HabilidadeUsuario para cada ritmo
    //   7. Bolsista  → define papel dominante e dias obrigatórios
    // ──────────────────────────────────────────────────────────────
    public async Task<ObterUsuarioResponse> CriarUsuarioAsync(CriarUsuarioRequest request)
    {
        // ── Validação de unicidade de e-mail ──────────────────────
        bool emailJaExiste = await _context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailJaExiste)
            throw new RegraNegocioException("Este e-mail já está em uso.");

        // ── Validação de unicidade de CPF (quando informado) ──────
        // Remove formatação antes de comparar para garantir consistência
        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpfDigitos = new string(request.Cpf.Where(char.IsDigit).ToArray());
            bool cpfJaExiste = await _context.Usuarios
                .AnyAsync(u => u.Cpf == cpfDigitos);
            if (cpfJaExiste)
                throw new RegraNegocioException("Este CPF já está cadastrado no sistema.");
        }

        string senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);

        // ── Cria o objeto correto pelo discriminador Tipo ──────────
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

        // ── Dados complementares comuns a todos ───────────────────
        usuario.DefinirDadosComplementares(
            request.DataNascimento,
            request.Telefone ?? "",
            request.Cpf);
        usuario.DefinirGenero(request.Genero ?? "Não informado");

        // ── Define foto se foi enviada ────────────────────────────
        if (!string.IsNullOrEmpty(request.FotoUrl))
            usuario.DefinirFoto(request.FotoUrl);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // ── Ações pós-criação por tipo ─────────────────────────────

        // PROFESSOR: vincula os ritmos que ele leciona
        // Cria um HabilidadeUsuario por ritmo com Nivel = "Professor"
        // Isso permite que o sistema saiba quais ritmos ele pode ensinar
        // e que o algoritmo de balanceamento o exclua das sugestões de bolsista
        if (request.Tipo == "Professor" && request.RitmosIdHash?.Any() == true)
        {
            foreach (var ritmoHash in request.RitmosIdHash)
            {
                var ritmoDecoded = _hashids.Decode(ritmoHash);
                if (ritmoDecoded.Length == 0) continue;

                var ritmoExiste = await _context.Ritmos
                    .AnyAsync(r => r.Id == ritmoDecoded[0]);
                if (!ritmoExiste) continue;

                _context.Set<HabilidadeUsuario>().Add(new HabilidadeUsuario
                {
                    UsuarioId = usuario.Id,
                    RitmoId = ritmoDecoded[0],
                    PapelDominante = "Ambos",    // Professor leciona ambos os papéis
                    Nivel = "Professor" // Distingue de alunos avançados
                });
            }
            await _context.SaveChangesAsync();
        }

        // BOLSISTA: define papel dominante e dias obrigatórios
        // O EF Core já rastreia o objeto — só precisamos chamar os métodos
        // e salvar novamente (sem nova query)
        if (request.Tipo == "Bolsista" && usuario is Bolsista bolsista)
        {
            if (!string.IsNullOrEmpty(request.PapelDominante))
                bolsista.DefinirPapelDominante(request.PapelDominante);

            if (request.DiaObrigatorio1.HasValue && request.DiaObrigatorio2.HasValue)
                bolsista.DefinirDiasObrigatorios(
                    (DayOfWeek)request.DiaObrigatorio1.Value,
                    (DayOfWeek)request.DiaObrigatorio2.Value);

            await _context.SaveChangesAsync();
        }

        return usuario.ToResponse(_hashids);
    }

    // ──────────────────────────────────────────────────────────────
    // INSERÇÃO EM MASSA — mantida para compatibilidade
    // ──────────────────────────────────────────────────────────────
    public async Task InserirUsuariosEmMassaAsync(IEnumerable<CriarUsuarioRequest> requests)
    {
        var lista = requests.ToList();

        // Verifica duplicatas de e-mail dentro da própria lista enviada
        var emailsDuplicados = lista
            .GroupBy(r => r.Email.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (emailsDuplicados.Any())
            throw new RegraNegocioException(
                $"Lista com e-mails duplicados: {string.Join(", ", emailsDuplicados)}");

        // Verifica e-mails já existentes no banco
        var emailsEnviados = lista.Select(r => r.Email).ToList();
        var emailsExistentes = await _context.Usuarios
            .Where(u => emailsEnviados.Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync();

        if (emailsExistentes.Any())
            throw new RegraNegocioException(
                $"E-mails já cadastrados: {string.Join(", ", emailsExistentes)}");

        var usuariosParaInserir = new List<Usuario>();
        foreach (var req in lista)
        {
            string senhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha);
            Usuario u = req.Tipo switch
            {
                "Aluno" => new Aluno(req.Nome, req.Email, senhaHash),
                "Professor" => new Professor(req.Nome, req.Email, senhaHash),
                "Bolsista" => new Bolsista(req.Nome, req.Email, senhaHash),
                "Gerente" => new Gerente(req.Nome, req.Email, senhaHash),
                "Recepção" => new Recepcao(req.Nome, req.Email, senhaHash),
                "Líder" => new Lider(req.Nome, req.Email, senhaHash),
                _ => throw new RegraNegocioException($"Tipo inválido: {req.Tipo}")
            };
            u.DefinirDadosComplementares(req.DataNascimento, req.Telefone ?? "", req.Cpf);
            if (!string.IsNullOrEmpty(req.FotoUrl)) u.DefinirFoto(req.FotoUrl);
            usuariosParaInserir.Add(u);
        }

        await _context.BulkInsertAsync(usuariosParaInserir);
    }

    // ── Listagens ─────────────────────────────────────────────────

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarTodosUsuariosAsync()
    {
        var usuarios = await _context.Usuarios.ToListAsync();
        return usuarios.Select(u => u.ToResponse(_hashids));
    }
    public async Task<PaginacaoResponse<ObterUsuarioResponse>> ListarUsuariosPaginadoAsync(
    int pagina,
    int tamanhoPagina,
    string? nome = null,
    string? tipo = null,
    string? status = "todos")
    {
        var query = _context.Usuarios.AsQueryable();

        // Aplicar os filtros diretamente no banco
        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(u => u.Nome.Contains(nome));

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(u => u.Tipo == tipo);

        if (status == "ativos")
            query = query.Where(u => u.Ativo);
        else if (status == "desativados")
            query = query.Where(u => !u.Ativo);

        // Contar o total de registros (necessário para o MudDataGrid saber quantas páginas existem)
        int total = await query.CountAsync();

        // Paginar com EF Core (pula X, pega Y)
        var usuarios = await query
            .OrderBy(u => u.Nome)
            .Skip(pagina * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        var itensResponse = usuarios.Select(u => u.ToResponse(_hashids));

        return new PaginacaoResponse<ObterUsuarioResponse>(itensResponse, total);
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

    public async Task<object> ListarUsuariosPorTipoAsync(string tipo, bool? ativo = null)
    {
        var query = _context.Usuarios.Where(u => u.Tipo.ToLower() == tipo.ToLower());
        if (ativo.HasValue) query = query.Where(u => u.Ativo == ativo.Value);

        int quantidade = await query.CountAsync();
        var lista = (await query.ToListAsync()).Select(u => u.ToResponse(_hashids));
        return new { Quantidade = quantidade, Usuarios = lista };
    }

    public async Task<IEnumerable<ObterUsuarioResponse>> ListarUsuariosPorTipoListaAsync(
        string tipo, bool? ativo = null)
    {
        var query = _context.Usuarios.Where(u => u.Tipo.ToLower() == tipo.ToLower());
        if (ativo.HasValue) query = query.Where(u => u.Ativo == ativo.Value);
        return (await query.ToListAsync()).Select(u => u.ToResponse(_hashids));
    }

    public async Task<ObterUsuarioResponse> ObterUsuarioPorIdAsync(int id)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u == null || !u.Ativo)
            throw new RegraNegocioException("Usuário não encontrado.");
        return u.ToResponse(_hashids);
    }

    public async Task AtualizarPerfilAsync(int id, EditarPerfilRequest request)
    {
        var u = await _context.Usuarios.FindAsync(id)
            ?? throw new RegraNegocioException("Usuário não encontrado.");
        u.EditarPerfil(request.FotoUrl, request.NomeSocial, request.Biografia);
        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(int id, bool ativar)
    {
        var u = await _context.Usuarios.FindAsync(id)
            ?? throw new RegraNegocioException("Usuário não encontrado.");
        if (ativar) u.Ativar(); else u.Desativar();
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirUsuarioAsync(int id)
    {
        var u = await _context.Usuarios.FindAsync(id)
            ?? throw new RegraNegocioException("Usuário não encontrado.");
        _context.Usuarios.Remove(u);
        await _context.SaveChangesAsync();
    }
    // ──────────────────────────────────────────────────────────────
    // BUSCAR USUÁRIOS (Alunos e Bolsistas) por nome ou CPF
    // Usado pela tela de matrícula administrativa
    // ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<BuscarUsuarioResponse>> BuscarUsuariosAsync(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Enumerable.Empty<BuscarUsuarioResponse>();

        var termo = q.Trim().ToLower();
        var cpfDigitos = new string(termo.Where(char.IsDigit).ToArray());

        var resultados = await _context.Usuarios
            .Where(u =>
                u.Ativo &&
                (u.Tipo == "Aluno" || u.Tipo == "Bolsista") &&
                (
                    u.Nome.ToLower().Contains(termo) ||
                    (cpfDigitos.Length >= 3 && u.Cpf != null && u.Cpf.StartsWith(cpfDigitos))
                )
            )
            .OrderBy(u => u.Nome)
            .Take(10)
            .ToListAsync();

        return resultados.Select(u => new BuscarUsuarioResponse(
            _hashids.Encode(u.Id),
            u.Nome,
            u.FotoUrl,
            u.Tipo,
            u.Genero
        ));
    }
    public async Task AlterarSenhaAsync(int id, AlterarSenhaRequest request)
    {
        var u = await _context.Usuarios.FindAsync(id)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        // 1. Verifica se a senha atual está correta (exemplo usando BCrypt)
        bool senhaCorreta = BCrypt.Net.BCrypt.Verify(request.SenhaAtual, u.SenhaHash);
        if (!senhaCorreta)
        {
            throw new RegraNegocioException("A senha atual informada está incorreta.");
        }

        // 2. Opcional: Impedir que a nova senha seja igual à antiga
        if (request.SenhaAtual == request.NovaSenha)
        {
            throw new RegraNegocioException("A nova senha não pode ser igual à senha atual.");
        }

        // 3. Gera o hash da nova senha
        string novoHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);

        // 4. Atualiza a entidade (você pode criar um método AlterarSenha na entidade Usuario)
        u.AlterarSenha(novoHash);

        await _context.SaveChangesAsync();
    }
}
