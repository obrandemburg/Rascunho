using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Shared.DTOs; // <-- NOVO NAMESPACE AQUI
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Mappers;       // <-- IMPORTANDO OS SEUS MAPPERS

namespace Rascunho.Services;

public class AulaExperimentalService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public AulaExperimentalService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterAulaExperimentalResponse> SolicitarAulaAsync(int alunoId, SolicitarAulaExperimentalRequest request)
    {
        var turmaDecoded = _hashids.Decode(request.TurmaIdHash);
        if (turmaDecoded.Length == 0) throw new RegraNegocioException("ID da turma inválido.");
        int turmaId = turmaDecoded[0];

        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        if (turma.Matriculas.Any(m => m.AlunoId == alunoId))
            throw new RegraNegocioException("Você já é um aluno matriculado nesta turma.");

        if (turma.Matriculas.Count >= turma.LimiteAlunos)
            throw new RegraNegocioException("Esta turma está cheia.");

        bool jaFezNesteRitmo = await _context.AulasExperimentais
            .Include(a => a.Turma)
            .AnyAsync(a => a.AlunoId == alunoId && a.Turma.RitmoId == turma.RitmoId && a.Status != "Cancelada");

        if (jaFezNesteRitmo)
            throw new RegraNegocioException("Você já solicitou ou realizou uma aula experimental para este ritmo.");

        // ==========================================
        // CRIAÇÃO DA ENTIDADE LIMPA
        // ==========================================
        var experimental = new AulaExperimental(alunoId, turmaId, request.DataAula);

        _context.AulasExperimentais.Add(experimental);
        await _context.SaveChangesAsync();

        await _context.Entry(experimental).Reference(a => a.Aluno).LoadAsync();
        await _context.Entry(experimental).Reference(a => a.Turma).LoadAsync();
        await _context.Entry(experimental.Turma).Reference(t => t.Ritmo).LoadAsync();

        // ==========================================
        // USO DO MAPPER INTELIGENTE
        // ==========================================
        return experimental.ToResponse(_hashids);
    }

    public async Task AlterarStatusAsync(int experimentalId, string novoStatus)
    {
        var aula = await _context.AulasExperimentais.FindAsync(experimentalId)
            ?? throw new RegraNegocioException("Aula experimental não encontrada.");

        switch (novoStatus)
        {
            case "Confirmada": aula.Confirmar(); break;
            case "Cancelada": aula.Cancelar(); break;
            case "Realizada": aula.MarcarComoRealizada(); break;
            default: throw new RegraNegocioException("Status inválido.");
        }

        await _context.SaveChangesAsync();
    }
}