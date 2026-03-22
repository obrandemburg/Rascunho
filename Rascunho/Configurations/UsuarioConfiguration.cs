// Localização: Rascunho/Configurations/UsuarioConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rascunho.Entities;

namespace Rascunho.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.SenhaHash).IsRequired();

        builder.Property(u => u.DataNascimento)
            .IsRequired()
            .HasColumnName("data_nascimento");

        // Telefone: opcional, máx 20 chars (acomoda (31) 99999-9999)
        builder.Property(u => u.Telefone)
            .HasMaxLength(20)
            .IsRequired(false);

        // CPF: opcional, armazenado SÓ com 11 dígitos (sem formatação)
        // char(11) seria mais restrito, mas varchar(11) é mais flexível
        // para migração de dados existentes sem CPF
        builder.Property(u => u.Cpf)
            .HasMaxLength(11)
            .IsRequired(false);

        // Índice único parcial: CPF único apenas quando não for nulo.
        // Isso permite múltiplos usuários sem CPF (null ≠ null no PostgreSQL).
        // HasFilter usa SQL do PostgreSQL — se usar SQL Server, ajuste para
        // WHERE [Cpf] IS NOT NULL
        builder.HasIndex(u => u.Cpf)
            .IsUnique()
            .HasFilter("\"Cpf\" IS NOT NULL")
            .HasDatabaseName("ix_usuarios_cpf_unique");

        // TPH — discriminador de tipo na tabela Usuarios
        builder.HasDiscriminator(u => u.Tipo)
            .HasValue<Aluno>("Aluno")
            .HasValue<Professor>("Professor")
            .HasValue<Bolsista>("Bolsista")
            .HasValue<Gerente>("Gerente")
            .HasValue<Recepcao>("Recepção")
            .HasValue<Lider>("Líder");
    }
}

/// <summary>
/// Configuração adicional específica da subclasse Bolsista.
/// Como usamos TPH, a tabela é a mesma — apenas adicionamos
/// as colunas extras que só existem para bolsistas.
/// </summary>
public class BolsistaConfiguration : IEntityTypeConfiguration<Bolsista>
{
    public void Configure(EntityTypeBuilder<Bolsista> builder)
    {
        // PapelDominante: "Condutor", "Conduzido" ou "Ambos"
        // Null para usuários que não são bolsistas (colunas TPH são nullable por padrão)
        builder.Property(b => b.PapelDominante)
            .HasMaxLength(20)
            .IsRequired(false);

        // DiaObrigatorio1 e DiaObrigatorio2 já são nullable (DayOfWeek?)
        // O EF Core mapeia enums nullable como INT nullable automaticamente
    }
}
