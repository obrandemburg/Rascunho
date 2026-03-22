// Localização: Rascunho/Mappers/UsuarioMapper.cs
using HashidsNet;
using Rascunho.Entities;
using Rascunho.Shared.DTOs;

namespace Rascunho.Mappers;

public static class UsuarioMapper
{
    public static ObterUsuarioResponse ToResponse(this Usuario u, IHashids hashids) =>
        new ObterUsuarioResponse(
            hashids.Encode(u.Id),
            u.Nome,
            u.Email,
            u.Tipo,
            u.NomeSocial,
            u.Biografia,
            u.FotoUrl,
            u.Ativo,
            u.Telefone,
            u.DataNascimento,
            FormatarCpf(u.Cpf)  // Formata no response: "12345678901" → "123.456.789-01"
        );

    /// <summary>
    /// Converte o CPF armazenado (11 dígitos sem formatação) para exibição.
    /// Se nulo ou inválido, retorna null sem lançar exceção.
    /// Exemplo: "12345678901" → "123.456.789-01"
    /// </summary>
    private static string? FormatarCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
            return null;

        return $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";
    }
}
