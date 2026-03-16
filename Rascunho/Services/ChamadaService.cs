using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.DTOs;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class ChamadaService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public ChamadaService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    // 1. MÉTODO DE LEITURA (Monta a lista para o aplicativo do professor)
    public async Task<ObterChamadaResponse> ObterListaParaChamadaAsync(int turmaId, DateOnly dataAula)
    {
        // Passo A: Pega a turma e quem está matriculado nela
        var turma = await _context.Turmas
            .Include(t => t.Matriculas).ThenInclude(m => m.Aluno)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        // Passo B: Pega se já existe alguma presença salva NESTE DIA específico
        var presencasSalvas = await _context.RegistrosPresencas
            .Where(rp => rp.TurmaId == turmaId && rp.DataAula == dataAula)
            .ToDictionaryAsync(rp => rp.AlunoId, rp => rp.Presente);

        var alunosResponse = new List<AlunoChamadaResponse>();

        // Passo C: Monta a lista final mesclando os dados
        foreach (var matricula in turma.Matriculas)
        {
            // Se o aluno já tem registro salvo, usa o que tá no banco. Se não, é Falta (false).
            bool estaPresente = presencasSalvas.ContainsKey(matricula.AlunoId) && presencasSalvas[matricula.AlunoId];

            alunosResponse.Add(new AlunoChamadaResponse(
                _hashids.Encode(matricula.AlunoId),
                matricula.Aluno.Nome,
                matricula.Aluno.FotoUrl,
                matricula.Papel,
                estaPresente
            ));
        }

        // Ordena alfabeticamente para o professor achar fácil no celular
        alunosResponse = alunosResponse.OrderBy(a => a.Nome).ToList();

        return new ObterChamadaResponse(_hashids.Encode(turma.Id), dataAula, alunosResponse);
    }


    // 2. MÉTODO DE ESCRITA (Quando o professor clica em "Salvar Chamada")
    public async Task RegistrarChamadaAsync(int turmaId, int usuarioLogadoId, string roleLogado, RegistrarChamadaRequest request)
    {
        var turma = await _context.Turmas
            .Include(t => t.Professores)
            .Include(t => t.Matriculas) // Precisamos disso para a trava de segurança
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        // TRAVA 1: Só o professor DESTA turma (ou a gerência) pode fazer a chamada
        if (roleLogado == "Professor" || roleLogado == "Assistente")
        {
            if (!turma.Professores.Any(p => p.ProfessorId == usuarioLogadoId))
                throw new RegraNegocioException("Você não tem permissão para fazer a chamada desta turma.");
        }

        // Pega as presenças que já estão no banco NESTE DIA (para sabermos se vamos fazer Insert ou Update)
        var presencasExistentes = await _context.RegistrosPresencas
            .Where(rp => rp.TurmaId == turmaId && rp.DataAula == request.DataAula)
            .ToListAsync();

        // Pega a lista de IDs dos alunos que realmente pertencem a essa turma
        var idsAlunosMatriculados = turma.Matriculas.Select(m => m.AlunoId).ToList();

        // Faz o loop na lista que veio do celular do professor
        foreach (var item in request.Presencas)
        {
            var alunoDecoded = _hashids.Decode(item.AlunoIdHash);
            if (alunoDecoded.Length == 0) continue;

            int alunoId = alunoDecoded[0];

            // TRAVA DE SEGURANÇA 2: O aluno que veio na requisição está matriculado na turma?
            if (!idsAlunosMatriculados.Contains(alunoId)) continue; // Se não estiver, ignora esse hacker

            // Padrão UPSERT (Update ou Insert)
            var registoExistente = presencasExistentes.FirstOrDefault(rp => rp.AlunoId == alunoId);

            if (registoExistente != null)
            {
                // UPDATE: O registro já existe. Apenas atualizamos (caso ele tenha mudado de falta pra presença)
                registoExistente.AtualizarPresenca(item.Presente);
            }
            else
            {
                // INSERT: O registro não existe ainda no banco para esse dia. Criamos um novo.
                _context.RegistrosPresencas.Add(new RegistroPresenca(turmaId, alunoId, request.DataAula, item.Presente));
            }
        }

        await _context.SaveChangesAsync();
    }
}