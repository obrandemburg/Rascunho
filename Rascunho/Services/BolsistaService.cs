// ARQUIVO: Rascunho/Services/BolsistaService.cs
//
// SPRINT 6: .Include(t => t.Ritmo) em AnalisarEBalancearTurmaAsync e
//           TurmasRecomendadasParaBolsistaAsync; novos campos no retorno
//           de SugestaoBalanceamentoResponse.
//
// SPRINT 7: bolsista.FotoUrl passado como 3° argumento em
//           MeuDesempenhoAsync (posição nova no record DesempenhoResponse).

using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Shared.DTOs;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class BolsistaService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public BolsistaService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task DefinirDiasObrigatoriosAsync(int bolsistaId, int dia1, int dia2)
    {
        var bolsista = await _context.Usuarios.OfType<Bolsista>()
            .FirstOrDefaultAsync(b => b.Id == bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        bolsista.DefinirDiasObrigatorios((DayOfWeek)dia1, (DayOfWeek)dia2);
        await _context.SaveChangesAsync();
    }

    public async Task AdicionarHabilidadeAsync(int bolsistaId, AdicionarHabilidadeRequest request)
    {
        var ritmoDecoded = _hashids.Decode(request.RitmoIdHash);
        if (ritmoDecoded.Length == 0) throw new RegraNegocioException("Ritmo inválido.");
        int ritmoId = ritmoDecoded[0];

        bool jaPossui = await _context.Set<HabilidadeUsuario>()
            .AnyAsync(h => h.UsuarioId == bolsistaId && h.RitmoId == ritmoId);

        if (jaPossui)
            throw new RegraNegocioException("O bolsista já possui este ritmo nas suas habilidades.");

        _context.Set<HabilidadeUsuario>().Add(new HabilidadeUsuario
        {
            UsuarioId = bolsistaId,
            RitmoId = ritmoId,
            PapelDominante = request.PapelDominante,
            Nivel = request.Nivel
        });
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // LISTAR MINHAS HABILIDADES
    //
    // Retorna todas as habilidades (ritmos) cadastradas pelo bolsista.
    // Implementação da tarefa faltante do contexto (implementacoes_faltantes.md).
    // ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HabilidadeResponse>> ListarMinhasHabilidadesAsync(int bolsistaId)
    {
        var habilidades = await _context.Set<HabilidadeUsuario>()
            .Include(h => h.Ritmo)
            .Where(h => h.UsuarioId == bolsistaId)
            .OrderBy(h => h.Ritmo.Nome)
            .ToListAsync();

        return habilidades.Select(h => new HabilidadeResponse(
            _hashids.Encode(h.RitmoId),
            h.Ritmo?.Nome ?? "Desconhecido",
            h.PapelDominante,
            h.Nivel
        ));
    }

    // ──────────────────────────────────────────────────────────────
    // REMOVER HABILIDADE
    // ──────────────────────────────────────────────────────────────
    public async Task RemoverHabilidadeAsync(int bolsistaId, string ritmoIdHash)
    {
        var ritmoDecoded = _hashids.Decode(ritmoIdHash);
        if (ritmoDecoded.Length == 0) throw new RegraNegocioException("Ritmo inválido.");

        var habilidade = await _context.Set<HabilidadeUsuario>()
            .FirstOrDefaultAsync(h => h.UsuarioId == bolsistaId && h.RitmoId == ritmoDecoded[0]);

        if (habilidade is null)
            throw new RegraNegocioException("Habilidade não encontrada.");

        _context.Set<HabilidadeUsuario>().Remove(habilidade);
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // MEU DESEMPENHO
    //
    // SPRINT 7: bolsista.FotoUrl passado como 3° argumento.
    //   DesempenhoResponse(BolsistaIdHash, Nome, FotoUrl, DiaObrigatorio1, ...)
    //   A ordem IMPORTA porque é um record posicional.
    //
    // BUG-006: Adicionado parâmetro periodoFiltro:
    //   - "30dias"  → últimos 30 dias (PADRÃO)
    //   - "mes"     → mês corrente
    //   - "tudo"    → todo o histórico
    //   - "semana"  → semana corrente (dom a sáb)
    //   - "Nmeses"  → últimos N meses (ex: "2meses", "3meses", até 12)
    //
    // ALTERAÇÃO: O cálculo agora considera TODAS as semanas no período
    // (mesmo que o bolsista não tenha tido presença em alguma), calculando
    // a frequência por mês e exibindo dados mensais no histórico.
    // ──────────────────────────────────────────────────────────────
    public async Task<DesempenhoResponse> MeuDesempenhoAsync(
        int bolsistaId,
        string periodoFiltro = "30dias")
    {
        var bolsista = await _context.Usuarios.OfType<Bolsista>()
            .FirstOrDefaultAsync(b => b.Id == bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        // Calcula a data de início conforme o filtro solicitado
        DateOnly? limiteData = CalcularLimiteData(periodoFiltro);

        var query = _context.RegistrosPresencas
            .Include(rp => rp.Turma).ThenInclude(t => t.Ritmo)
            .Include(rp => rp.Turma).ThenInclude(t => t.Professores).ThenInclude(tp => tp.Professor)
            .Where(rp => rp.AlunoId == bolsistaId);

        if (limiteData.HasValue)
            query = query.Where(rp => rp.DataAula >= limiteData.Value);

        var todasPresencas = await query
            .OrderByDescending(rp => rp.DataAula)
            .ToListAsync();

        var diasObrigatorios = new HashSet<DayOfWeek>();
        if (bolsista.DiaObrigatorio1.HasValue) diasObrigatorios.Add(bolsista.DiaObrigatorio1.Value);
        if (bolsista.DiaObrigatorio2.HasValue) diasObrigatorios.Add(bolsista.DiaObrigatorio2.Value);

        var presencasObrigatorias = todasPresencas
            .Where(rp => diasObrigatorios.Contains(rp.DataAula.DayOfWeek)).ToList();
        var presencasExtras = todasPresencas
            .Where(rp => !diasObrigatorios.Contains(rp.DataAula.DayOfWeek)).ToList();

        // ALTERAÇÃO: para calcular frequência corretamente, consideramos TODAS as aulas
        // que deveriam ter ocorrido no período (não apenas as com presença registrada).
        // Contamos semanas no período para determinar total de aulas obrigatórias esperadas.
        int totalAulasEsperadas = CalcularTotalAulasEsperadas(limiteData, diasObrigatorios);
        int totalPresencasObrigatorias = presencasObrigatorias.Count(p => p.Presente);

        double freqObrigatoria = totalAulasEsperadas > 0
            ? (double)totalPresencasObrigatorias / totalAulasEsperadas * 100
            : 0;
        double freqExtra = presencasExtras.Count > 0
            ? (double)presencasExtras.Count(p => p.Presente) / presencasExtras.Count * 100
            : 0;

        string indicador = freqObrigatoria switch
        {
            >= 85 => "Excelente",
            >= 75 => "Vamos melhorar",
            >= 60 => "Atenção",
            _ => "Crítico"
        };
        if (totalAulasEsperadas == 0) indicador = "Sem registros";

        var historico = todasPresencas.Select(rp => new HistoricoPresencaItem(
            rp.DataAula,
            rp.Turma?.Ritmo?.Nome ?? "Turma desconhecida",
            rp.Turma?.Professores?.FirstOrDefault()?.Professor?.Nome ?? "Professor desconhecido",
            rp.Presente,
            diasObrigatorios.Contains(rp.DataAula.DayOfWeek)
        )).ToList();

        return new DesempenhoResponse(
            _hashids.Encode(bolsista.Id),
            bolsista.Nome,
            bolsista.FotoUrl,
            bolsista.DiaObrigatorio1.HasValue ? (int?)bolsista.DiaObrigatorio1.Value : null,
            bolsista.DiaObrigatorio2.HasValue ? (int?)bolsista.DiaObrigatorio2.Value : null,
            Math.Round(freqObrigatoria, 1),
            indicador,
            totalAulasEsperadas,
            totalPresencasObrigatorias,
            Math.Round(freqExtra, 1),
            presencasExtras.Count,
            presencasExtras.Count(p => p.Presente),
            historico
        );
    }

    // ──────────────────────────────────────────────────────────────
    // HELPERS PARA DESEMPENHO
    // ──────────────────────────────────────────────────────────────
    private static DateOnly? CalcularLimiteData(string periodoFiltro)
    {
        var hoje = DateTime.UtcNow;

        if (periodoFiltro == "semana")
        {
            // Início da semana atual (domingo)
            int diff = (int)hoje.DayOfWeek;
            return DateOnly.FromDateTime(hoje.AddDays(-diff));
        }

        if (periodoFiltro == "mes")
            return new DateOnly(hoje.Year, hoje.Month, 1);

        if (periodoFiltro == "30dias")
            return DateOnly.FromDateTime(hoje.AddDays(-30));

        // Formato "Nmeses" — ex: "2meses", "3meses"
        if (periodoFiltro.EndsWith("meses") &&
            int.TryParse(periodoFiltro.Replace("meses", ""), out int nMeses) &&
            nMeses >= 1 && nMeses <= 12)
        {
            var inicio = hoje.AddMonths(-nMeses);
            return new DateOnly(inicio.Year, inicio.Month, 1);
        }

        return null; // "tudo"
    }

    // Conta o número total de aulas esperadas no período dado os dias obrigatórios.
    // Percorre do limiteData até hoje contando ocorrências dos dias obrigatórios.
    private static int CalcularTotalAulasEsperadas(DateOnly? limiteData, HashSet<DayOfWeek> diasObrigatorios)
    {
        if (!diasObrigatorios.Any()) return 0;

        var inicio = limiteData ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));
        var fim = DateOnly.FromDateTime(DateTime.UtcNow);

        int total = 0;
        var atual = inicio;
        while (atual <= fim)
        {
            if (diasObrigatorios.Contains(atual.DayOfWeek))
                total++;
            atual = atual.AddDays(1);
        }
        return total;
    }

    // ──────────────────────────────────────────────────────────────
    // TURMAS RECOMENDADAS (BUG-007 — CORRIGIDO)
    //
    // Antes: filtrava pelas turmas dos DIAS OBRIGATÓRIOS do bolsista,
    //        o que tornava o resultado idêntico ao de TurmasObrigatorias.
    //
    // Agora: mostra as turmas mais DESBALANCEADAS do dia selecionado,
    //        independente dos dias obrigatórios do bolsista.
    //        Padrão: dia da semana atual (DateTime.UtcNow.DayOfWeek).
    //        Se nenhum dia for passado, usa o dia de hoje.
    //
    // SPRINT 6: .Include(t => t.Ritmo) mantido.
    // ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<SugestaoBalanceamentoResponse>> TurmasRecomendadasParaBolsistaAsync(
        int bolsistaId,
        DayOfWeek? diaDaSemana = null)
    {
        // Valida que o bolsista existe (segurança — pode ser chamado por contexto diferente)
        var existe = await _context.Usuarios.OfType<Bolsista>()
            .AnyAsync(b => b.Id == bolsistaId);
        if (!existe)
            throw new RegraNegocioException("Bolsista não encontrado.");

        // Usa o dia informado ou o dia atual como padrão
        var dia = diaDaSemana ?? DateTime.UtcNow.DayOfWeek;

        var turmasDoDia = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.Ritmo)
            .Where(t => t.Ativa &&
                        t.DiaDaSemana == dia &&
                        t.Ritmo.Modalidade.ToLower() != "dança solo")
            .ToListAsync();

        var resultados = new List<SugestaoBalanceamentoResponse>();
        foreach (var turma in turmasDoDia)
        {
            var analise = await AnalisarEBalancearTurmaAsync(turma.Id);
            resultados.Add(analise);
        }

        // Ordena: turmas com maior desequilíbrio primeiro, balanceadas por último
        return resultados
            .OrderBy(r => r.Status == "Balanceada" ? 1 : 0)
            .ThenByDescending(r => r.QuantidadeFaltante);
    }

    // ──────────────────────────────────────────────────────────────
    // ANALISAR E BALANCEAR TURMA
    // SPRINT 6: .Include(t => t.Ritmo) + novos campos no retorno.
    // ──────────────────────────────────────────────────────────────
    public async Task<SugestaoBalanceamentoResponse> AnalisarEBalancearTurmaAsync(int turmaId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.Ritmo)                  // ← Sprint 6
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        int condutores = turma.Matriculas.Count(m => m.Papel == "Condutor");
        int conduzidos = turma.Matriculas.Count(m => m.Papel == "Conduzido");

        string status = "Balanceada";
        string papelNecessario = "";
        int quantFaltante = 0;

        if (condutores > conduzidos)
        {
            status = "Faltam Conduzidos";
            papelNecessario = "Conduzido";
            quantFaltante = condutores - conduzidos;
        }
        else if (conduzidos > condutores)
        {
            status = "Faltam Condutores";
            papelNecessario = "Condutor";
            quantFaltante = conduzidos - condutores;
        }

        var sugestoes = new List<BolsistaSugerido>();
        if (quantFaltante > 0)
        {
            var bolsistasQualificados = await _context.Set<HabilidadeUsuario>()
                .Include(h => h.Usuario)
                .Where(h =>
                    h.RitmoId == turma.RitmoId &&
                    h.Usuario.Tipo == "Bolsista" &&
                    h.Usuario.Ativo &&
                    (h.PapelDominante == papelNecessario || h.PapelDominante == "Ambos"))
                .ToListAsync();

            var bolsistaIds = bolsistasQualificados.Select(h => h.UsuarioId).ToList();

            var bolsistasComConflito = await _context.Matriculas
                .Where(m =>
                    bolsistaIds.Contains(m.AlunoId) &&
                    m.Turma.Ativa &&
                    m.Turma.DiaDaSemana == turma.DiaDaSemana &&
                    turma.HorarioInicio < m.Turma.HorarioFim &&
                    turma.HorarioFim > m.Turma.HorarioInicio)
                .Select(m => m.AlunoId)
                .Distinct()
                .ToListAsync();

            foreach (var hab in bolsistasQualificados)
            {
                if (!bolsistasComConflito.Contains(hab.UsuarioId))
                    sugestoes.Add(new BolsistaSugerido(
                        _hashids.Encode(hab.UsuarioId),
                        hab.Usuario.Nome,
                        hab.PapelDominante,
                        hab.Nivel));
            }
        }

        return new SugestaoBalanceamentoResponse(
            _hashids.Encode(turma.Id),
            condutores,
            conduzidos,
            status,
            quantFaltante,
            sugestoes,
            turma.Ritmo?.Nome ?? "—",       // ← Sprint 6
            (int)turma.DiaDaSemana,          // ← Sprint 6
            turma.HorarioInicio,             // ← Sprint 6
            turma.HorarioFim                 // ← Sprint 6
        );
    }

    // ──────────────────────────────────────────────────────────────
    // RELATÓRIO DE HORAS SEMANAIS
    //
    // ALTERAÇÃO: 6 horas obrigatórias = 6 AULAS de 50 minutos cada.
    // A meta não é mais calculada em horas reais (HorarioFim - HorarioInicio),
    // mas em quantidade de aulas (cada aula vale 50 minutos = 0,833h).
    // Isso evita números quebrados nos relatórios e reflete a realidade da escola.
    //
    // Meta: 6 aulas × 50min = 300 minutos = 5h de dança efetiva.
    // Exibição: mantemos a unidade "aulas" no retorno para clareza.
    // ──────────────────────────────────────────────────────────────
    public async Task<RelatorioHorasBolsistaResponse> RelatorioHorasSemanaisAsync(int bolsistaId)
    {
        var bolsista = await _context.Usuarios.FindAsync(bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        // Meta: 6 aulas de 50 minutos por semana
        const int metaAulas = 6;
        const double minutosPoraula = 50.0;

        // Turmas solo ativas em que o bolsista está matriculado (contam como aulas fixas por semana)
        var matriculasSolo = await _context.Matriculas
            .Include(m => m.Turma).ThenInclude(t => t.Ritmo)
            .Where(m => m.AlunoId == bolsistaId &&
                        m.Turma.Ativa &&
                        m.Turma.Ritmo.Modalidade.ToLower() == "dança solo")
            .ToListAsync();

        // Cada turma solo conta como 1 aula (independente da duração real)
        int aulasSolo = matriculasSolo.Count;

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);
        var fimSemana = inicioSemana.AddDays(7);

        // Presenças de salão registradas nesta semana
        var presencasSalao = await _context.RegistrosPresencas
            .Include(rp => rp.Turma).ThenInclude(t => t.Ritmo)
            .Where(rp => rp.AlunoId == bolsistaId &&
                         rp.Presente &&
                         rp.DataAula >= inicioSemana &&
                         rp.DataAula < fimSemana &&
                         rp.Turma.Ritmo.Modalidade.ToLower() == "dança de salão")
            .ToListAsync();

        // Cada presença de salão conta como 1 aula
        int aulasSalao = presencasSalao.Count;

        int totalAulas = aulasSolo + aulasSalao;
        double horasCumpridas = Math.Round(totalAulas * minutosPoraula / 60.0, 1);
        double metaHoras = Math.Round(metaAulas * minutosPoraula / 60.0, 1); // = 5,0h

        return new RelatorioHorasBolsistaResponse(
            _hashids.Encode(bolsista.Id),
            bolsista.Nome,
            horasCumpridas,
            Math.Max(0, Math.Round(metaHoras - horasCumpridas, 1)),
            totalAulas >= metaAulas);
    }
}
