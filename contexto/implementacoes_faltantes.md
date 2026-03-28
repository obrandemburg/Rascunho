# Implementações Faltantes — Ponto da Dança

> Gerado em: 27/03/2026 | Base: análise completa do código (backend + frontend)

---

## Como ler este documento

Cada item indica **o que falta**, **onde falta** (backend/frontend/ambos) e **qual o impacto**. Prioridades:
- 🔴 **Crítico** — bloqueia funcionalidade central do MVP
- 🟠 **Alto** — funcionalidade planejada no MVP, ausente
- 🟡 **Médio** — complemento importante, mas sistema funciona sem ele
- 🟢 **Baixo/Futuro** — fases 1.2, 1.3, 1.4

---

## 1.1 MVP — Implementações Faltantes

### Backend

#### 🔴 Endpoint `GET /api/turmas/{idHash}/alunos` — AUSENTE

**Impacto:** `GerenciarTurmas.razor` (Admin) e `MinhasTurmas.razor` (Professor) chamam esse endpoint para listar os alunos matriculados em uma turma. O endpoint NÃO existe em `TurmaEndpoints.cs`. Qualquer tentativa de visualizar alunos resulta em erro 404 silencioso.

**Onde implementar:** `TurmaEndpoints.cs` + `TurmaService.cs`

**O que deve retornar:** Lista de `{ AlunoIdHash, Nome, FotoUrl, Papel }` dos alunos com matrícula ativa (não cancelada) na turma informada.

**Perfis que devem acessar:** Professor (apenas suas turmas) e Recepção/Gerente (qualquer turma).

---

#### 🔴 Notificações Push (FCM) — Stub em produção

**Impacto:** `NotificacaoServiceStub` registrado em `Program.cs` nunca envia nada. Todas as notificações planejadas no MVP estão silenciosas:

| Regra | Gatilho | Destinatário |
|---|---|---|
| BOL08 | Professor finaliza chamada com bolsista presente | Bolsista |
| TUR04 | Recepção encerra turma | Todos os matriculados |
| AP aceite | Professor aceita aula particular | Aluno/Bolsista |
| AP recusa | Professor recusa aula particular | Aluno/Bolsista |
| AP cancelamento | Professor cancela aula | Aluno/Bolsista |
| Lista de espera | Vaga liberada para aluno na fila | Aluno notificado |

**O que falta:**
1. Implementar `FirebaseNotificacaoService` substituindo o stub
2. Adicionar campo `FcmToken` à entidade `Usuario` (nova migration)
3. Adicionar endpoint para registro/atualização do token FCM no cliente (`PUT /api/usuarios/fcm-token`)
4. Registrar o service worker do Firebase no frontend (`firebase-messaging-sw.js`)
5. Inicializar o SDK do Firebase no `wwwroot/` do cliente

---

#### 🟠 Endpoint `GET /api/bolsistas/sugestoes-novos-bolsistas` — Lógica existe, sem rota

**Impacto:** O planejamento do MVP (tela 24 — Sistema de Bolsistas da Recepção) prevê a funcionalidade de sugestão automática de novos bolsistas. A lógica de seleção pode existir no `BolsistaService`, mas não há endpoint público exposto nem tela no frontend.

**O que falta:** Criar endpoint em `BolsistaEndpoints.cs` restrito a `Recepção, Gerente` e conectar à tela de sistema de bolsistas (ver frontend abaixo).

---

#### 🟡 Endpoint `GET /api/gerencia/dashboard-resumo` — Inexistente

**Impacto:** `Dashboard.razor` da Gerência faz 4 chamadas separadas para montar o painel (`/api/usuarios/tipo/Aluno/ativos`, `/api/usuarios/tipo/Professor/ativos`, `/api/usuarios/tipo/Bolsista/ativos`, `/api/turmas/`). Causa latência desnecessária e o próprio código comenta que deveria existir um endpoint dedicado.

**O que falta:** Criar `GET /api/gerente/dashboard-resumo` que retorna todos os dados do dashboard em uma única query agregada.

---

#### 🟡 Persistência das Configurações (`ConfiguracaoService`)

**Impacto:** O `ConfiguracaoService` altera valores em memória via `IConfigurationRoot`. Ao reiniciar o servidor (deploy), todas as configurações modificadas pelo Gerente (preço de aula particular, janela de reposição) voltam para os valores do `appsettings.json`.

**O que falta:** Criar tabela `Configuracoes(Chave, Valor)` no banco, migrar o `ConfiguracaoService` para ler/gravar no banco em vez de memória.

---

### Frontend

#### 🔴 Tela: Recepção — Dashboard / Início (Tela 20 do spec)

**Rota prevista:** `/admin` ou `/admin/inicio`
**Status:** Não existe. Após o login, a Recepção não tem uma tela inicial própria.

**O que a spec prevê:**
- Total de alunos ativos
- Total de bolsistas ativos
- Turmas em andamento no dia
- Avisos vigentes

**Onde criar:** `Rascunho.Client/Pages/Admin/InicioAdmin.razor`

---

#### 🔴 Tela: Recepção — Sistema de Bolsistas (Tela 24 do spec)

**Rota prevista:** `/admin/bolsistas`
**Status:** Não existe. Não há link no NavMenu nem página criada.

**O que a spec prevê:**
- Visualizar sugestões automáticas de novos bolsistas
- Confirmar ou rejeitar sugestão
- Listar bolsistas ativos com tipo de desconto concedido
- Acompanhar frequência geral nos dias obrigatórios (somente leitura)

**Onde criar:** `Rascunho.Client/Pages/Admin/SistemaBolsistas.razor`
**Dependência backend:** Endpoint de sugestões mencionado acima.

---

#### 🟠 Tela: Recepção — Gerenciar Turmas — Editar Turma

**Status:** `GerenciarTurmas.razor` permite criar turma, ver alunos, ir para fila de espera e encerrar. Não permite **editar** uma turma existente (professor, sala, dia/horário, nível, vagas).

**O que falta no frontend:** Botão "Editar" no card/row da turma + formulário/modal de edição que chame os endpoints de edição existentes no backend (`PUT /{turmaIdHash}/trocar-sala` existe, mas outros campos de edição precisam de endpoint dedicado).

**O que falta no backend:** Endpoint `PUT /api/turmas/{idHash}/editar` com suporte a alterar ritmo, professor, nível, horários e vagas (com validações TUR01–TUR03).

---

#### 🟠 Tela: Aluno — Quadro de Turmas (Tela 2 do spec — versão autenticada)

**Status:** Existe `Pages/Public/Turmas.razor` (lista pública de ritmos). Não existe uma tela de "Quadro de Turmas" para o Aluno logado que permita se matricular.

**O que a spec prevê (tela autenticada):**
- Cards de turmas ativas com filtros (ritmo, professor, dia, horário, nível, data de início)
- Botão "Matricular" com confirmação
- Botão "Marcar interesse" (entrar na fila de espera) para turmas lotadas

**Onde criar:** `Rascunho.Client/Pages/Aluno/QuadroTurmas.razor` ou `Pages/Turmas/QuadroTurmas.razor`
**Observação:** O link `/turmas` no NavMenu do Aluno aponta para a versão pública. Precisaria ser substituído ou complementado com a versão autenticada.

---

#### 🟠 Tela: MinhasAulas — "Ver Avisos da Turma" por card

**Status:** `MinhasAulas.razor` mostra as turmas matriculadas com link WhatsApp, mas não tem botão "Ver avisos da turma" por card (previsto na spec, tela 5).

**O que falta:** Botão em cada card que abra avisos específicos daquela turma. Depende de um endpoint ou filtro no backend para avisos por turma.

---

#### 🟠 Tela: Professor — Minha Disponibilidade (Aba 3 de AulasParticulares)

**Status:** `AulasParticulares.razor` do professor tem 2 abas (Pendentes e Agendadas). A aba 3 "Minha disponibilidade" está listada na spec (tela 17) mas não existe no código.

**O que falta:**
- Aba "Minha disponibilidade" em `Pages/Professor/AulasParticulares.razor`
- Grid de dias e horários editável
- Chamadas para `GET /api/professor/disponibilidade` e `PUT /api/professor/disponibilidade`

---

#### 🟠 Tela: Professor — Perfil (Tela 18 do spec)

**Status:** O NavMenu do Professor não tem link para `/perfil`. A rota e a página existem (`Perfil.razor`), mas o professor está fora do NavMenu para esta seção.

**O que falta:** Adicionar `<MudNavLink Href="/perfil">Meu Perfil</MudNavLink>` ao bloco Professor no `NavMenu.razor`.

---

#### 🟠 Tela: Professor — Avisos da Turma (em MinhasTurmas)

**Status:** `MinhasTurmas.razor` tem botão "Ver alunos" por card, mas não "Ver/publicar avisos da turma" (previsto na spec, tela 15).

**O que falta:** Botão por card que abra modal com avisos da turma + formulário para o professor publicar aviso exclusivo daquela turma.

---

#### 🟠 Tela: Gerente — Quadro de Desempenho — Ações por Bolsista

**Status:** `QuadroDesempenho.razor` tem cards com foto, indicador e filtros. Ao clicar no card, abre modal de detalhes com histórico de presenças. Porém as ações planejadas na spec não existem:

| Ação | Status |
|---|---|
| Editar dias obrigatórios | ❌ Ausente (endpoint backend existe: `PUT /api/bolsistas/{idHash}/dias-obrigatorios`) |
| Registrar conversa/observação | ❌ Ausente (backend e frontend) |
| Desativar bolsa (converter em aluno) | ❌ Ausente (backend e frontend) |
| Renovar bolsa | ❌ Ausente (backend e frontend) |

**Filtro por papel (condutor/conduzido) no Quadro:** Ausente. Spec prevê filtro por papel, mas o select de filtros tem apenas "Situação" e "Dia Obrigatório".

---

#### 🟡 NavMenu: Bolsista sem link para "Reagendar Aula"

**Status:** A rota `/reagendar` existe e aceita `Authorize(Roles = "Aluno,Bolsista")`, mas o NavMenu do Bolsista não tem esse link. O bolsista pode ter faltas elegíveis para reposição mas não encontra a tela.

**O que falta:** Adicionar `<MudNavLink Href="/reagendar">Reagendar Aula</MudNavLink>` ao bloco Bolsista no `NavMenu.razor`.

---

#### ✅ Telas duplicadas: TurmasObrigatorias e TurmasRecomendadas — RESOLVIDO (BUG-007)

**Status:** Corrigido em 28/03/2026.
- `TurmasObrigatorias.razor` **deletada**.
- `TurmasRecomendadas.razor` renomeada para **"Turmas do Dia"** e refatorada com filtro de dia (padrão = dia atual).
- Backend aceita `?diaDaSemana=N` e retorna turmas mais desbalanceadas do dia filtrado.

---

#### 🟡 MinhasHabilidades — Listagem das habilidades cadastradas

**Status:** `MinhasHabilidades.razor` tem formulário para adicionar habilidade mas não exibe as habilidades já cadastradas do bolsista.

**O que falta:** Endpoint `GET /api/bolsistas/minhas-habilidades` (retorna habilidades do bolsista logado) e listagem no frontend. Backend também precisaria de endpoint para remover habilidade.

---

#### 🟡 CriarAviso — Editar/Remover avisos existentes

**Status:** `CriarAviso.razor` (rota `/admin/avisos`) só cria avisos. A spec (tela 23) prevê editar conteúdo/período e remover antes do término.

**O que falta:** Listagem de avisos existentes (ativos + expirados) com ações de editar e remover por item.

---

### Infraestrutura / Configuração

#### 🔴 IP da VPS hardcoded no frontend

**Arquivo:** `Rascunho.Client/Program.cs`
**Linha:** `BaseAddress = new Uri("http://5.161.202.169:8080/")`

**Problema:** Qualquer mudança de servidor ou IP exige recompilação e redeploy do frontend. Impossível ter ambientes separados (dev/staging/prod) sem alterar o código.

**Solução:**
1. Criar `wwwroot/appsettings.json` no projeto cliente com `{ "ApiBaseUrl": "http://5.161.202.169:8080/" }`
2. Ler esse valor em `Program.cs` via `builder.Configuration["ApiBaseUrl"]`
3. Passar o valor correto por variável de ambiente no `docker-compose.yml` e no workflow do GitHub Actions

---

#### 🟠 CORS AllowAnyOrigin em produção

**Arquivo:** `Rascunho/Program.cs`
**Problema:** A política CORS atual aceita qualquer origem. Em produção isso permite que qualquer domínio faça chamadas autenticadas à API.

**Solução:** Restringir para os domínios conhecidos (`5.161.202.169`, domínio definitivo do app).

---

#### 🟡 Migrations sem Designer file (últimas 2)

**Migrations afetadas:** `AddListaEspera` e `CorrigirListaEsperaDataTypes`
**Problema:** Sem o arquivo `*.Designer.cs`, o comando `dotnet ef migrations script` pode falhar ou gerar scripts incompletos. Não é bloqueante em produção.

**Solução:** Rodar `dotnet ef migrations add` novamente ou adicionar o designer manualmente.

---

## 1.2 Futuro (Confirmado) — Pendentes Integralmente

| Funcionalidade | Status Backend | Status Frontend |
|---|---|---|
| Eventos e Ingressos | ⚠️ Entidades + endpoints básicos criados | ⚠️ `Ingressos.razor` existe mas fora do nav |
| Financeiro básico | ❌ Não iniciado | ❌ Não iniciado |
| Dashboard administrativo completo | ⚠️ Parcial (Gerente tem dashboard) | ⚠️ Parcial |
| Aula Experimental — UX completa | ⚠️ Entidades + endpoints existem | ⚠️ `AulaExperimental.razor` existe, UX incompleta |
| Perfil e telas do Líder (faturamento) | ❌ Não iniciado | ❌ Não iniciado |
| Sugestão automática de bolsistas (UX) | ⚠️ Lógica parcial em `BolsistaService` | ❌ Não iniciado |

---

## 1.3 Funcionalidades Opcionais — Não Iniciadas

- Gamificação (sistema de pontos/conquistas)
- Programa de fidelidade
- Indicação de amigos (referral)
- Relatórios avançados (exportação CSV/Excel de presenças, financeiro, desempenho)

---

## 1.4 Ideias Interessantes — Não Iniciadas

- IA para atendimento (chatbot integrado)
- Integração WhatsApp (envio de mensagens automáticas via API)
- Sugestão de turmas automática baseada em perfil/histórico do aluno
- Ranking de alunos por frequência/engajamento
