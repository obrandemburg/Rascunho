using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.DTOs;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class EventoService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    // Taxa fixa do aplicativo cobrada nas vendas de ingressos
    private const decimal TAXA_APP = 5.00m;

    public EventoService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    /// <summary>
    /// Cria um novo evento (Baile ou Workshop) no sistema.
    /// Apenas Gerentes e Recepção devem ter acesso a esta funcionalidade.
    /// </summary>
    public async Task<ObterEventoResponse> CriarEventoAsync(CriarEventoRequest request)
    {
        var evento = new Evento(request.Nome, request.Descricao, request.DataHora, request.Tipo, request.Capacidade, request.Preco);

        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();

        return ObterEventoResponse.DeEntidade(evento, _hashids);
    }

    /// <summary>
    /// Lista os eventos ativos da escola.
    /// Pode filtrar para mostrar apenas os eventos que ainda vão acontecer (futuros) ou o histórico (passados).
    /// </summary>
    public async Task<IEnumerable<ObterEventoResponse>> ListarEventosAsync(bool apenasFuturos = true)
    {
        var query = _context.Eventos
            .Include(e => e.Ingressos)
            .Where(e => e.Ativo);

        if (apenasFuturos)
        {
            query = query.Where(e => e.DataHora >= DateTime.UtcNow);
        }

        // Ordena os mais próximos de acontecer primeiro
        var eventos = await query.OrderBy(e => e.DataHora).ToListAsync();

        return eventos.Select(e => ObterEventoResponse.DeEntidade(e, _hashids));
    }

    /// <summary>
    /// Processa a compra ou emissão de cortesia de um ingresso.
    /// Aplica a regra de gratuidade para Bolsistas em Bailes e calcula a taxa do app.
    /// </summary>
    public async Task<ComprarIngressoResponse> ComprarIngressoAsync(int eventoId, int usuarioLogadoId, string roleLogado)
    {
        var evento = await _context.Eventos
            .Include(e => e.Ingressos)
            .FirstOrDefaultAsync(e => e.Id == eventoId)
            ?? throw new RegraNegocioException("Evento não encontrado.");

        // TRAVA 1: O evento já aconteceu ou foi inativado?
        if (!evento.Ativo || evento.DataHora < DateTime.UtcNow)
            throw new RegraNegocioException("Não é possível comprar ingressos para um evento inativo ou que já passou.");

        // TRAVA 2: Overbooking (Esgotado)
        if (evento.Ingressos.Count(i => i.Status != "Cancelado") >= evento.Capacidade)
            throw new RegraNegocioException("Os ingressos para este evento estão esgotados.");

        // TRAVA 3: Cambismo/Duplicidade (1 ingresso por CPF/Usuário)
        bool jaComprou = evento.Ingressos.Any(i => i.UsuarioId == usuarioLogadoId && i.Status != "Cancelado");
        if (jaComprou)
            throw new RegraNegocioException("Você já possui um ingresso ativo para este evento.");

        // CÁLCULO FINANCEIRO
        decimal valorFinalCobranca = evento.Preco + TAXA_APP;
        string statusInicial = "Pendente"; // Aguardará a confirmação do Gateway de Pagamento (Fase 8)

        // REGRA DE NEGÓCIO ESPECÍFICA: Bolsistas têm acesso grátis AOS BAILES
        if (roleLogado == "Bolsista" && evento.Tipo == "Baile")
        {
            valorFinalCobranca = 0;
            statusInicial = "Pago"; // Sendo isento, o ingresso já nasce validado
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