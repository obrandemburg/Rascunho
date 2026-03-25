// Em Rascunho.Shared/DTOs/ErrosGlobaisDto.cs
namespace Rascunho.Shared.DTOs;

public class ErroGenericoDto
{
    public string? Erro { get; set; }
    public string? Detalhes { get; set; }
}

public class ValidationProblemDto
{
    public Dictionary<string, string[]>? Errors { get; set; }
}