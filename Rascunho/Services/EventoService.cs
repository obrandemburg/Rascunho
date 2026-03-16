using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class EventoService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;
    private const decimal TAXA_APP = 5.00m;

    public EventoService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterEventoResponse> CriarEventoAsync(CriarEventoRequest request)
    {
        var evento = new Evento(request.Nome, request.Descricao, request.DataHora, request.Tipo, request.Capacidade, request.Preco);

        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();

        return evento.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterEventoResponse>> ListarEventosAsync(bool apenasFuturos = true)
    {
        var query = _context.Eventos
            .Include(e => e.Ingressos)
            .Where(e => e.Ativo);

        if (apenasFuturos)
        {
            query = query.Where(e => e.DataHora >= DateTime.UtcNow);
        }

        var eventos = await query.OrderBy(e => e.DataHora).ToListAsync();

        return eventos.Select(e => e.ToResponse(_hashids));
    }

    public async Task<ComprarIngressoResponse> ComprarIngressoAsync(int eventoId, int usuarioLogadoId, string roleLogado)
    {
        var evento = await _context.Eventos
            .Include(e => e.Ingressos)
            .FirstOrDefaultAsync(e => e.Id == eventoId)
            ?? throw new RegraNegocioException("Evento não encontrado.");

        if (!evento.Ativo || evento.DataHora < DateTime.UtcNow)
            throw new RegraNegocioException("Não é possível comprar ingressos para um evento inativo ou que já passou.");

        if (evento.Ingressos.Count(i => i.Status != "Cancelado") >= evento.Capacidade)
            throw new RegraNegocioException("Os ingressos para este evento estão esgotados.");

        bool jaComprou = evento.Ingressos.Any(i => i.UsuarioId == usuarioLogadoId && i.Status != "Cancelado");
        if (jaComprou)
            throw new RegraNegocioException("Você já possui um ingresso ativo para este evento.");

        decimal valorFinalCobranca = evento.Preco + TAXA_APP;
        string statusInicial = "Pendente";

        if (roleLogado == "Bolsista" && evento.Tipo == "Baile")
        {
            valorFinalCobranca = 0;
            statusInicial = "Pago";
        }

        var ingresso = new Ingresso(evento.Id, usuarioLogadoId, valorFinalCobranca, statusInicial);
        _context.Ingressos.Add(ingresso);
        await _context.SaveChangesAsync();

        string mensagemRetorno = valorFinalCobranca == 0
            ? "Ingresso de cortesia gerado com sucesso! Divirta-se no baile."
            : "Ingresso reservado. Aguardando confirmação de pagamento.";

        return new ComprarIngressoResponse(_hashids.Encode(ingresso.Id), mensagemRetorno, valorFinalCobranca);
    }
}