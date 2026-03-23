// Localização: Rascunho/Services/RitmoService.cs
//
// SPRINT 8: Adicionado ExcluirRitmoAsync.
//
// Regras de negócio da exclusão:
//   1. Ritmo com qualquer turma associada (ativa OU inativa) não pode ser excluído.
//      O motivo: mesmo turmas encerradas fazem parte do histórico de chamadas e
//      presenças — excluir o ritmo tornaria esses registros sem sentido.
//   2. Registros de HabilidadeUsuario que referenciam o ritmo são removidos em
//      cascata lógica antes da exclusão (sem cascade configurado no EF Core).
//   3. Se o banco retornar FK violation inesperada, o GlobalExceptionHandler
//      captura e retorna HTTP 500 com mensagem genérica.

using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Entities;
using Rascunho.Exceptions;
using Rascunho.Shared.DTOs;
using Rascunho.Mappers;

namespace Rascunho.Services;

public class RitmoService
{
    private readonly AppDbContext _context;
    private readonly IHashids _hashids;

    public RitmoService(AppDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<ObterRitmoResponse> CriarRitmoAsync(CriarRitmoRequest request)
    {
        var ritmo = new Ritmo(request.Nome, request.Descricao, request.Modalidade);
        _context.Ritmos.Add(ritmo);
        await _context.SaveChangesAsync();
        return ritmo.ToResponse(_hashids);
    }

    public async Task<IEnumerable<ObterRitmoResponse>> ListarTodosAsync(bool apenasAtivos = false)
    {
        var query = _context.Ritmos.AsQueryable();
        if (apenasAtivos) query = query.Where(r => r.Ativo);
        var ritmos = await query.ToListAsync();
        return ritmos.Select(r => r.ToResponse(_hashids));
    }

    public async Task<ObterRitmoResponse> ObterRitmoPorIdAsync(int id)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");
        return ritmo.ToResponse(_hashids);
    }

    public async Task AtualizarRitmoAsync(int id, AtualizarRitmoRequest request)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");
        ritmo.Atualizar(request.Nome, request.Descricao, request.Modalidade);
        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(int id, bool ativo)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");

        if (ativo)
        {
            ritmo.Ativar();
        }
        else
        {
            // Bloqueio: não pode desativar ritmo com turmas ativas
            bool emUso = await _context.Turmas.AnyAsync(t => t.RitmoId == id && t.Ativa);
            if (emUso)
                throw new RegraNegocioException(
                    "Não é possível inativar um ritmo que possui turmas ativas. " +
                    "Encerre as turmas primeiro.");
            ritmo.Desativar();
        }
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // EXCLUIR RITMO (SPRINT 8)
    //
    // Por que verificar turmas inativas também?
    // Uma turma encerrada (Ativa = false) ainda mantém registros de
    // RegistroPresenca que referenciam a turma, que por sua vez
    // referencia o ritmo. Excluir o ritmo tornaria o histórico
    // pedagógico sem sentido e poderia causar erros em relatórios futuros.
    //
    // Por que remover HabilidadeUsuario manualmente?
    // A HabilidadeUsuarioConfiguration não define OnDelete (padrão = Restrict).
    // Se tentarmos excluir o ritmo sem limpar as habilidades, o banco
    // retornará uma FK violation. Limpamos explicitamente antes.
    // ──────────────────────────────────────────────────────────────
    public async Task ExcluirRitmoAsync(int id)
    {
        var ritmo = await _context.Ritmos.FindAsync(id)
            ?? throw new RegraNegocioException("Ritmo não encontrado.");

        // Verifica se existe qualquer turma (ativa ou encerrada) usando este ritmo
        bool possuiTurmas = await _context.Turmas.AnyAsync(t => t.RitmoId == id);
        if (possuiTurmas)
            throw new RegraNegocioException(
                "Não é possível excluir um ritmo que possui turmas associadas. " +
                "Encerre todas as turmas deste ritmo antes de excluí-lo.");

        // Remove vínculos de HabilidadeUsuario (sem cascade configurado)
        // Esses registros são os papéis e níveis de bolsistas para este ritmo
        var habilidades = await _context.Set<HabilidadeUsuario>()
            .Where(h => h.RitmoId == id)
            .ToListAsync();

        if (habilidades.Any())
            _context.Set<HabilidadeUsuario>().RemoveRange(habilidades);

        _context.Ritmos.Remove(ritmo);
        await _context.SaveChangesAsync();
    }
}
