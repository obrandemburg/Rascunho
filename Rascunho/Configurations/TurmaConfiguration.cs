// Localização: Rascunho/Configurations/TurmaConfiguration.cs
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

        builder.Property(t => t.DataInicio)
               .IsRequired()
               .HasColumnType("date");
        builder.HasOne(t => t.Ritmo).WithMany().HasForeignKey(t => t.RitmoId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Sala).WithMany().HasForeignKey(t => t.SalaId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TurmaProfessorConfiguration : IEntityTypeConfiguration<TurmaProfessor>
{
    public void Configure(EntityTypeBuilder<TurmaProfessor> builder)
    {
        builder.ToTable("TurmaProfessores");
        builder.HasKey(tp => new { tp.TurmaId, tp.ProfessorId });
    }
}

public class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("Matriculas");
        builder.HasKey(m => new { m.TurmaId, m.AlunoId });

        builder.Property(m => m.Papel).IsRequired().HasMaxLength(20);

        // NOVO Sprint 4: colunas para rastreamento de desconto
        // nullable decimal(10,2): null = sem preço definido (padrão)
        builder.Property(m => m.ValorMensalidade)
               .HasColumnType("decimal(10,2)")
               .IsRequired(false);

        // Máximo de 20 chars: "Bolsista50%" é o valor atual, mas deixa espaço para futuros
        builder.Property(m => m.OrigemDesconto)
               .HasMaxLength(20)
               .IsRequired(false);
    }
}

// InteresseConfiguration removida — BUG-010 (28/03/2026)
// A entidade Interesse foi substituída funcionalmente pela ListaEspera (Feature #3).
// A migration RemoveInteresseObsoleto remove a tabela Interesses do banco.

public class ListaEsperaConfiguration : IEntityTypeConfiguration<ListaEspera>
{
    public void Configure(EntityTypeBuilder<ListaEspera> builder)
    {
        builder.ToTable("ListasEspera");
        builder.HasKey(le => le.Id);

        builder.Property(le => le.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(le => le.DataEntrada)
               .IsRequired();

        // Índice para otimizar buscas por (turma, aluno)
        builder.HasIndex(le => new { le.TurmaId, le.AlunoId });

        // Índice para otimizar a busca do próximo na fila por posição
        builder.HasIndex(le => new { le.TurmaId, le.Status, le.Posicao });

        builder.HasOne(le => le.Turma)
               .WithMany(t => t.ListaDeEspera)
               .HasForeignKey(le => le.TurmaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(le => le.Aluno)
               .WithMany()
               .HasForeignKey(le => le.AlunoId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}