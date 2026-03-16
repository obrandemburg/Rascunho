using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class AulaExperimentalConfiguration : IEntityTypeConfiguration<AulaExperimental>
{
    public void Configure(EntityTypeBuilder<AulaExperimental> builder)
    {
        builder.ToTable("AulasExperimentais");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(20);

        // Relacionamentos
        builder.HasOne(a => a.Aluno)
               .WithMany()
               .HasForeignKey(a => a.AlunoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Turma)
               .WithMany()
               .HasForeignKey(a => a.TurmaId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}