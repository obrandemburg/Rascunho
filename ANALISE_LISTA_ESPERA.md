# ANÁLISE: Funcionalidade de Lista de Espera — Problemas e Soluções

**Data:** 26 de março de 2026
**Sistema:** Rascunho (Ponto da Dança)
**Módulo:** Lista de Espera (Feature #3)

---

## 📋 RESUMO EXECUTIVO

Foram identificados **2 problemas críticos** na funcionalidade de lista de espera:

| # | Problema | Severidade | Status |
|---|----------|-----------|---------|
| **P1** | Botão "Entrar na fila" pode ser clicado múltiplas vezes, registrando o mesmo usuário repetidamente | **CRÍTICA** | ⏳ A corrigir |
| **P2** | Erros de tipo `DateTime/DateOnly` ao carregar página "Minhas Esperas" | **ALTA** | ⏳ A corrigir |

---

## 🔴 PROBLEMA 1: Duplicação de Entradas na Fila

### Descrição
Um aluno ou bolsista consegue clicar quantas vezes quiser no botão "Entrar na fila". A cada clique, o sistema registra o mesmo usuário **novamente** na lista de espera, criando entradas duplicadas.

### Root Cause (Causa Raiz)

No arquivo **`Rascunho/Services/ListaEsperaService.cs`**, o método `EntrarNaFilaAsync()` **não valida se o usuário já existe** na fila com status ativo:

```csharp
public async Task<string> EntrarNaFilaAsync(int turmaId, int alunoId)
{
    // ❌ PROBLEMA: Não verifica se alunoId já está na fila ativa
    int proximaPosicao = await _context.ListasEspera
        .CountAsync(le => le.TurmaId == turmaId &&
                          (le.Status == StatusListaEspera.Aguardando ||
                           le.Status == StatusListaEspera.Notificado)) + 1;

    var entrada = new ListaEspera { /* ... */ };
    _context.ListasEspera.Add(entrada);  // ← Adiciona sem validação!
    await _context.SaveChangesAsync();
    return $"...posição {proximaPosicao}.";
}
```

**Fluxo de ataque:**
1. Usuário clica botão → entra em posição 1
2. Usuário clica novamente → **entra em posição 2** (mesma turma, mesmo usuário)
3. Usuário clica mais vezes → entra em posições 3, 4, 5...

### Impacto
- **Integridade de dados:** Banco fica com registros duplicados
- **Experiência do usuário:** Confusão ao ver-se em múltiplas posições
- **Lógica de notificação:** O sistema pode notificar o mesmo aluno várias vezes
- **Relatórios:** Contagem de fila fica incorreta

### Solução Implementada

**Adicionar validação de duplicata** no início de `EntrarNaFilaAsync()`:

```csharp
public async Task<string> EntrarNaFilaAsync(int turmaId, int alunoId)
{
    // ✅ NOVA VALIDAÇÃO: Verificar se aluno já está ativo na fila
    var entradaExistente = await _context.ListasEspera
        .FirstOrDefaultAsync(le =>
            le.TurmaId == turmaId &&
            le.AlunoId == alunoId &&
            (le.Status == StatusListaEspera.Aguardando ||
             le.Status == StatusListaEspera.Notificado));

    if (entradaExistente != null)
    {
        // Retorna a posição atual sem criar duplicata
        return $"Você já está na fila de espera desta turma na posição {entradaExistente.Posicao}.";
    }

    // Resto do código...
}
```

### Melhorias na UI

Além da validação no backend, **a página deve refletir visualmente** que o usuário já está na fila:

**Estado atual (MinhasEsperas.razor):**
```
[Entrar na Fila]  [Cancelar Espera]
```

**Estado melhorado:**
```
Você está na posição 5 de espera  [Cancelar Espera]
```

---

## 🔴 PROBLEMA 2: Erro de Tipo DateTime na Página "Minhas Esperas"

### Descrição
Quando um usuário com entradas salvas no banco acessa a página `/minhas-esperas`, ocorrem erros relacionados ao formato de data:
- `DateTime` vs `DateOnly` mismatch
- Desserialização falhando no frontend
- Inconsistência entre camadas

### Root Cause (Causa Raiz)

**Entidade (ListaEspera.cs):**
```csharp
public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
public DateTime? DataNotificacao { get; set; }
public DateTime? DataExpiracao { get; set; }
```

**DTO (TurmaDTOs.cs):**
```csharp
public record MinhaEsperaResponse(
    // ...
    DateTime DataEntrada,      // ← DateTime com hora
    DateTime? DataExpiracao    // ← DateTime com hora
);
```

**Frontend (MinhasEsperas.razor):**
```csharp
public class MinhaEsperaDto
{
    public DateTime DataEntrada { get; set; }    // ← DateTime
    public DateTime? DataExpiracao { get; set; } // ← DateTime
}
```

**O Problema:**
- Em algumas migrações anteriores (`20260325151124_AlterarDataInicioParaDateOnly`), houve conversão para `DateOnly`
- Há inconsistência entre `DateTime` (com hora UTC) e `DateOnly` (só data)
- Se o banco armazenou como `DateOnly`, a desserialização para `DateTime` falha
- O JSON pode estar vindo em formato incompatível

### Impacto
- ❌ Página `/minhas-esperas` não carrega para usuários com esperas
- ❌ API retorna erro 500 ou dados malformados
- ❌ Usuário não consegue ver suas entradas na fila

### Solução Recomendada

**Usar `DateTimeOffset` em vez de `DateTime`:**

✅ **Vantagens:**
- Inclui timezone (mais preciso que UTC)
- Melhor para serialização JSON
- Desserializa corretamente em C# e JavaScript
- Seguro para comparações (sempre com timezone)

**Mudanças necessárias:**

1. **ListaEspera.cs:** Alterar para `DateTimeOffset`
2. **ListaEsperaService.cs:** Atualizar DateTime.UtcNow → DateTimeOffset.UtcNow
3. **DTOs (TurmaDTOs.cs):** Atualizar tipos de resposta
4. **MinhasEsperas.razor:** Atualizar DTO se necessário
5. **Migration:** Criar para converter dados existentes

---

## ✅ CORREÇÕES A IMPLEMENTAR

### 1️⃣ **ListaEspera.cs** — Alterar tipos de data

```diff
- public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
+ public DateTimeOffset DataEntrada { get; set; } = DateTimeOffset.UtcNow;

- public DateTime? DataNotificacao { get; set; }
+ public DateTimeOffset? DataNotificacao { get; set; }

- public DateTime? DataExpiracao { get; set; }
+ public DateTimeOffset? DataExpiracao { get; set; }
```

### 2️⃣ **ListaEsperaService.cs** — Adicionar validação + atualizar datas

```diff
// No método EntrarNaFilaAsync:
+ // Validar duplicata
+ var entradaExistente = await _context.ListasEspera
+     .FirstOrDefaultAsync(le =>
+         le.TurmaId == turmaId &&
+         le.AlunoId == alunoId &&
+         (le.Status == StatusListaEspera.Aguardando ||
+          le.Status == StatusListaEspera.Notificado));
+
+ if (entradaExistente != null)
+     return $"Você já está na fila de espera desta turma na posição {entradaExistente.Posicao}.";

// Atualizar DateTime.UtcNow:
- public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
+ public DateTimeOffset DataEntrada { get; set; } = DateTimeOffset.UtcNow;

// E em outros métodos (NotificarProximoAsync, etc.):
- DateTime dataExpiracao = DateTime.UtcNow.AddHours(_prazoConfirmacaoHoras);
+ DateTimeOffset dataExpiracao = DateTimeOffset.UtcNow.AddHours(_prazoConfirmacaoHoras);

- proximo.DataNotificacao = DateTime.UtcNow;
+ proximo.DataNotificacao = DateTimeOffset.UtcNow;

// Em comparações:
- le.DataExpiracao < DateTime.UtcNow
+ le.DataExpiracao < DateTimeOffset.UtcNow
```

### 3️⃣ **TurmaDTOs.cs** — Atualizar MinhaEsperaResponse

```diff
public record MinhaEsperaResponse(
    string TurmaIdHash,
    string RitmoNome,
    string SalaNome,
    string Nivel,
    int DiaDaSemana,
    string HorarioInicio,
    string HorarioFim,
    int Posicao,
    string Status,
-   DateTime DataEntrada,
-   DateTime? DataExpiracao
+   DateTimeOffset DataEntrada,
+   DateTimeOffset? DataExpiracao
);
```

### 4️⃣ **TurmaEndpoints.cs** — Adicionar validação no endpoint

```diff
// No MapPost para lista-espera:
+ // Validar se aluno já está na fila
+ var entradaExistente = await _context.ListasEspera
+     .FirstOrDefaultAsync(le =>
+         le.TurmaId == decodedIds[0] &&
+         le.AlunoId == alunoId &&
+         (le.Status == StatusListaEspera.Aguardando ||
+          le.Status == StatusListaEspera.Notificado));
+
+ if (entradaExistente != null)
+     return Results.Conflict(new {
+         erro = $"Você já está na fila de espera desta turma na posição {entradaExistente.Posicao}."
+     });
```

### 5️⃣ **MinhasEsperas.razor** — Atualizar DTO

```csharp
public class MinhaEsperaDto
{
    public string TurmaIdHash { get; set; } = "";
    public string RitmoNome   { get; set; } = "";
    public string SalaNome    { get; set; } = "";
    public string Nivel       { get; set; } = "";
    public int    DiaDaSemana { get; set; }
    public string HorarioInicio { get; set; } = "";
    public string HorarioFim    { get; set; } = "";
    public int    Posicao    { get; set; }
    public string Status     { get; set; } = "";
-   public DateTime DataEntrada { get; set; }
-   public DateTime? DataExpiracao { get; set; }
+   public DateTimeOffset DataEntrada { get; set; }
+   public DateTimeOffset? DataExpiracao { get; set; }
}
```

### 6️⃣ **Migration** — Converter dados existentes

Criar migration para alterar colunas:
```sql
ALTER TABLE ListasEspera
ALTER COLUMN DataEntrada DATETIMEOFFSET NOT NULL;

ALTER TABLE ListasEspera
ALTER COLUMN DataNotificacao DATETIMEOFFSET NULL;

ALTER TABLE ListasEspera
ALTER COLUMN DataExpiracao DATETIMEOFFSET NULL;
```

---

## 📊 Tabela de Mudanças

| Arquivo | Mudança | Tipo |
|---------|---------|------|
| `ListaEspera.cs` | `DateTime` → `DateTimeOffset` | Entidade |
| `ListaEsperaService.cs` | Adicionar validação duplicata + atualizar tipos | Serviço |
| `TurmaDTOs.cs` | `DateTime` → `DateTimeOffset` | DTO |
| `TurmaEndpoints.cs` | Adicionar validação no endpoint | Endpoint |
| `MinhasEsperas.razor` | Atualizar DTO local | Frontend |
| `Migration 20260326_...` | Converter colunas de data | BD |

---

## 🧪 Testes Sugeridos

### Teste 1: Evitar Duplicata
```
1. Aluno acessa turma lotada
2. Clica "Entrar na Fila" → Posição 1 ✓
3. Clica novamente → Mensagem: "Já na posição 1" ✓
4. Não cria entrada duplicada ✓
```

### Teste 2: Carregar Minhas Esperas
```
1. Aluno com entradas existentes acessa /minhas-esperas
2. Página carrega sem erros ✓
3. Exibe data/hora corretamente ✓
4. Botões funcionam (confirmar/sair) ✓
```

### Teste 3: Prazo de Expiração
```
1. Aluno é notificado (Notificado)
2. Sistema calcula prazo: Now + 48h
3. Exibição: "Confirmar até 28/03 14:30" ✓
```

---

## 📝 Notas Importantes

- **DateTimeOffset vs DateTime:** `DateTimeOffset` é superior porque preserva o timezone. `DateTime.UtcNow` perde essa informação.
- **Backward Compatibility:** A migration converterá dados existentes sem perder informações.
- **API JSON:** ASP.NET Core desserializa automaticamente `DateTimeOffset` do JSON sem configs adicionais.
- **Frontend:** JavaScript/TypeScript também suporta `DateTimeOffset` ISO 8601 nativamente.

---

## ✨ Benefícios Esperados

✅ Evita registros duplicados
✅ Melhora integridade de dados
✅ Corrige erros de desserialização
✅ Interface mais clara ("Posição X" em vez de botão ambíguo)
✅ Maior confiabilidade na notificação de vagas
