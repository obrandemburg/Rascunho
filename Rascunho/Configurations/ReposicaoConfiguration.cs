using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class ReposicaoConfiguration : IEntityTypeConfiguration<Reposicao>
{
    public void Configure(EntityTypeBuilder<Reposicao> builder)
    {
        builder.ToTable("Reposicoes");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(20);

        // Relacionamento com o Aluno
        builder.HasOne(r => r.Aluno)
               .WithMany()
               .HasForeignKey(r => r.AlunoId)
               .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com a Turma de Origem (onde houve a falta)
        builder.HasOne(r => r.TurmaOrigem)
               .WithMany()
               .HasForeignKey(r => r.TurmaOrigemId)
               .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com a Turma de Destino (onde ocorrerá a reposição)
        builder.HasOne(r => r.TurmaDestino)
               .WithMany()
               .HasForeignKey(r => r.TurmaDestinoId)
               .OnDelete(DeleteBehavior.Restrict);

        // Índice único: um aluno não pode ter duas reposições ativas para a mesma falta.
        // Isso previne duplicatas quando o aluno cancela e tenta reagendar novamente.
        // O índice inclui Status para permitir múltiplas tentativas após cancelamentos.
        builder.HasIndex(r => new { r.AlunoId, r.TurmaOrigemId, r.DataFalta, r.Status })
               .IsUnique()
               .HasFilter("\"Status\" = 'Agendada'"); // PostgreSQL partial index
    }
}