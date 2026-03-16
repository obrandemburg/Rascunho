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

    public SalaService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterSalaResponse> CriarSalaAsync(CriarSalaRequest request)
    {
        var sala = new Sala(request.Nome, request.CapacidadeMaxima);
        _context.Salas.Add(sala);
        await _context.SaveChangesAsync();
        return sala.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterSalaResponse>> ListarSalasAsync()
    {
        var salas = await _context.Salas.ToListAsync();
        return salas.Select(s => s.ToResponse(_hashids));
    }

    public async Task<ObterSalaResponse> ObterSalaPorIdAsync(int id)
    {
        var sala = await _context.Salas.FindAsync(id)
            ?? throw new RegraNegocioException("Sala não encontrada.");
        return sala.ToResponse(_hashids);
    }

    public async Task AtualizarSalaAsync(int id, AtualizarSalaRequest request)
    {
        var sala = await _context.Salas.FindAsync(id)
            ?? throw new RegraNegocioException("Sala não encontrada.");

        if (request.CapacidadeMaxima < sala.CapacidadeMaxima)
        {
            throw new RegraNegocioException("A capacidade de uma sala não pode ser reduzida por questões de segurança de turmas já ativas.");
        }

        // CORREÇÃO: Utilizando a função exata da sua entidade
        sala.Atualizar(request.Nome, request.CapacidadeMaxima);
        await _context.SaveChangesAsync();
    }

    public async Task InativarSalaAsync(int id)
    {
        var sala = await _context.Salas.FindAsync(id)
            ?? throw new RegraNegocioException("Sala não encontrada.");

        bool emUso = await _context.Turmas.AnyAsync(t => t.SalaId == id && t.Ativa);
        if (emUso) throw new RegraNegocioException("Não é possível inativar uma sala que possui turmas ativas.");

        // CORREÇÃO: Utilizando a função exata da sua entidade
        sala.Desativar();
        await _context.SaveChangesAsync();
    }
}