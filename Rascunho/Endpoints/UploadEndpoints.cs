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

    // Mapa de extensão → MIME type para servir fotos corretamente
    private static readonly Dictionary<string, string> ExtensaoParaMime =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"]  = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"]  = "image/png",
            [".webp"] = "image/webp",
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
        // CORREÇÃO Mixed Content / multi-ambiente:
        //   A URL pública agora é sempre /api/fotos/{nomeArquivo} (relativa).
        //   Isso garante que a foto seja carregada pelo mesmo domínio/protocolo
        //   da aplicação, eliminando erros de Mixed Content (HTTPS → HTTP) e
        //   problemas de host hardcoded (IP da VPS vs localhost em dev).
        //   O Traefik/proxy roteia /api/* para o backend em todos os ambientes.
        //
        // Segurança implementada:
        //   ✅ Validação de MIME type
        //   ✅ Limite de 5 MB
        //   ✅ Nome gerado por GUID (sem path traversal)
        //   ✅ Requer autenticação (qualquer usuário logado)
        //   ✅ DisableAntiforgery (endpoints de API não usam cookie antiforgery)
        // ══════════════════════════════════════════════════════════
        group.MapPost("/foto", async (IFormFile foto) =>
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
            if (!MimesAceitos.Contains(foto.ContentType))
                return Results.BadRequest(new
                {
                    erro = "Formato inválido. Aceitos: JPG, PNG, WEBP."
                });

            // ── Gera nome único com GUID ──────────────────────────
            var extensao = Path.GetExtension(foto.FileName).ToLowerInvariant();
            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";

            // ── Garante que a pasta existe ────────────────────────
            var pasta = Path.Combine(Directory.GetCurrentDirectory(), PastaRelativa);
            Directory.CreateDirectory(pasta);

            // ── Salva em disco ────────────────────────────────────
            var caminhoCompleto = Path.Combine(pasta, nomeArquivo);
            await using var stream = new FileStream(caminhoCompleto, FileMode.Create);
            await foto.CopyToAsync(stream);

            // ── URL pública: sempre relativa via /api/fotos/ ──────
            // Antes usava o host da requisição (ex: http://5.161.202.169:8080/uploads/...).
            // Isso causava Mixed Content quando o frontend rodava em HTTPS e o
            // backend estava atrás de um reverse proxy com IP/porta diferente.
            // Agora a URL é sempre /api/fotos/{nomeArquivo}, que o browser
            // resolve contra a origem atual — funciona em dev e produção.
            var urlPublica = $"/api/fotos/{nomeArquivo}";

            return Results.Ok(new UploadFotoResponse(
                Url: urlPublica,
                NomeArquivo: nomeArquivo,
                TamanhoBytes: foto.Length
            ));
        })
        .DisableAntiforgery(); // Endpoints Minimal API de upload precisam disso

        // ══════════════════════════════════════════════════════════
        // GET /api/fotos/{nomeArquivo}
        //
        // Serve arquivos de foto diretamente via API.
        //
        // MOTIVAÇÃO:
        //   A aplicação usa Traefik como reverse proxy. Apenas rotas /api/*
        //   são encaminhadas ao container do backend. O caminho /uploads/*
        //   vai para o container nginx do frontend, que não tem esses arquivos.
        //   Servindo via /api/fotos/, garantimos que o Traefik roteie
        //   corretamente para o backend em qualquer ambiente (dev e produção).
        //
        // RETROCOMPATIBILIDADE:
        //   Fotos antigas armazenadas com URL http://IP:8080/uploads/fotos/uuid.jpg
        //   são tratadas no componente UserAvatar.razor (FotoUrlNormalizada),
        //   que extrai o nome do arquivo e usa /api/fotos/{nomeArquivo}.
        //
        // SEGURANÇA:
        //   Path.GetFileName() neutraliza path traversal antes de qualquer I/O.
        //   Nenhuma autenticação necessária — fotos são recursos públicos por design.
        // ══════════════════════════════════════════════════════════
        app.MapGet("/api/fotos/{nomeArquivo}", (string nomeArquivo) =>
        {
            // Sanitiza o nome para prevenir path traversal
            var nomeSanitizado = Path.GetFileName(nomeArquivo);
            if (string.IsNullOrEmpty(nomeSanitizado))
                return Results.BadRequest(new { erro = "Nome de arquivo inválido." });

            var caminho = Path.Combine(
                Directory.GetCurrentDirectory(), PastaRelativa, nomeSanitizado);

            if (!File.Exists(caminho))
                return Results.NotFound(new { erro = "Foto não encontrada." });

            // Determina o Content-Type pela extensão do arquivo
            var extensao = Path.GetExtension(nomeSanitizado).ToLowerInvariant();
            var contentType = ExtensaoParaMime.GetValueOrDefault(extensao, "application/octet-stream");

            return Results.File(caminho, contentType);
        });

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
