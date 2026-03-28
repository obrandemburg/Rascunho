# Ponto da Dança — Camada 3: Estado Atual

> Última atualização: 28/03/2026 (BUG-001 a BUG-015 corrigidos; MinhasHabilidades CRUD implementado; contexto atualizado)

---

## Fase Atual do Projeto

O projeto está em **desenvolvimento ativo do MVP (Fase 1.1)**. O backend está substancialmente mais avançado do que o frontend. A infraestrutura de deploy está operacional.

---

## Cronologia das Migrations (Histórico de Desenvolvimento)

As migrations revelam a ordem em que os módulos foram construídos:

| Data | Migration | O que foi feito |
|---|---|---|
| 10/03/2026 | `InitialCreateInt` | Schema inicial: usuários, turmas, matrículas, ritmos, salas |
| 11/03/2026 | `AtualizacaoModelos` | Ajustes nos modelos iniciais |
| 11/03/2026 | `CorrecaoDeMigration` | Correção de inconsistência |
| 12/03/2026 | `AdicionandoMuralDeAvisos` | Módulo de avisos |
| 13/03/2026 | `AdicionandoChamadas` | Módulo de chamada de presença |
| 13/03/2026 | `AdicionandoAulasParticulares` | Módulo de aulas particulares |
| 13/03/2026 | `EngenhariaDoBolsista` | Sistema de bolsistas (dias obrigatórios, desconto, frequência) |
| 16/03/2026 | `AdicionandoModuloComercialEventos` | Eventos e ingressos (antecipação fase 1.2) |
| 16/03/2026 | `AjustesFinaisModelos` | Ajustes finais nos modelos |
| 22/03/2026 | `Sprint2_ObservacaoChamada_ProfessorDisponibilidade` | Campo de observação na chamada + grade de disponibilidade do professor |
| 22/03/2026 | `Sprint3_Reposicao_ValorCobrado` | Módulo de reposição de aulas + campo de valor cobrado em particulares |
| 22/03/2026 | `Sprint4` | Ajustes sprint 4 |
| 22/03/2026 | `AdicionaCpfTelefoneEPapelBolsista` (V1, V2, V3) | CPF, telefone e papel dominante do bolsista (3 tentativas de ajuste) |
| 23/03/2026 | `Algo_A_Migrar` | Ajuste adicional |
| 25/03/2026 | `AlterarDataInicioParaDateOnly` | Tipo da data de início de turma alterado para `DateOnly` |
| 25/03/2026 | `VerificandoMigration` | Verificação de migration |
| 25/03/2026 | `AddGeneroToUsuario` | Campo gênero adicionado ao usuário |
| 26/03/2026 | `AddListaEspera` | Sistema de lista de espera com posição e status |
| 26/03/2026 | `CorrigirListaEsperaDataTypes` | Correção de tipos de data na lista de espera |
| 28/03/2026 | `RemoveInteresseObsoleto` | BUG-010: remoção da tabela `Interesses` (substituída por `ListaEspera`) |

---

## Status por Módulo

### ✅ Backend — Completo ou Substancialmente Implementado

| Módulo | Status | Observações |
|---|---|---|
| Autenticação/JWT | ✅ Completo | Login, hash BCrypt, token JWT, roles |
| Usuários (CRUD) | ✅ Completo | Cadastro em massa, edição, desativação, foto, gênero, CPF |
| Ritmos e Salas | ✅ Completo | CRUD simples |
| Turmas | ✅ Completo | CRUD com validações TUR01-TUR06 |
| Chamada | ✅ Completo | Registro, participantes extras, validações CHA01-CHA05 |
| Aulas Particulares | ✅ Completo | Agendamento, aceite/recusa, disponibilidade professor, validações AP01-AP06 |
| Sistema de Bolsistas | ✅ Completo | Dias obrigatórios, frequência, papel dominante, BOL01-BOL09 |
| Avisos | ✅ Completo | CRUD com período, público-alvo, expiração |
| Reposição de Aulas | ✅ Completo | Elegibilidade, agendamento, REP01-REP04 |
| Lista de Espera | ✅ Completo | Fila com posição, status (Aguardando/Notificado/Expirado/Convertido), expiração lazy |
| Upload de Fotos | ✅ Completo | Upload, storage em disco, serviço estático |
| Eventos/Ingressos | ⚠️ Parcial | Entidades criadas, endpoints existem — funcionalidade completa é Fase 1.2 |
| Aulas Experimentais | ⚠️ Parcial | Entidades e endpoints existem — UX completa a definir |
| Notificações Push | ❌ Stub | `NotificacaoServiceStub` — sem envio real. FCM é Feature #4 pendente |
| Quadro de Desempenho | ✅ Completo | `GerenteEndpoints` implementado |
| Sugestão de Bolsistas | ⚠️ Pendente de validação | Lógica presente no `BolsistaService` — telas de UX parciais |

### ⚠️ Frontend — Em Desenvolvimento

O frontend tem as páginas criadas, mas o grau de completude varia. Páginas existentes:

| Área | Páginas Existentes | Status Estimado |
|---|---|---|
| Auth | `Login.razor` | ✅ Funcional |
| Aluno | `PainelAluno` (BUG-008 ✅), `MinhasAulas`, `AulasParticulares`, `Reagendar`, `MinhasEsperas`, `AulaExperimental`, `Ingressos` | 🔄 Parcial |
| Bolsista | `TurmasRecomendadas` (BUG-007 ✅ — "Turmas do Dia"), `Desempenho` (BUG-006 ✅), `RelatorioHoras`, `MinhasHabilidades` (✅ CRUD completo 28/03) | 🔄 Parcial |
| Professor | `MinhasTurmas`, `FazerChamada`, `AulasParticulares` | 🔄 Parcial |
| Admin (Recepção) | `GerenciarTurmas` (BUG-001 ✅ modal de alunos), `GerenciarSalas`, `GerenciarRitmos`, `CriarUsuario`, `MatricularAluno`, `CriarAviso`, `FilaEspera` | 🔄 Parcial |
| Gerência | `Dashboard`, `GestaoUsuarios`, `QuadroDesempenho` | 🔄 Parcial |
| Público | `Turmas` (BUG-002 ✅ botão desativado para bolsistas), `RitmosPublico` | 🔄 Parcial |
| Shared | `PainelAluno`, `QuadroAvisos`, `AvisosEquipe`, `Perfil` | 🔄 Parcial |

---

## Débitos Técnicos Conhecidos

### Crítico
- **IP da VPS hardcoded** no `Rascunho.Client/Program.cs`: `http://5.161.202.169:8080/`
  - Deve ser movido para `appsettings.json` ou variável de ambiente
- **Notificações Push não funcionam** — `NotificacaoServiceStub` não envia nada
  - Bloqueia: confirmação de presença de bolsistas, notificação de vaga na lista de espera, alertas de aulas particulares
  - Solução: implementar `FirebaseNotificacaoService` (Feature #4)
- **Migrations sem Designer gerado** para as últimas (`AddListaEspera`, `CorrigirListaEsperaDataTypes`, `RemoveInteresseObsoleto`)
  - Não é bloqueante para o funcionamento, mas pode causar problemas se precisar reverter

### Médio
- **CORS configurado como `AllowAnyOrigin`** — aceitável para desenvolvimento, deve ser restrito em produção (BUG-011 pendente)
- **`appsettings.Development.json`** com dados sensíveis gerenciados via `UserSecrets` — confirmar que secrets estão configurados na VPS

### Baixo
- Algumas pages do Bolsista têm nomes que podem não refletir o planejamento final (`MinhasHabilidades`, `RelatorioHoras`, `TurmasRecomendadas`) — podem ter sido criadas antes da definição final do MVP
- O `Dockerfile.Client` usa NGINX — verificar se a config do `nginx.conf` do cliente está correta para roteamento SPA

---

## Próximos Passos Mapeados (em ordem de prioridade)

### Feature #4 — Notificações Push (FCM)
- Implementar `FirebaseNotificacaoService` substituindo o stub
- Adicionar FCM token ao cadastro do usuário
- Desbloqueia: lista de espera, confirmação de chamada para bolsista, aulas particulares, encerramento de turma

### Completar UX do Frontend
- Terminar telas do Aluno: fluxo completo de matrícula, lista de espera, reagendamento
- Terminar telas do Professor: chamada com participantes extras, disponibilidade, aba Minha Disponibilidade em AulasParticulares
- Terminar telas do Bolsista: desempenho completo (filtros ✅), MinhasHabilidades (CRUD ✅ 28/03)
- Terminar telas da Recepção: fluxo completo de usuários, bolsistas, tela InicioAdmin e SistemaBolsistas (ausentes)
- Terminar Gerência: quadro de desempenho de bolsistas — ações por bolsista (editar dias, desativar bolsa, registrar conversa)

### Correções de Configuração
- Mover IP da API para configuração de ambiente no frontend
- Restringir CORS em produção
- Garantir que `ListaEspera:PrazoConfirmacaoHoras` está configurado no `appsettings.json` da VPS

### Fase 1.2 (Futuro Confirmado)
- Eventos e ingressos (base já existe)
- Financeiro básico
- Dashboard administrativo
- Aula experimental (UX completa)
- Perfil e telas do Líder (faturamento)

---

## Informações de Ambiente

### Desenvolvimento Local
- IDE: Visual Studio 2022+ (solução `.slnx`)
- Runtime: .NET 10 SDK
- Banco: PostgreSQL local (connection string via UserSecrets)
- Frontend: `dotnet run` no projeto `Rascunho.Client` com DevServer

### Produção (VPS)
- Endereço da API: `http://5.161.202.169:8080`
- Deploy: Docker Compose via GitHub Actions (push em `main`)
- Imagens: `ghcr.io/obrandemburg/rascunho:latest` (backend) e `ghcr.io/obrandemburg/rascunho-client:latest` (frontend)
- Volume: `pd_uploads` para fotos de perfil persistentes
- Pasta na VPS: `/home/usuario/pontodadanca`

### Git
- Branch de produção: `main`
- Branch de desenvolvimento: `develop`
- Repositório: `github.com/obrandemburg/rascunho`

### Documentação da API (Desenvolvimento)
- Scalar UI: `http://localhost:{porta}/scalar`
- OpenAPI JSON: `http://localhost:{porta}/openapi/v1.json`

---

## Restrições e Decisões de Arquitetura Importantes

- **Sem AutoMapper** — todos os mapeamentos são manuais (em `Mappers/`)
- **Sem controllers** — apenas Minimal API com extension methods por domínio
- **Sem repositório pattern** — EF Core acessado diretamente nos Services
- **TPH para usuários** — todos os perfis em uma tabela, discriminados por `Tipo`
- **IDs públicos ofuscados** — Hashids usado em todos os endpoints que expõem IDs
- **CPF apenas com dígitos** no banco — formatação feita no frontend
- **Português em todo o código** — nomes de classes, métodos, variáveis e comentários em PT-BR
- **Notificações são assíncronas por design** — interface `INotificacaoService` com `Task` permite troca futura de implementação sem mudar os Services
