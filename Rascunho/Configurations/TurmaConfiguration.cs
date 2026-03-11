using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class TurmaConfiguration : IEntityTypeConfiguration<Turma>
{
    public void Configure(EntityTypeBuilder<Turma> builder)
    {
        builder.ToTable("Turmas");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nivel).IsRequired().HasMaxLength(50);
        builder.Property(t => t.LinkWhatsApp).HasMaxLength(255);

        // Relacionamentos 1:N diretos
        builder.HasOne(t => t.Ritmo).WithMany().HasForeignKey(t => t.RitmoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Sala).WithMany().HasForeignKey(t => t.SalaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TurmaProfessorConfiguration : IEntityTypeConfiguration<TurmaProfessor>
{
    public void Configure(EntityTypeBuilder<TurmaProfessor> builder)
    {
        builder.ToTable("TurmaProfessores");
        builder.HasKey(tp => new { tp.TurmaId, tp.ProfessorId }); // Chave Composta
    }
}

public class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("Matriculas");
        builder.HasKey(m => new { m.TurmaId, m.AlunoId });
        builder.Property(m => m.Papel).IsRequired().HasMaxLength(20);
    }
}

public class InteresseConfiguration : IEntityTypeConfiguration<Interesse>
{
    public void Configure(EntityTypeBuilder<Interesse> builder)
    {
        builder.ToTable("Interesses");
        builder.HasKey(i => new { i.TurmaId, i.AlunoId });
    }
}