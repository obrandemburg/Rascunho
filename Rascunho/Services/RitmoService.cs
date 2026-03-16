using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
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
        var ritmo = new Ritmo(request.Nome, request.Descricao, request.Modalidade);
        _context.Ritmos.Add(ritmo);
        await _context.SaveChangesAsync();
        return ritmo.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterRitmoResponse>> ListarRitmosAsync()
    {
        var ritmos = await _context.Ritmos.ToListAsync();
        return ritmos.Select(r => r.ToResponse(_hashids));
    }

    public async Task<ObterRitmoResponse> ObterRitmoPorIdAsync(int id)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");
        return ritmo.ToResponse(_hashids);
    }

    public async Task AtualizarRitmoAsync(int id, AtualizarRitmoRequest request)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");

        // CORREÇÃO: Utilizando a função exata da sua entidade
        ritmo.Atualizar(request.Nome, request.Descricao, request.Modalidade);
        await _context.SaveChangesAsync();
    }

    public async Task InativarRitmoAsync(int id)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");

        bool emUso = await _context.Turmas.AnyAsync(t => t.RitmoId == id && t.Ativa);
        if (emUso) throw new RegraNegocioException("Não é possível inativar um ritmo que possui turmas ativas.");

        // CORREÇÃO: Utilizando a função exata da sua entidade
        ritmo.Desativar();
        await _context.SaveChangesAsync();
    }
}