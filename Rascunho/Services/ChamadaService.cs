// ARQUIVO: Rascunho/Services/ChamadaService.cs
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Shared.DTOs;
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

    // ──────────────────────────────────────────────────────────────
    // OBTER LISTA PARA CHAMADA
    //
    // MODIFICADO Sprint 2:
    // 1. Carrega RegistroPresenca com Include(Aluno) para ter dados do extra
    // 2. Retorna Observacao de cada aluno
    // 3. Retorna Extras: participantes não matriculados que já têm
    //    RegistroPresenca salvo para esta data (professor já fez chamada antes)
    // ──────────────────────────────────────────────────────────────
    public async Task<ObterChamadaResponse> ObterListaParaChamadaAsync(int turmaId, DateOnly dataAula)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas).ThenInclude(m => m.Aluno)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        // MODIFICADO: carrega o Aluno junto (necessário para extras)
        // e armazena o RegistroPresenca completo (não só o bool)
        var presencasDict = await _context.RegistrosPresencas
            .Include(rp => rp.Aluno)
            .Where(rp => rp.TurmaId == turmaId && rp.DataAula == dataAula)
            .ToDictionaryAsync(rp => rp.AlunoId);

        // IDs dos alunos formalmente matriculados
        var idsMatriculados = turma.Matriculas.Select(m => m.AlunoId).ToHashSet();

        // Seção A: alunos matriculados formalmente
        var alunosResponse = turma.Matriculas
            .Select(matricula =>
            {
                presencasDict.TryGetValue(matricula.AlunoId, out var reg);
                return new AlunoChamadaResponse(
                    _hashids.Encode(matricula.AlunoId),
                    matricula.Aluno.Nome,
                    matricula.Aluno.FotoUrl,
                    matricula.Papel,
                    reg?.Presente ?? false,
                    reg?.Observacao            // null se nenhuma observação registrada
                );
            })
            .OrderBy(a => a.Nome)
            .ToList();

        // Seção B: participantes extras já salvos para esta data
        // (bolsistas/experimentais que o professor adicionou em sessões anteriores)
        var extrasResponse = presencasDict
            .Where(kv => !idsMatriculados.Contains(kv.Key))
            .Select(kv => new AlunoChamadaResponse(
                _hashids.Encode(kv.Key),
                kv.Value.Aluno?.Nome ?? "Desconhecido",
                kv.Value.Aluno?.FotoUrl ?? "",
                "Extra",       // não têm papel formal na turma
                kv.Value.Presente,
                kv.Value.Observacao
            ))
            .ToList();

        return new ObterChamadaResponse(
            _hashids.Encode(turma.Id),
            dataAula,
            alunosResponse,
            extrasResponse
        );
    }

    // ──────────────────────────────────────────────────────────────
    // NOVO Sprint 2: BUSCAR PARTICIPANTES EXTRAS (Seção B)
    //
    // Busca usuários que podem ser adicionados manualmente à chamada:
    //   1. Bolsistas ativos cujo nome contenha o termo buscado
    //   2. Alunos com AulaExperimental ativa para ESTA turma
    //
    // Reposição será incluída na Sprint 3, quando ReposicaoService existir.
    //
    // SEGURANÇA: Exclui quem já está matriculado formalmente na turma
    // (não faz sentido adicionar na Seção B quem já está na Seção A)
    // ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ParticipanteExtraResponse>> BuscarParticipantesExtrasAsync(
        int turmaId,
        string termoBusca)
    {
        // IDs já matriculados — para excluir do resultado
        var idsMatriculados = await _context.Matriculas
            .Where(m => m.TurmaId == turmaId)
            .Select(m => m.AlunoId)
            .ToHashSetAsync();

        var resultados = new List<ParticipanteExtraResponse>();
        var termoLower = termoBusca.ToLower().Trim();

        // 1. Bolsistas ativos que correspondem ao termo
        var bolsistas = await _context.Usuarios
            .OfType<Bolsista>()
            .Where(b =>
                b.Ativo &&
                b.Nome.ToLower().Contains(termoLower) &&
                !idsMatriculados.Contains(b.Id))
            .Select(b => new ParticipanteExtraResponse(
                _hashids.Encode(b.Id),
                b.Nome,
                b.FotoUrl,
                "Bolsista"
            ))
            .Take(10)
            .ToListAsync();

        resultados.AddRange(bolsistas);

        // 2. Alunos com AulaExperimental ativa para esta turma
        // Status Pendente ou Confirmada = aula ainda válida para ocorrer
        var idsJaEncontrados = resultados.Select(r => r.UsuarioIdHash).ToHashSet();

        var experimentais = await _context.AulasExperimentais
            .Include(a => a.Aluno)
            .Where(a =>
                a.TurmaId == turmaId &&
                (a.Status == "Pendente" || a.Status == "Confirmada") &&
                a.Aluno.Nome.ToLower().Contains(termoLower) &&
                !idsMatriculados.Contains(a.AlunoId))
            .Select(a => new ParticipanteExtraResponse(
                _hashids.Encode(a.AlunoId),
                a.Aluno.Nome,
                a.Aluno.FotoUrl,
                "Experimental"
            ))
            .Take(10)
            .ToListAsync();

        // Adiciona apenas experimentais que NÃO são bolsistas já encontrados
        // (um usuário não pode ser Bolsista E ter AulaExperimental, mas por segurança filtramos)
        foreach (var exp in experimentais)
        {
            if (!idsJaEncontrados.Contains(exp.UsuarioIdHash))
                resultados.Add(exp);
        }

        return resultados.OrderBy(r => r.Nome);
    }

    // ──────────────────────────────────────────────────────────────
    // REGISTRAR CHAMADA
    //
    // MODIFICADO Sprint 2:
    // 1. Aceita Observacao em cada AlunoPresencaRequest
    // 2. Aceita ExtrasPresencas (lista separada, sem checagem de matrícula)
    // 3. CORREÇÃO: removida referência a role "Assistente" (tipo inexistente)
    // 4. Otimização: validação de extras em batch (sem N+1)
    // ──────────────────────────────────────────────────────────────
    public async Task RegistrarChamadaAsync(
        int turmaId,
        int usuarioLogadoId,
        string roleLogado,
        RegistrarChamadaRequest request)
    {
        var turma = await _context.Turmas
            .Include(t => t.Professores)
            .Include(t => t.Matriculas)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        // RN-CHA04: Só o professor desta turma (ou staff) pode registrar chamada
        // CORREÇÃO: removido "Assistente" — tipo inexistente no sistema
        if (roleLogado == "Professor")
        {
            if (!turma.Professores.Any(p => p.ProfessorId == usuarioLogadoId))
                throw new RegraNegocioException("Você não tem permissão para fazer a chamada desta turma.");
        }

        // Busca registros já existentes para este dia (para o UPSERT)
        var presencasExistentes = await _context.RegistrosPresencas
            .Where(rp => rp.TurmaId == turmaId && rp.DataAula == request.DataAula)
            .ToListAsync();

        var idsAlunosMatriculados = turma.Matriculas.Select(m => m.AlunoId).ToHashSet();

        // ── Processa alunos MATRICULADOS (Seção A) ────────────────
        foreach (var item in request.Presencas)
        {
            var alunoDecoded = _hashids.Decode(item.AlunoIdHash);
            if (alunoDecoded.Length == 0) continue;
            int alunoId = alunoDecoded[0];

            // Segurança: ignora IDs que não pertencem à turma (proteção contra manipulação)
            if (!idsAlunosMatriculados.Contains(alunoId)) continue;

            var registroExistente = presencasExistentes.FirstOrDefault(rp => rp.AlunoId == alunoId);
            if (registroExistente != null)
                registroExistente.AtualizarPresenca(item.Presente, item.Observacao);
            else
                _context.RegistrosPresencas.Add(
                    new RegistroPresenca(turmaId, alunoId, request.DataAula, item.Presente, item.Observacao));
        }

        // ── Processa EXTRAS (Seção B: bolsistas/experimentais) ────
        if (request.ExtrasPresencas?.Any() == true)
        {
            // Decodifica todos os IDs de uma vez (evita N+1)
            var extraIdsMap = request.ExtrasPresencas
                .Select(ep => new { Request = ep, Decoded = _hashids.Decode(ep.AlunoIdHash) })
                .Where(x => x.Decoded.Length > 0)
                .ToDictionary(x => x.Decoded[0], x => x.Request);

            // Uma única query para validar quais usuários existem e estão ativos
            var usuariosExtrasValidos = await _context.Usuarios
                .Where(u => extraIdsMap.Keys.Contains(u.Id) && u.Ativo)
                .Select(u => u.Id)
                .ToHashSetAsync();

            foreach (var (alunoId, item) in extraIdsMap)
            {
                if (!usuariosExtrasValidos.Contains(alunoId)) continue;

                var registroExistente = presencasExistentes.FirstOrDefault(rp => rp.AlunoId == alunoId);
                if (registroExistente != null)
                    registroExistente.AtualizarPresenca(item.Presente, item.Observacao);
                else
                    _context.RegistrosPresencas.Add(
                        new RegistroPresenca(turmaId, alunoId, request.DataAula, item.Presente, item.Observacao));
            }
        }

        await _context.SaveChangesAsync();
    }
}