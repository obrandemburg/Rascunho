# Bugs e Erros Lógicos — Ponto da Dança

> Gerado em: 27/03/2026 | Atualizado em: 28/03/2026
> BUG-001 a BUG-012, BUG-014 corrigidos em 28/03/2026

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

## Resumo de Bugs por Severidade

| ID | Descrição curta | Severidade | Status |
|---|---|---|---|
| BUG-001 | Endpoint `GET /api/turmas/{idHash}/alunos` ausente | 🔴 Crítico | ✅ Corrigido |
| BUG-013 | IP da VPS hardcoded no frontend | 🔴 Crítico | ⏳ Pendente |
| BUG-015 | Endpoint duplicado causava lista de alunos vazia | 🔴 Crítico | ✅ Corrigido |
| BUG-002 | RN-BOL05 bloqueia só "solo", não "salão" | 🟠 Alto | ✅ Corrigido |
| BUG-003 | Fila de espera com buracos de posição | 🟠 Alto | ✅ Corrigido |
| BUG-004 | ConfiguracaoService perde dados no restart | 🟠 Alto | ⏳ Pendente |
| BUG-011 | CORS AllowAnyOrigin em produção | 🟠 Alto | ⏳ Pendente |
| BUG-005 | ReposicaoService e ConfiguracaoService dessincronizados | 🟡 Médio | ✅ Corrigido |
| BUG-006 | Frequência calculada sobre todo o histórico | 🟡 Médio | ✅ Corrigido |
| BUG-007 | TurmasObrigatorias e TurmasRecomendadas duplicadas | 🟡 Médio | ✅ Corrigido |
| BUG-008 | PainelAluno com DTO local incompatível | 🟡 Médio | ✅ Corrigido |
| BUG-012 | Bolsista sem link para Reagendar no NavMenu | 🟡 Médio | ✅ Corrigido |
| BUG-014 | AulaExperimental exposta mas incompleta | 🟡 Médio | ✅ Corrigido |
| BUG-009 | GerenciarTurmas sem aviso no erro de recarregamento | 🟢 Baixo | ✅ Corrigido |
| BUG-010 | Entidade `Interesse` obsoleta no banco | 🟢 Baixo | ✅ Corrigido |
