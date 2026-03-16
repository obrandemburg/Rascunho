using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class AvisoService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public AvisoService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterAvisoResponse> CriarAvisoAsync(CriarAvisoRequest request, int autorId)
    {
        var aviso = new Aviso(request.Titulo, request.Mensagem, request.DataExpiracao, request.TipoVisibilidade, autorId);

        _context.Avisos.Add(aviso);
        await _context.SaveChangesAsync();

        await _context.Entry(aviso).Reference(a => a.Autor).LoadAsync();

        return aviso.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterAvisoResponse>> ListarAvisosAtivosAsync(string tipoVisibilidade)
    {
        var avisos = await _context.Avisos
            .Include(a => a.Autor)
            .Where(a => a.TipoVisibilidade == tipoVisibilidade && a.DataExpiracao > DateTime.UtcNow)
            .OrderByDescending(a => a.DataPublicacao)
            .ToListAsync();

        return avisos.Select(a => a.ToResponse(_hashids));
    }

    public async Task AtualizarAvisoAsync(int id, AtualizarAvisoRequest request)
    {
        var aviso = await _context.Avisos.FindAsync(id)
            ?? throw new RegraNegocioException("Aviso não encontrado.");

        aviso.Atualizar(request.Titulo, request.Mensagem, request.DataExpiracao, request.TipoVisibilidade);
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirAvisoAsync(int id)
    {
        var aviso = await _context.Avisos.FindAsync(id)
            ?? throw new RegraNegocioException("Aviso não encontrado.");

        _context.Avisos.Remove(aviso);
        await _context.SaveChangesAsync();
    }
}