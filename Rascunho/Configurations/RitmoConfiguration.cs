using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class RitmoConfiguration : IEntityTypeConfiguration<Ritmo>
{
    public void Configure(EntityTypeBuilder<Ritmo> builder)
    {
        builder.ToTable("Ritmos");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Descricao)
            .HasMaxLength(500);

        builder.Property(r => r.Modalidade)
            .IsRequired()
            .HasMaxLength(50);
    }
}