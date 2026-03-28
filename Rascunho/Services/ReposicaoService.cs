// Localização: Rascunho/Services/ReposicaoService.cs
//
// BUG-005: ReposicaoService agora injeta ConfiguracaoService em vez de IConfiguration
// diretamente. Isso garante que ambos os services leem do mesmo lugar —
// se o Gerente alterar a janela via ConfiguracaoService, o ReposicaoService
// usará o novo valor imediatamente e de forma consistente.
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class ReposicaoService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;
    private readonly ConfiguracaoService _configuracaoService;

    public ReposicaoService(AppDbContext context, IHashids hashids, ConfiguracaoService configuracaoService)
    {
        _context = context;
        _hashids = hashids;
        _configuracaoService = configuracaoService;
    }

    // BUG-005: Lê via ConfiguracaoService (fonte única) em vez de IConfiguration diretamente.
    // Garante consistência: se o Gerente altera via endpoint, esse service também reflete.
    private int JanelaElegibilidadeDias =>
        _configuracaoService.ObterJanelaReposicaoDias();

    // ──────────────────────────────────────────────────────────────────────
    // OBTER FALTAS ELEGÍVEIS
    //
    // Retorna todas as faltas do aluno dentro da janela configurada,
    // incluindo as inelegíveis com o motivo explicado — para o frontend
    // poder informar claramente ao aluno por que não pode repor.
    //
    // RN-REP01: falta dentro da janela
    // RN-REP02: falta não reposta e sem agendamento ativo
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<FaltaElegivelResponse>> ObterFaltasElegiveisAsync(int alunoId)
    {
        var limiteData = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-JanelaElegibilidadeDias));

        // Uma única query para todas as faltas dentro da janela
        var faltas = await _context.RegistrosPresencas
            .Include(rp => rp.Turma)
                .ThenInclude(t => t.Ritmo)
            .Where(rp =>
                rp.AlunoId == alunoId &&
                !rp.Presente &&
                rp.DataAula >= limiteData)
            .OrderByDescending(rp => rp.DataAula)
            .ToListAsync();

        if (!faltas.Any())
            return Enumerable.Empty<FaltaElegivelResponse>();

        // Busca todas as reposições do aluno de uma vez — evita N+1
        var reposicoes = await _context.Reposicoes
            .Where(r => r.AlunoId == alunoId)
            .ToListAsync();

        var resultado = new List<FaltaElegivelResponse>();

        foreach (var falta in faltas)
        {
            bool jaReposta = reposicoes.Any(r =>
                r.TurmaOrigemId == falta.TurmaId &&
                r.DataFalta == falta.DataAula &&
                r.Status == "Realizada");

            bool temAgendamento = reposicoes.Any(r =>
                r.TurmaOrigemId == falta.TurmaId &&
                r.DataFalta == falta.DataAula &&
                r.Status == "Agendada");

            var nomeTurma = $"{falta.Turma?.Ritmo?.Nome ?? "?"} — {falta.Turma?.Nivel ?? "?"}";

            string? motivo = null;
            if (jaReposta)
                motivo = "Esta falta já foi reposta.";
            else if (temAgendamento)
                motivo = "Você já tem uma reposição agendada. Cancele-a para reagendar.";

            resultado.Add(new FaltaElegivelResponse(
                _hashids.Encode(falta.TurmaId),
                falta.Turma?.Ritmo?.Nome ?? "Desconhecido",
                nomeTurma,
                falta.DataAula,
                motivo
            ));
        }

        return resultado;
    }

    // ──────────────────────────────────────────────────────────────────────
    // AGENDAR REPOSIÇÃO
    //
    // SIMPLIFICADO: O aluno pode repor em QUALQUER turma ativa disponível,
    // independente do ritmo. Isso atende à necessidade pedagógica de
    // flexibilidade — o aluno pode experimentar outros ritmos como reposição.
    //
    // Validações mantidas:
    //   1. A falta é elegível (dentro da janela, não reposta, sem agendamento)
    //   2. A turma destino existe e está ativa
    //   3. A data de reposição é futura
    //   4. A data cai no dia da semana da turma destino
    //   5. A turma destino tem vagas
    // ──────────────────────────────────────────────────────────────────────
    public async Task<ObterReposicaoResponse> AgendarReposicaoAsync(
        int alunoId,
        AgendarReposicaoRequest request)
    {
        var turmaOrigemDecoded = _hashids.Decode(request.TurmaOrigemIdHash);
        var turmaDestinoDecoded = _hashids.Decode(request.TurmaDestinoIdHash);

        if (turmaOrigemDecoded.Length == 0 || turmaDestinoDecoded.Length == 0)
            throw new RegraNegocioException("IDs de turma inválidos.");

        int turmaOrigemId = turmaOrigemDecoded[0];
        int turmaDestinoId = turmaDestinoDecoded[0];

        var limiteData = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-JanelaElegibilidadeDias));

        // Verificação 1: A falta existe e está dentro da janela
        bool faltaExiste = await _context.RegistrosPresencas.AnyAsync(rp =>
            rp.AlunoId == alunoId &&
            rp.TurmaId == turmaOrigemId &&
            rp.DataAula == request.DataFalta &&
            !rp.Presente &&
            rp.DataAula >= limiteData);

        if (!faltaExiste)
            throw new RegraNegocioException(
                $"Falta não encontrada ou fora da janela de elegibilidade " +
                $"({JanelaElegibilidadeDias} dias). [RN-REP01]");

        // Verificação 2: A falta já foi reposta?
        bool jaReposta = await _context.Reposicoes.AnyAsync(r =>
            r.AlunoId == alunoId &&
            r.TurmaOrigemId == turmaOrigemId &&
            r.DataFalta == request.DataFalta &&
            r.Status == "Realizada");

        if (jaReposta)
            throw new RegraNegocioException("Esta falta já foi reposta. [RN-REP02]");

        // Verificação 3: Já tem agendamento ativo?
        bool temAgendamentoAtivo = await _context.Reposicoes.AnyAsync(r =>
            r.AlunoId == alunoId &&
            r.TurmaOrigemId == turmaOrigemId &&
            r.DataFalta == request.DataFalta &&
            r.Status == "Agendada");

        if (temAgendamentoAtivo)
            throw new RegraNegocioException(
                "Você já tem uma reposição agendada para esta falta. " +
                "Cancele-a primeiro para reagendar. [RN-REP02]");

        // Verificação 4: Turma destino existe e está ativa
        var turmaDestino = await _context.Turmas
            .Include(t => t.Ritmo)
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaDestinoId && t.Ativa)
            ?? throw new RegraNegocioException("Turma de destino não encontrada ou inativa.");

        // Verificação 5: Data futura
        if (request.DataReposicaoAgendada <= DateTime.UtcNow)
            throw new RegraNegocioException("A data de reposição deve ser no futuro.");

        // Verificação 6: Data cai no dia da semana correto da turma destino
        var diaDaReposicao = DateOnly.FromDateTime(request.DataReposicaoAgendada).DayOfWeek;

        if (diaDaReposicao != turmaDestino.DiaDaSemana)
        {
            var diaEsperado = turmaDestino.DiaDaSemana switch
            {
                DayOfWeek.Monday => "segunda-feira",
                DayOfWeek.Tuesday => "terça-feira",
                DayOfWeek.Wednesday => "quarta-feira",
                DayOfWeek.Thursday => "quinta-feira",
                DayOfWeek.Friday => "sexta-feira",
                DayOfWeek.Saturday => "sábado",
                _ => "domingo"
            };
            throw new RegraNegocioException(
                $"Esta turma ocorre às {diaEsperado}s. " +
                "Escolha uma data que caia neste dia da semana.");
        }

        // Verificação 7: Turma destino tem vagas
        if (turmaDestino.Matriculas.Count >= turmaDestino.LimiteAlunos)
            throw new RegraNegocioException(
                "A turma de destino está com capacidade máxima. Escolha outra turma ou data.");

        // Cria a Reposição
        var reposicao = new Reposicao(
            alunoId,
            turmaOrigemId,
            request.DataFalta,
            turmaDestinoId,
            request.DataReposicaoAgendada);

        _context.Reposicoes.Add(reposicao);
        await _context.SaveChangesAsync();

        // Carrega navegações para o mapper sem múltiplos roundtrips
        await _context.Entry(reposicao)
            .Reference(r => r.TurmaOrigem)
            .Query()
            .Include(t => t.Ritmo)
            .LoadAsync();

        await _context.Entry(reposicao)
            .Reference(r => r.TurmaDestino)
            .Query()
            .Include(t => t.Ritmo)
            .LoadAsync();

        return reposicao.ToResponse(_hashids);
    }

    // ──────────────────────────────────────────────────────────────────────
    // CANCELAR REPOSIÇÃO (RN-REP04)
    //
    // Ao cancelar, a falta original volta a ser elegível automaticamente,
    // pois a query de elegibilidade filtra Status == "Agendada".
    // Com o cancelamento, o Status passa para "Cancelada" e a falta
    // voltará a aparecer na lista sem nenhuma lógica adicional.
    // ──────────────────────────────────────────────────────────────────────
    public async Task CancelarReposicaoAsync(int alunoId, int reposicaoId)
    {
        var reposicao = await _context.Reposicoes.FindAsync(reposicaoId)
            ?? throw new RegraNegocioException("Reposição não encontrada.");

        if (reposicao.AlunoId != alunoId)
            throw new RegraNegocioException("Sem permissão para cancelar esta reposição.");

        if (reposicao.Status == "Cancelada")
            throw new RegraNegocioException("Esta reposição já está cancelada.");

        if (reposicao.Status == "Realizada")
            throw new RegraNegocioException(
                "Não é possível cancelar uma reposição já realizada. " +
                "Entre em contato com a recepção se houver um erro no registro.");

        reposicao.Cancelar();
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // LISTAR MINHAS REPOSIÇÕES
    // ──────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ObterReposicaoResponse>> ListarMinhasReposicoesAsync(int alunoId)
    {
        var reposicoes = await _context.Reposicoes
            .Include(r => r.TurmaOrigem).ThenInclude(t => t.Ritmo)
            .Include(r => r.TurmaDestino).ThenInclude(t => t.Ritmo)
            .Where(r => r.AlunoId == alunoId)
            .OrderByDescending(r => r.DataSolicitacao)
            .ToListAsync();

        return reposicoes.Select(r => r.ToResponse(_hashids));
    }
}