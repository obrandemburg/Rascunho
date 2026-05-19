# Contexto Consolidado — Ponto da Dança

> Última atualização: 19/05/2026
> Fonte: auditoria completa de código vs. documentação — verificação linha a linha

---

## O Projeto

**Ponto da Dança** — sistema de gestão para academia de dança, desenvolvido como PWA fullstack. Código no repositório **Rascunho**.

Digitaliza: turmas, chamadas, aulas particulares, sistema de bolsistas, avisos, lista de espera, reposições.

---

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Backend | ASP.NET Core 10, Minimal API, .NET 10 |
| Frontend | Blazor WebAssembly 10, MudBlazor 9.1.0, PWA |
| Banco | PostgreSQL + EF Core 10 + Npgsql |
| Auth | JWT Bearer, BCrypt, HashidsNet |
| Validação | FluentValidation |
| Deploy | Docker + Traefik v2 + GitHub Actions → VPS Hetzner |
| Domínio | https://pontodadanca.trindaflow.com.br |

---

## Arquitetura (NUNCA violar)

- **Sem controllers** — Minimal APIs em extension methods estáticos por domínio
- **Sem AutoMapper** — mappers manuais em `Rascunho/Mappers/`
- **Sem repositório pattern** — `AppDbContext` diretamente nos Services
- **Sem lógica de negócio em endpoints** — toda lógica nos Services
- **TPH para usuários** — tabela única `Usuarios`, discriminador `Tipo`
- **IDs ofuscados** — Hashids em todos os endpoints públicos
- **Código em português** — classes, métodos, variáveis, comentários

---

## Perfis de Usuário

| Perfil | Role JWT | Acesso |
|--------|----------|--------|
| Aluno | "Aluno" | Suas aulas, particulares, avisos |
| Bolsista | "Bolsista" | Salão gratuito, 50% solo/particulares |
| Professor | "Professor" | Suas turmas, chamadas, particulares |
| Recepção | "Recepção" | Admin de turmas, usuários, avisos |
| Gerente | "Gerente" | Tudo da Recepção + desempenho bolsistas |
| Líder | "Líder" | Faturamento (Fase 1.2) |

---

## Estrutura de Pastas

```
Rascunho.slnx
├── Rascunho/                     ← Backend
│   ├── Configurations/           ← Fluent EF Core por entidade
│   ├── Data/AppDbContext.cs
│   ├── Endpoints/                ← Um arquivo por domínio
│   ├── Entities/                 ← Entidades (TPH Usuarios)
│   ├── Exceptions/RegraNegocioException.cs
│   ├── Infraestrutura/           ← GlobalExceptionHandler, ValidationFilter
│   ├── Mappers/                  ← Conversão Entidade → DTO
│   ├── Migrations/               ← 20+ migrations EF Core
│   ├── Services/                 ← Lógica de negócio
│   ├── Validations/              ← FluentValidation validators
│   └── Program.cs
├── Rascunho.Client/              ← Frontend Blazor
│   ├── Infraestrutura/HttpInterceptorHandler.cs
│   ├── Layout/                   ← MainLayout, AuthLayout, NavMenu
│   ├── Pages/                    ← Por perfil: Aluno, Bolsista, Professor, Admin, Gerencia, Public
│   ├── Security/CustomAuthStateProvider.cs
│   ├── Services/AuthService.cs
│   └── Shared/                   ← Componentes reutilizáveis
└── Rascunho.Shared/
    └── DTOs/                     ← Um arquivo por domínio
```

---

## Regras de Negócio Críticas

| Código | Domínio | Resumo |
|--------|---------|--------|
| BOL04 | Bolsista | Não matricular em solo/salão nos dias obrigatórios |
| BOL05 | Bolsista | Não agendar particular nos dias obrigatórios |
| BOL07 | Bolsista | Frequência calculada APENAS pelos dias obrigatórios |
| BOL09 | Bolsista | Não faz matrícula formal em turmas de salão |
| TUR01 | Turma | Professor não pode ter duas turmas no mesmo horário |
| TUR02 | Turma | Sala não pode estar ocupada no mesmo horário |
| TUR03 | Turma | Mesmas validações ao editar (TUR01 + TUR02) |
| TUR04 | Turma | Ao encerrar: cancelar experimentais pendentes e reposições agendadas nessa turma como destino ⚠️ não implementado — código apenas marca Ativa=false |
| TUR05 | Turma | Aluno não matricula duas vezes na mesma turma |
| TUR06 | Turma | Aluno não pode ter duas turmas no mesmo horário |
| CHA01 | Chamada | Janela de 24h para registrar |
| CHA04 | Chamada | Professor só registra chamada das próprias turmas |
| CHA05 | Chamada | Não pode ter chamada duplicada na mesma data |
| AP03 | Particular | Cancelamento bloqueado com <12h (aluno/bolsista) |
| AP05 | Particular | Professor sem dois agendamentos no mesmo horário |
| AP06 | Particular | Aluno/bolsista sem dois agendamentos no mesmo horário |
| REP04 | Reposição | Não pode reagendar sem cancelar anterior |
| ACE07 | Acesso | Validação no backend via token — não confiar na URL |

---

## Estado Atual (19/05/2026)

### Backend — Substancialmente Completo
Todos os módulos MVP implementados: Usuários, Turmas, Chamada, Aulas Particulares, Bolsistas, Avisos, Reposição, Lista de Espera, Upload de Fotos.

Exceção: Notificações Push é **stub** (`NotificacaoServiceStub`) — sem envio real. Feature #4 (Sprint 15).

### Frontend — Em Desenvolvimento
Páginas criadas com grau variado de completude. Telas faltantes críticas:
- `InicioAdmin.razor` — Dashboard centralizado da Recepção (não existe)
- `SistemaBolsistas.razor` — Painel unificado de bolsistas para Recepção (não existe)
- Versão autenticada de QuadroTurmas para Aluno (existe versão pública em `/turmas`, falta versão em `Pages/Aluno/`)

### Bugs Pendentes
| Bug | Severidade | Descrição | Arquivo |
|-----|-----------|-----------|---------|
| BUG-004 | 🔴 Alto | ConfiguracaoService perde dados no restart — em memória (`AddSingleton`), sem persistência no banco | `Services/ConfiguracaoService.cs` |
| BUG-025 | 🔴 Alto | `GerenciarTurmas.razor` chama `api/turmas/listar-ativas` em vez de `api/turmas/` — admin não vê turmas encerradas | `Pages/Admin/GerenciarTurmas.razor:309` |
| SEC-16 | 🟡 Médio | `registrar-conversa` retorna sucesso mas não persiste dados — TODO no código | `Endpoints/GerenteEndpoints.cs:97` |

### Issues de Segurança Pendentes
| ID | Descrição | Severidade |
|----|-----------|-----------|
| SEC-06 | Desmatrícula sem restrição de role em alguns fluxos | 🔴 Alto |
| SEC-12 | Headers HTTP de segurança ausentes (CSP, X-Frame-Options) | 🟡 Médio |
| SEC-13 | Sem rate limiting no `POST /api/auth/login` | 🟡 Médio |

---

## Próximos Passos (por prioridade)

1. **Corrigir BUG-025** — trocar `listar-ativas` por `api/turmas/` em `GerenciarTurmas.razor` + filtro de status
2. **Corrigir TUR04** — implementar cancelamento de experimentais e reposições ao encerrar turma em `TurmaService.EncerrarTurmaAsync`
3. **Persistir ConfiguracaoService** — criar tabela `Configuracoes` com migration (BUG-004)
4. **Persistir `registrar-conversa`** — criar entidade `RegistroConversa` e migration (SEC-16)
5. **Implementar `InicioAdmin.razor`** — dashboard de atalhos para Recepção
6. **Implementar Editar Turma** — `PUT /api/turmas/{id}/editar` + UI em `GerenciarTurmas.razor` (incluir trocar sala)
7. **Implementar FCM** — Feature #4, Sprint 15

---

## Deploy e Ambiente

| Ambiente | URL/Endereço |
|----------|-------------|
| Produção | https://pontodadanca.trindaflow.com.br |
| VPS | 5.161.202.169 (Hetzner) |
| Branch produção | `main` (trigger CI/CD) |
| Branch dev | `develop` |
| Reverse Proxy | Traefik v2 com SSL Let's Encrypt automático |
| CI/CD | GitHub Actions → build Docker → SSH VPS → `docker compose pull && up -d` |
| Imagens | `ghcr.io/obrandemburg/rascunho:latest` (backend) e `ghcr.io/obrandemburg/rascunho-client:latest` (frontend) |
| API docs (dev) | `http://localhost:{porta}/scalar` |

---

## Agentes Disponíveis

| Agente | Quando usar |
|--------|-------------|
| `architect` | Planejar qualquer task, analisar impacto, orquestrar |
| `backend` | Endpoints, services, validações, mappers |
| `frontend` | Páginas Blazor, componentes, integração API |
| `db` | Entidades, migrations, queries, schema |
| `auth` | JWT, roles, Hashids, isolamento, segurança |
| `tests` | Testes de regras de negócio, unitários e integração |
| `reviewer` | Code review antes do merge |
