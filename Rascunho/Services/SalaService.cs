using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class SalaService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public SalaService(AppDbContext context, IHashids hashids) { _context = context; _hashids = hashids; }

    public async Task<ObterSalaResponse> CriarSalaAsync(CriarSalaRequest request)
    {
        var sala = new Sala(request.Nome, request.CapacidadeMaxima);
        _context.Salas.Add(sala);
        await _context.SaveChangesAsync();
        return sala.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterSalaResponse>> ListarTodasAsync(bool apenasAtivas = false)
    {
        var query = _context.Salas.AsQueryable();

        if (apenasAtivas) query = query.Where(s => s.Ativo);

        var salas = await query.ToListAsync();
        return salas.Select(s => s.ToResponse(_hashids));
    }

    public async Task<ObterSalaResponse> ObterSalaPorIdAsync(int id)
    {
        var sala = await _context.Salas.FindAsync(id) ?? throw new RegraNegocioException("Sala não encontrada.");
        return sala.ToResponse(_hashids);
    }

    public async Task AtualizarSalaAsync(int id, AtualizarSalaRequest request)
    {
        var sala = await _context.Salas.FindAsync(id) ?? throw new RegraNegocioException("Sala não encontrada.");
        if (request.CapacidadeMaxima < sala.CapacidadeMaxima)
            throw new RegraNegocioException("A capacidade não pode ser reduzida por segurança das turmas ativas.");

        sala.Atualizar(request.Nome, request.CapacidadeMaxima);
        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(int id, bool ativo)
    {
        var sala = await _context.Salas.FindAsync(id) ?? throw new RegraNegocioException("Sala não encontrada.");
        if (ativo)
        {
            sala.Ativar();
        }
        else
        {
            bool emUso = await _context.Turmas.AnyAsync(t => t.SalaId == id && t.Ativa);
            if (emUso) throw new RegraNegocioException("Não é possível inativar uma sala com turmas ativas.");
            sala.Desativar();
        }
        await _context.SaveChangesAsync();
    }
}