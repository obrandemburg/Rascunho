using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(150);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.SenhaHash).IsRequired();
        builder.Property(a => a.DataNascimento)
            .IsRequired()
            .HasColumnName("data_nascimento");

        // Configuração TPH (Table-Per-Hierarchy)
        builder.HasDiscriminator(u => u.Tipo)
            .HasValue<Aluno>("Aluno")
            .HasValue<Professor>("Professor")
            .HasValue<Bolsista>("Bolsista")
            .HasValue<Gerente>("Gerente")
            .HasValue<Recepcao>("Recepção")
            .HasValue<Lider>("Líder");
    }
}