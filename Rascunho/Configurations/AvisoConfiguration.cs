using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class AvisoConfiguration : IEntityTypeConfiguration<Aviso>
{
    public void Configure(EntityTypeBuilder<Aviso> builder)
    {
        builder.ToTable("Avisos");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Titulo).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Mensagem).IsRequired().HasMaxLength(1000); // Avisos podem ser longos
        builder.Property(a => a.TipoVisibilidade).IsRequired().HasMaxLength(20);

        // Relacionamento com o Autor
        builder.HasOne(a => a.Autor)
               .WithMany()
               .HasForeignKey(a => a.AutorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}