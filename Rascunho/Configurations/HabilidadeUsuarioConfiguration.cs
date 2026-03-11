using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

public class HabilidadeUsuarioConfiguration : IEntityTypeConfiguration<HabilidadeUsuario>
{
    public void Configure(EntityTypeBuilder<HabilidadeUsuario> builder)
    {
        builder.ToTable("HabilidadesUsuarios");
        builder.HasKey(h => new { h.UsuarioId, h.RitmoId });
    }
}