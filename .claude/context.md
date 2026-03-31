# Contexto Consolidado — Ponto da Dança

> Última atualização: 31/03/2026
> Fonte: análise completa de camada1, camada2, camada3, implementacoes_faltantes, bugs_e_erros_logicos, planejamento_sprints

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
| Deploy | Docker + GitHub Actions → VPS Hetzner |

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
| TUR03 | Turma | Mesmas validações ao editar |
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

## Estado Atual (31/03/2026)

### Backend — Substancialmente Completo
Todos os módulos MVP implementados: Usuários, Turmas, Chamada, Aulas Particulares, Bolsistas, Avisos, Reposição, Lista de Espera, Upload de Fotos.

Exceção: Notificações Push é **stub** (`NotificacaoServiceStub`) — sem envio real. Feature #4.

### Frontend — Em Desenvolvimento
Páginas criadas com grau variado de completude. Telas faltantes críticas:
- `/admin` — Dashboard da Recepção (`InicioAdmin.razor`)
- `/admin/bolsistas` — Sistema de Bolsistas (`SistemaBolsistas.razor`)
- `/quadro-turmas` — Quadro de Turmas autenticado para Aluno (`QuadroTurmas.razor`)

### Bugs Pendentes
| Bug | Severidade | Descrição |
|-----|-----------|-----------|
| BUG-004 | 🟠 Alto | ConfiguracaoService não persiste entre restarts |
| BUG-011 | 🟠 Alto | CORS AllowAnyOrigin em produção |
| BUG-013 | 🔴 Crítico | IP da VPS hardcoded no frontend |

---

## Próximos Passos (por prioridade)

1. **BUG-013** — Externalizar URL da API do frontend para `wwwroot/appsettings.json`
2. **InicioAdmin.razor** — Tela de dashboard da Recepção (`/admin`)
3. **SistemaBolsistas.razor** — Sistema de bolsistas para Recepção (`/admin/bolsistas`)
4. **QuadroTurmas.razor** — Quadro de turmas autenticado para Aluno
5. **BUG-011** — Restringir CORS em produção
6. **BUG-004** — Persistir configurações no banco
7. **Feature #4** — Notificações Push FCM (Sprint 15)

---

## Deploy e Ambiente

| Ambiente | URL/Endereço |
|----------|-------------|
| Produção | `http://5.161.202.169:8080` (API) |
| Branch produção | `main` (trigger CI/CD) |
| Branch dev | `develop` |
| CI/CD | GitHub Actions → Docker → VPS |
| Imagens | `ghcr.io/obrandemburg/rascunho:latest` |
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
