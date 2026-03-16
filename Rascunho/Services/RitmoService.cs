using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Shared.DTOs;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class RitmoService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public RitmoService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterRitmoResponse> CriarRitmoAsync(CriarRitmoRequest request)
    {
        // Regra: Não podemos ter dois ritmos com o mesmo nome (ex: dois "Forró")
        bool ritmoJaExiste = await _context.Ritmos.AnyAsync(r => r.Nome.ToLower() == request.Nome.ToLower());
        if (ritmoJaExiste)
            throw new RegraNegocioException("Já existe um ritmo cadastrado com este nome.");

        var ritmo = new Ritmo(request.Nome, request.Descricao, request.Modalidade);

        _context.Ritmos.Add(ritmo);
        await _context.SaveChangesAsync();

        return ObterRitmoResponse.DeEntidade(ritmo, _hashids);
    }

    public async Task<IEnumerable<ObterRitmoResponse>> ListarTodosAsync(bool? ativo = null)
    {
        var query = _context.Ritmos.AsQueryable();

        if (ativo.HasValue)
            query = query.Where(r => r.Ativo == ativo.Value);

        var ritmos = await query.ToListAsync();
        return ritmos.Select(r => ObterRitmoResponse.DeEntidade(r, _hashids));
    }

    public async Task AtualizarRitmoAsync(int id, AtualizarRitmoRequest request)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");

        // Verifica se está tentando mudar o nome para um que já existe em outro ID
        bool nomeEmUso = await _context.Ritmos.AnyAsync(r => r.Nome.ToLower() == request.Nome.ToLower() && r.Id != id);
        if (nomeEmUso)
            throw new RegraNegocioException("Já existe outro ritmo cadastrado com este nome.");

        ritmo.Atualizar(request.Nome, request.Descricao, request.Modalidade);
        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(int id, bool ativar)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");

        if (ativar)
        {
            ritmo.Ativar();
        }
        else
        {
            // TODO: REGRA DE NEGÓCIO DA TURMA (Implementaremos no próximo passo)
            // bool possuiTurmasAtivas = await _context.Turmas.AnyAsync(t => t.RitmoId == id && t.Ativa);
            // if (possuiTurmasAtivas)
            //     throw new RegraNegocioException("Não é possível desativar este ritmo, pois existem turmas ativas vinculadas a ele.");

            ritmo.Desativar();
        }

        await _context.SaveChangesAsync();
    }
}