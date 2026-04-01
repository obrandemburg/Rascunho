# Planejamento de Sprints — Ponto da Dança

> Gerado em: 27/03/2026 | Atualizado em: 31/03/2026
> Sprint 9 concluída | Auditoria de segurança realizada | SEC-01/02/03 corrigidos em produção

---

## Premissas do Planejamento

- **Sprint atual concluída:** Sprint 9 (BUG-001, BUG-002, BUG-003, BUG-011, BUG-012 — todos corrigidos)
- **Próxima sprint:** Sprint 10
- **Duração estimada por sprint:** ~1 semana de desenvolvimento ativo
- **Convenção:** `BE` = backend, `FE` = frontend, `INFRA` = infraestrutura
- **Prioridade máxima:** Fechar o MVP (fase 1.1) antes de iniciar a fase 1.2
- **Critérios de conclusão de sprint:** Todos os itens implementados, sem regressões conhecidas, deploy em `develop` estável

---

## Sprint 9 — Bugs Críticos + Endpoint Ausente

**Objetivo:** Eliminar os dois bugs críticos (BUG-001 e BUG-013) e corrigir os bugs de alto impacto mais rápidos de resolver.

**Estimativa:** 3–5 dias

### Itens

#### [BE] BUG-001: Criar `GET /api/turmas/{idHash}/alunos`

**Arquivo:** `Rascunho/Endpoints/TurmaEndpoints.cs` + `Rascunho/Services/TurmaService.cs`

**O que implementar:**

1. No `TurmaService`, criar método `ListarAlunosDaTurmaAsync(int turmaId)`:
   ```csharp
   // Retorna alunos com matrícula ativa (Status != "Cancelada")
   // Inclui: AlunoIdHash, Nome, FotoUrl, Papel
   ```

2. No `TurmaEndpoints`, adicionar:
   ```csharp
   group.MapGet("/{turmaIdHash}/alunos", async (...) => { ... })
       .RequireAuthorization(policy => policy.RequireRole("Professor", "Recepção", "Gerente"));
   ```

3. **Validação especial para Professor (RN-CHA04):** Verificar se a turma pertence ao professor logado antes de retornar os dados. Professor não pode ver alunos de turma alheia.

**DTO de retorno:** `List<AlunoMatriculadoDto>` com `{ AlunoIdHash, Nome, FotoUrl, Papel }`

---

#### [FE] BUG-013: Externalizar URL da API do frontend

**Arquivo:** `Rascunho.Client/Program.cs` + novo `Rascunho.Client/wwwroot/appsettings.json`

**Passos:**

1. Criar `Rascunho.Client/wwwroot/appsettings.json`:
   ```json
   {
     "ApiBaseUrl": "http://5.161.202.169:8080/"
   }
   ```

2. Em `Program.cs`, substituir o hardcode:
   ```csharp
   // ANTES:
   BaseAddress = new Uri("http://5.161.202.169:8080/")

   // DEPOIS:
   var apiUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000/";
   BaseAddress = new Uri(apiUrl)
   ```

3. No `Dockerfile.Client` e `docker-compose.yml`, garantir que o arquivo `appsettings.json` seja copiado corretamente (já é incluído no `wwwroot`).

---

#### [BE] BUG-002: Corrigir RN-BOL05 para bloquear salão também

**Arquivo:** `Rascunho/Services/AulaParticularService.cs`

**Mudança cirúrgica:** Remover a condição `if (ehSolo)`. O bloqueio deve ocorrer para **qualquer** aula particular no dia obrigatório do bolsista:

```csharp
// ANTES: bloqueava apenas solo
if (ehSolo && eDiaObrigatorio) throw ...

// DEPOIS: bloqueia qualquer modalidade
if (eDiaObrigatorio) throw new RegraNegocioException(
    "Bolsistas não podem agendar aulas particulares nos seus dias obrigatórios. [RN-BOL05]");
```

---

#### [BE] BUG-003: Recalcular posições da fila após saída

**Arquivo:** `Rascunho/Services/ListaEsperaService.cs` — método `SairDaFilaAsync`

**Implementação:**
Após `_context.SaveChangesAsync()`, buscar todos os registros `Aguardando` ou `Notificado` da turma, ordenar por `Posicao` e reatribuir posições sequenciais (1, 2, 3...).

**Observação:** O mesmo recalculo deve ser aplicado após `NotificarProximoAsync` quando uma entrada expira ou é convertida.

---

#### [BE] BUG-011: Restringir CORS em produção

**Arquivo:** `Rascunho/Program.cs`

**Implementação:**
Ler os domínios permitidos de `appsettings.json` via `builder.Configuration.GetSection("Cors:Origins").Get<string[]>()`. No `appsettings.Production.json`, listar apenas o IP/domínio da VPS. Em desenvolvimento, permitir localhost.

---

#### [FE] BUG-012: Adicionar link "Reagendar Aula" ao NavMenu do Bolsista

**Arquivo:** `Rascunho.Client/Layout/NavMenu.razor`

Inserir no bloco `AuthorizeView Roles="Bolsista"`:
```razor
<MudNavLink Href="/reagendar" Icon="@Icons.Material.Filled.EventRepeat">
    Reagendar Aula
</MudNavLink>
```

---

## Sprint 10 — UX da Recepção Completa

**Objetivo:** Fechar as telas que faltam para o perfil de Recepção ter o MVP completo.

**Estimativa:** 5–7 dias

### Itens

#### [FE] Criar tela: Recepção — Início/Dashboard (Tela 20 do spec)

**Arquivo:** `Rascunho.Client/Pages/Admin/InicioAdmin.razor`
**Rota:** `/admin`

**Dados a exibir (calculados a partir de endpoints existentes):**

| Métrica | Endpoint |
|---|---|
| Total de alunos ativos | `GET /api/usuarios/tipo/Aluno/ativos` |
| Total de bolsistas ativos | `GET /api/usuarios/tipo/Bolsista/ativos` |
| Professores ativos | `GET /api/usuarios/tipo/Professor/ativos` |
| Turmas hoje | `GET /api/turmas/listar-ativas` → filtrar por dia da semana |
| Avisos vigentes | endpoint de avisos |

**Layout:** Cards de métricas no topo + lista de turmas do dia + lista de avisos vigentes.

**NavMenu:** Adicionar rota `/admin` para Recepção e Gerente com ícone `Dashboard`.

---

#### [FE] Criar tela: Recepção — Sistema de Bolsistas (Tela 24 do spec)

**Arquivo:** `Rascunho.Client/Pages/Admin/SistemaBolsistas.razor`
**Rota:** `/admin/bolsistas`

**Seções:**

1. **Lista de bolsistas ativos:**
   - Endpoint: `GET /api/gerente/desempenho-bolsistas` (já existe, também acessível para Recepção se ajustar a role)
   - Exibir: foto, nome, papel, dias obrigatórios, frequência e indicador de situação
   - Somente leitura para Recepção (sem ações de gerência)

2. **Sugestões de novos bolsistas:**
   - Requer novo endpoint no backend (ver Sprint 10 — Backend)
   - Exibir alunos candidatos com critérios atendidos
   - Botões: "Indicar para bolsista" (abre modal de confirmação) / "Ignorar"

**NavMenu:** Adicionar ao grupo Admin:
```razor
<MudNavLink Href="/admin/bolsistas" Icon="@Icons.Material.Filled.WorkspacePremium">
    Sistema de Bolsistas
</MudNavLink>
```

---

#### [BE] Expor endpoint de sugestões de bolsistas para Recepção

**Arquivo:** `Rascunho/Endpoints/BolsistaEndpoints.cs`

Criar `GET /api/bolsistas/sugestoes-candidatos` acessível para `Recepção, Gerente`. A lógica deve retornar alunos ativos que atendam critérios configuráveis (frequência alta, múltiplas turmas, tempo de casa). Implementar método `SugerirCandidatosABolsistaAsync` no `BolsistaService`.

---

#### [FE + BE] GerenciarTurmas — Editar turma

**Frontend:** Adicionar botão "Editar" na lista de turmas de `GerenciarTurmas.razor`. Ao clicar, abrir modal com formulário pré-preenchido dos dados da turma.

**Backend:** Criar endpoint `PUT /api/turmas/{idHash}/editar` no `TurmaEndpoints.cs` + método `EditarTurmaAsync` no `TurmaService` com as validações TUR01–TUR03.

**Campos editáveis:** Professor, sala, dia da semana, horário início/fim, nível, vagas máximas, link WhatsApp.

---

#### [FE] CriarAviso — Listagem com editar e remover

**Arquivo:** `Rascunho.Client/Pages/Admin/CriarAviso.razor`

**O que adicionar:**
1. Seção "Avisos Publicados" abaixo do formulário de criação
2. Listar avisos ativos e expirados em tabela/lista
3. Botão "Editar" → abre modal com formulário pré-preenchido → chama `PUT /api/avisos/{idHash}`
4. Botão "Remover" → confirmação → chama `DELETE /api/avisos/{idHash}`

**Verificar no backend:** Se os endpoints `PUT` e `DELETE` já existem em `AvisoEndpoints.cs`.

---

## Sprint 11 — UX do Aluno Completa + Quadro de Turmas

**Objetivo:** Fechar as telas que faltam para o Aluno ter o MVP completo.

**Estimativa:** 5–7 dias

### Itens

#### [FE] Criar: Aluno — Quadro de Turmas autenticado (Tela 2 do spec)

**Arquivo:** `Rascunho.Client/Pages/Aluno/QuadroTurmas.razor`
**Rota:** `/quadro-turmas`

**Funcionalidades:**

1. **Listagem de turmas ativas** via `GET /api/turmas/listar-ativas`
2. **Filtros:** ritmo, professor, dia da semana, horário, nível, data de início
3. **Card de cada turma com:**
   - Nome do ritmo, professor, dia/horário, sala, nível, vagas disponíveis
   - **Se tem vaga:** botão "Matricular" → modal de confirmação → `POST /api/turmas/{idHash}/matricular`
   - **Se lotada:** botão "Entrar na Fila de Espera" → `POST /api/turmas/{idHash}/lista-espera`
4. **Badge "Já matriculado"** nas turmas em que o aluno já está inscrito

**NavMenu:** Atualizar link do Aluno de `/turmas` (público) para `/quadro-turmas` (autenticado).

---

#### [FE] MinhasAulas — Botão "Ver Avisos da Turma"

**Arquivo:** `Rascunho.Client/Pages/Aluno/MinhasAulas.razor`

**O que adicionar a cada card de turma:**
```razor
<MudButton Href="@($"/turma/{turma.IdHash}/avisos")"
           Variant="Variant.Text" Color="Color.Info"
           StartIcon="@Icons.Material.Filled.Campaign">
    Ver Avisos da Turma
</MudButton>
```

**Backend:** Verificar se `AvisoEndpoints` tem `GET /api/avisos/turma/{idHash}`. Se não, adicionar filtro de `TurmaId` ao endpoint de listagem de avisos.

---

#### [BE + FE] Avisos por turma — endpoint e tela

**Backend:** Garantir que `GET /api/avisos?turmaIdHash=xxx` ou `GET /api/avisos/turma/{idHash}` exista e retorne apenas avisos daquela turma, sem expirados.

**Frontend:** Criar `Rascunho.Client/Pages/Aluno/AvisosDaTurma.razor` em rota `/turma/{IdHash}/avisos` ou exibir via modal em `MinhasAulas.razor`.

---

#### [FE] BUG-008: Padronizar DTOs locais com DTOs compartilhados

**Arquivo:** Vários componentes Blazor (`PainelAluno.razor`, `MinhasAulas.razor`, etc.)

**Ação:** Substituir DTOs locais definidos em `@code { class TurmaXyzDto {...} }` pelos DTOs de `Rascunho.Shared`. Isso garante que desserialização do JSON do backend funcione corretamente.

**Impacto:** Requer que `Rascunho.Client` referenciea `Rascunho.Shared` (já deve estar configurado) e use `@using Rascunho.Shared.DTOs` nos componentes.

---

## Sprint 12 — UX do Professor Completa

**Objetivo:** Fechar as telas que faltam para o Professor ter o MVP completo.

**Estimativa:** 4–6 dias

### Itens

#### [FE] MinhasTurmas — "Ver/Publicar Avisos da Turma" por card

**Arquivo:** `Rascunho.Client/Pages/Professor/MinhasTurmas.razor`

**O que adicionar ao card de cada turma:**
1. Botão "Ver Avisos" → modal com lista de avisos da turma
2. Botão "Publicar Aviso" → modal com formulário de aviso direcionado à turma

**Backend:** Verificar e ajustar `POST /api/avisos` para aceitar `TurmaIdHash` opcional. O professor só pode publicar avisos para suas próprias turmas (validação no backend via Claims).

---

#### [FE] AulasParticulares do Professor — Aba "Minha Disponibilidade"

**Arquivo:** `Rascunho.Client/Pages/Professor/AulasParticulares.razor`

**O que implementar:**
1. Adicionar 3ª aba `<MudTabPanel Text="Minha Disponibilidade">`
2. Carregar disponibilidades: `GET /api/professor/disponibilidade` → grade de dias/horários
3. Interface de edição: checkboxes ou grade visual (dia × horário)
4. Salvar: `PUT /api/professor/disponibilidade` com lista de slots selecionados

**Backend:** Verificar se `ProfessorDisponibilidadeService` já tem os métodos necessários e se os endpoints estão em `ProfessorEndpoints.cs`.

---

#### [FE] NavMenu do Professor — Adicionar "Meu Perfil"

**Arquivo:** `Rascunho.Client/Layout/NavMenu.razor`

Simples adição no bloco Professor:
```razor
<MudNavLink Href="/perfil" Icon="@Icons.Material.Filled.Person">
    Meu Perfil
</MudNavLink>
```

---

#### [FE] BUG-014: Remover link AulaExperimental do NavMenu do Aluno (temporário)

**Arquivo:** `Rascunho.Client/Layout/NavMenu.razor`

Comentar ou remover o link `/aula-experimental` do menu do Aluno até que a UX da tela seja definida e implementada (fase 1.2).

---

## Sprint 13 — Gerente com Ações Completas no Quadro de Desempenho

**Objetivo:** Completar as ações por bolsista no Quadro de Desempenho do Gerente (spec tela 25).

**Estimativa:** 4–6 dias

### Itens

#### [FE + BE] Editar dias obrigatórios de bolsista

**Frontend:** No modal de detalhes do bolsista em `QuadroDesempenho.razor`, adicionar seção com `MudSelect` para os 2 dias obrigatórios + botão "Salvar".

**Backend:** Endpoint já existe: `PUT /api/bolsistas/{idHash}/dias-obrigatorios`. Apenas conectar o frontend.

---

#### [BE + FE] Registrar conversa/observação sobre bolsista

**Backend:**
1. Criar entidade `ObservacaoBolsista { Id, BolsistaId, GerenteId, Texto, DataRegistro }`
2. Migration
3. Endpoint `POST /api/bolsistas/{idHash}/observacoes` (role Gerente)
4. Endpoint `GET /api/bolsistas/{idHash}/observacoes` (role Gerente)

**Frontend:** No modal de detalhes, adicionar textarea + botão "Registrar" + histórico de observações em timeline.

---

#### [BE + FE] Desativar bolsa (converter bolsista em aluno)

**Backend:**
1. Endpoint `POST /api/bolsistas/{idHash}/desativar-bolsa` (role Gerente)
2. Lógica no `BolsistaService`: converter tipo de `Bolsista` para `Aluno` no TPH (alterar discriminador `Tipo`)
3. Preservar histórico de presenças e habilidades

**Frontend:** Botão "Desativar Bolsa" no modal de detalhes → confirmação → feedback de sucesso.

---

#### [FE] Filtro por Papel (condutor/conduzido) no Quadro

**Arquivo:** `Rascunho.Client/Pages/Gerencia/QuadroDesempenho.razor`

Adicionar `MudSelect` de filtro por papel ao painel de filtros. Aplicar `Where(b => filtroPapel == "Todos" || b.Papel == filtroPapel)` na lista client-side.

---

#### [BE] BUG-006: Limitar cálculo de frequência a período configurável

**Arquivo:** `Rascunho/Services/BolsistaService.cs`

Adicionar parâmetro de janela temporal (padrão: 90 dias) ao cálculo de frequência. Ler valor de configuração via `ConfiguracaoService`.

---

## Sprint 14 — Segurança, Desempenho e Dívida Técnica

**Objetivo:** Eliminar os débitos técnicos e implementar as correções de segurança e desempenho identificadas na auditoria de 31/03/2026.

> Ver detalhamento completo de cada item em `contexto/auditoria_seguranca_desempenho.md`

**Estimativa:** 5–7 dias

### Itens de Segurança

#### [BE] SEC-05 + SEC-14: Validar magic bytes no upload de foto

**Arquivo:** `Rascunho/Endpoints/UploadEndpoints.cs`

Ler os primeiros 4 bytes do stream e verificar assinatura JPEG (`FF D8 FF`), PNG (`89 50 4E 47`) ou WebP (`52 49 46 46`). Derivar extensão do MIME validado, nunca de `foto.FileName`. Rejeitar com 400 se magic bytes não corresponderem a nenhum formato aceito.

---

#### [BE] SEC-06: Restringir role na desmatriculação

**Arquivo:** `Rascunho/Endpoints/TurmaEndpoints.cs`

Adicionar `.RequireAuthorization(policy => policy.RequireRole("Aluno", "Bolsista", "Líder"))` ao endpoint `DELETE /{turmaIdHash}/desmatricular`.

---

#### [BE] SEC-08: Padronizar tamanho mínimo de senha

**Arquivos:**
- `Rascunho/Validations/CriarUsuarioRequestValidator.cs` — alterar de 6 para 8 caracteres mínimos
- `Rascunho/Validations/AlterarSenhaRequestValidator.cs` — já está em 8, confirmar

---

#### [BE] SEC-09: Verificar retorno de `int.TryParse` em BolsistaEndpoints

**Arquivo:** `Rascunho/Endpoints/BolsistaEndpoints.cs` (linhas 130–132)

```csharp
if (!int.TryParse(idClaim, out int usuarioLogadoId))
    return Results.Unauthorized();
```

---

#### [BE] SEC-13: Rate limiting no endpoint de login

**Arquivo:** `Rascunho/Program.cs` + `Rascunho/Endpoints/AuthEndpoints.cs`

```csharp
builder.Services.AddRateLimiter(options =>
    options.AddSlidingWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.SegmentsPerWindow = 1;
    }));
// No endpoint:
.RequireRateLimiting("login")
```

---

#### [BE] SEC-12: Headers HTTP de segurança

**Arquivo:** `Rascunho/Program.cs`

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

### Itens de Desempenho

#### [BE] PERF-05 + PERF-08: Índices e eliminação de `ToLower()` em queries

**Migration:** `AddIndicesDesempenho`

1. Padronizar `Email` para lowercase no cadastro (gravar sempre em `request.Email.ToLower()`) e remover `.ToLower()` nas buscas
2. Criar índices via migration:

```csharp
migrationBuilder.CreateIndex("ix_usuarios_ativo_tipo", "Usuarios", new[] { "Ativo", "Tipo" });
migrationBuilder.CreateIndex("ix_registrospresencas_alunoid", "RegistrosPresencas", "AlunoId");
migrationBuilder.CreateIndex("ix_aulasparticulares_alunoid_status", "AulasParticulares", new[] { "AlunoId", "Status" });
migrationBuilder.CreateIndex("ix_aulasparticulares_professorid_status", "AulasParticulares", new[] { "ProfessorId", "Status" });
migrationBuilder.Sql(@"CREATE UNIQUE INDEX ix_usuarios_email_lower ON ""Usuarios"" (LOWER(""Email""))");
```

---

#### [BE] PERF-01: Eliminar N+1 em `TurmasRecomendadasParaBolsistaAsync`

**Arquivo:** `Rascunho/Services/BolsistaService.cs`

Refatorar `AnalisarEBalancearTurmaAsync` para aceitar o objeto `Turma` já carregado em vez do ID, evitando re-queries para cada turma do loop.

---

#### [BE] PERF-02: Eliminar N+1 em `desempenho-bolsistas`

**Arquivo:** `Rascunho/Services/BolsistaService.cs` + `GerenteEndpoints.cs`

Reescrever para buscar todos os registros de presença de todos os bolsistas com uma única query `WHERE AlunoId IN (...)` e calcular indicadores em memória.

---

#### [BE] PERF-11: `SairDaFilaAsync` com UPDATE único

**Arquivo:** `Rascunho/Services/ListaEsperaService.cs`

Substituir N updates de reordenação por um único `ExecuteSqlRawAsync`:
```csharp
await _context.Database.ExecuteSqlRawAsync(
    @"UPDATE ""ListasEspera"" SET ""Posicao"" = ""Posicao"" - 1
      WHERE ""TurmaId"" = {0} AND ""Posicao"" > {1}
        AND ""Status"" IN ('Aguardando', 'Notificado')",
    turmaId, posicaoRemovida);
```

---

#### [BE] PERF-10: Filtro de `DayOfWeek` via SQL em `MeuDesempenhoAsync`

**Arquivo:** `Rascunho/Services/BolsistaService.cs`

```csharp
var diasComoInt = diasObrigatorios.Select(d => (int)d).ToList();
.Where(rp => diasComoInt.Contains((int)rp.DataAula.DayOfWeek))
```

---

### Itens de Dívida Técnica (originais da Sprint 14)

#### [BE] BUG-004 + BUG-005: Persistir Configurações no Banco

**Passos:**
1. Criar entidade `Configuracao { Chave string, Valor string }`
2. Migration `AddConfiguracoes`
3. Popular com valores padrão: `AulaParticular:PrecoPadrao = "80.00"`, `Reposicao:JanelaElegibilidadeDias = "30"`, etc.
4. Refatorar `ConfiguracaoService` para ler/gravar no banco via `AppDbContext`
5. Remover a dependência de `IConfigurationRoot` do `ConfiguracaoService`

---

#### [BE] BUG-010: Remover entidade `Interesse` obsoleta

**Passos:**
1. Verificar se há referências a `Interesse` em algum endpoint ativo
2. Criar migration `RemoverInteresses` com `migrationBuilder.DropTable("Interesses")`
3. Remover `DbSet<Interesse>` do `AppDbContext` e a classe da pasta `Entities`

---

#### [INFRA] Migrations sem Designer file

**Ação:** Rodar `dotnet ef migrations add CorrecaoDesignerListaEspera --project Rascunho` para regenerar as migrations faltantes com os arquivos `.Designer.cs` corretos.

---

#### [INFRA] Endpoint de saúde (healthcheck)

**Motivação:** O CI/CD faz `docker compose up -d` mas não verifica se o backend iniciou corretamente. Se houver falha de migração ou configuração, o deploy parece bem-sucedido mas a API está fora.

**Implementação:**
```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();
```

Adicionar ao `deploy.yml` um step de verificação pós-deploy:
```yaml
- name: Verify deployment
  run: curl -f http://5.161.202.169:8080/health || exit 1
```

---

#### [INFRA] Dashboard endpoint de resumo para Gerente

**Arquivo:** `Rascunho/Endpoints/GerenteEndpoints.cs`

Criar `GET /api/gerente/dashboard-resumo` que retorna em uma chamada: total de alunos, professores, bolsistas, turmas ativas hoje, próximos eventos. Atualizar `Dashboard.razor` para usar este endpoint em vez de 4 chamadas paralelas.

---

## Sprint 15 — Notificações Push (FCM)

**Objetivo:** Implementar notificações push reais via Firebase Cloud Messaging.

**Estimativa:** 7–10 dias (complexidade alta)

### Itens

#### [BE] Adicionar campo `FcmToken` ao usuário

1. Adicionar `FcmToken string?` à entidade `Usuario`
2. Migration `AddFcmTokenToUsuario`
3. Endpoint `PUT /api/usuarios/fcm-token` (role: qualquer usuário autenticado):
   ```csharp
   group.MapPut("/fcm-token", async (AtualizarFcmTokenRequest req, UsuarioService service, ClaimsPrincipal user) => { ... });
   ```

---

#### [BE] Implementar `FirebaseNotificacaoService`

**Arquivo:** `Rascunho/Services/FirebaseNotificacaoService.cs`

**Dependências:** `FirebaseAdmin` NuGet package

**Implementação:**
```csharp
public class FirebaseNotificacaoService : INotificacaoService
{
    public async Task EnviarAsync(int usuarioId, string titulo, string corpo)
    {
        // 1. Buscar FcmToken do usuário no banco
        // 2. Se token nulo, ignorar (usuário sem PWA instalado)
        // 3. Enviar via FirebaseMessaging.DefaultInstance.SendAsync(message)
        // 4. Se token inválido/expirado, limpar o token no banco
    }
}
```

**Configuração:** `appsettings.json` com `Firebase:ServiceAccountPath` apontando para o JSON de credenciais. O arquivo deve estar no volume Docker e nunca no repositório.

---

#### [FE] Registrar Service Worker Firebase e obter token

**Arquivo:** `Rascunho.Client/wwwroot/firebase-messaging-sw.js` (novo)

```javascript
importScripts('https://www.gstatic.com/firebasejs/10.x.x/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.x.x/firebase-messaging-compat.js');

firebase.initializeApp({ /* config */ });
const messaging = firebase.messaging();

messaging.onBackgroundMessage(payload => {
    self.registration.showNotification(payload.notification.title, {
        body: payload.notification.body,
        icon: '/icons/icon-192.png'
    });
});
```

**Interop Blazor → JS:** Criar `wwwroot/js/fcm.js` com função `registerFcmToken()` que solicita permissão ao usuário e retorna o token. Chamar após login em `AuthService.cs`.

---

#### [FE] Enviar token FCM ao backend após login

**Arquivo:** `Rascunho.Client/Services/AuthService.cs`

Após login bem-sucedido e armazenamento do JWT no localStorage, chamar `PUT /api/usuarios/fcm-token` com o token obtido do Firebase.

---

## Sprint 16 — Fase 1.2: Eventos, Experimental e Líder

**Objetivo:** Iniciar implementação das funcionalidades confirmadas para a fase 1.2.

**Estimativa:** 10–14 dias

### Itens

#### [FE] Aula Experimental — UX Completa

**Arquivo:** `Rascunho.Client/Pages/Aluno/AulaExperimental.razor`

**Fluxo a implementar:**
1. Aluno busca turmas disponíveis para aula experimental (filtros: ritmo, professor, dia)
2. Seleciona turma e data
3. Sistema verifica disponibilidade via `AulaExperimentalService`
4. Confirmação + notificação ao professor

**Backend:** Verificar e completar `AulaExperimentalEndpoints.cs` e `AulaExperimentalService.cs`.

---

#### [FE] Eventos e Ingressos — UX Completa

**Frontend:**
1. Reintroduzir `Ingressos.razor` no NavMenu do Aluno
2. Tela de listagem de eventos futuros com detalhes e botão de compra de ingresso
3. Tela de gerenciamento de eventos para Recepção/Gerente

**Backend:** Completar `EventoEndpoints.cs` e `EventoService.cs` com:
- Listagem pública de eventos futuros
- Criação/edição/encerramento de eventos (Recepção/Gerente)
- Compra de ingresso (Aluno)
- Listagem de ingressos do usuário

---

#### [FE + BE] Telas e perfil do Líder

**Perfil Líder:** Segundo o spec, visualiza faturamento. Definir escopo exato:
- Visualizar receita por turma/mês
- Visualizar inadimplência (se houver controle financeiro)
- Acesso a dashboard de alto nível

**Criar:** `Rascunho.Client/Pages/Lider/` com as telas do Líder + atualizar NavMenu.

---

## Roadmap Macro

```
Sprint 9  (1 semana)  → Bugs críticos + endpoint alunos da turma
Sprint 10 (1-2 sem)   → Recepção completa (dashboard, bolsistas, editar turma)
Sprint 11 (1-2 sem)   → Aluno completo (quadro de turmas, avisos da turma)
Sprint 12 (1 sem)     → Professor completo (disponibilidade, avisos, perfil)
Sprint 13 (1 sem)     → Gerente completo (ações no quadro de desempenho)
Sprint 14 (1 sem)     → Dívida técnica (persistência config, healthcheck, CORS)
Sprint 15 (2 sem)     → Notificações Push FCM ← desbloqueia 6 funcionalidades
Sprint 16 (2-3 sem)   → Fase 1.2: Eventos, Experimental, Líder
```

---

## Critérios de "MVP Completo" (pré-Sprint 16)

O MVP (fase 1.1) estará completo quando:

- [ ] Todos os 6 perfis têm acesso funcional a todas as telas planejadas na spec
- [ ] Notificações push funcionam para todos os gatilhos mapeados
- [ ] Nenhum bug crítico (🔴) está aberto
- [ ] Nenhum bug alto (🟠) que viole regra de negócio está aberto
- [ ] URL da API não está hardcoded no código-fonte
- [ ] CORS restrito ao domínio de produção
- [ ] Configurações persistem entre deploys
- [ ] `GET /api/turmas/{idHash}/alunos` implementado e funcional

---

## Instruções para o Claude ao iniciar cada sprint

Ao começar uma nova sessão de implementação, informe o Claude:

1. **Qual sprint está em andamento** (ex: "Estamos na Sprint 10")
2. **Quais itens já foram concluídos** (ex: "BUG-001 já foi resolvido")
3. **Arquivos de contexto relevantes:** sempre inclua `camada2_stack_tecnico.md`, `bugs_e_erros_logicos.md` e este arquivo
4. **Branch atual de desenvolvimento** (ex: `develop` ou `feature/sprint-10`)

Exemplo de prompt de abertura de sessão:
```
[Inclua camada1_visao_geral.md, camada2_stack_tecnico.md, implementacoes_faltantes.md, bugs_e_erros_logicos.md, planejamento_sprints.md]

Estamos na Sprint 10. Já concluímos a Sprint 9 (BUG-001, BUG-013, BUG-002, BUG-003, BUG-011, BUG-012).

Hoje vamos implementar a tela de Dashboard da Recepção (/admin).
Branch: develop
```
