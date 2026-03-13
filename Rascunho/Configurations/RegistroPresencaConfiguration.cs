using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class RegistroPresencaConfiguration : IEntityTypeConfiguration<RegistroPresenca>
{
    public void Configure(EntityTypeBuilder<RegistroPresenca> builder)
    {
        builder.ToTable("RegistrosPresencas");

        // A magia da Chave Composta Tripla para evitar duplicados
        builder.HasKey(rp => new { rp.TurmaId, rp.AlunoId, rp.DataAula });

        builder.HasOne(rp => rp.Turma)
               .WithMany()
               .HasForeignKey(rp => rp.TurmaId)
               .OnDelete(DeleteBehavior.Cascade); // Se a turma acabar, o histórico morre junto

        builder.HasOne(rp => rp.Aluno)
               .WithMany()
               .HasForeignKey(rp => rp.AlunoId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}