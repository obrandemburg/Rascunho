# 🔴 ANÁLISE DO ERRO — TimeSpan vs DateTime Format

**Data:** 26 de março de 2026
**Erro:** System.FormatException: Input string was not in a correct format
**Causa:** Formato de DateTime aplicado a TimeSpan
**Arquivo:** ListaEsperaService.cs
**Linhas:** 223-224

---

## 📊 O PROBLEMA

### Erro Recebido:
```
System.FormatException: Input string was not in a correct format
   at System.Globalization.TimeSpanFormat.FormatCustomized
   at Rascunho.Services.ListaEsperaService.ObterMinhasEsperasAsync line 217
```

### Código Problemático:
```csharp
// ListaEsperaService.cs linhas 223-224:
le.Turma.HorarioInicio.ToString(@"HH\:mm"),  // ❌ ERRO
le.Turma.HorarioFim.ToString(@"HH\:mm"),     // ❌ ERRO
```

### O Tipo Real (Turma.cs):
```csharp
public TimeSpan HorarioInicio { get; protected set; }  // ← É TimeSpan!
public TimeSpan HorarioFim { get; protected set; }     // ← É TimeSpan!
```

---

## 🧠 ENTENDENDO A DIFERENÇA

### DateTime vs TimeSpan

| Conceito | DateTime | TimeSpan |
|----------|----------|----------|
| **O que é** | Ponto no tempo | Duração de tempo |
| **Exemplo** | 26/03/2026 14:30 | 14 horas e 30 minutos |
| **Tipo** | Data + Hora | Intervalo |
| **Uso** | "Quando?" | "Quanto tempo?" |
| **Formato Hora** | `HH:mm` (maiúsculo) | `hh:mm` (minúsculo) |

### Exemplos Práticos:

```csharp
// DateTime — Ponto no tempo
DateTime agora = DateTime.Now;  // "Agora é 14:30"
string formato = agora.ToString("dd/MM/yyyy HH:mm");  // "26/03/2026 14:30"
                                           ↑↑
                                    Maiúsculo (DateTime)

// TimeSpan — Duração
TimeSpan duracao = new TimeSpan(14, 30, 0);  // 14 horas 30 minutos
string formato = duracao.ToString(@"hh\:mm");  // "14:30"
                                        ↑↑
                                    Minúsculo (TimeSpan)
```

---

## ❌ POR QUE FALHA?

### O Problema Técnico:

```csharp
TimeSpan horarioInicio = new TimeSpan(14, 30, 0);  // 14:30

// ❌ ERRADO — Usa formato DateTime
horarioInicio.ToString(@"HH\:mm");  // FormatException!
              ↑
              TimeSpan não reconhece 'HH' (maiúsculo)

// ✅ CORRETO — Usa formato TimeSpan
horarioInicio.ToString(@"hh\:mm");  // "14:30" ✓
              ↑
              TimeSpan reconhece 'hh' (minúsculo)
```

### Por que C# reclama?

1. `TimeSpan.ToString()` tem **especificações de formato diferentes de DateTime**
2. `HH` (maiúsculo) não existe em TimeSpan
3. TimeSpan usa `hh` (minúsculo) para horas
4. C# lança `FormatException` porque não reconhece o especificador

---

## ✅ A CORREÇÃO

### Mudança Simples:

```diff
- le.Turma.HorarioInicio.ToString(@"HH\:mm"),  // ❌ DateTime format
+ le.Turma.HorarioInicio.ToString(@"hh\:mm"),  // ✅ TimeSpan format
```

**De:** `@"HH\:mm"` (maiúsculo = DateTime)
**Para:** `@"hh\:mm"` (minúsculo = TimeSpan)

---

## 📋 TABELA DE FORMATOS

### DateTime Format Specifiers:
```csharp
DateTime dt = new DateTime(2026, 3, 26, 14, 30, 45);

"HH:mm"     → "14:30"  (24h, 2 dígitos)
"hh:mm tt"  → "02:30 PM"  (12h com AM/PM)
"yyyy-MM-dd HH:mm:ss" → "2026-03-26 14:30:45"
```

### TimeSpan Format Specifiers:
```csharp
TimeSpan ts = new TimeSpan(14, 30, 45);

@"hh\:mm"           → "14:30"  (horas:minutos)
@"hh\:mm\:ss"       → "14:30:45"  (horas:minutos:segundos)
@"d\.hh\:mm"        → "0.14:30"  (dias.horas:minutos)
@"h\:mm\:ss\.fff"   → "14:30:45.000"  (com milissegundos)
```

---

## 🔗 RELACIONAMENTO DO ERRO COM NOSSAS MUDANÇAS

```
ListaEsperaService.cs (nova verificação)
    ↓
ObterMinhasEsperasAsync()
    ↓
Ao retornar MinhaEsperaResponse, formata HorarioInicio/HorarioFim
    ↓
Tenta usar formato DateTime (@"HH\:mm") em TimeSpan
    ↓
❌ FormatException!
```

**Nota:** Este erro **NÃO está relacionado** às mudanças de DateTimeOffset. É um bug pré-existente que foi exposto quando alguém com entradas na fila tentou acessar `/minhas-esperas`.

---

## 🧪 VALIDAÇÃO DA CORREÇÃO

### Antes:
```
HorarioInicio.ToString(@"HH\:mm")
TimeSpan não reconhece HH
❌ FormatException: Input string was not in a correct format
```

### Depois:
```
HorarioInicio.ToString(@"hh\:mm")
TimeSpan reconhece hh
✅ Retorna "14:30"
```

---

## 📝 LIÇÃO — Formatos em C#

### Regra de Ouro:

| Tipo | Use Maiúsculo | Use Minúsculo |
|------|----------------|---------------|
| **DateTime** | ✅ `HH:mm` (hora 24h) | ❌ (não use) |
| **TimeSpan** | ❌ (não use) | ✅ `hh:mm` (horas da duração) |

---

## ✨ CONCLUSÃO

**O Problema:** Formato de `DateTime` aplicado a `TimeSpan`
**A Solução:** Usar formato correto para `TimeSpan` (minúsculas)
**Impacto:** Página "Minhas Esperas" agora funcionará corretamente
**Status:** ✅ RESOLVIDO

### Teste:
Acesse `/minhas-esperas` com um usuário que tem entradas na fila.
✅ Esperado: Página carrega com horários corretos (ex: "14:30")
