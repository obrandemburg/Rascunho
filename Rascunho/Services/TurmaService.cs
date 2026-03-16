using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class TurmaService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public TurmaService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterTurmaResponse> CriarTurmaAsync(CriarTurmaRequest request)
    {
        var salaDecoded = _hashids.Decode(request.SalaIdHash);
        var ritmoDecoded = _hashids.Decode(request.RitmoIdHash);

        if (salaDecoded.Length == 0 || ritmoDecoded.Length == 0)
            throw new RegraNegocioException("ID de Sala ou Ritmo inválido.");

        int salaIdReal = salaDecoded[0];
        int ritmoIdReal = ritmoDecoded[0];

        var professoresIdsReais = new List<int>();
        foreach (var profHash in request.ProfessoresIdsHash)
        {
            var profDecoded = _hashids.Decode(profHash);
            if (profDecoded.Length == 0) throw new RegraNegocioException($"O ID de professor '{profHash}' é inválido.");
            professoresIdsReais.Add(profDecoded[0]);
        }

        var sala = await _context.Salas.FindAsync(salaIdReal) ?? throw new RegraNegocioException("Sala não encontrada.");

        if (request.LimiteAlunos > sala.CapacidadeMaxima)
            throw new RegraNegocioException($"O limite de alunos ({request.LimiteAlunos}) excede a capacidade máxima da {sala.Nome} ({sala.CapacidadeMaxima}).");

        var diaDaSemanaEnum = (DayOfWeek)request.DiaDaSemana;

        bool choqueSala = await _context.Turmas.AnyAsync(t =>
            t.SalaId == salaIdReal &&
            t.DiaDaSemana == diaDaSemanaEnum &&
            t.Ativa &&
            (request.HorarioInicio < t.HorarioFim && request.HorarioFim > t.HorarioInicio));

        if (choqueSala)
            throw new RegraNegocioException("Já existe uma turma ativa nesta sala, neste mesmo dia e horário.");

        foreach (var profId in professoresIdsReais)
        {
            var professor = await _context.Usuarios.FindAsync(profId);
            if (professor == null || (professor.Tipo != "Professor" && professor.Tipo != "Assistente"))
                throw new RegraNegocioException($"O usuário '{professor?.Nome}' não tem permissão para ser professor da turma.");

            bool choqueProfessor = await _context.TurmaProfessores
                .Include(tp => tp.Turma)
                .AnyAsync(tp =>
                    tp.ProfessorId == profId &&
                    tp.Turma.DiaDaSemana == diaDaSemanaEnum &&
                    tp.Turma.Ativa &&
                    (request.HorarioInicio < tp.Turma.HorarioFim && request.HorarioFim > tp.Turma.HorarioInicio));

            if (choqueProfessor)
                throw new RegraNegocioException($"O professor {professor.Nome} já possui uma turma neste mesmo dia e horário em outra sala.");
        }

        // CRIAÇÃO SEGURA
        var turma = new Turma(ritmoIdReal, salaIdReal, request.DataInicio, diaDaSemanaEnum, request.HorarioInicio, request.HorarioFim, request.Nivel, request.LimiteAlunos, request.LinkWhatsApp);
        _context.Turmas.Add(turma);

        foreach (var profId in professoresIdsReais)
        {
            _context.TurmaProfessores.Add(new TurmaProfessor { Turma = turma, ProfessorId = profId });
        }

        await _context.SaveChangesAsync();

        // Carrega referências para que o Mapper consiga pegar os nomes
        await _context.Entry(turma).Reference(t => t.Ritmo).LoadAsync();
        await _context.Entry(turma).Reference(t => t.Sala).LoadAsync();
        await _context.Entry(turma).Collection(t => t.Professores).Query().Include(p => p.Professor).LoadAsync();

        return turma.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterTurmaResponse>> ListarTurmasAsync(string? ritmoIdHash, string? professorIdHash, int? diaDaSemana, TimeSpan? horario)
    {
        var query = _context.Turmas
            .Include(t => t.Ritmo)
            .Include(t => t.Sala)
            .Include(t => t.Matriculas)
            .Include(t => t.Professores).ThenInclude(tp => tp.Professor)
            .AsQueryable();

        if (!string.IsNullOrEmpty(ritmoIdHash))
        {
            var ritmoDecoded = _hashids.Decode(ritmoIdHash);
            if (ritmoDecoded.Length > 0)
                query = query.Where(t => t.RitmoId == ritmoDecoded[0]);
        }

        if (!string.IsNullOrEmpty(professorIdHash))
        {
            var profDecoded = _hashids.Decode(professorIdHash);
            if (profDecoded.Length > 0)
                query = query.Where(t => t.Professores.Any(p => p.ProfessorId == profDecoded[0]));
        }

        if (diaDaSemana.HasValue)
        {
            var diaEnum = (DayOfWeek)diaDaSemana.Value;
            query = query.Where(t => t.DiaDaSemana == diaEnum);
        }

        if (horario.HasValue)
        {
            query = query.Where(t => horario.Value >= t.HorarioInicio && horario.Value < t.HorarioFim);
        }

        var turmas = await query.ToListAsync();
        return turmas.Select(t => t.ToResponse(_hashids));
    }

    public async Task TrocarSalaAsync(int turmaId, int novaSalaId, int novoLimiteAlunos)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.ListaDeEspera)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        int totalMatriculados = turma.Matriculas.Count;

        var novaSala = await _context.Salas.FindAsync(novaSalaId)
            ?? throw new RegraNegocioException("Nova sala não encontrada.");

        if (novoLimiteAlunos > novaSala.CapacidadeMaxima)
            throw new RegraNegocioException($"O novo limite ({novoLimiteAlunos}) excede a capacidade da {novaSala.Nome} ({novaSala.CapacidadeMaxima}).");

        if (novoLimiteAlunos < totalMatriculados)
            throw new RegraNegocioException($"Não é possível diminuir o limite para {novoLimiteAlunos}, pois a turma já possui {totalMatriculados} alunos matriculados.");

        bool choqueNovaSala = await _context.Turmas.AnyAsync(t =>
            t.Id != turmaId &&
            t.SalaId == novaSalaId &&
            t.DiaDaSemana == turma.DiaDaSemana &&
            t.Ativa &&
            (turma.HorarioInicio < t.HorarioFim && turma.HorarioFim > t.HorarioInicio));

        if (choqueNovaSala)
            throw new RegraNegocioException("A nova sala já está ocupada por outra turma neste mesmo dia e horário.");

        int vagasAnteriores = turma.LimiteAlunos - totalMatriculados;
        turma.AtualizarSalaELimite(novaSalaId, novoLimiteAlunos);
        int vagasNovas = turma.LimiteAlunos - totalMatriculados;

        if (vagasAnteriores <= 0 && vagasNovas > 0 && turma.ListaDeEspera.Any())
        {
            // TODO: Notificações Fila de espera
        }

        await _context.SaveChangesAsync();
    }

    public async Task<string> MatricularAlunoAsync(int turmaId, int alunoId, string papel)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.ListaDeEspera)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        var aluno = await _context.Usuarios.FindAsync(alunoId)
            ?? throw new RegraNegocioException("Aluno não encontrado.");

        if (turma.Matriculas.Any(m => m.AlunoId == alunoId))
            throw new RegraNegocioException("O aluno já está matriculado nesta turma.");

        if (turma.ListaDeEspera.Any(e => e.AlunoId == alunoId))
            throw new RegraNegocioException("O aluno já está na fila de espera desta turma.");

        if (turma.Matriculas.Count >= turma.LimiteAlunos)
        {
            _context.Interesses.Add(new Interesse { TurmaId = turmaId, AlunoId = alunoId });
            await _context.SaveChangesAsync();
            return "A turma está cheia. Você foi adicionado à fila de espera.";
        }

        _context.Matriculas.Add(new Matricula { TurmaId = turmaId, AlunoId = alunoId, Papel = papel });
        await _context.SaveChangesAsync();
        return "Matrícula realizada com sucesso.";
    }

    public async Task DesmatricularAlunoAsync(int turmaId, int alunoId)
    {
        var turma = await _context.Turmas
            .Include(t => t.Matriculas)
            .Include(t => t.ListaDeEspera)
            .FirstOrDefaultAsync(t => t.Id == turmaId)
            ?? throw new RegraNegocioException("Turma não encontrada.");

        var matricula = turma.Matriculas.FirstOrDefault(m => m.AlunoId == alunoId);
        if (matricula != null)
        {
            _context.Matriculas.Remove(matricula);
        }
        else
        {
            var interesse = turma.ListaDeEspera.FirstOrDefault(e => e.AlunoId == alunoId);
            if (interesse != null)
                _context.Interesses.Remove(interesse);
            else
                throw new RegraNegocioException("O aluno não está matriculado nem na fila de espera desta turma.");
        }

        await _context.SaveChangesAsync();
    }
}