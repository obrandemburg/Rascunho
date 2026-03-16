using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.DTOs;
using Rascunho.Entities;
using Rascunho.Exceptions;
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
        bool salaJaExiste = await _context.Salas.AnyAsync(s => s.Nome.ToLower() == request.Nome.ToLower());
        if (salaJaExiste)
            throw new RegraNegocioException("Já existe uma sala cadastrada com este nome/número.");

        var sala = new Sala(request.Nome, request.CapacidadeMaxima);

        _context.Salas.Add(sala);
        await _context.SaveChangesAsync();

        return ObterSalaResponse.DeEntidade(sala, _hashids);
    }

    public async Task<IEnumerable<ObterSalaResponse>> ListarTodasAsync(bool? ativo = null)
    {
        var query = _context.Salas.AsQueryable();

        if (ativo.HasValue)
            query = query.Where(s => s.Ativo == ativo.Value);

        var salas = await query.ToListAsync();
        return salas.Select(s => ObterSalaResponse.DeEntidade(s, _hashids));
    }

    public async Task AtualizarSalaAsync(int id, AtualizarSalaRequest request)
    {
        var sala = await _context.Salas.FindAsync(id)
            ?? throw new RegraNegocioException("Sala não encontrada.");

        bool nomeEmUso = await _context.Salas.AnyAsync(s => s.Nome.ToLower() == request.Nome.ToLower() && s.Id != id);
        if (nomeEmUso)
            throw new RegraNegocioException("Já existe outra sala cadastrada com este nome.");

        // NOVA REGRA: A capacidade nunca pode ser reduzida
        if (request.CapacidadeMaxima < sala.CapacidadeMaxima)
            throw new RegraNegocioException("A capacidade de uma sala não pode ser reduzida. Crie uma nova sala ou mantenha a capacidade atual.");

        sala.Atualizar(request.Nome, request.CapacidadeMaxima);
        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(int id, bool ativar)
    {
        var sala = await _context.Salas.FindAsync(id)
            ?? throw new RegraNegocioException("Sala não encontrada.");

        if (ativar)
        {
            sala.Ativar();
        }
        else
        {
            // NOVA REGRA: Instrução explícita para o Front-end desalocar turmas
            // TODO: Descomentar quando as entidades Turma e Aluguel existirem
            // bool possuiAtividades = await _context.Turmas.AnyAsync(t => t.SalaId == id && t.Ativa) ||
            //                         await _context.Alugueis.AnyAsync(a => a.SalaId == id && a.DataHora > DateTime.UtcNow);
            //
            // if (possuiAtividades)
            // {
            //     throw new RegraNegocioException("Existem turmas ou aluguéis marcados para esta sala. Por favor, desaloque as atividades desta sala antes de inativá-la.");
            // }

            sala.Desativar();
        }

        await _context.SaveChangesAsync();
    }
}