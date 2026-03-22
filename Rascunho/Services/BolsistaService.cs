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

        if (jaPossui) throw new RegraNegocioException("O bolsista já possui este ritmo nas suas habilidades.");

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
    // NOVO Sprint 2: MEU DESEMPENHO
    //
    // Como funciona o cálculo:
    //   1. Busca TODOS os RegistroPresenca do bolsista (qualquer turma)
    //   2. Classifica cada registro como "dia obrigatório" ou "dia extra"
    //      comparando o DayOfWeek da DataAula com DiaObrigatorio1/2
    //   3. Calcula percentuais separados
    //   4. Determina o indicador de situação (baseado só no obrigatório)
    //   5. Monta histórico cronológico aula-a-aula
    //
    // IMPORTANTE: O bolsista não se matricula formalmente em turmas de salão
    // (RN-BOL09), mas RegistroPresenca pode existir para qualquer turma.
    // Por isso usamos RegistroPresenca como fonte de verdade, não Matriculas.
    // ──────────────────────────────────────────────────────────────
    public async Task<DesempenhoResponse> MeuDesempenhoAsync(int bolsistaId)
    {
        var bolsista = await _context.Usuarios.OfType<Bolsista>()
            .FirstOrDefaultAsync(b => b.Id == bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        // Uma única query com todos os includes necessários para o histórico
        var todasPresencas = await _context.RegistrosPresencas
            .Include(rp => rp.Turma).ThenInclude(t => t.Ritmo)
            .Include(rp => rp.Turma).ThenInclude(t => t.Professores).ThenInclude(tp => tp.Professor)
            .Where(rp => rp.AlunoId == bolsistaId)
            .OrderByDescending(rp => rp.DataAula)
            .ToListAsync();

        // Monta a lista de dias obrigatórios para comparação eficiente
        var diasObrigatorios = new HashSet<DayOfWeek>();
        if (bolsista.DiaObrigatorio1.HasValue) diasObrigatorios.Add(bolsista.DiaObrigatorio1.Value);
        if (bolsista.DiaObrigatorio2.HasValue) diasObrigatorios.Add(bolsista.DiaObrigatorio2.Value);

        // Separa em memória — sem query adicional
        var presencasObrigatorias = todasPresencas
            .Where(rp => diasObrigatorios.Contains(rp.DataAula.DayOfWeek))
            .ToList();

        var presencasExtras = todasPresencas
            .Where(rp => !diasObrigatorios.Contains(rp.DataAula.DayOfWeek))
            .ToList();

        // Calcula percentuais (0% se não houver registros ainda)
        double freqObrigatoria = presencasObrigatorias.Count > 0
            ? (double)presencasObrigatorias.Count(p => p.Presente) / presencasObrigatorias.Count * 100
            : 0;

        double freqExtra = presencasExtras.Count > 0
            ? (double)presencasExtras.Count(p => p.Presente) / presencasExtras.Count * 100
            : 0;

        // Indicador de situação baseado na frequência obrigatória
        // Thresholds definidos no planejamento MVP v3
        string indicador = freqObrigatoria switch
        {
            >= 85 => "Excelente",
            >= 75 => "Vamos melhorar",
            >= 60 => "Atenção",
            _ => "Crítico"
        };

        // Caso especial: sem registros ainda → situação neutra
        if (!todasPresencas.Any())
            indicador = "Sem registros";

        // Monta histórico com todas as presenças ordenadas por data
        var historico = todasPresencas.Select(rp => new HistoricoPresencaItem(
            rp.DataAula,
            rp.Turma?.Ritmo?.Nome ?? "Turma desconhecida",
            // Pega o primeiro professor da turma (turmas geralmente têm 1 professor)
            rp.Turma?.Professores?.FirstOrDefault()?.Professor?.Nome ?? "Professor desconhecido",
            rp.Presente,
            diasObrigatorios.Contains(rp.DataAula.DayOfWeek)
        )).ToList();

        return new DesempenhoResponse(
            _hashids.Encode(bolsista.Id),
            bolsista.Nome,
            bolsista.DiaObrigatorio1.HasValue ? (int?)bolsista.DiaObrigatorio1.Value : null,
            bolsista.DiaObrigatorio2.HasValue ? (int?)bolsista.DiaObrigatorio2.Value : null,
            Math.Round(freqObrigatoria, 1),
            indicador,
            presencasObrigatorias.Count,
            presencasObrigatorias.Count(p => p.Presente),
            Math.Round(freqExtra, 1),
            presencasExtras.Count,
            presencasExtras.Count(p => p.Presente),
            historico
        );
    }

    public async Task<IEnumerable<SugestaoBalanceamentoResponse>> TurmasRecomendadasParaBolsistaAsync(int bolsistaId)
    {
        var bolsista = await _context.Usuarios.OfType<Bolsista>()
            .FirstOrDefaultAsync(b => b.Id == bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        if (!bolsista.DiaObrigatorio1.HasValue || !bolsista.DiaObrigatorio2.HasValue)
            return Enumerable.Empty<SugestaoBalanceamentoResponse>();

        var diasObrigatorios = new[] { bolsista.DiaObrigatorio1.Value, bolsista.DiaObrigatorio2.Value };

        var turmasDoDia = await _context.Turmas
            .Include(t => t.Matriculas)
            .Where(t => t.Ativa && diasObrigatorios.Contains(t.DiaDaSemana))
            .ToListAsync();

        var resultados = new List<SugestaoBalanceamentoResponse>();
        foreach (var turma in turmasDoDia)
        {
            var analise = await AnalisarEBalancearTurmaAsync(turma.Id);
            resultados.Add(analise);
        }

        return resultados
            .OrderBy(r => r.Status == "Balanceada" ? 1 : 0)
            .ThenByDescending(r => r.QuantidadeFaltante);
    }

    public async Task<SugestaoBalanceamentoResponse> AnalisarEBalancearTurmaAsync(int turmaId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        int condutores = turma.Matriculas.Count(m => m.Papel == "Condutor");
        int conduzidos = turma.Matriculas.Count(m => m.Papel == "Conduzido");

        string status = "Balanceada";
        string papelNecessario = "";
        int quantidadeFaltante = 0;

        if (condutores > conduzidos)
        {
            status = "Faltam Conduzidos";
            papelNecessario = "Conduzido";
            quantidadeFaltante = condutores - conduzidos;
        }
        else if (conduzidos > condutores)
        {
            status = "Faltam Condutores";
            papelNecessario = "Condutor";
            quantidadeFaltante = conduzidos - condutores;
        }

        var sugestoes = new List<BolsistaSugerido>();
        if (quantidadeFaltante > 0)
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
            _hashids.Encode(turma.Id), condutores, conduzidos, status, quantidadeFaltante, sugestoes);
    }

    // ──────────────────────────────────────────────────────────────────────
    // RELATÓRIO DE HORAS SEMANAIS — CORRIGIDO (RN-BOL09)
    //
    // PROBLEMA ANTERIOR: usava apenas Matriculas para calcular horas.
    // Bolsistas em turmas de SALÃO não têm Matricula formal (RN-BOL09),
    // então suas horas de salão nunca eram contabilizadas. Bug crítico.
    //
    // CORREÇÃO:
    //   → Horas de turmas SOLO: via Matriculas (bolsista é formalmente matriculado)
    //   → Horas de turmas de SALÃO: via RegistroPresença da semana atual
    //     (bolsista frequenta sem matrícula; a chamada registra a presença)
    // ──────────────────────────────────────────────────────────────────────
    public async Task<RelatorioHorasBolsistaResponse> RelatorioHorasSemanaisAsync(int bolsistaId)
    {
        var bolsista = await _context.Usuarios.FindAsync(bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        // ── 1. Horas de turmas SOLO (via Matriculas) ─────────────────
        // Bolsista SE MATRICULA em turmas de dança solo — as horas vêm da grade
        var matriculasSolo = await _context.Matriculas
            .Include(m => m.Turma).ThenInclude(t => t.Ritmo)
            .Where(m => m.AlunoId == bolsistaId &&
                        m.Turma.Ativa &&
                        m.Turma.Ritmo.Modalidade.ToLower() == "dança solo")
            .ToListAsync();

        // Soma a duração de cada turma solo (HorarioFim - HorarioInicio em horas)
        double horasSolo = matriculasSolo.Sum(m =>
            (m.Turma.HorarioFim - m.Turma.HorarioInicio).TotalHours);

        // ── 2. Horas de turmas de SALÃO (via RegistroPresença) ───────
        // Bolsista NÃO se matricula em salão — presença é via chamada do professor.
        // Contamos apenas a semana corrente para mostrar o progresso atual.
        //
        // "Início da semana" = domingo anterior ao dia de hoje.
        // DayOfWeek.Sunday = 0, então subtrai 0 a 6 dias para achar o domingo.
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);   // retrocede ao Domingo
        var fimSemana = inicioSemana.AddDays(7);                  // próximo Domingo (exclusive)

        var presencasSalao = await _context.RegistrosPresencas
            .Include(rp => rp.Turma).ThenInclude(t => t.Ritmo)
            .Where(rp => rp.AlunoId == bolsistaId &&
                         rp.Presente &&                           // só presença confirmada
                         rp.DataAula >= inicioSemana &&
                         rp.DataAula < fimSemana &&
                         rp.Turma.Ritmo.Modalidade.ToLower() == "dança de salão")
            .ToListAsync();

        double horasSalao = presencasSalao.Sum(rp =>
            (rp.Turma.HorarioFim - rp.Turma.HorarioInicio).TotalHours);

        // ── 3. Total e cálculo de meta ────────────────────────────────
        double totalHoras = horasSolo + horasSalao;
        const double metaSemanal = 6.0;

        return new RelatorioHorasBolsistaResponse(
            _hashids.Encode(bolsista.Id),
            bolsista.Nome,
            Math.Round(totalHoras, 1),
            Math.Max(0, metaSemanal - totalHoras),
            totalHoras >= metaSemanal);
    }
}