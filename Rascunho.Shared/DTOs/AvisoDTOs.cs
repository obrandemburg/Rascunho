using System;

namespace Rascunho.Shared.DTOs;

public record CriarAvisoRequest(string Titulo, string Mensagem, DateTime DataExpiracao, string TipoVisibilidade);
public record AtualizarAvisoRequest(string Titulo, string Mensagem, DateTime DataExpiracao, string TipoVisibilidade);
public record ObterAvisoResponse(string IdHash, string Titulo, string Mensagem, DateTime DataPublicacao, DateTime DataExpiracao, string TipoVisibilidade, string NomeAutor);