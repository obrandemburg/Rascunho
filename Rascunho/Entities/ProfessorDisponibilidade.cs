// ARQUIVO: Rascunho/Entities/ProfessorDisponibilidade.cs
namespace Rascunho.Entities;

/// <summary>
/// Representa um bloco de tempo em que o professor está disponível
/// para aulas particulares. Cada registro é um slot independente:
/// Professor João → Segunda-feira → 18:00–19:00
/// Professor João → Segunda-feira → 20:00–21:00
/// Professor João → Quarta-feira → 18:00–19:30
/// </summary>
public class ProfessorDisponibilidade
{
    public int Id { get; protected set; }

    public int ProfessorId { get; protected set; }
    public Usuario Professor { get; protected set; } = null!;

    // DayOfWeek: 0=Domingo, 1=Segunda ... 6=Sábado
    public DayOfWeek DiaDaSemana { get; protected set; }
    public TimeSpan HorarioInicio { get; protected set; }
    public TimeSpan HorarioFim { get; protected set; }

    // Ativo = false quando o professor suspende temporariamente um slot
    // sem precisar deletar (ex: durante férias)
    public bool Ativo { get; protected set; } = true;

    protected ProfessorDisponibilidade() { }

    public ProfessorDisponibilidade(int professorId, DayOfWeek dia, TimeSpan inicio, TimeSpan fim)
    {
        if (fim <= inicio)
            throw new ArgumentException("O horário de fim deve ser maior que o de início.");

        ProfessorId = professorId;
        DiaDaSemana = dia;
        HorarioInicio = inicio;
        HorarioFim = fim;
        Ativo = true;
    }

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}