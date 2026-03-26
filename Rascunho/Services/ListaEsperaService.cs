// Localização: Rascunho/Services/ListaEsperaService.cs
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;

namespace Rascunho.Services;

/// <summary>
/// Gerencia a fila de espera de turmas: entradas, saídas, notificações e expiração.
/// </summary>
public class ListaEsperaService
{
    private readonly AppDbContext _context;
    private readonly INotificacaoService _notificacaoService;
    private readonly IHashids _hashids;
    private readonly int _prazoConfirmacaoHoras;

    public ListaEsperaService(
        AppDbContext context,
        INotificacaoService notificacaoService,
        IHashids hashids,
        IConfiguration configuration)
    {
        _context = context;
        _notificacaoService = notificacaoService;
        _hashids = hashids;
        _prazoConfirmacaoHoras = configuration.GetValue<int>("ListaEspera:PrazoConfirmacaoHoras", 48);
    }

    // ──────────────────────────────────────────────────────────────────────
    // ENTRAR NA FILA
    // Chamado pelo TurmaService quando a turma está lotada e o aluno
    // não possui entrada ativa na fila.
    // ──────────────────────────────────────────────────────────────────────
    public async Task<string> EntrarNaFilaAsync(int turmaId, int alunoId)
    {
        // VALIDAÇÃO: Verificar se aluno já está na fila com status ativo
        var entradaExistente = await _context.ListasEspera
            .FirstOrDefaultAsync(le =>
                le.TurmaId == turmaId &&
                le.AlunoId == alunoId &&
                (le.Status == StatusListaEspera.Aguardando ||
                 le.Status == StatusListaEspera.Notificado));

        if (entradaExistente != null)
        {
            // Aluno já está na fila — retornar posição atual sem criar duplicata
            return $"Você já está na fila de espera desta turma na posição {entradaExistente.Posicao}.";
        }

        // Conta apenas entradas ativas para determinar a próxima posição
        int proximaPosicao = await _context.ListasEspera
            .CountAsync(le => le.TurmaId == turmaId &&
                              (le.Status == StatusListaEspera.Aguardando ||
                               le.Status == StatusListaEspera.Notificado)) + 1;

        var entrada = new ListaEspera
        {
            TurmaId = turmaId,
            AlunoId = alunoId,
            DataEntrada = DateTimeOffset.UtcNow,
            Posicao = proximaPosicao,
            Status = StatusListaEspera.Aguardando
        };

        _context.ListasEspera.Add(entrada);
        await _context.SaveChangesAsync();

        return $"A turma está cheia. Você foi adicionado à fila de espera na posição {proximaPosicao}.";
    }

    // ──────────────────────────────────────────────────────────────────────
    // SAIR DA FILA
    // Remove o aluno de entradas ativas (Aguardando ou Notificado).
    // ──────────────────────────────────────────────────────────────────────
    public async Task SairDaFilaAsync(int turmaId, int alunoId)
    {
        var entrada = await _context.ListasEspera
            .FirstOrDefaultAsync(le =>
                le.TurmaId == turmaId &&
                le.AlunoId == alunoId &&
                (le.Status == StatusListaEspera.Aguardando ||
                 le.Status == StatusListaEspera.Notificado))
            ?? throw new RegraNegocioException("Você não está na fila de espera desta turma.");

        _context.ListasEspera.Remove(entrada);
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // NOTIFICAR PRÓXIMO
    // Chamado quando uma vaga é aberta (desmatrícula ou aumento de limite).
    // Fluxo: expira vencidos → verifica vagas → notifica primeiro Aguardando.
    // ──────────────────────────────────────────────────────────────────────
    public async Task NotificarProximoAsync(int turmaId)
    {
        // 1. Expirar notificações cujo prazo já passou
        await ExpirarNotificacoesVencidasAsync(turmaId);

        // 2. Confirmar que ainda há vaga disponível após as expirações
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId);

        if (turma == null) return;

        int vagasDisponiveis = turma.LimiteAlunos - turma.Matriculas.Count;
        if (vagasDisponiveis <= 0) return;

        // 3. Não notificar se já existe alguém com vaga reservada (Notificado dentro do prazo)
        bool temNotificacaoAtiva = await _context.ListasEspera
            .AnyAsync(le => le.TurmaId == turmaId &&
                            le.Status == StatusListaEspera.Notificado);
        if (temNotificacaoAtiva) return;

        // 4. Buscar o próximo da fila (menor posição entre Aguardando)
        var proximo = await _context.ListasEspera
            .Include(le => le.Turma).ThenInclude(t => t.Ritmo)
            .Where(le => le.TurmaId == turmaId &&
                         le.Status == StatusListaEspera.Aguardando)
            .OrderBy(le => le.Posicao)
            .FirstOrDefaultAsync();

        if (proximo == null) return; // Fila vazia

        // 5. Reservar a vaga: marcar como Notificado com prazo de confirmação
        DateTimeOffset dataExpiracao = DateTimeOffset.UtcNow.AddHours(_prazoConfirmacaoHoras);
        proximo.Status = StatusListaEspera.Notificado;
        proximo.DataNotificacao = DateTimeOffset.UtcNow;
        proximo.DataExpiracao = dataExpiracao;

        await _context.SaveChangesAsync();

        // 6. Enviar push notification (stub → será substituído pelo Feature #4 FCM)
        string ritmoNome = proximo.Turma?.Ritmo?.Nome ?? "turma";
        await _notificacaoService.NotificarVagaDisponivelAsync(
            proximo.AlunoId, ritmoNome, dataExpiracao);
    }

    // ──────────────────────────────────────────────────────────────────────
    // MARCAR COMO CONVERTIDO
    // Chamado pelo TurmaService quando o aluno Notificado confirma a vaga
    // ao chamar o endpoint de matrícula.
    // ──────────────────────────────────────────────────────────────────────
    public async Task MarcarConvertidoAsync(int turmaId, int alunoId)
    {
        var entrada = await _context.ListasEspera
            .FirstOrDefaultAsync(le =>
                le.TurmaId == turmaId &&
                le.AlunoId == alunoId &&
                le.Status == StatusListaEspera.Notificado);

        if (entrada == null) return; // Já expirada ou inexistente — sem ação

        entrada.Status = StatusListaEspera.Convertido;
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // OBTER FILA — visão admin/recepção/gerente
    // Retorna apenas entradas ativas (Aguardando + Notificado), por posição.
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ListaEsperaAdminResponse>> ObterFilaAsync(int turmaId)
    {
        var fila = await _context.ListasEspera
            .Include(le => le.Aluno)
            .Where(le => le.TurmaId == turmaId &&
                         (le.Status == StatusListaEspera.Aguardando ||
                          le.Status == StatusListaEspera.Notificado))
            .OrderBy(le => le.Posicao)
            .ToListAsync();

        return fila.Select(le => new ListaEsperaAdminResponse(
            _hashids.Encode(le.AlunoId),
            le.Aluno.Nome,
            le.Aluno.FotoUrl ?? "",
            le.Posicao,
            le.Status.ToString(),
            le.DataEntrada,
            le.DataExpiracao
        ));
    }

    // ──────────────────────────────────────────────────────────────────────
    // OBTER MINHAS ESPERAS — visão do aluno logado
    // Verifica expirações lazy antes de retornar.
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<MinhaEsperaResponse>> ObterMinhasEsperasAsync(int alunoId)
    {
        // Verificação lazy: expirar notificações vencidas deste aluno
        var notificadasExpiradas = await _context.ListasEspera
            .Where(le =>
                le.AlunoId == alunoId &&
                le.Status == StatusListaEspera.Notificado &&
                le.DataExpiracao < DateTimeOffset.UtcNow)
            .ToListAsync();

        if (notificadasExpiradas.Any())
        {
            foreach (var e in notificadasExpiradas)
                e.Status = StatusListaEspera.Expirado;
            await _context.SaveChangesAsync();
        }

        var esperas = await _context.ListasEspera
            .Include(le => le.Turma).ThenInclude(t => t.Ritmo)
            .Include(le => le.Turma).ThenInclude(t => t.Sala)
            .Where(le => le.AlunoId == alunoId &&
                         (le.Status == StatusListaEspera.Aguardando ||
                          le.Status == StatusListaEspera.Notificado))
            .OrderBy(le => le.DataEntrada)
            .ToListAsync();

        return esperas.Select(le => new MinhaEsperaResponse(
            _hashids.Encode(le.TurmaId),
            le.Turma.Ritmo?.Nome ?? "N/A",
            le.Turma.Sala?.Nome ?? "N/A",
            le.Turma.Nivel,
            (int)le.Turma.DiaDaSemana,
            le.Turma.HorarioInicio.ToString(@"hh\:mm"),
            le.Turma.HorarioFim.ToString(@"hh\:mm"),
            le.Posicao,
            le.Status.ToString(),
            le.DataEntrada,
            le.DataExpiracao
        ));
    }

    // ──────────────────────────────────────────────────────────────────────
    // EXPIRAR NOTIFICAÇÕES VENCIDAS DE UMA TURMA (interno)
    // ──────────────────────────────────────────────────────────────────────
    private async Task ExpirarNotificacoesVencidasAsync(int turmaId)
    {
        var expiradas = await _context.ListasEspera
            .Where(le =>
                le.TurmaId == turmaId &&
                le.Status == StatusListaEspera.Notificado &&
                le.DataExpiracao < DateTimeOffset.UtcNow)
            .ToListAsync();

        if (!expiradas.Any()) return;

        foreach (var e in expiradas)
            e.Status = StatusListaEspera.Expirado;

        await _context.SaveChangesAsync();
    }
}
