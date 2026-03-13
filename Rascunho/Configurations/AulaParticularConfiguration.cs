using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class AulaParticularConfiguration : IEntityTypeConfiguration<AulaParticular>
{
    public void Configure(EntityTypeBuilder<AulaParticular> builder)
    {
        builder.ToTable("AulasParticulares");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).IsRequired().HasMaxLength(20);
        builder.Property(a => a.ObservacaoAluno).HasMaxLength(500);

        // Relacionamentos
        builder.HasOne(a => a.Aluno)
               .WithMany()
               .HasForeignKey(a => a.AlunoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Professor)
               .WithMany()
               .HasForeignKey(a => a.ProfessorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Ritmo)
               .WithMany()
               .HasForeignKey(a => a.RitmoId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}