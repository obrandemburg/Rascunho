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

    // ==========================================
    // 1. GESTÃO DO BOLSISTA
    // ==========================================

    public async Task DefinirDiasObrigatoriosAsync(int bolsistaId, int dia1, int dia2)
    {
        var bolsista = await _context.Usuarios.OfType<Bolsista>().FirstOrDefaultAsync(b => b.Id == bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        bolsista.DefinirDiasObrigatorios((DayOfWeek)dia1, (DayOfWeek)dia2);
        await _context.SaveChangesAsync();
    }

    public async Task AdicionarHabilidadeAsync(int bolsistaId, AdicionarHabilidadeRequest request)
    {
        var ritmoDecoded = _hashids.Decode(request.RitmoIdHash);
        if (ritmoDecoded.Length == 0) throw new RegraNegocioException("Ritmo inválido.");

        int ritmoId = ritmoDecoded[0];

        // Verifica se já tem esse ritmo cadastrado
        bool jaPossui = await _context.Set<HabilidadeUsuario>()
            .AnyAsync(h => h.UsuarioId == bolsistaId && h.RitmoId == ritmoId);

        if (jaPossui) throw new RegraNegocioException("O bolsista já possui este ritmo cadastrado nas suas habilidades.");

        var habilidade = new HabilidadeUsuario
        {
            UsuarioId = bolsistaId,
            RitmoId = ritmoId,
            PapelDominante = request.PapelDominante,
            Nivel = request.Nivel
        };

        _context.Set<HabilidadeUsuario>().Add(habilidade);
        await _context.SaveChangesAsync();
    }

    // ==========================================
    // 2. INTELIGÊNCIA E AUTOMAÇÃO
    // ==========================================

    public async Task<SugestaoBalanceamentoResponse> AnalisarEBalancearTurmaAsync(int turmaId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        // 1. Conta quem é quem na turma
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

        // 2. Se a turma estiver desbalanceada, procuramos os heróis (Bolsistas)
        if (quantidadeFaltante > 0)
        {
            // Busca bolsistas que dominam o ritmo E o papel necessário (ou que fazem "Ambos")
            var bolsistasQualificados = await _context.Set<HabilidadeUsuario>()
                .Include(h => h.Usuario)
                .Where(h => h.RitmoId == turma.RitmoId && h.Usuario.Tipo == "Bolsista" && h.Usuario.Ativo)
                .Where(h => h.PapelDominante == papelNecessario || h.PapelDominante == "Ambos")
                .ToListAsync();

            // 3. Aplica o filtro do Paradoxo do Espaço-Tempo (O bolsista já tem aula nesse horário?)
            foreach (var hab in bolsistasQualificados)
            {
                bool choqueHorario = await _context.Matriculas
                    .Include(m => m.Turma)
                    .AnyAsync(m =>
                        m.AlunoId == hab.UsuarioId &&
                        m.Turma.Ativa &&
                        m.Turma.DiaDaSemana == turma.DiaDaSemana &&
                        (turma.HorarioInicio < m.Turma.HorarioFim && turma.HorarioFim > m.Turma.HorarioInicio));

                if (!choqueHorario)
                {
                    sugestoes.Add(new BolsistaSugerido(
                        _hashids.Encode(hab.UsuarioId),
                        hab.Usuario.Nome,
                        hab.PapelDominante,
                        hab.Nivel
                    ));
                }
            }
        }

        return new SugestaoBalanceamentoResponse(
            _hashids.Encode(turma.Id), condutores, conduzidos, status, quantidadeFaltante, sugestoes);
    }

    public async Task<RelatorioHorasBolsistaResponse> RelatorioHorasSemanaisAsync(int bolsistaId)
    {
        var bolsista = await _context.Usuarios.FindAsync(bolsistaId)
            ?? throw new RegraNegocioException("Bolsista não encontrado.");

        // Pega as turmas ativas que o bolsista está matriculado
        var matriculas = await _context.Matriculas
            .Include(m => m.Turma)
            .Where(m => m.AlunoId == bolsistaId && m.Turma.Ativa)
            .ToListAsync();

        double totalHoras = 0;
        foreach (var mat in matriculas)
        {
            // Calcula a duração de cada aula na semana
            totalHoras += (mat.Turma.HorarioFim - mat.Turma.HorarioInicio).TotalHours;
        }

        return new RelatorioHorasBolsistaResponse(
            _hashids.Encode(bolsista.Id),
            bolsista.Nome,
            Math.Round(totalHoras, 1),
            Math.Max(0, 6 - totalHoras),
            totalHoras >= 6
        );
    }
}