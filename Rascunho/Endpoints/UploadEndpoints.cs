using Rascunho.Shared.DTOs;

namespace Rascunho.Endpoints;

public static class UploadEndpoints
{
    // Pasta relativa ao diretório de trabalho do container (/app)
    // Em produção via Docker: /app/uploads/fotos/
    // Essa pasta é mapeada para o volume Docker pd_uploads
    private const string PastaRelativa = "uploads/fotos";

    // 5 MB — suficiente para foto de perfil de boa qualidade
    private const long TamanhoMaximoBytes = 5 * 1024 * 1024;

    // Tipos MIME aceitos — apenas imagens comuns
    private static readonly HashSet<string> MimesAceitos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp"
        };

    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/upload").RequireAuthorization();

        // ══════════════════════════════════════════════════════════
        // POST /api/upload/foto
        //
        // Aceita: multipart/form-data com campo "foto" (IFormFile)
        // Retorna: UploadFotoResponse { Url, NomeArquivo, TamanhoBytes }
        //
        // Segurança implementada:
        //   ✅ Validação de MIME type
        //   ✅ Limite de 5 MB
        //   ✅ Nome gerado por GUID (sem path traversal)
        //   ✅ Requer autenticação (qualquer usuário logado)
        //   ✅ DisableAntiforgery (endpoints de API não usam cookie antiforgery)
        // ══════════════════════════════════════════════════════════
        group.MapPost("/foto", async (IFormFile foto, HttpContext ctx) =>
        {
            // ── Validação 1: arquivo presente ─────────────────────
            if (foto == null || foto.Length == 0)
                return Results.BadRequest(new { erro = "Nenhum arquivo enviado." });

            // ── Validação 2: tamanho máximo ───────────────────────
            if (foto.Length > TamanhoMaximoBytes)
                return Results.BadRequest(new
                {
                    erro = $"Arquivo muito grande. Máximo: {TamanhoMaximoBytes / 1024 / 1024} MB."
                });

            // ── Validação 3: tipo de arquivo ──────────────────────
            // ContentType é definido pelo navegador com base na extensão.
            // Para segurança máxima em produção, adicione a lib FileSignatures
            // que verifica os "magic bytes" do arquivo (independe da extensão).
            if (!MimesAceitos.Contains(foto.ContentType))
                return Results.BadRequest(new
                {
                    erro = "Formato inválido. Aceitos: JPG, PNG, WEBP."
                });

            // ── Gera nome único com GUID ──────────────────────────
            // GUID garante que dois uploads simultâneos nunca colidam.
            // Manter a extensão original facilita a exibição pelo navegador.
            var extensao = Path.GetExtension(foto.FileName).ToLowerInvariant();
            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";

            // ── Garante que a pasta existe ────────────────────────
            var pasta = Path.Combine(Directory.GetCurrentDirectory(), PastaRelativa);
            Directory.CreateDirectory(pasta);

            // ── Salva em disco ────────────────────────────────────
            var caminhoCompleto = Path.Combine(pasta, nomeArquivo);
            await using var stream = new FileStream(caminhoCompleto, FileMode.Create);
            await foto.CopyToAsync(stream);

            // ── Monta URL pública ─────────────────────────────────
            // Usa o mesmo host da requisição — funciona em dev e produção
            // sem precisar configurar URLs em appsettings
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var urlPublica = $"{baseUrl}/{PastaRelativa}/{nomeArquivo}";

            return Results.Ok(new UploadFotoResponse(
                Url: urlPublica,
                NomeArquivo: nomeArquivo,
                TamanhoBytes: foto.Length
            ));
        })
        .DisableAntiforgery(); // Endpoints Minimal API de upload precisam disso

        // ══════════════════════════════════════════════════════════
        // DELETE /api/upload/foto/{nomeArquivo}
        //
        // Remove um arquivo de foto do servidor.
        // Chamado quando o usuário troca a foto — apaga a antiga.
        //
        // Segurança: Path.GetFileName() remove qualquer tentativa de
        // path traversal (ex: "../../etc/passwd" → "passwd" → não encontrado)
        // ══════════════════════════════════════════════════════════
        group.MapDelete("/foto/{nomeArquivo}", (string nomeArquivo) =>
        {
            var nomeSanitizado = Path.GetFileName(nomeArquivo);
            if (string.IsNullOrEmpty(nomeSanitizado))
                return Results.BadRequest(new { erro = "Nome de arquivo inválido." });

            var caminho = Path.Combine(
                Directory.GetCurrentDirectory(), PastaRelativa, nomeSanitizado);

            if (!File.Exists(caminho))
                return Results.NotFound(new { erro = "Arquivo não encontrado." });

            File.Delete(caminho);
            return Results.Ok(new { Mensagem = "Foto removida com sucesso." });
        });
    }
}
