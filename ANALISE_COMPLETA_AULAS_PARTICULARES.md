# 📋 Análise Completa do Sistema de Aulas Particulares

**Data:** 26 de Março de 2026
**Status:** Bugs Identificados e Documentados

---

## 🔴 BUGS IDENTIFICADOS

### BUG #1: Erro 403 ao Acessar Aulas Particulares (CRÍTICO)

**Localização:**
- Frontend: `Rascunho.Client/Pages/Aluno/AulasParticulares.razor` (linha 267)
- Backend: `Rascunho/Endpoints/GerenteEndpoints.cs` (linha 14-21)

**Problema:**
```
GET http://5.161.202.169:8080/api/gerente/configuracoes 403 Forbidden
```

**Causa Raiz:**
O componente Razor tenta acessar `/api/gerente/configuracoes` para obter o preço padrão de aulas particulares:

```csharp
// ❌ ERRADO - Requer role "Gerente"
var configTask = Http.GetFromJsonAsync<ConfigDto>("api/gerente/configuracoes");
```

No backend, o endpoint está dentro do grupo de Gerente que requer role "Gerente":

```csharp
var group = app.MapGroup("/api/gerente")
    .RequireAuthorization(policy => policy.RequireRole("Gerente"));

// Essa linha tenta sobrescrever, mas não funciona:
group.MapGet("/configuracoes", (ConfiguracaoService cfg) =>
Results.Ok(cfg.ObterConfiguracoes()))
.RequireAuthorization(policy => policy.RequireAuthenticatedUser());
```

O `RequireAuthorization` no endpoint individual não consegue sobrescrever a restrição do grupo.

**Impacto:**
- ❌ Alunos e Bolsistas recebem erro 403 ao tentar acessar a aba "Aulas Particulares"
- ❌ Componente não carrega corretamente
- ❌ Fluxo completo de aulas particulares é bloqueado

**Solução:**
Criar um novo endpoint `/api/aulas-particulares/configuracoes` que:
1. Permite acesso para usuários autenticados (Aluno, Bolsista, Líder)
2. Retorna apenas informações de LEITURA (preço, janela de reposição)
3. Não permite alterações

---

## 📊 FLUXO DO SISTEMA DE AULAS PARTICULARES

### 1. SOLICITAR AULA PARTICULAR

**Endpoints:**
```
POST /api/aulas-particulares/solicitar
Roles: Aluno, Bolsista, Líder
```

**Fluxo:**
```
1. Aluno acessa página "/aulas-particulares"
2. Preenche formulário:
   - Seleciona Professor
   - Seleciona Ritmo
   - Define Data e Horários
   - (Opcional) Adiciona observação
3. Sistema valida:
   ✓ Horários (início < fim, futuro)
   ✓ Professor existe e é válido
   ✓ Bolsista não agenda dança solo em dia obrigatório [RN-BOL05]
   ✓ Sem conflito de horários [RN-AP06]
4. Sistema calcula valor:
   - Se Bolsista: 50% do preço padrão [RN-BOL03]
   - Se Aluno: 100% do preço padrão
5. Aula criada com Status = "Pendente"
6. Email notifica o professor
```

**Regras de Negócio Aplicadas:**
- **RN-BOL03**: Bolsista paga 50% do preço padrão
- **RN-BOL05**: Bolsista não pode agendar solo nos dias obrigatórios
- **RN-AP06**: Valida conflito de horários para aluno

**Validações (FluentValidation):**
```csharp
✓ DataHoraInicio > DateTime.UtcNow
✓ DataHoraFim > DataHoraInicio
✓ Observacao.Length ≤ 500
```

---

### 2. RESPONDER SOLICITAÇÃO

**Endpoints:**
```
PUT /api/aulas-particulares/{aulaIdHash}/responder
Roles: Professor
```

**Fluxo:**
```
1. Professor recebe notificação de solicitação
2. Acessa interface e vê:
   - Nome do aluno
   - Ritmo solicitado
   - Data e horário
   - Valor que receberá
3. Professor responde:
   - ACEITAR: Aula passa para "Aceita"
   - RECUSAR: Aula passa para "Recusada"
```

**Validações ao Aceitar:**
- ✓ Professor é o dono da solicitação
- ✓ Status é "Pendente"
- ✓ Sem conflito com outras particulares do professor [RN-AP06]
- ✓ Sem conflito com turmas do professor [RN-AP06]

**Status Transitions:**
```
Pendente → (Aceitar) → Aceita
Pendente → (Recusar) → Recusada
```

---

### 3. CANCELAR AULA

**Endpoints:**
```
DELETE /api/aulas-particulares/{aulaIdHash}/cancelar
Roles: Aluno, Bolsista, Líder, Professor, Recepção, Gerente
```

**Fluxo:**
```
1. Usuário solicita cancelamento
2. Sistema verifica:
   ✓ Usuário tem permissão (dono ou staff)
   ✓ Status é "Pendente" ou "Aceita"
   ✓ Para Aluno: horas até aula ≥ 12 [RN-AP03]
3. Aula passa para "Cancelada"
4. Notificações enviadas
```

**Regra RN-AP03 (Regra das 12 horas):**
- Aluno só pode cancelar com ≥ 12h de antecedência
- Professor pode cancelar a qualquer momento
- Recepção/Gerente podem cancelar a qualquer momento

**Status Final:**
```
Pendente → (Cancelar) → Cancelada
Aceita   → (Cancelar) → Cancelada
```

---

### 4. REAGENDAR AULA (NOVO - Sprint 4)

**Endpoints:**
```
PUT /api/aulas-particulares/{aulaIdHash}/reagendar
Roles: Aluno, Bolsista, Líder
```

**Fluxo:**
```
1. Aluno clica em "Reagendar" na aula
2. Dialog abre com novos campos:
   - Nova Data
   - Novo Horário de Início
   - Novo Horário de Fim
3. Sistema valida:
   ✓ Aluno é dono da aula
   ✓ Status é "Pendente" ou "Aceita"
   ✓ Se "Aceita": horas até aula ≥ 12 [RN-AP03]
   ✓ Novos horários são válidos (futuro, fim > início)
   ✓ Sem conflito no novo horário [RN-AP06]
4. Aula atual é cancelada
5. Nova solicitação criada:
   - Mesmo professor
   - Mesmo ritmo
   - Mesmo valor (importante!)
   - Status = "Pendente" (professor aceita novamente)
6. Professor recebe notificação de novo horário
```

**Por que volta para Pendente?**
O professor aceitou o horário ORIGINAL, não o novo. Para garantir consentimento com a mudança, ele deve aceitar novamente.

---

### 5. LISTAR MINHAS AULAS

**Endpoints:**
```
GET /api/aulas-particulares/minhas
GET /api/aulas-particulares/minhas-aulas
Roles: Todos autenticados
```

**Filtros por Role:**
- **Aluno/Bolsista/Líder**: Vê aulas onde é aluno
- **Professor**: Vê aulas onde é professor

**Retorna:**
```csharp
List<ObterAulaParticularResponse> {
    IdHash,
    ProfessorIdHash,
    NomeProfessor,
    NomeAluno,
    NomeRitmo,
    DataHoraInicio,
    DataHoraFim,
    Status,
    Observacao,
    ValorCobrado
}
```

---

## 🎯 REGRAS DO SISTEMA (RESUMO)

| Regra | Descrição | Afetados |
|-------|-----------|----------|
| **RN-BOL03** | Bolsista paga 50% do preço padrão em aulas particulares | Bolsistas |
| **RN-BOL05** | Bolsista não pode agendar solo nos dias obrigatórios | Bolsistas |
| **RN-AP03** | Cancelamento requer ≥ 12h de antecedência (alunos) | Alunos |
| **RN-AP06** | Sem conflito de horários para aluno ou professor | Todos |

---

## 🔧 COMO TESTAR

### Teste 1: Acesso Básico (Aluno)
```
1. Login como Aluno
2. Acesse /aulas-particulares
3. ✓ Página carrega sem erro 403
4. ✓ Preço padrão exibido corretamente
5. ✓ Select de professores preenchido
6. ✓ Select de ritmos preenchido
```

### Teste 2: Solicitar Aula
```
1. Selecione professor e ritmo
2. Defina data (amanhã no mínimo)
3. Defina horário (início < fim)
4. Clique em "Solicitar Aula"
5. ✓ Aula criada com Status = "Pendente"
6. ✓ Aluno vê na lista
7. ✓ Botão "Cancelar" habilitado
8. ✓ Botão "Reagendar" habilitado
```

### Teste 3: Bolsista - Desconto
```
1. Login como Bolsista
2. Acesse /aulas-particulares
3. ✓ Preço mostrado = 50% do padrão
4. ✓ Aviso "desconto bolsista" exibido
5. Solicite aula
6. ✓ ValorCobrado = 50% no banco
```

### Teste 4: Bolsista - Restrição Solo
```
1. Login como Bolsista com dias obrigatórios
2. Tente solicitar solo no dia obrigatório
3. ✓ Erro: "Bolsistas não podem agendar..."
4. ✓ Em outro dia, permite normalmente
```

### Teste 5: Professor - Responder
```
1. Login como Professor
2. Acesse /aulas-particulares
3. ✓ Vê aulas onde é professor
4. ✓ Botões "Aceitar" e "Recusar" disponíveis
5. Clique "Aceitar"
6. ✓ Status muda para "Aceita"
7. ✓ Aluno recebe notificação
```

### Teste 6: Cancelamento com Regra 12h
```
1. Crie aula com status "Aceita"
2. Menos de 12h antes da aula:
   - Aluno clica cancelar
   - ✓ Erro: "Cancelamento com menos de 12h"
3. Professor tenta cancelar:
   - ✓ Sucesso (sem restrição)
4. Recepção tenta cancelar:
   - ✓ Sucesso (sem restrição)
```

### Teste 7: Reagendamento
```
1. Crie aula "Pendente"
2. Clique "Reagendar"
3. Selecione nova data/hora
4. ✓ Nova aula criada (status "Pendente")
5. ✓ Aula anterior cancelada
6. ✓ Professor recebe notificação
7. Professor aceita novo horário
8. ✓ Status muda para "Aceita"
```

### Teste 8: Conflito de Horários
```
1. Crie aula "Aceita" para amanhã 14:00-15:00
2. Tente criar outra aula no mesmo horário
3. ✓ Erro: "Você já possui uma aula neste horário"
4. Tente criar aula 14:30-15:30 (sobrepõe)
5. ✓ Erro: "Você já possui uma aula neste horário"
6. Tente criar 15:00-16:00 (sem sobreposição)
7. ✓ Sucesso
```

---

## 📝 CORREÇÕES NECESSÁRIAS

### Correção #1: Novo Endpoint para Obter Configurações

**Arquivo:** `Rascunho/Endpoints/AulaParticularEndpoints.cs`

**Adicionar:**
```csharp
// Novo endpoint que alunos podem acessar
group.MapGet("/configuracoes", (ConfiguracaoService cfg) =>
    Results.Ok(new
    {
        PrecoAulaParticular = cfg.ObterPrecoAulaParticular(),
        JanelaReposicaoDias = cfg.ObterJanelaReposicaoDias()
    }))
.WithName("ObterConfiguracoesAulasParticulares")
.WithOpenApi();
```

### Correção #2: Atualizar Frontend

**Arquivo:** `Rascunho.Client/Pages/Aluno/AulasParticulares.razor`

**Linha 267 - Mudar de:**
```csharp
var configTask = Http.GetFromJsonAsync<ConfigDto>("api/gerente/configuracoes");
```

**Para:**
```csharp
var configTask = Http.GetFromJsonAsync<ConfigDto>("api/aulas-particulares/configuracoes");
```

---

## ✅ CHECKLIST DE VALIDAÇÃO

- [ ] Erro 403 foi eliminado
- [ ] Aluno consegue acessar /aulas-particulares
- [ ] Preço carrega corretamente
- [ ] Todos os professores aparecem na lista
- [ ] Todos os ritmos aparecem na lista
- [ ] Solicitar aula funciona
- [ ] Responder aula funciona (professor)
- [ ] Cancelar aula funciona
- [ ] Regra 12h é respeitada
- [ ] Bolsista vê desconto de 50%
- [ ] Bolsista não pode agendar solo em dia obrigatório
- [ ] Reagendamento funciona corretamente
- [ ] Sem conflitos de horários
- [ ] Banco de dados está consistente
- [ ] Validações funcionam

---

## 📚 REFERÊNCIA RÁPIDA - DTOs

### Request para Solicitar
```csharp
SolicitarAulaParticularRequest {
    string ProfessorIdHash,
    string RitmoIdHash,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    string Observacao
}
```

### Response de Aula
```csharp
ObterAulaParticularResponse {
    string IdHash,
    string ProfessorIdHash,
    string NomeProfessor,
    string NomeAluno,
    string NomeRitmo,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    string Status,
    string Observacao,
    decimal ValorCobrado
}
```

### Estados de Aula
- **Pendente** - Aguardando resposta do professor
- **Aceita** - Professor aceitou, aula confirmada
- **Recusada** - Professor recusou a solicitação
- **Cancelada** - Aula foi cancelada

---

## 🔄 DIAGRAMA DE ESTADOS

```
                   ┌─────────────┐
                   │  Pendente   │
                   └─────────────┘
                        │
            ┌───────────┴───────────┐
            │                       │
        [Aceitar]              [Recusar]
            │                       │
            ▼                       ▼
    ┌─────────────┐         ┌─────────────┐
    │   Aceita    │         │  Recusada   │
    └─────────────┘         └─────────────┘
            │
        [Cancelar]
            │
            ▼
    ┌─────────────┐
    │ Cancelada   │
    └─────────────┘
```

---

**Documento gerado em:** 26/03/2026
**Versão:** 1.0
**Status:** Pronto para implementação das correções
