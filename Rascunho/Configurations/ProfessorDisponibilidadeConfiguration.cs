using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class ProfessorDisponibilidadeConfiguration : IEntityTypeConfiguration<ProfessorDisponibilidade>
{
    public void Configure(EntityTypeBuilder<ProfessorDisponibilidade> builder)
    {
        builder.ToTable("ProfessorDisponibilidades");
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.Professor)
               .WithMany()
               .HasForeignKey(d => d.ProfessorId)
               .OnDelete(DeleteBehavior.Cascade); // Se o professor for excluído, slots somem junto

        // Índice único: um professor não pode ter dois slots com o mesmo início no mesmo dia
        // Isso impede duplicatas acidentais
        builder.HasIndex(d => new { d.ProfessorId, d.DiaDaSemana, d.HorarioInicio })
               .IsUnique();
    }
}