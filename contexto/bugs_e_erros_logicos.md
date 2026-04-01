# Bugs e Erros Lógicos — Ponto da Dança

> Gerado em: 27/03/2026 | Atualizado em: 31/03/2026
> BUG-001 a BUG-012, BUG-014, BUG-015 corrigidos em 28/03/2026 | BUG-016 a BUG-022 corrigidos em 30/03/2026
> BUG-023 corrigido em 31/03/2026
> SEC-01, SEC-02, SEC-03 corrigidos em 31/03/2026 (auditoria de segurança — ver `auditoria_seguranca_desempenho.md`)

---

## Convenção de severidade

- 🔴 **Crítico** — quebra funcionalidade em produção, dados errados ou endpoint ausente
- 🟠 **Alto** — comportamento errado perceptível ao usuário, viola regra de negócio
- 🟡 **Médio** — degradação de qualidade ou inconsistência que não bloqueia uso
- 🟢 **Baixo** — má prática, dívida técnica, impacto mínimo imediato

---

## ~~BUG-001~~ — ✅ CORRIGIDO — Endpoint `GET /api/turmas/{idHash}/alunos` não existe no backend

**Severidade:** 🔴 Crítico → ✅ Resolvido em 28/03/2026
**Tipo:** Endpoint ausente / 404 silencioso

**Descrição:**
O frontend chamava `api/turmas/{idHash}/alunos` em duas páginas (`GerenciarTurmas.razor` e `MinhasTurmas.razor`), mas o endpoint não existia no backend.

**Correção aplicada:**
- `TurmaService.cs`: adicionado método `ListarAlunosDaTurmaAsync`
- `TurmaEndpoints.cs`: adicionado `GET /{turmaIdHash}/alunos` com autorização para Professor, Recepção e Gerente

---

## ~~BUG-002~~ — ✅ CORRIGIDO — RN-BOL05 implementada parcialmente: bloqueia só "solo", não "salão"

**Severidade:** 🟠 Alto → ✅ Resolvido em 28/03/2026
**Tipo:** Regra de negócio violada

**Correção aplicada (três camadas):**
- `AulaParticularService.cs`: removida condicional `if (ehSolo)` — agora qualquer modalidade é bloqueada em dias obrigatórios [RN-BOL05]
- `TurmaService.cs` (BOL04): variável `ehDancaRestrita` agora inclui "Dança de salão" além de "Dança solo"
- `QuadroTurmas.razor`: botão "Matricular" desativado para bolsistas em turmas de salão ou solo nos seus dias obrigatórios; `Modalidade` adicionada a `ObterTurmaResponse` (DTO compartilhado) e ao mapper

---

## ~~BUG-003~~ — ✅ CORRIGIDO — Fila de espera: posições ficam com buracos após saída

**Severidade:** 🟠 Alto → ✅ Resolvido em 28/03/2026
**Tipo:** Erro lógico / dados inconsistentes

**Correção aplicada:**
- `ListaEsperaService.cs` — método `SairDaFilaAsync`: após remover o registro, reordena as posições de todos os demais com status `Aguardando` ou `Notificado`, eliminando buracos na sequência.

---

## BUG-004 — ConfiguracaoService não persiste entre reinicializações do servidor

**Severidade:** 🟠 Alto
**Tipo:** Perda de dados de configuração
**Arquivo:** `Rascunho/Services/ConfiguracaoService.cs`

**Descrição:**
O `ConfiguracaoService` altera valores via `IConfigurationRoot.GetSection(key).Value = novoValor`. Essas alterações ficam **em memória** e são perdidas ao reiniciar o servidor (deploy, crash, atualização).

Situação concreta: o Gerente atualiza o preço da aula particular para R$ 100,00. Após o próximo deploy (que acontece automaticamente a cada push em `main`), o preço volta para R$ 80,00.

O próprio código documenta isso com `// IMPORTANTE: Essas alterações são IN-MEMORY.`

**Correção:**
Criar tabela `Configuracoes` no banco e migrar `ConfiguracaoService` para persistir/ler do banco. Alternativa de curto prazo: usar arquivo `appsettings.override.json` gravável no volume Docker.

---

## ~~BUG-005~~ — ✅ CORRIGIDO — ReposicaoService e ConfiguracaoService leem fontes diferentes

**Severidade:** 🟡 Médio → ✅ Resolvido em 28/03/2026
**Tipo:** Inconsistência de configuração em runtime

**Correção aplicada:**
- `ReposicaoService.cs`: substituída injeção direta de `IConfiguration` por `ConfiguracaoService`
- `JanelaElegibilidadeDias` agora lê via `_configuracaoService.ObterJanelaReposicaoDias()` (fonte única)
- Nota: BUG-004 (perda de dados no restart) permanece como dívida técnica a resolver com persistência em banco

---

## ~~BUG-006~~ — ✅ CORRIGIDO — Indicador de frequência do bolsista calculado sobre todo o histórico

**Severidade:** 🟡 Médio → ✅ Resolvido em 28/03/2026
**Tipo:** Erro lógico / métrica enganosa

**Correção aplicada:**
- `BolsistaService.cs` — `MeuDesempenhoAsync`: adicionado parâmetro `periodoFiltro` (padrão `"30dias"`). Suporta `"30dias"`, `"mes"` e `"tudo"`.
- `BolsistaEndpoints.cs`: endpoint `/meu-desempenho` aceita `?periodo=` via `[FromQuery]`
- `Desempenho.razor`: adicionado seletor de período (MudChipSet) com 3 opções; método `CarregarDesempenho()` separado para recarregar ao mudar filtro; título do histórico reflete o período ativo

---

## ~~BUG-007~~ — ✅ CORRIGIDO — `TurmasObrigatorias.razor` e `TurmasRecomendadas.razor` chamam o mesmo endpoint

**Severidade:** 🟡 Médio → ✅ Resolvido em 28/03/2026
**Tipo:** Funcionalidade duplicada / confusão de UX

**Correção aplicada:**
- `TurmasObrigatorias.razor`: **deletado**
- `NavMenu.razor`: link "Turmas Obrigatórias" removido; link "Turmas Recomendadas" renomeado para **"Turmas do Dia"**
- `BolsistaService.cs` — `TurmasRecomendadasParaBolsistaAsync`: refatorado para aceitar `DayOfWeek? diaDaSemana` (padrão = dia atual). Agora retorna turmas mais desbalanceadas do dia selecionado, independente dos dias obrigatórios do bolsista.
- `BolsistaEndpoints.cs`: endpoint `/turmas-recomendadas` aceita `?diaDaSemana=N` (0=Dom … 6=Sáb) via `[FromQuery]`
- `TurmasRecomendadas.razor`: adicionado filtro de dia (MudSelect, padrão = dia atual); API chamada com `?diaDaSemana={diaSelecionado}`; página renomeada para "Turmas do Dia"

---

## ~~BUG-008~~ — ✅ CORRIGIDO — `PainelAluno.razor` usa DTO local incompatível com o response do backend

**Severidade:** 🟡 Médio → ✅ Resolvido em 28/03/2026
**Tipo:** Potencial desserialização incorreta / dados vazios

**Correção aplicada:**
- `PainelAluno.razor`: removida classe local `TurmaResumoDto` (que tinha `TimeSpan HorarioInicio`)
- Adicionado `@using Rascunho.Shared.DTOs`
- `proximaAula` tipado como `ObterTurmaResponse?` (DTO compartilhado com `string HorarioInicio`)
- Template do banner de "Próxima aula" atualizado: `@proximaAula.HorarioInicio` diretamente (string, sem `.ToString("hh\\:mm")`)

---

## BUG-009 — `GerenciarTurmas.razor` sem tratamento de erro ao recarregar lista após criar turma

**Severidade:** 🟢 Baixo
**Tipo:** UX degradada em falha de rede
**Arquivo:** `Rascunho.Client/Pages/Admin/GerenciarTurmas.razor`

**Descrição:**
Após criar uma turma com sucesso, o código tenta recarregar a lista:
```csharp
try
{
    turmas = await Http.GetFromJsonAsync<List<TurmaListaDto>>("api/turmas/listar-ativas") ?? new();
}
catch
{
    // ⚠️ Recarregamento falhou, mas a turma foi criada
    // Usuário verá a confirmação, mas lista não atualiza
}
```

Se o recarregamento falhar, o usuário vê "Turma criada com sucesso!" mas a lista não inclui a nova turma, sem qualquer aviso. Próxima navegação mostraria corretamente, mas é confuso.

**Correção:** Snackbar de aviso adicionado no `catch` do recarregamento em `GerenciarTurmas.razor`.

---

## ~~BUG-009~~ — ✅ CORRIGIDO — `GerenciarTurmas.razor` sem tratamento de erro ao recarregar lista após criar turma

**Severidade:** 🟢 Baixo → ✅ Resolvido em 28/03/2026
**Correção aplicada:**
- `GerenciarTurmas.razor`: `Snackbar.Add(...)` adicionado no catch do recarregamento, informando o usuário que a lista pode não estar atualizada.

---

## ~~BUG-010~~ — ✅ CORRIGIDO — Entidade `Interesse` obsoleta ainda presente no banco

**Severidade:** 🟢 Baixo → ✅ Resolvido em 28/03/2026
**Tipo:** Dívida técnica / entidade legada

**Correção aplicada:**
- `AppDbContext.cs`: `DbSet<Interesse>` removido
- `TurmaConfiguration.cs`: `InteresseConfiguration` removida
- `Entities/Interesse.cs`: arquivo deletado
- Migration `20260328000001_RemoveInteresseObsoleto.cs` criada — executa `DROP TABLE "Interesses"`
- `AppDbContextModelSnapshot.cs` atualizado (blocos da entidade `Interesse` removidos)

---

## BUG-011 — CORS AllowAnyOrigin em produção

**Severidade:** 🟠 Alto
**Tipo:** Vulnerabilidade de segurança
**Arquivo:** `Rascunho/Program.cs`

**Descrição:**
A configuração CORS aceita requisições de qualquer domínio. Em produção isso permite que scripts maliciosos de outros domínios façam requisições autenticadas à API usando credenciais do usuário (cookies/tokens armazenados).

**Correção:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
            "http://5.161.202.169",
            "https://seudominio.com.br"
        )
        .AllowAnyHeader()
        .AllowAnyMethod());
});
```

---

## ~~BUG-012~~ — ✅ CORRIGIDO — NavMenu do Bolsista não tem link para "Reagendar Aula"

**Severidade:** 🟡 Médio → ✅ Resolvido em 28/03/2026
**Tipo:** Funcionalidade inacessível via navegação

**Correção aplicada:**
- `NavMenu.razor`: `<MudNavLink Href="/reagendar" Icon="@Icons.Material.Filled.EventRepeat">Reagendar Aula</MudNavLink>` adicionado ao bloco `<AuthorizeView Roles="Bolsista">`

---

## BUG-013 — IP da VPS hardcoded no frontend

**Severidade:** 🔴 Crítico
**Tipo:** Configuração de infraestrutura incorreta
**Arquivo:** `Rascunho.Client/Program.cs`

**Descrição:**
```csharp
BaseAddress = new Uri("http://5.161.202.169:8080/")
```

O IP está hardcoded no código-fonte. Impactos:
1. Qualquer mudança de servidor exige recompilação e redeploy
2. Impossível ter ambiente de staging/dev apontando para servidor diferente sem alterar o código
3. O endpoint usa HTTP (não HTTPS) — tráfego não criptografado em produção

**Correção:** Externalizar para `wwwroot/appsettings.json` e ler via `builder.Configuration["ApiBaseUrl"]`.

---

## ~~BUG-014~~ — ✅ CORRIGIDO — `AulaExperimental.razor` acessível pelo Aluno mas UX indefinida

**Severidade:** 🟡 Médio → ✅ Resolvido em 28/03/2026
**Tipo:** Tela incompleta exposta na navegação

**Correção aplicada:**
- `NavMenu.razor`: link `/aula-experimental` removido do bloco Aluno (comentado com nota de fase 1.2)
- `AulaExperimental.razor`: banner `MudAlert` com "Funcionalidade em desenvolvimento" adicionado no topo da página — se o aluno acessar diretamente via URL, verá o aviso

---

## BUG-015 — Endpoint duplicado `GET /api/turmas/{idHash}/alunos` causava lista de alunos vazia

**Severidade:** 🔴 Crítico → ✅ Resolvido em 28/03/2026
**Tipo:** Conflito de rota / retorno vazio silencioso

**Descrição:**
O endpoint `GET /api/turmas/{turmaIdHash}/alunos` estava registrado em dois lugares:
1. `TurmaEndpoints.cs` (via group `/api/turmas`) — implementação correta usando `TurmaService`
2. `ProfessorEndpoints.cs` (via `app.MapGet(...)` absoluto) — implementação duplicada sem null checks

O registro duplicado causava comportamento imprevisível de roteamento no ASP.NET Core Minimal API. Além disso, `GerenciarTurmas.razor` capturava o erro silenciosamente (catch vazio), exibindo "Nenhum aluno matriculado" mesmo quando havia alunos. `MinhasTurmas.razor` tinha `TimeSpan HorarioInicio` no DTO local, mas a API retorna string — causando falha na desserialização de turmas.

**Correção aplicada:**
- `ProfessorEndpoints.cs`: endpoint duplicado removido — rota canônica permanece em `TurmaEndpoints.cs`
- `GerenciarTurmas.razor`: catch da requisição `/alunos` agora exibe `Snackbar` de erro em vez de silenciar a falha
- `MinhasTurmas.razor`: `TurmaDto.HorarioInicio` e `HorarioFim` corrigidos de `TimeSpan` para `string` (espelhando `ObterTurmaResponse`)

---

## ~~BUG-016~~ — ✅ CORRIGIDO — Migration `CorrigirListaEsperaDataTypes` com tipos SQL Server bloqueava startup

**Severidade:** 🔴 Crítico → ✅ Resolvido em 30/03/2026
**Tipo:** Migration inválida / startup falhando

**Descrição:**
A migration `20260326002000_CorrigirListaEsperaDataTypes` foi gerada com tipos do **SQL Server** (`datetimeoffset`, `datetime2`) em vez de tipos do **PostgreSQL**. Ao iniciar, a aplicação tentava executar:
```sql
ALTER TABLE "ListasEspera" ALTER COLUMN "DataEntrada" TYPE datetimeoffset;
```
PostgreSQL não reconhece o tipo `datetimeoffset` (erro 42704), causando falha em todas as 5 tentativas de migration com retry policy. Como essa migration bloqueava a sequência, as migrations seguintes também não rodavam, incluindo `20260330000001_AddUltimoLogoutEmUtc` — fazendo a coluna `UltimoLogoutEmUtc` não existir no banco e causar erros 42703 em runtime.

**Efeito cascata:**
1. `20260326002000_CorrigirListaEsperaDataTypes` → falha (tipo SQL Server)
2. `20260328000001_RemoveInteresseObsoleto` → bloqueada
3. `20260330000001_AddUltimoLogoutEmUtc` → bloqueada → coluna ausente → erro 42703

**Correção aplicada:**
- `20260326002000_CorrigirListaEsperaDataTypes.cs`: substituídos tipos inválidos nos métodos `Up()` e `Down()`:
  - `DateTimeOffset` / `"datetimeoffset"` → `DateTime` / `"timestamp with time zone"`
  - `DateTime` / `"datetime2"` → `DateTime` / `"timestamp without time zone"`

---

## ~~BUG-017~~ — ✅ CORRIGIDO — Avatar da AppBar não carregava foto/inicial após login

**Severidade:** 🟠 Alto → ✅ Resolvido em 30/03/2026
**Tipo:** Bug de estado reativo / UX

**Descrição:**
`MainLayout.razor` carregava `_fotoUrl` apenas em `OnInitializedAsync`. Quando o usuário fazia login na mesma sessão (sem recarregar a página), o `AuthenticationStateChanged` era disparado pelo `CustomAuthStateProvider`, o `AuthorizeView` re-renderizava mostrando o `UserAvatar`, mas `_fotoUrl` nunca era atualizado — então a foto/inicial não aparecia corretamente.

Adicionalmente, o método `Sair()` inline não chamava `AuthService.LogoutAsync()`, perdendo a revogação do token no servidor (POST `/api/auth/logout`) que atualiza `UltimoLogoutEmUtc`.

**Correção aplicada em `MainLayout.razor`:**
- Adicionado `@implements IDisposable`
- Subscrito `AuthStateProvider.AuthenticationStateChanged += OnAuthStateChanged` em `OnInitializedAsync`
- `OnAuthStateChanged`: recarrega `_fotoUrl` do LocalStorage e chama `InvokeAsync(StateHasChanged)`
- Implementado `Dispose()` para remover a subscricão e evitar memory leak
- `Sair()` agora delega para `AuthService.LogoutAsync()` (revogação no servidor + limpeza local)

---

## ~~BUG-018~~ — ✅ CORRIGIDO — Avatar exibia texto "Foto de X" quando a imagem falhava ao carregar

**Severidade:** 🟠 Alto → ✅ Resolvido em 30/03/2026
**Tipo:** Bug visual / fallback ausente

**Descrição:**
O `UserAvatar.razor` tentava carregar `<img src="@FotoUrl">` sem tratar falha de rede. Quando a URL não era acessível (ex: foto salva com host da VPS `5.161.202.169:8080` sendo acessada em ambiente diferente), o browser exibia o texto do atributo `alt="Foto de @Nome"` no espaço circular do avatar. Na AppBar e na página Desempenho aparecia o texto "Foto" (truncado) dentro do círculo amarelo.

**Correção aplicada em `UserAvatar.razor`:**
- Adicionado campo `_imgErro = false`
- Adicionado `@onerror="OnImageError"` na tag `<img>` → seta `_imgErro = true` e força re-render
- Condição alterada para `!string.IsNullOrWhiteSpace(FotoUrl) && !_imgErro`
- `alt` esvaziado (`alt=""`) para não exibir texto em falha enquanto o estado é atualizado
- `OnParametersSet` reseta `_imgErro` quando `FotoUrl` muda (troca de usuário/foto)

---

## ~~BUG-019~~ — ✅ CORRIGIDO — Clicar no avatar da AppBar não abria o menu de opções

**Severidade:** 🟠 Alto → ✅ Resolvido em 30/03/2026
**Tipo:** Bug de interação / evento bloqueado

**Descrição:**
O `MudTooltip` envolvendo o `UserAvatar` dentro do `ActivatorContent` do `MudMenu` interceptava os eventos de mouse antes que o `MudMenu` pudesse detectá-los. Resultado: clicar no avatar não abria o dropdown com "Ver perfil" e "Sair".

**Correção aplicada em `MainLayout.razor`:**
- Removido `<MudTooltip>` do `ActivatorContent` do `MudMenu`
- O nome do usuário já é exibido como cabeçalho dentro do `ChildContent` do menu, tornando o tooltip redundante

---

## ~~BUG-020~~ — ✅ CORRIGIDO — MudMenu não abria ao clicar no avatar (MudBlazor v9 breaking change)

**Severidade:** 🔴 Crítico → ✅ Resolvido em 30/03/2026
**Tipo:** Breaking change MudBlazor v9 / menu inacessível

**Descrição:**
Em MudBlazor v9, `MudMenu.ActivatorContent` mudou de `RenderFragment` para `RenderFragment<MenuContext>`. O menu não abre mais automaticamente via `ActivationEvent` — é obrigatório chamar `context.ToggleAsync()` explicitamente no evento de clique do ativador. O código anterior usava `ActivationEvent="@MouseEvent.LeftClick"` e um `UserAvatar` simples sem `@onclick`, então o menu nunca abria.

**Correção aplicada em `MainLayout.razor`:**
- Removido `ActivationEvent="@MouseEvent.LeftClick"` (não tem efeito em v9 com ActivatorContent customizado)
- Adicionado `<div @onclick="context.ToggleAsync" style="cursor: pointer;">` envolvendo o `UserAvatar`
- O `context` é o `MenuContext` tipado injetado pelo `RenderFragment<MenuContext>` do MudBlazor v9

---

## ~~BUG-021~~ — ✅ CORRIGIDO — Foto não carregava em ambiente diferente do upload (URL com host fixo)

**Severidade:** 🟠 Alto → ✅ Resolvido em 30/03/2026
**Tipo:** Bug de ambiente / URL absoluta dependente de host

**Descrição:**
A foto era armazenada no banco com a URL absoluta do host onde foi feita o upload (ex: `http://5.161.202.169:8080/uploads/fotos/uuid.jpg`). Em desenvolvimento local, esse host não era acessível, então a imagem falhava silenciosamente (ou exibia o texto do alt — ver BUG-018).

**Correção aplicada em `UserAvatar.razor`:**
- Adicionada propriedade computada `FotoUrlNormalizada` que extrai apenas o `PathAndQuery` da URL absoluta usando `Uri.TryCreate`
- Exemplos: `http://5.161.202.169:8080/uploads/fotos/uuid.jpg` → `/uploads/fotos/uuid.jpg`
- O browser carrega o caminho relativo contra a origem atual (dev local ou produção), sem depender do host original
- O `<img src>` agora usa `FotoUrlNormalizada` em vez de `FotoUrl` diretamente

---

## ~~BUG-022~~ — ✅ CORRIGIDO — Mixed Content: foto HTTP bloqueada em site HTTPS

**Severidade:** 🔴 Crítico → ✅ Resolvido em 30/03/2026
**Tipo:** Mixed Content / arquitetura de URL

**Causa raiz:**
O `UploadEndpoints.cs` construía a URL pública da foto usando `ctx.Request.Scheme + ctx.Request.Host`, que refletia o host interno do backend (`http://5.161.202.169:8080`), não o domínio público. Atrás do reverse proxy Traefik, o backend não recebia o host original da requisição. Resultado: fotos salvas com `http://5.161.202.169:8080/uploads/fotos/uuid.jpg`. O frontend HTTPS em `pontodadanca.trindaflow.com.br` tentava carregar essa URL HTTP com IP e o browser bloqueava.

Além disso, o caminho `/uploads/fotos/...` no Traefik era roteado para o container nginx do frontend, que não tem esses arquivos — mesmo que o Mixed Content fosse resolvido, as fotos dariam 404.

**Correção em 3 partes:**

1. **`UploadEndpoints.cs` — Novos uploads**: URL pública agora é sempre `/api/fotos/{nomeArquivo}` (relativa). O Traefik roteia `/api/*` para o backend em todos os ambientes.

2. **`UploadEndpoints.cs` — Novo endpoint**: `GET /api/fotos/{nomeArquivo}` serve o arquivo diretamente pelo backend (sem autenticação, com Content-Type correto por extensão). Sem path traversal (Path.GetFileName).

3. **`UserAvatar.razor` — Retrocompatibilidade**: `FotoUrlNormalizada` agora extrai o `nomeArquivo` de qualquer formato de URL (absoluta com IP, relativa `/uploads/`, ou já no novo formato `/api/fotos/`) e sempre retorna `/api/fotos/{nomeArquivo}`. Fotos antigas no banco continuam funcionando sem migration.

---

## ~~BUG-023~~ — ✅ CORRIGIDO — "Turmas do Dia" indicava turmas de "Dança solo" para bolsistas

**Severidade:** 🟠 Alto → ✅ Resolvido em 31/03/2026
**Tipo:** Regra de negócio violada
**Arquivo:** `Rascunho/Services/BolsistaService.cs`

**Descrição:**
O método `TurmasRecomendadasParaBolsistaAsync` buscava todas as turmas ativas do dia selecionado sem filtrar pela modalidade. Turmas de "Dança solo" eram incluídas na lista de recomendações exibida no endpoint `/turmas-recomendadas` ("Turmas do Dia"). Como turmas solo são individuais, não se beneficiam da presença de bolsistas para balancear pares — a indicação era incorreta e potencialmente confusa.

**Correção aplicada:**
- `BolsistaService.cs` — `TurmasRecomendadasParaBolsistaAsync`: adicionada condição `t.Ritmo.Modalidade.ToLower() != "dança solo"` ao `Where` da query de `turmasDoDia`. O filtro ocorre na mesma query EF Core (não em memória), aproveitando o `.Include(t => t.Ritmo)` já existente.

**Observação:** A lógica de bloqueio de matrícula (BOL04) e de aulas particulares (BOL05) não foi alterada — essas regras já estavam corretas e independentes desta indicação.

---

## Resumo de Bugs por Severidade

| ID | Descrição curta | Severidade | Status |
|---|---|---|---|
| BUG-001 | Endpoint `GET /api/turmas/{idHash}/alunos` ausente | 🔴 Crítico | ✅ Corrigido |
| BUG-013 | IP da VPS hardcoded no frontend | 🔴 Crítico | ⏳ Pendente |
| BUG-015 | Endpoint duplicado causava lista de alunos vazia | 🔴 Crítico | ✅ Corrigido |
| BUG-002 | RN-BOL05 bloqueia só "solo", não "salão" | 🟠 Alto | ✅ Corrigido |
| BUG-003 | Fila de espera com buracos de posição | 🟠 Alto | ✅ Corrigido |
| BUG-004 | ConfiguracaoService perde dados no restart | 🟠 Alto | ⏳ Pendente |
| BUG-011 | CORS AllowAnyOrigin em produção | 🟠 Alto | ✅ Corrigido (31/03) |
| BUG-005 | ReposicaoService e ConfiguracaoService dessincronizados | 🟡 Médio | ✅ Corrigido |
| BUG-006 | Frequência calculada sobre todo o histórico | 🟡 Médio | ✅ Corrigido |
| BUG-007 | TurmasObrigatorias e TurmasRecomendadas duplicadas | 🟡 Médio | ✅ Corrigido |
| BUG-008 | PainelAluno com DTO local incompatível | 🟡 Médio | ✅ Corrigido |
| BUG-012 | Bolsista sem link para Reagendar no NavMenu | 🟡 Médio | ✅ Corrigido |
| BUG-014 | AulaExperimental exposta mas incompleta | 🟡 Médio | ✅ Corrigido |
| BUG-009 | GerenciarTurmas sem aviso no erro de recarregamento | 🟢 Baixo | ✅ Corrigido |
| BUG-010 | Entidade `Interesse` obsoleta no banco | 🟢 Baixo | ✅ Corrigido |
| BUG-016 | Migration com tipos SQL Server bloqueava startup | 🔴 Crítico | ✅ Corrigido |
| BUG-017 | Avatar da AppBar não carregava foto/inicial após login | 🟠 Alto | ✅ Corrigido |
| BUG-018 | Avatar exibia texto "Foto de X" com imagem inacessível | 🟠 Alto | ✅ Corrigido |
| BUG-019 | Clicar no avatar não abria o menu (MudTooltip bloqueava cliques) | 🟠 Alto | ✅ Corrigido |
| BUG-020 | MudMenu não abria — MudBlazor v9 exige context.ToggleAsync() | 🔴 Crítico | ✅ Corrigido |
| BUG-021 | Foto não carregava em ambiente diferente do upload (URL com host fixo) | 🟠 Alto | ✅ Corrigido |
| BUG-022 | Mixed Content: foto HTTP bloqueada em site HTTPS (URL via IP da VPS) | 🔴 Crítico | ✅ Corrigido |
| BUG-023 | "Turmas do Dia" indicava turmas de "Dança solo" para bolsistas | 🟠 Alto | ✅ Corrigido |

---

## Issues de Segurança (auditoria 31/03/2026)

> Detalhamento completo em `auditoria_seguranca_desempenho.md`

| ID | Descrição curta | Severidade | Status |
|---|---|---|---|
| SEC-01 | Stack trace completo exposto em produção | 🔴 Crítico | ✅ Corrigido (31/03) |
| SEC-02 | CORS `AllowAnyOrigin` em produção | 🔴 Crítico | ✅ Corrigido (31/03) |
| SEC-03 | `POST /cadastrar` sem autenticação — qualquer um criava Gerente | 🔴 Crítico | ✅ Corrigido (31/03) |
| SEC-04 | `AllowedHosts: "*"` — desativa proteção contra Host Header Injection | 🔴 Crítico | 🚫 Revertido (causa 400 em produção atrás do Traefik) |
| SEC-05 | Upload valida apenas `Content-Type` do cliente (falsificável) | 🟠 Alto | ⏳ Pendente |
| SEC-06 | Desmatriculação sem restrição de role | 🟠 Alto | ⏳ Pendente |
| SEC-08 | Política de senha inconsistente (6 chars na criação, 8 na alteração) | 🟠 Alto | ⏳ Pendente |
| SEC-09 | `int.TryParse` sem verificação de retorno em BolsistaEndpoints | 🟠 Alto | ⏳ Pendente |
| SEC-10 | ConfiguracaoService sem persistência — redefinição de preços no restart | 🟠 Alto | ⏳ Pendente (= BUG-004) |
| SEC-12 | Headers HTTP de segurança ausentes (X-Frame-Options, CSP, etc.) | 🟡 Médio | ⏳ Pendente |
| SEC-13 | Sem rate limiting no `POST /api/auth/login` | 🟡 Médio | ⏳ Pendente |
| SEC-16 | Endpoint `registrar-conversa` não persiste dados | 🟡 Médio | ⏳ Pendente (= Sprint 13) |
