using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class RegistroPresencaConfiguration : IEntityTypeConfiguration<RegistroPresenca>
{
    public void Configure(EntityTypeBuilder<RegistroPresenca> builder)
    {
        builder.ToTable("RegistrosPresencas");
        builder.HasKey(rp => new { rp.TurmaId, rp.AlunoId, rp.DataAula });

        // NOVO Sprint 2: coluna nullable para observação
        builder.Property(rp => rp.Observacao)
               .HasMaxLength(500)
               .IsRequired(false); // null é válido — sem observação

        builder.HasOne(rp => rp.Turma)
               .WithMany()
               .HasForeignKey(rp => rp.TurmaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Aluno)
               .WithMany()
               .HasForeignKey(rp => rp.AlunoId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}