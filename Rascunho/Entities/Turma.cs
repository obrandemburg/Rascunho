namespace Rascunho.Entities;

public class Turma
{
    public int Id { get; protected set; }
    public int RitmoId { get; protected set; }
    public Ritmo Ritmo { get; protected set; } = null!;

    public int SalaId { get; protected set; }
    public Sala Sala { get; protected set; } = null!;

    public DateTime DataInicio { get; protected set; }
    public DayOfWeek DiaDaSemana { get; protected set; }
    public TimeSpan HorarioInicio { get; protected set; }
    public TimeSpan HorarioFim { get; protected set; } // Calculado (Ex: Inicio + 50 min)

    public string Nivel { get; protected set; } = string.Empty;
    public int LimiteAlunos { get; protected set; }
    public string LinkWhatsApp { get; protected set; } = string.Empty;
    public bool Ativa { get; protected set; } = true;

    // Relacionamentos N:N
    public ICollection<TurmaProfessor> Professores { get; protected set; } = new List<TurmaProfessor>();
    public ICollection<Matricula> Matriculas { get; protected set; } = new List<Matricula>();
    public ICollection<Interesse> ListaDeEspera { get; protected set; } = new List<Interesse>();

    protected Turma() { }

    public Turma(int ritmoId, int salaId, DateTime dataInicio, DayOfWeek diaDaSemana, TimeSpan horarioInicio, TimeSpan horarioFim, string nivel, int limiteAlunos, string linkWhatsApp)
    {
        RitmoId = ritmoId;
        SalaId = salaId;
        DataInicio = dataInicio.Date;
        DiaDaSemana = diaDaSemana;
        HorarioInicio = horarioInicio;
        HorarioFim = horarioFim;
        Nivel = nivel;
        LimiteAlunos = limiteAlunos;
        LinkWhatsApp = linkWhatsApp;
        Ativa = true;
    }

    public void AtualizarSalaELimite(int salaId, int limite) { SalaId = salaId; LimiteAlunos = limite; }
}