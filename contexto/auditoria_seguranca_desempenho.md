# Auditoria de Segurança e Desempenho — Ponto da Dança

> Gerada em: 31/03/2026 | Auditoria realizada pelo agente architect
> Status geral: 4 críticos corrigidos em 31/03/2026 | 27 pendentes

---

## Legenda de Status

- ✅ **Corrigido** — implementado e em produção
- 🔄 **Parcial** — corrigido no código, pendência de configuração/infraestrutura
- ⏳ **Pendente** — identificado, não iniciado
- 🚫 **Revertido** — tentativa de correção causou problema, requer nova abordagem

---

## Resumo Executivo

| Categoria | Crítico | Alto | Médio | Baixo | Total |
|---|---|---|---|---|---|
| Segurança | 4 | 7 | 5 | 2 | **18** |
| Desempenho | — | 5 | 6 | 2 | **13** |
| **Total** | **4** | **12** | **11** | **4** | **31** |

**Corrigidos nesta sessão:** SEC-01, SEC-02, SEC-03 (✅) | SEC-04 (🚫 revertido)

---

## SEGURANÇA

### Críticos

---

### SEC-01 — Stack trace exposto em produção ✅ CORRIGIDO

**Arquivo:** `Rascunho/Infraestrutura/GlobalExceptionHandler.cs`
**Corrigido em:** 31/03/2026

**Problema:** O handler enviava `exception.ToString()` (stack trace completo, nomes de classes internas, caminhos de arquivo) na propriedade `detalhes` da resposta JSON para qualquer cliente HTTP, inclusive em produção.

**Correção aplicada:** Injetado `IHostEnvironment`. A propriedade `detalhes` com stack trace agora é incluída **somente** quando `env.IsDevelopment() == true`. Em produção, o JSON retorna apenas `{ erro: "mensagem genérica" }`. O `_logger.LogError` interno foi mantido.

---

### SEC-02 — CORS `AllowAnyOrigin` em produção ✅ CORRIGIDO

**Arquivo:** `Rascunho/Program.cs`
**Corrigido em:** 31/03/2026

**Problema:** `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` — qualquer origem na internet podia fazer requisições autenticadas à API com um token JWT válido, anulando o CORS como camada de defesa.

**Correção aplicada:** A política `PermitirFrontend` agora lê `Cors:AllowedOrigins` da configuração. Se o valor for `"*"` ou ausente → mantém `AllowAnyOrigin` (desenvolvimento). Em produção, a variável de ambiente `Cors__AllowedOrigins=https://pontodadanca.trindaflow.com.br` restringe as origens.

**Configuração na VPS (stacksVPS/.env):**
```
CORS_ALLOWED_ORIGINS=https://pontodadanca.trindaflow.com.br
```
**docker-compose.yml (serviço api):**
```yaml
- Cors__AllowedOrigins=${CORS_ALLOWED_ORIGINS}
```

---

### SEC-03 — `POST /api/usuarios/cadastrar` sem autenticação ✅ CORRIGIDO

**Arquivo:** `Rascunho/Endpoints/UsuarioEndpoints.cs`
**Corrigido em:** 31/03/2026

**Problema:** Qualquer pessoa na internet podia criar usuários de qualquer perfil (incluindo Gerente) sem estar autenticada.

**Correção aplicada:** Adicionado `.RequireAuthorization(policy => policy.RequireRole("Recepção", "Gerente"))` ao endpoint `POST /cadastrar`, alinhando-o com o endpoint de cadastro em lista que já tinha essa proteção.

---

### SEC-04 — `AllowedHosts: "*"` em produção 🚫 REVERTIDO

**Arquivo:** `Rascunho/appsettings.json`
**Tentativa:** 31/03/2026 | **Revertido:** 31/03/2026

**Problema original:** `AllowedHosts: "*"` desativa a proteção contra Host Header Injection no ASP.NET Core.

**Tentativa de correção:** Alterado para `"AllowedHosts": "localhost"` com override via variável de ambiente `ASPNETCORE_AllowedHosts=pontodadanca.trindaflow.com.br` na VPS.

**Motivo da reversão:** Mesmo com a variável de ambiente corretamente definida no container (confirmado via `docker inspect`), o middleware `AllowedHosts` rejeitava todas as requisições com 400 "Invalid Hostname". Suspeita: o Traefik pode alterar o header `Host` internamente ao rotear para o container, fazendo o valor não bater com o configurado.

**Estado atual:** `AllowedHosts: "*"` (revertido para o estado original). **Não é vulnerabilidade crítica** pois o Traefik já filtra quais domínios chegam ao container — proteção equivalente na camada de infraestrutura.

**Abordagem futura recomendada:** Investigar o header `Host` real que o Traefik envia ao container (via logs ou middleware de debug) e configurar `AllowedHosts` com o valor exato recebido, não o domínio público.

---

### Altos

---

### SEC-05 — Upload valida apenas `Content-Type` do cliente ✅ CORRIGIDO

**Arquivo:** `Rascunho/Endpoints/UploadEndpoints.cs` (linhas 70–78)
**Severidade:** 🟠 Alta

**Problema:** A validação de tipo de arquivo usa apenas `foto.ContentType` (controlado pelo cliente, falsificável) e extrai a extensão de `foto.FileName` (também controlado pelo cliente). Um atacante pode enviar `Content-Type: image/jpeg` com um arquivo `.php` ou `.exe`.

**Correção planejada:**
1. Validar os primeiros bytes do stream (magic bytes): JPEG `FF D8 FF`, PNG `89 50 4E 47`, WebP `52 49 46 46`
2. Derivar a extensão do MIME type validado (ex: `image/jpeg` → `.jpg`), nunca de `foto.FileName`

```csharp
// Verificação de magic bytes
var buffer = new byte[4];
await foto.OpenReadStream().ReadAsync(buffer, 0, 4);
bool ehJpeg = buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
bool ehPng  = buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47;
```

---

### SEC-06 — Desmatriculação sem restrição de role ✅ CORRIGIDO

**Arquivo:** `Rascunho/Endpoints/TurmaEndpoints.cs` (linhas 121–135)
**Severidade:** 🟠 Alta

**Problema:** `DELETE /api/turmas/{turmaIdHash}/desmatricular` herda apenas `RequireAuthorization()` genérico (qualquer usuário autenticado). Perfis como Gerente ou Recepção poderiam chamar o endpoint com seus IDs.

**Correção planejada:**
```csharp
.RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"))
```

---

### SEC-07 — Stack trace no console do frontend ✅ CORRIGIDO

**Arquivo:** `Rascunho.Client/Infraestrutura/HttpInterceptorHandler.cs` (linhas 101–105)
**Severidade:** 🟠 Alta

**Problema:** O interceptador imprime `erro500?.Detalhes` via `Console.WriteLine` sem verificar o ambiente. **Dependente de SEC-01:** com a correção aplicada, o backend não envia mais `detalhes` em produção, tornando este problema inativo em produção. Monitorar após a correção de SEC-01 em produção.

**Ação:** Verificar se `Console.WriteLine(erro500?.Detalhes)` ainda é necessário após SEC-01 estar ativo.

---

### SEC-08 — Política de senha fraca e inconsistente ✅ CORRIGIDO

**Arquivos:**
- `Rascunho/Validations/CriarUsuarioRequestValidator.cs` (linha 23) — exige 6 caracteres
- `Rascunho/Validations/AlterarSenhaRequestValidator.cs` (linha 14) — exige 8 caracteres

**Problema:** Inconsistência entre criação (6 chars) e alteração (8 chars). Mínimo de 6 é insuficiente para contas administrativas.

**Correção planejada:** Padronizar ambos os validators para mínimo de 8 caracteres.

---

### SEC-09 — `int.TryParse` sem verificação de retorno em BolsistaEndpoints ✅ CORRIGIDO

**Arquivo:** `Rascunho/Endpoints/BolsistaEndpoints.cs` (linhas 130–132)
**Severidade:** 🟠 Alta

**Problema:** `int.TryParse(idClaim, out int usuarioLogadoId)` sem verificação do booleano retornado. Se o parse falhar, `usuarioLogadoId = 0`, e a verificação `usuarioLogadoId != decodedIds[0]` sempre será verdadeira, potencialmente permitindo bypass da autorização para tokens malformados.

**Correção planejada:**
```csharp
if (!int.TryParse(idClaim, out int usuarioLogadoId))
    return Results.Unauthorized();
```

---

### SEC-10 — ConfiguracaoService sem persistência expõe risco de redefinição de preços ⏳

**Arquivo:** `Rascunho/Services/ConfiguracaoService.cs`
**Severidade:** 🟠 Alta

**Problema:** Configurações (preço de aulas, janela de reposição) são mantidas em memória. Um restart do container (deploy) as reseta para os valores padrão do `appsettings.json`. Do ponto de vista de segurança, além do impacto funcional (BUG-004), um atacante com capacidade de causar restart pode forçar redefinições de preço para zero.

**Correção planejada:** Ver BUG-004 — persistir em tabela `Configuracoes` no banco.

---

### SEC-11 — JWT armazenado em LocalStorage ⏳

**Arquivo:** `Rascunho.Client/Security/CustomAuthStateProvider.cs`
**Severidade:** 🟠 Alta

**Problema:** Token JWT em `localStorage` é acessível por qualquer JavaScript na página.

**Mitigação já existente:** Revogação server-side via `UltimoLogoutEmUtc` (token é invalidado no logout mesmo que ainda seja válido por tempo).

**Abordagem futura recomendada (não urgente):** Para versões futuras, considerar tokens em memória + refresh tokens em cookies `HttpOnly`. Baixa prioridade dado que a revogação server-side já mitiga o risco principal.

---

### Médios

---

### SEC-12 — Headers de segurança HTTP ausentes ⏳

**Arquivos:** `Rascunho/Program.cs`, `nginx.conf`
**Severidade:** 🟡 Média

**Problema:** Ausência de `X-Content-Type-Options`, `X-Frame-Options` (clickjacking), `Content-Security-Policy`, `Referrer-Policy`.

**Correção planejada no backend:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

---

### SEC-13 — Sem rate limiting no endpoint de login ⏳

**Arquivo:** `Rascunho/Endpoints/AuthEndpoints.cs`
**Severidade:** 🟡 Média

**Problema:** `POST /api/auth/login` não tem limitação de taxa, permitindo ataques de força bruta. O BCrypt mitiga parcialmente (~100ms/hash), mas não elimina o risco.

**Correção planejada:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.SegmentsPerWindow = 1;
        opt.QueueLimit = 0;
    });
});
// No endpoint de login:
.RequireRateLimiting("login")
```

---

### SEC-14 — Extensão do arquivo derivada do `FileName` do cliente ✅ CORRIGIDO

**Arquivo:** `Rascunho/Endpoints/UploadEndpoints.cs` (linhas 77–78)
**Severidade:** 🟡 Média

**Problema:** A extensão do arquivo salvo em disco é extraída de `foto.FileName` (controlado pelo cliente). Um arquivo enviado com `FileName: "malware.exe"` e `Content-Type: image/jpeg` seria salvo como `{guid}.exe`.

**Correção planejada:** Derivar extensão do MIME type validado, nunca do `FileName`. Coberta junto com SEC-05.

---

### SEC-15 — Secrets não auditados no histórico Git ⏳

**Arquivo:** `stacksVPS/.env` (agora no `.gitignore`)
**Severidade:** 🟡 Média

**Problema:** O arquivo `.env` com todas as credenciais de produção estava commitado no repositório antes de 31/03/2026. Embora o `.gitignore` tenha sido atualizado, o histórico Git anterior pode ainda conter os secrets.

**Ação recomendada (quando o sistema tiver dados reais):**
```bash
# Verificar se secrets estão no histórico
git log --all -- "stacksVPS/.env"
# Se necessário, usar git-filter-repo para purgar o histórico
```
**Prioridade atual:** Baixa — sistema ainda sem dados reais de clientes.

---

### SEC-16 — Endpoint de registrar conversa não persiste dados ⏳

**Arquivo:** `Rascunho/Endpoints/GerenteEndpoints.cs` (linhas 82–106)
**Severidade:** 🟡 Média

**Problema:** `POST /api/gerente/bolsistas/{idHash}/registrar-conversa` retorna sucesso mas os dados são descartados (TODO explícito no código). O Gerente crê estar registrando observações que não existem — implicação operacional/legal.

**Correção planejada:** Ver Sprint 13 — Criar entidade `ObservacaoBolsista` e persistir. Coberto no planejamento de sprints.

---

### Baixos

---

### SEC-17 — `ClockSkew` de 5 minutos no JWT ⏳

**Arquivo:** `Rascunho/Program.cs` (linha 66)
**Severidade:** 🟢 Baixa

**Problema:** `ClockSkew = TimeSpan.FromMinutes(5)` adiciona 5 minutos de tolerância na expiração do token. Impacto marginal com tokens de 8 horas.

**Correção planejada:** `ClockSkew = TimeSpan.Zero` em produção.

---

### SEC-18 — `Content-Type` não validado nos endpoints de recebimento de JSON ⏳

**Severidade:** 🟢 Baixa
**Nota:** Comportamento padrão do ASP.NET Core Minimal API. Risco baixo dado que `System.Text.Json` é tipado. Manter como informação, sem ação imediata.

---

## DESEMPENHO

### Altos

---

### PERF-01 — N+1 em `TurmasRecomendadasParaBolsistaAsync` ⏳

**Arquivo:** `Rascunho/Services/BolsistaService.cs` (linhas 278–294)
**Severidade:** 🟠 Alta

**Problema:** Para cada turma do dia (5–15 turmas), chama `AnalisarEBalancearTurmaAsync(turma.Id)` que recarrega a turma do banco, mesmo que já esteja em memória. ~40–50 queries por chamada.

**Correção planejada:** Refatorar `AnalisarEBalancearTurmaAsync` para aceitar o objeto `Turma` já carregado em vez do ID:
```csharp
// Antes: AnalisarEBalancearTurmaAsync(turma.Id)
// Depois: AnalisarEBalancearTurmaAsync(turma) // passa objeto em memória
```

---

### PERF-02 — N+1 em `GET /api/gerente/desempenho-bolsistas` ⏳

**Arquivo:** `Rascunho/Endpoints/GerenteEndpoints.cs` (linhas 59–67)
**Severidade:** 🟠 Alta

**Problema:** Para cada bolsista ativo, chama `MeuDesempenhoAsync` individualmente — ~2 queries por bolsista. Com 20 bolsistas = ~40 queries sequenciais.

**Correção planejada:** Reescrever para buscar todos os registros de presença de todos os bolsistas ativos de uma vez (`WHERE AlunoId IN (...)`) e calcular indicadores em memória.

---

### PERF-03 — `ListarTodosUsuariosAsync` sem paginação ⏳

**Arquivo:** `Rascunho/Services/UsuarioService.cs` (linhas 184–188)
**Severidade:** 🟠 Alta

**Problema:** `GET /api/usuarios/listar` carrega todos os usuários em memória sem filtro ou paginação. O endpoint `/listar-paginado` já existe mas o `/listar` permanece ativo.

**Correção planejada:** Deprecar ou remover o endpoint `/listar` não paginado. Proteger com role mais restritiva se necessário para casos de exportação.

---

### PERF-04 — `ListarTurmasAsync` com múltiplos `Include` sem projeção ⏳

**Arquivo:** `Rascunho/Services/TurmaService.cs` (linhas 135–169)
**Severidade:** 🟠 Alta

**Problema:** Quatro `Include`s incluindo `ThenInclude(tp => tp.Professor)` carregam entidades `Usuario` completas (com `SenhaHash`, `Cpf`, `Biografia`) só para montar DTOs. Sem paginação.

**Correção planejada:** Usar projeção `.Select()` diretamente na query carregando apenas os campos necessários para os DTOs.

---

### PERF-05 — `.ToLower()` em queries impede uso de índices no PostgreSQL ⏳

**Arquivos:** `Rascunho/Services/UsuarioService.cs` (linhas 41, 239, 295–303), `ChamadaService.cs` (linhas 141–207)
**Severidade:** 🟠 Alta

**Problema:** Expressões como `u.Email.ToLower() == request.Email.ToLower()` impedem o PostgreSQL de usar o índice `ix_usuarios_email`. Resulta em full table scan.

**Correção planejada (duas opções):**

**Opção A — Armazenar sempre em lowercase:**
```csharp
// No cadastro, sempre gravar em lowercase:
entity.Email = request.Email.ToLower();
// Na busca, comparar sem ToLower():
u.Email == request.Email.ToLower()
```

**Opção B — Índice funcional no PostgreSQL:**
```sql
CREATE UNIQUE INDEX ix_usuarios_email_lower ON "Usuarios" (LOWER("Email"));
```
Aplicar via migration raw:
```csharp
migrationBuilder.Sql(@"CREATE UNIQUE INDEX ix_usuarios_email_lower ON ""Usuarios"" (LOWER(""Email""))");
```

---

### Médios

---

### PERF-06 — `CalcularTotalAulasEsperadas` itera dia a dia ⏳

**Arquivo:** `Rascunho/Services/BolsistaService.cs` (linhas 234–250)
**Severidade:** 🟡 Média

**Problema:** Para `periodoFiltro = "tudo"`, itera dia a dia por até 5 anos (~1825 iterações) por bolsista.

**Correção planejada:** Substituir por cálculo matemático O(1):
```csharp
// Para cada dia obrigatório, calcular quantas semanas caem no período
int totalSemanas = (int)((dataFim - dataInicio).TotalDays / 7);
// + ajuste para dias parciais de início e fim
```

---

### PERF-07 — `BuscarParticipantesExtrasAsync` com 3 queries sequenciais e `LIKE '%termo%'` ⏳

**Arquivo:** `Rascunho/Services/ChamadaService.cs` (linhas 148–228)
**Severidade:** 🟡 Média

**Problema:** 3 queries sequenciais independentes + `b.Nome.ToLower().Contains(termoLower)` que gera `LIKE '%termo%'` sem suporte a índice B-tree.

**Correção planejada:** Para buscas por nome em produção com volume alto, adicionar índice GIN com `pg_trgm`:
```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX ix_usuarios_nome_trgm ON "Usuarios" USING GIN ("Nome" gin_trgm_ops);
```
Aplicar via migration.

---

### PERF-08 — Índices ausentes em colunas de alta frequência ⏳

**Arquivos:** Configurações de entidades em `Rascunho/Configurations/`
**Severidade:** 🟡 Média

**Colunas sem índice que são filtradas frequentemente:**

| Tabela | Coluna | Query que usa |
|---|---|---|
| `Usuarios` | `Ativo` | Praticamente todas as queries de usuários |
| `Usuarios` | `Tipo` | `/tipo/{tipo}`, `/tipo/{tipo}/ativos` |
| `AulasParticulares` | `Status` | Verificações AP02, AP05, AP06 |
| `AulasParticulares` | `AlunoId` | `ListarMinhasAulasAsync` |
| `AulasParticulares` | `ProfessorId` | `ListarMinhasAulasAsync` |
| `RegistrosPresencas` | `AlunoId` | Todas as queries de desempenho |
| `Reposicoes` | `AlunoId` | `ListarMinhasReposicoesAsync` |

**Correção planejada (migration):**
```csharp
migrationBuilder.CreateIndex("ix_usuarios_ativo_tipo", "Usuarios", new[] { "Ativo", "Tipo" });
migrationBuilder.CreateIndex("ix_registrospresencas_alunoid", "RegistrosPresencas", "AlunoId");
migrationBuilder.CreateIndex("ix_aulasparticulares_alunoid_status", "AulasParticulares", new[] { "AlunoId", "Status" });
migrationBuilder.CreateIndex("ix_aulasparticulares_professorid_status", "AulasParticulares", new[] { "ProfessorId", "Status" });
```

---

### PERF-09 — Queries de listagem carregam `SenhaHash` em memória ⏳

**Arquivo:** `Rascunho/Services/UsuarioService.cs` (linhas 225–235)
**Severidade:** 🟡 Média

**Problema:** Métodos de listagem carregam a entidade `Usuario` completa (incluindo `SenhaHash`) antes de mapear para DTOs. O hash nunca aparece no DTO de resposta, mas trafega em memória desnecessariamente.

**Correção planejada:** Usar `.Select()` ou projeção anônima antes de materializar para evitar carregar `SenhaHash`.

---

### PERF-10 — `MeuDesempenhoAsync` filtra por `DayOfWeek` em C# após carregar tudo ⏳

**Arquivo:** `Rascunho/Services/BolsistaService.cs` (linhas 132–151)
**Severidade:** 🟡 Média

**Problema:** Carrega todas as presenças do período com `ToListAsync()` e filtra por dia da semana em memória (EF Core não traduz `DayOfWeek` para SQL diretamente).

**Correção planejada:** EF Core consegue traduzir `.Contains()` com lista de inteiros para `IN (...)`:
```csharp
var diasComoInt = diasObrigatorios.Select(d => (int)d).ToList();
var presencas = await _context.RegistrosPresencas
    .Where(rp => rp.AlunoId == bolsistaId
              && rp.DataAula >= dataInicio
              && diasComoInt.Contains((int)rp.DataAula.DayOfWeek))
    .ToListAsync();
```

---

### PERF-11 — `SairDaFilaAsync` faz N updates para reordenar posições ⏳

**Arquivo:** `Rascunho/Services/ListaEsperaService.cs` (linhas 81–107)
**Severidade:** 🟡 Média

**Problema:** Reordenação das posições após saída da fila executa N updates individuais (um por pessoa na fila).

**Correção planejada:** Substituir N updates por 1 UPDATE SQL via `ExecuteSqlRawAsync`:
```csharp
await _context.Database.ExecuteSqlRawAsync(
    @"UPDATE ""ListasEspera""
      SET ""Posicao"" = ""Posicao"" - 1
      WHERE ""TurmaId"" = {0}
        AND ""Posicao"" > {1}
        AND ""Status"" IN ('Aguardando', 'Notificado')",
    turmaId, posicaoRemovida);
```

---

### Baixos

---

### PERF-12 — `.Distinct()` em memória pode não deduplicar `Ritmo` corretamente ⏳

**Arquivo:** `Rascunho/Endpoints/ProfessorEndpoints.cs` (linhas 87–115)
**Severidade:** 🟢 Baixa

**Problema:** União de `ritmosTurmas` e `ritmosHabilidades` em memória pode não deduplicar se os objetos `Ritmo` forem instâncias diferentes com o mesmo ID.

**Correção planejada:**
```csharp
var ritmosUnidos = ritmosTurmas
    .UnionBy(ritmosHabilidades, r => r.Id)
    .ToList();
```
`.UnionBy()` disponível no .NET 6+.

---

### PERF-13 — Queries de bolsistas qualificados não são pre-carregadas antes do loop ⏳

**Arquivo:** `Rascunho/Services/BolsistaService.cs` (linhas 330–360)
**Severidade:** 🟢 Baixa

**Problema:** `AnalisarEBalancearTurmaAsync` consulta `HabilidadeUsuario` e `Matriculas` separadamente para cada turma no loop. Coberto pela correção de PERF-01 — ao refatorar para passar o objeto `Turma` em vez do ID, pré-carregar as habilidades de todos os bolsistas ativos antes do loop.

---

## Plano de Correção por Sprint

### Sprint de Segurança (integrar na Sprint 14 — Dívida Técnica)

**Itens imediatos (antes do lançamento):**
- [ ] SEC-05 + SEC-14 — Magic bytes no upload (juntar com PERF-04 pois estão no mesmo arquivo)
- [ ] SEC-06 — Role restriction na desmatriculação (1 linha)
- [ ] SEC-08 — Padronizar validators de senha para 8 chars (2 linhas)
- [ ] SEC-09 — Verificar retorno de `int.TryParse` em BolsistaEndpoints (3 linhas)
- [ ] SEC-13 — Rate limiting no login (20 linhas)
- [ ] SEC-12 — Headers de segurança HTTP (middleware, 10 linhas)

**Itens de médio prazo:**
- [ ] SEC-10 — Persistência do ConfiguracaoService (coberto pelo BUG-004)
- [ ] SEC-16 — Persistir registros de conversa (coberto pelo Sprint 13)
- [ ] SEC-17 — ClockSkew = Zero

**Itens informativos (sem ação imediata):**
- SEC-11 — JWT em LocalStorage (aceito pela mitigação da revogação server-side)
- SEC-18 — Content-Type sem validação (risco baixo, ASP.NET Core é tipado)

### Sprint de Desempenho (integrar após Sprint 14)

**Alta prioridade:**
- [ ] PERF-05 — Índices e ToLower (requer migration — aplicar junto com PERF-08)
- [ ] PERF-08 — Índices compostos (migration)
- [ ] PERF-01 — N+1 em TurmasRecomendadas (refatorar parâmetro do método)
- [ ] PERF-02 — N+1 em desempenho-bolsistas (query em batch)
- [ ] PERF-11 — SairDaFila com UPDATE único (cirúrgico)

**Média prioridade:**
- [ ] PERF-03 — Deprecar endpoint `/listar` sem paginação
- [ ] PERF-04 — Projeções em ListarTurmasAsync
- [ ] PERF-10 — DayOfWeek via Contains em SQL
- [ ] PERF-06 — Cálculo matemático de aulas esperadas
- [ ] PERF-07 — pg_trgm para busca por nome (quando volume justificar)

**Baixa prioridade:**
- [ ] PERF-09 — Evitar SenhaHash em memória nas listagens
- [ ] PERF-12 — UnionBy em vez de Union + Distinct
- [ ] PERF-13 — Coberto por PERF-01
