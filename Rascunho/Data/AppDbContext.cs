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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
