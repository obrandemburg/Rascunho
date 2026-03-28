# Contexto do Projeto — Ponto da Dança

Este diretório contém os arquivos de contexto estruturados em camadas para uso em conversas com o Claude.
Inclua os arquivos desta pasta no início de qualquer conversa sobre o projeto.

---

## Como usar este contexto

Para uma conversa sobre o projeto, anexe ou cole o conteúdo dos arquivos abaixo conforme a profundidade necessária:

| Arquivo | Quando usar |
|---|---|
| `camada1_visao_geral.md` | Sempre — para qualquer conversa sobre o projeto |
| `camada2_stack_tecnico.md` | Quando for implementar funcionalidades, corrigir bugs ou discutir arquitetura |
| `camada3_estado_atual.md` | Quando for planejar próximos passos, priorizar tarefas ou retomar o desenvolvimento |
| `planejamento_MVP_Pontodadanca.md` | Quando for planejar próximos passos, priorizar tarefas ou retomar o desenvolvimento |

Para conversas completas (ex: "o que devo fazer a seguir?" ou "implemente X"), inclua os 3 arquivos.

---

## Resumo Ultra-Rápido (para contexto mínimo)

**Projeto:** Ponto da Dança — sistema de gestão para academia de dança, PWA completo.

**Stack:** .NET 10 / ASP.NET Core Minimal API + Blazor WebAssembly + PostgreSQL + MudBlazor + Docker + GitHub Actions CI/CD em VPS.

**Perfis:** Aluno, Bolsista, Professor, Recepção, Gerente, Líder.

**Módulos MVP implementados no backend:** Usuários, Turmas, Chamada, Aulas Particulares, Bolsistas, Avisos, Reposição, Lista de Espera, Upload de Fotos.

**Pendente crítico:** Notificações Push (FCM) — stub ativo; frontend parcialmente completo.

**Deploy:** Push em `main` → GitHub Actions → Docker images em `ghcr.io/obrandemburg` → VPS `5.161.202.169`.

**Convenção:** Código em português. Sem AutoMapper. Sem controllers. Sem repositório pattern. IDs ofuscados com Hashids.

---

## Estrutura do Repositório

```
Rascunho.slnx                     ← Solução Visual Studio
├── Rascunho/                     ← Backend (ASP.NET Core Minimal API, net10.0)
│   ├── Configurations/           ← Fluent EF Core config por entidade
│   ├── Data/AppDbContext.cs      ← DbContext com todos os DbSets
│   ├── Endpoints/                ← Minimal API (um arquivo por domínio)
│   ├── Entities/                 ← Entidades de domínio (TPH para Usuários)
│   ├── Exceptions/               ← RegraNegocioException
│   ├── Infraestrutura/           ← GlobalExceptionHandler, ValidationFilter
│   ├── Mappers/                  ← Conversão manual Entidade → DTO
│   ├── Migrations/               ← EF Core migrations (20+ migrations)
│   ├── Services/                 ← Lógica de negócio por domínio
│   ├── Validations/              ← FluentValidation validators
│   └── Program.cs                ← Composição da aplicação
├── Rascunho.Client/              ← Frontend (Blazor WebAssembly PWA, net10.0)
│   ├── Infraestrutura/           ← HttpInterceptorHandler (injeta JWT)
│   ├── Layout/                   ← MainLayout, AuthLayout, NavMenu
│   ├── Pages/                    ← Páginas por perfil (Aluno, Bolsista, Professor, Admin, Gerencia, Public)
│   ├── Security/                 ← CustomAuthStateProvider
│   ├── Services/                 ← AuthService
│   ├── Shared/                   ← Componentes reutilizáveis
│   └── wwwroot/                  ← Assets, manifest PWA, service worker
├── Rascunho.Shared/              ← DTOs compartilhados backend↔frontend
│   └── DTOs/                     ← Um arquivo por domínio
├── Documentação/                 ← Docs de referência (.docx)
├── contexto/                     ← ← VOCÊ ESTÁ AQUI — arquivos de contexto para o Claude
├── Dockerfile                    ← Build backend (Alpine multi-stage)
├── Dockerfile.Client             ← Build frontend (NGINX Alpine)
└── .github/workflows/deploy.yml ← CI/CD GitHub Actions
```

---

## Regras de Negócio (índice de referência rápida)

| Código | Domínio | Descrição curta |
|---|---|---|
| BOL01 | Bolsista | Salão gratuito |
| BOL02 | Bolsista | 50% em dança solo |
| BOL03 | Bolsista | 50% em aulas particulares |
| BOL04 | Bolsista | Não pode se matricular em solo nos dias obrigatórios |
| BOL05 | Bolsista | Não pode agendar particular nos dias obrigatórios |
| BOL06 | Bolsista | Dois dias obrigatórios, mínimo 6h/semana |
| BOL07 | Bolsista | Frequência calculada só pelos dias obrigatórios |
| BOL08 | Bolsista | Push de confirmação de presença ao bolsista |
| BOL09 | Bolsista | Não faz matrícula formal em turmas de salão |
| AP01 | Particular | Nunca exibir horários com conflito de turma do professor |
| AP02 | Particular | Verificar conflito novamente ao aceitar a solicitação |
| AP03 | Particular | Cancelamento bloqueado com menos de 12h (aluno/bolsista) |
| AP04 | Particular | Professor pode cancelar a qualquer momento |
| AP05 | Particular | Professor não pode ter duas particulares no mesmo horário |
| AP06 | Particular | Aluno/bolsista não pode agendar duas no mesmo horário |
| CHA01 | Chamada | Janela: no dia ou até 24h de atraso |
| CHA02 | Chamada | Finalizar chamada atualiza histórico e elegibilidade de reposição |
| CHA03 | Chamada | Sistema identifica tipo do participante extra automaticamente |
| CHA04 | Chamada | Professor só pode registrar chamada das próprias turmas |
| CHA05 | Chamada | Não pode lançar chamada duplicada na mesma data |
| TUR01 | Turma | Professor não pode ter duas turmas no mesmo horário |
| TUR02 | Turma | Sala não pode estar ocupada no mesmo horário |
| TUR03 | Turma | Mesmas validações ao editar turma |
| TUR04 | Turma | Push para alunos ao encerrar turma |
| TUR05 | Turma | Aluno não pode se matricular duas vezes na mesma turma |
| TUR06 | Turma | Aluno não pode ter duas turmas no mesmo horário |
| REP01 | Reposição | Apenas faltas dentro do período máximo são elegíveis |
| REP02 | Reposição | 4 motivos de perda de elegibilidade |
| REP03 | Reposição | Sistema bloqueia agendamento com motivo específico |
| REP04 | Reposição | Não pode reagendar sem cancelar a anterior |
| ACE01–06 | Acesso | Isolamento total por perfil |
| ACE07 | Acesso | Validação de rota no backend via token — não confia em URL |
