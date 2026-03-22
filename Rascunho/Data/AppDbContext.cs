using Rascunho.Entities;
using Microsoft.EntityFrameworkCore;

namespace Rascunho.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Ritmo> Ritmos { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<Aluno> Alunos { get; set; }
        public DbSet<Professor> Professores { get; set; }
        public DbSet<Bolsista> Bolsistas { get; set; }
        public DbSet<Gerente> Gerentes { get; set; }
        public DbSet<Recepcao> Recepcoes { get; set; }
        public DbSet<Lider> Lideres { get; set; }
        public DbSet<Turma> Turmas { get; set; }
        public DbSet<TurmaProfessor> TurmaProfessores { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<Interesse> Interesses { get; set; }
        public DbSet<Aviso> Avisos { get; set; }
        public DbSet<RegistroPresenca> RegistrosPresencas { get; set; }
        public DbSet<AulaParticular> AulasParticulares { get; set; }
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Ingresso> Ingressos { get; set; }
        public DbSet<AulaExperimental> AulasExperimentais { get; set; }
        public DbSet<ProfessorDisponibilidade> ProfessorDisponibilidades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
