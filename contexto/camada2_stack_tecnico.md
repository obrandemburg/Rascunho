# Ponto da Dança — Camada 2: Stack Técnico

## Visão Geral da Arquitetura

O projeto é uma aplicação **full-stack .NET 10** dividida em 3 projetos dentro de uma única solução Visual Studio (`.slnx`):

```
Rascunho.slnx
├── Rascunho/              ← Backend: ASP.NET Core Minimal API
├── Rascunho.Client/       ← Frontend: Blazor WebAssembly (PWA)
└── Rascunho.Shared/       ← Biblioteca compartilhada: DTOs
```

---

## Backend — `Rascunho/`

### Tecnologia Principal
- **ASP.NET Core 10** com **Minimal APIs** (sem controllers, endpoints são mapeados em classes estáticas de extensão)
- **Target Framework:** `net10.0`

### Banco de Dados
- **PostgreSQL** via **Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0**
- **Entity Framework Core 10** com migrations automáticas na inicialização (retry policy de 5 tentativas)
- Herança de entidades com **TPH (Table-Per-Hierarchy)** — todos os perfis de usuário (Aluno, Professor, Bolsista, Gerente, Recepção, Líder) ficam em uma única tabela `Usuarios`, discriminados pelo campo `Tipo`

### Autenticação e Segurança
- **JWT Bearer Tokens** (`Microsoft.AspNetCore.Authentication.JwtBearer 10.0.4`)
- **BCrypt.Net-Next 4.1.0** para hash de senhas
- **HashidsNet 1.7.0** — ofuscação de IDs internos em URLs públicas (salt + comprimento mínimo de 8 caracteres)
- Controle de acesso por role via Claims do token JWT (`ClaimTypes.Role`)

### Validação
- **FluentValidation 11.3.1/12.1.1** — validadores por entidade, registrados via `AddValidatorsFromAssemblyContaining<Program>()`
- **ValidationFilter** — middleware que aplica validação automaticamente nos endpoints

### Documentação da API
- **Scalar.AspNetCore 2.13.2** — interface visual da API (disponível apenas em desenvolvimento, em `/scalar`)
- **Microsoft.AspNetCore.OpenApi 10.0.3** — geração do schema OpenAPI

### Outros Pacotes do Backend
- **EFCore.BulkExtensions.PostgreSql 10.0.1** — operações em lote no banco
- **GlobalExceptionHandler** — captura exceções globais e retorna ProblemDetails padronizados
- **RegraNegocioException** — exception customizada para violações de regra de negócio

### Estrutura de Pastas do Backend

```
Rascunho/
├── Configurations/     ← Configurações Fluent do EF Core por entidade
├── Data/
│   └── AppDbContext.cs ← DbContext com todos os DbSets
├── Endpoints/          ← Minimal API endpoints (um arquivo por domínio)
├── Entities/           ← Entidades de domínio (modelos do banco)
├── Exceptions/         ← RegraNegocioException
├── Infraestrutura/     ← GlobalExceptionHandler, ValidationFilter, BearerSecuritySchemeTransformer
├── Mappers/            ← Conversão Entidade → DTO (manuais, sem AutoMapper)
├── Migrations/         ← Migrations geradas pelo EF Core
├── Services/           ← Lógica de negócio por domínio
├── Validations/        ← Validators FluentValidation por entidade
├── appsettings.json
└── Program.cs
```

### Entidades de Domínio

| Entidade | Descrição |
|---|---|
| `Usuario` (abstract) | Base TPH para todos os perfis |
| `Aluno`, `Professor`, `Bolsista`, `Gerente`, `Recepcao`, `Lider` | Subclasses concretas de Usuario |
| `Turma` | Turma de dança com professor, sala, ritmo, dia/horário |
| `TurmaProfessor` | Relação N:N turma-professor |
| `Matricula` | Vínculo aluno-turma |
| `Interesse` | Interesse de aluno em turma lotada |
| `ListaEspera` | Fila de espera com data de expiração |
| `Ritmo` | Ritmo de dança (Forró, Samba, Tango, etc.) |
| `Sala` | Sala física disponível |
| `RegistroPresenca` | Registro de chamada por turma/data |
| `AulaParticular` | Agendamento de aula particular |
| `ProfessorDisponibilidade` | Grade de horários disponíveis do professor |
| `Reposicao` | Reposição de falta elegível |
| `Aviso` | Aviso/comunicado com público-alvo e período de exibição |
| `AulaExperimental` | Aula experimental (módulo futuro em antecipação) |
| `Evento`, `Ingresso` | Eventos e ingressos (módulo futuro em antecipação) |
| `HabilidadeUsuario` | Habilidades/ritmos que o usuário domina |

### Serviços do Backend

Cada serviço encapsula toda a lógica de negócio de um domínio:

| Serviço | Responsabilidade |
|---|---|
| `UsuarioService` | CRUD de usuários, autenticação, perfis |
| `TokenService` | Geração e validação de JWT |
| `TurmaService` | CRUD de turmas, validações TUR01-TUR06 |
| `ChamadaService` | Registro de chamadas, validações CHA01-CHA05 |
| `AulaParticularService` | Agendamento, aceite/recusa, validações AP01-AP06 |
| `BolsistaService` | Gestão de bolsistas, frequência, validações BOL01-BOL09 |
| `ReposicaoService` | Elegibilidade de reposições, validações REP01-REP04 |
| `ListaEsperaService` | Fila de espera + notificação de vaga |
| `ProfessorDisponibilidadeService` | Grade de disponibilidade do professor |
| `AvisoService` | CRUD de avisos com filtro por período |
| `RitmoService`, `SalaService` | CRUD de ritmos e salas |
| `AulaExperimentalService` | Aulas experimentais |
| `EventoService` | Eventos e ingressos |
| `ConfiguracaoService` | Configurações globais (singleton) |
| `INotificacaoService` | Interface para notificações push (stub atualmente) |

### Notificações Push
- Interface `INotificacaoService` definida com contrato formal
- Implementação atual: `NotificacaoServiceStub` — stub provisório que não envia nada
- Implementação planejada: **Firebase Cloud Messaging (FCM)** — Feature #4
- As notificações estão comentadas no código com `// Feature #4: FCM` para fácil localização futura

### Upload de Arquivos
- Endpoint `UploadEndpoints` — upload de fotos de perfil
- Armazenamento em disco: `/app/uploads/fotos/` (fora do `wwwroot`)
- Servido via `StaticFileOptions` com `PhysicalFileProvider` em `/uploads/fotos/{uuid}.jpg`
- Em Docker: mapeado para o volume persistente `pd_uploads`

---

## Frontend — `Rascunho.Client/`

### Tecnologia Principal
- **Blazor WebAssembly 10** com suporte a **PWA** (Progressive Web App)
- `ServiceWorkerAssetsManifest` configurado para cache offline
- `manifest.webmanifest` com ícones 192px e 512px

### Biblioteca de UI
- **MudBlazor 9.1.0** — componentes Material Design para Blazor

### Autenticação no Cliente
- **Blazored.LocalStorage** (fork concorrente: `ch1seL.Blazored.LocalStorage.Concurrent 0.0.4`) — armazenamento do JWT no localStorage
- **CustomAuthStateProvider** — implementação customizada de `AuthenticationStateProvider` que lê o token do localStorage e expõe o usuário autenticado para o Blazor
- **HttpInterceptorHandler** — `DelegatingHandler` que injeta automaticamente o header `Authorization: Bearer {token}` em todas as requisições HTTP
- **AuthService** — serviço de login/logout no cliente

### Estrutura de Pastas do Frontend

```
Rascunho.Client/
├── Infraestrutura/
│   └── HttpInterceptorHandler.cs  ← Injeta JWT em todas as requisições
├── Layout/
│   ├── AuthLayout.razor           ← Layout sem menu (tela de login)
│   ├── MainLayout.razor           ← Layout principal com sidebar
│   └── NavMenu.razor              ← Menu lateral (filtrado por perfil)
├── Pages/
│   ├── Auth/Login.razor
│   ├── Aluno/                     ← PainelAluno, MinhasAulas, AulasParticulares, Reagendar...
│   ├── Bolsista/                  ← TurmasRecomendadas (Turmas do Dia), Desempenho, RelatorioHoras...
│   ├── Professor/                 ← MinhasTurmas, FazerChamada, AulasParticulares
│   ├── Admin/                     ← GerenciarTurmas, CriarUsuario, FilaEspera, CriarAviso...
│   ├── Gerencia/                  ← Dashboard, GestaoUsuarios, QuadroDesempenho
│   └── Public/                    ← Turmas, RitmosPublico (sem login)
├── Security/
│   └── CustomAuthStateProvider.cs
├── Services/
│   └── AuthService.cs
├── Shared/                        ← Componentes reutilizáveis
│   ├── ConfirmDialog.razor
│   ├── PwaBaixarApp.razor         ← Botão PWA de instalação
│   ├── RedirectToLogin.razor
│   └── UserAvatar.razor
└── wwwroot/
    ├── css/app.css
    ├── js/
    │   ├── fileUpload.js           ← Interop para upload de foto
    │   └── pwaInstall.js          ← Lógica de detecção/instalação do PWA
    ├── manifest.webmanifest
    └── service-worker.js
```

### URL Base da API (Hardcoded no momento)
```csharp
BaseAddress = new Uri("http://5.161.202.169:8080/")
```
> ⚠️ **Atenção:** O IP da VPS está hardcoded no `Program.cs` do cliente. Isso deve ser movido para configuração de ambiente.

---

## Projeto Shared — `Rascunho.Shared/`

Biblioteca compartilhada entre backend e frontend contendo apenas **DTOs (Data Transfer Objects)**:

| DTO | Domínio |
|---|---|
| `UsuarioDTOs.cs` | Cadastro, login, perfil, edição |
| `TurmaDTOs.cs` | Criação, listagem, matrícula |
| `ChamadaDTOs.cs` | Registro de chamada e presenças |
| `AulaParticularDTOs.cs` | Agendamento e gestão de particulares |
| `BolsistaDTOs.cs` | Dados de bolsista e frequência |
| `AvisoDTOs.cs` | Criação e listagem de avisos |
| `ReposicaoDTOs.cs` | Elegibilidade e agendamento de reposição |
| `RitmoDTOs.cs` | Ritmos de dança |
| `SalaDTOs.cs` | Salas disponíveis |
| `ProfessorDTOs.cs` | Perfil e disponibilidade do professor |
| `EventoDTOs.cs` | Eventos e ingressos |
| `AulaExperimentalDTOs.cs` | Aulas experimentais |
| `ErrosGlobaisDto.cs` | Padronização de erros da API |

---

## Infraestrutura e Deploy

### Containerização
- **Docker** com builds multi-estágio (Alpine Linux para imagem enxuta)
- Dois Dockerfiles separados:
  - `Dockerfile` — Backend (ASP.NET Core, porta 8080)
  - `Dockerfile.Client` — Frontend (Blazor WASM servido via NGINX)
- `nginx.conf` — configuração do NGINX para o frontend SPA (redirect all to `index.html`)
- `.dockerignore` — exclui artefatos de build desnecessários

### CI/CD
- **GitHub Actions** (`.github/workflows/deploy.yml`)
- Fluxo de deploy em 3 jobs:
  1. **build-backend** e **build-frontend** em paralelo → constroem imagens Docker e fazem push para **GitHub Container Registry (ghcr.io)**
  2. **deploy** (depende de ambos) → conecta na VPS via SSH e executa `docker compose pull && docker compose up -d`
- Trigger: push no branch `main`
- Imagens publicadas em: `ghcr.io/obrandemburg/rascunho:latest` e `ghcr.io/obrandemburg/rascunho-client:latest`

### Ambiente de Produção
- **VPS** (Hetzner ou similar) com IP `5.161.202.169`
- Deploy via SSH com `docker compose`
- Volume persistente `pd_uploads` para fotos de perfil
- Pasta de deploy na VPS: `/home/usuario/pontodadanca`

### Branches do Git
- `main` — produção (trigger do CI/CD)
- `develop` — desenvolvimento ativo
- `claude/modest-feynman` — branch usado em sessões anteriores com o Claude

---

## Convenções e Padrões do Projeto

- **Idioma do código:** Português (nomes de classes, métodos, variáveis e comentários em PT-BR)
- **Mapeamento manual:** Sem AutoMapper — mappers escritos manualmente em `Mappers/`
- **Sem controllers:** Apenas Minimal APIs com endpoints mapeados em classes estáticas de extensão
- **Validação centralizada:** FluentValidation com registro automático + ValidationFilter
- **Tratamento de erros:** GlobalExceptionHandler retorna RFC 7807 ProblemDetails
- **IDs públicos ofuscados:** Hashids para não expor IDs sequenciais internos
- **CPF armazenado apenas com dígitos:** A formatação visual `123.456.789-01` é feita no frontend
- **Globalização:** `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` com ICU libs instaladas na imagem Docker para suporte correto a pt-BR
