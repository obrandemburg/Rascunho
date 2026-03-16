using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("Eventos");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Nome).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Tipo).IsRequired().HasMaxLength(50);
    }
}

public class IngressoConfiguration : IEntityTypeConfiguration<Ingresso>
{
    public void Configure(EntityTypeBuilder<Ingresso> builder)
    {
        builder.ToTable("Ingressos");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Status).IsRequired().HasMaxLength(20);

        // Um usuário só pode comprar 1 ingresso por evento (Evita cambistas)
        builder.HasIndex(i => new { i.EventoId, i.UsuarioId }).IsUnique();
    }
}