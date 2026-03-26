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

        // Seção B — parte 1: participantes extras já salvos para esta data
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

        // Seção B — parte 2: pré-popula alunos com Reposição agendada para esta turma/data.
        // Esses alunos ainda não têm RegistroPresenca salvo (seriam incluídos na parte 1 se
        // tivessem), por isso buscamos explicitamente para facilitar o trabalho do professor.
        var idsJaNaSecaoB = extrasResponse.Select(e => _hashids.Decode(e.AlunoIdHash)[0]).ToHashSet();

        var reposicoesDoDia = await _context.Reposicoes
            .Include(r => r.Aluno)
            .Where(r =>
                r.TurmaDestinoId == turmaId &&
                r.Status == "Agendada" &&
                DateOnly.FromDateTime(r.DataReposicaoAgendada) == dataAula &&
                !idsMatriculados.Contains(r.AlunoId) &&
                !idsJaNaSecaoB.Contains(r.AlunoId))
            .ToListAsync();

        foreach (var rep in reposicoesDoDia)
        {
            extrasResponse.Add(new AlunoChamadaResponse(
                _hashids.Encode(rep.AlunoId),
                rep.Aluno.Nome,
                rep.Aluno.FotoUrl,
                "Reposição",
                false,   // presença padrão = false; professor confirma na tela
                null
            ));
        }

        return new ObterChamadaResponse(
            _hashids.Encode(turma.Id),
            dataAula,
            alunosResponse,
            extrasResponse
        );
    }

    // ──────────────────────────────────────────────────────────────
    // BUSCAR PARTICIPANTES EXTRAS (Seção B)
    //
    // Busca usuários que podem ser adicionados manualmente à chamada:
    //   1. Bolsistas ativos cujo nome ou CPF contenha o termo buscado
    //   2. Alunos com AulaExperimental ativa para ESTA turma (nome ou CPF)
    //   3. Alunos com Reposição agendada para ESTA turma (nome ou CPF)
    //
    // BUSCA POR CPF: o termo pode vir formatado ("123.456.789-01") ou só
    // dígitos ("12345678901"). Normalizamos para dígitos antes de comparar
    // com o CPF armazenado no banco (que também fica só com dígitos).
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

        // Normaliza o termo para dígitos (busca por CPF).
        // Se o usuário digitou "123.456.789-01", buscamos "12345678901".
        var termoCpf = new string(termoBusca.Where(char.IsDigit).ToArray());
        bool buscaPorCpf = termoCpf.Length >= 3; // mínimo de 3 dígitos para CPF ser útil

        // ── 1. Bolsistas ativos (nome ou CPF) ─────────────────────────
        var bolsistas = await _context.Usuarios
            .OfType<Bolsista>()
            .Where(b =>
                b.Ativo &&
                !idsMatriculados.Contains(b.Id) &&
                (b.Nome.ToLower().Contains(termoLower) ||
                 (buscaPorCpf && b.Cpf != null && b.Cpf.Contains(termoCpf))))
            .Select(b => new ParticipanteExtraResponse(
                _hashids.Encode(b.Id),
                b.Nome,
                b.FotoUrl,
                "Bolsista"
            ))
            .Take(10)
            .ToListAsync();

        resultados.AddRange(bolsistas);

        // ── 2. Alunos com AulaExperimental ativa para esta turma ──────
        // Status Pendente ou Confirmada = aula ainda válida para ocorrer
        var idsJaEncontrados = resultados.Select(r => _hashids.Decode(r.UsuarioIdHash)[0]).ToHashSet();

        var experimentais = await _context.AulasExperimentais
            .Include(a => a.Aluno)
            .Where(a =>
                a.TurmaId == turmaId &&
                (a.Status == "Pendente" || a.Status == "Confirmada") &&
                !idsMatriculados.Contains(a.AlunoId) &&
                (a.Aluno.Nome.ToLower().Contains(termoLower) ||
                 (buscaPorCpf && a.Aluno.Cpf != null && a.Aluno.Cpf.Contains(termoCpf))))
            .Select(a => new ParticipanteExtraResponse(
                _hashids.Encode(a.AlunoId),
                a.Aluno.Nome,
                a.Aluno.FotoUrl,
                "Experimental"
            ))
            .Take(10)
            .ToListAsync();

        foreach (var exp in experimentais)
        {
            var expId = _hashids.Decode(exp.UsuarioIdHash)[0];
            if (!idsJaEncontrados.Contains(expId))
            {
                resultados.Add(exp);
                idsJaEncontrados.Add(expId);
            }
        }

        // ── 3. Alunos com Reposição agendada para esta turma ──────────
        // Inclui alunos de qualquer turma de origem que agendaram reposição
        // nesta turma destino e cuja reposição ainda está pendente.
        var reposicoes = await _context.Reposicoes
            .Include(r => r.Aluno)
            .Where(r =>
                r.TurmaDestinoId == turmaId &&
                r.Status == "Agendada" &&
                !idsMatriculados.Contains(r.AlunoId) &&
                (r.Aluno.Nome.ToLower().Contains(termoLower) ||
                 (buscaPorCpf && r.Aluno.Cpf != null && r.Aluno.Cpf.Contains(termoCpf))))
            .Select(r => new ParticipanteExtraResponse(
                _hashids.Encode(r.AlunoId),
                r.Aluno.Nome,
                r.Aluno.FotoUrl,
                "Reposição"
            ))
            .Take(10)
            .ToListAsync();

        foreach (var rep in reposicoes)
        {
            var repId = _hashids.Decode(rep.UsuarioIdHash)[0];
            if (!idsJaEncontrados.Contains(repId))
            {
                resultados.Add(rep);
                idsJaEncontrados.Add(repId);
            }
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

        // ── Processa EXTRAS (Seção B: bolsistas/experimentais/reposições) ────
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

            // IDs dos extras marcados como presentes nesta chamada
            // (necessário para auto-marcar reposições como Realizadas)
            var extrasPresentes = new HashSet<int>();

            foreach (var (alunoId, item) in extraIdsMap)
            {
                if (!usuariosExtrasValidos.Contains(alunoId)) continue;

                var registroExistente = presencasExistentes.FirstOrDefault(rp => rp.AlunoId == alunoId);
                if (registroExistente != null)
                    registroExistente.AtualizarPresenca(item.Presente, item.Observacao);
                else
                    _context.RegistrosPresencas.Add(
                        new RegistroPresenca(turmaId, alunoId, request.DataAula, item.Presente, item.Observacao));

                if (item.Presente)
                    extrasPresentes.Add(alunoId);
            }

            // ── Auto-marca Reposições como Realizadas ──────────────────────────
            // Quando um aluno extra é marcado como PRESENTE e tem uma Reposição
            // agendada para esta turma/data, a reposição é automaticamente concluída.
            // Isso evita que o professor precise fazer essa marcação manualmente.
            if (extrasPresentes.Count > 0)
            {
                var reposicoesParaRealizar = await _context.Reposicoes
                    .Where(r =>
                        r.TurmaDestinoId == turmaId &&
                        r.Status == "Agendada" &&
                        DateOnly.FromDateTime(r.DataReposicaoAgendada) == request.DataAula &&
                        extrasPresentes.Contains(r.AlunoId))
                    .ToListAsync();

                foreach (var rep in reposicoesParaRealizar)
                    rep.MarcarRealizada();
            }
        }

        await _context.SaveChangesAsync();
    }
}