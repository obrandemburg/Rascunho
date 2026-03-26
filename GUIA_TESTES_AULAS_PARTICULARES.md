# 🧪 Guia Completo de Testes - Sistema de Aulas Particulares

**Data:** 26 de Março de 2026
**Versão:** 1.0
**Objetivo:** Validar todas as funcionalidades do sistema de aulas particulares

---

## 📋 PRÉ-REQUISITOS

### Usuários de Teste Necessários

1. **Aluno Common**
   - Email: `aluno@teste.com`
   - Senha: `senha123`
   - Role: Aluno

2. **Bolsista**
   - Email: `bolsista@teste.com`
   - Senha: `senha123`
   - Role: Bolsista
   - Dias Obrigatórios: Segunda e Quarta

3. **Professor**
   - Email: `professor@teste.com`
   - Senha: `senha123`
   - Role: Professor

4. **Gerente**
   - Email: `gerente@teste.com`
   - Senha: `senha123`
   - Role: Gerente

### Dados de Teste Necessários

- **Ritmo 1:** Dança Latino (Modalidade: Grupo) - ATIVO
- **Ritmo 2:** Dança Solo (Modalidade: Dança solo) - ATIVO
- **Professor 1:** Com disponibilidade ampla
- **Professor 2:** Com disponibilidade limitada

---

## 🔴 TESTE 1: BUG #1 - Acesso à Página (CRÍTICO)

### Objetivo
Validar que alunos/bolsistas conseguem acessar a página de aulas particulares sem erro 403.

### Passos

1. **Abra a aplicação**
   - URL: `http://localhost:3000`
   - Limpe o cache (F12 → Network → Disable cache)

2. **Faça login como Aluno**
   - Clique em "Login"
   - Email: `aluno@teste.com`
   - Senha: `senha123`
   - Clique em "Entrar"
   - ✓ Deve redirecionar para dashboard

3. **Acesse a aba "Aulas Particulares"**
   - No menu, clique em "Aulas Particulares"
   - URL deve ser: `http://localhost:3000/aulas-particulares`

4. **Validações**
   - ❌ **ANTES DA CORREÇÃO:** Página em branco, console mostra:
     ```
     GET http://localhost:8080/api/gerente/configuracoes 403 Forbidden
     ```
   - ✅ **DEPOIS DA CORREÇÃO:** Página carrega corretamente com:
     ```
     ✓ Seção "Solicitar Nova Aula" visível
     ✓ Select de Professores preenchido
     ✓ Select de Ritmos preenchido
     ✓ Seção "Minhas Aulas" visível
     ✓ Console mostra sucesso em: GET /api/aulas-particulares/configuracoes 200
     ```

### Validações no Console

**Abra DevTools (F12) → Console**

```javascript
// ❌ Não deve aparecer:
GET http://localhost:8080/api/gerente/configuracoes 403 Forbidden

// ✅ Deve aparecer:
GET http://localhost:8080/api/aulas-particulares/configuracoes 200 OK

// Verificar resposta:
{
  "precoAulaParticular": 80.00,
  "janelaReposicaoDias": 30
}
```

---

## 🟡 TESTE 2: Solicitar Aula - Aluno

### Objetivo
Validar que alunos conseguem solicitar aulas particulares com sucesso.

### Passos

1. **Login como Aluno**
   - Acesse `/aulas-particulares`

2. **Preencha o formulário**
   - **Professor:** Selecione "Professor 1"
   - **Ritmo:** Selecione "Dança Latino"
   - **Data:** Amanhã (mínimo)
   - **Horário de Início:** 14:00
   - **Horário de Fim:** 15:00
   - ✓ Preço exibido: R$ 80.00

3. **Clique em "Solicitar Aula"**
   - ✓ Mensagem de sucesso: "Solicitação enviada! ✅"
   - ✓ Aula aparece na lista com Status = "Pendente"
   - ✓ Botão "Cancelar" está habilitado
   - ✓ Botão "Reagendar" está habilitado

4. **Validações no Banco**
   - Execute no BD:
     ```sql
     SELECT * FROM "AulasParticulares"
     WHERE "AlunoId" = (SELECT "Id" FROM "Usuarios" WHERE "Email" = 'aluno@teste.com')
     ORDER BY "DataSolicitacao" DESC LIMIT 1;
     ```
   - ✓ Status = 'Pendente'
   - ✓ ValorCobrado = 80.00
   - ✓ DataHoraInicio e DataHoraFim estão corretos (UTC)

---

## 🟡 TESTE 3: Solicitar Aula - Bolsista (com desconto)

### Objetivo
Validar que bolsistas recebem desconto de 50% em aulas particulares.

### Passos

1. **Login como Bolsista**
   - Email: `bolsista@teste.com`
   - Acesse `/aulas-particulares`

2. **Verifique o preço exibido**
   - ✓ Seção de preço deve mostrar:
     ```
     💰 Seu valor: R$ 40.00 (50% de desconto bolsista)
     ```
   - ✓ Aviso em verde (Severity.Success)

3. **Preencha e solicite aula**
   - **Professor:** "Professor 1"
   - **Ritmo:** "Dança Latino" (NÃO solo)
   - **Data:** Amanhã
   - **Hora:** 15:00-16:00
   - Clique "Solicitar Aula"
   - ✓ Sucesso

4. **Validações**
   - ✓ Aula aparece na lista
   - ✓ Valor exibido: "R$ 40.00 (50% desconto bolsista)"
   - ✓ No banco: ValorCobrado = 40.00

---

## 🔴 TESTE 4: Bolsista - Restrição de Dia Obrigatório

### Objetivo
Validar que bolsista NÃO consegue agendar solo no dia obrigatório.

### Pressupostos
- Bolsista tem dias obrigatórios: Segunda (1) e Quarta (3)

### Passos

1. **Login como Bolsista**

2. **Tente agendar solo na SEGUNDA**
   - **Ritmo:** Selecione "Dança Solo" (modalidade: Dança solo)
   - **Data:** Próxima segunda-feira
   - **Hora:** 14:00-15:00
   - Clique "Solicitar Aula"
   - ❌ Erro esperado:
     ```
     Bolsistas não podem agendar aulas particulares de dança solo
     nos seus dias obrigatórios. [RN-BOL05]
     ```

3. **Agende solo na TERÇA (permitido)**
   - **Ritmo:** "Dança Solo"
   - **Data:** Próxima terça-feira
   - **Hora:** 14:00-15:00
   - Clique "Solicitar Aula"
   - ✅ Sucesso

4. **Validações no Banco**
   - ✓ Apenas a terça foi registrada
   - ✓ Segunda não tem registro da tentativa

---

## 🟡 TESTE 5: Professor - Responder Solicitação

### Objetivo
Validar que professor consegue aceitar/recusar solicitações.

### Passos

1. **Login como Professor**
   - Email: `professor@teste.com`

2. **Acesse `/aulas-particulares`**
   - ✓ Vê aulas onde é PROFESSOR
   - ✓ Vê as solicitações de Test 2 e/ou Test 3

3. **Clique em "Aceitar"**
   - ✓ Status muda para "Aceita" imediatamente
   - ✓ Mensagem: "Aula aceita!"
   - ✓ Botão "Reagendar" aparece para aluno

4. **Teste "Recusar"**
   - Crie uma nova aula como aluno
   - Login como professor
   - Clique em "Recusar"
   - ✓ Status muda para "Recusada"
   - ✓ Aluno não consegue mais reagendar

5. **Validação de Conflitos**
   - Aceite aula 1 (14:00-15:00)
   - Tente aceitar aula 2 (14:30-15:30) do mesmo professor
   - ❌ Erro:
     ```
     Você já tem uma aula ou turma neste horário.
     ```

---

## 🔴 TESTE 6: Cancelamento - Regra 12h

### Objetivo
Validar que alunos só podem cancelar com ≥ 12h de antecedência.

### Passos

1. **Crie aula "Aceita" para daqui a 8 horas**
   - Login como Aluno
   - Solicite aula para hoje às 21:00-22:00
   - (Ou simule no BD com `UPDATE`)
   - Login como Professor
   - Aceite a aula

2. **Tente cancelar como Aluno**
   - Aula na lista tem Status "Aceita"
   - Clique "Cancelar"
   - ❌ Erro esperado:
     ```
     Cancelamento com menos de 12 horas de antecedência não é permitido.
     Entre em contato com o professor ou a recepção. [RN-AP03]
     ```

3. **Professor cancela (sem restrição)**
   - Login como Professor
   - Mesma aula tem botão "Cancelar"
   - Clique "Cancelar"
   - ✅ Sucesso (sem erro)
   - ✓ Status = "Cancelada"

4. **Recepção cancela (sem restrição)**
   - Crie nova aula "Aceita"
   - Login com usuário "Recepção"
   - Clique "Cancelar"
   - ✅ Sucesso

---

## 🟢 TESTE 7: Reagendamento

### Objetivo
Validar o fluxo completo de reagendamento.

### Passos

1. **Crie aula "Pendente" com 24h+ de antecedência**
   - Login como Aluno
   - Solicite para 48 horas à frente
   - 14:00-15:00
   - ✓ Aula criada (Status = "Pendente")

2. **Clique "Reagendar"**
   - Dialog abre
   - ✓ Campos vazios (exceto hora sugerida)
   - ✓ Aviso: "A aula reagendada volta para Pendente"

3. **Preencha novo horário**
   - **Nova Data:** 48 horas + 2 dias
   - **Novo Horário:** 16:00-17:00
   - Clique "Confirmar"
   - ✓ Mensagem: "Aula reagendada! ✅"

4. **Validações**
   - ✓ Aula original desaparece (ou mostra "Cancelada")
   - ✓ Nova aula aparece com Status = "Pendente"
   - ✓ Horário é o novo (16:00-17:00)
   - ✓ Valor mantém o mesmo (RN-BOL03 preservado)

5. **Professor aceita novo horário**
   - Login como Professor
   - Vê nova aula com Status "Pendente"
   - Clique "Aceitar"
   - ✓ Status = "Aceita"

6. **Validação no BD**
   - Aula original: Status = "Cancelada"
   - Nova aula: Status = "Aceita", DataHoraInicio = novo horário

---

## 🔴 TESTE 8: Conflito de Horários

### Objetivo
Validar que sistema impede aulas em horários conflitantes.

### Passos

1. **Crie aula 1 "Aceita"**
   - Data: Amanhã
   - Hora: 14:00-15:00
   - ✓ Aceita pelo professor

2. **Tente criar aula 2 no MESMO horário**
   - Data: Amanhã
   - Hora: 14:00-15:00
   - Clique "Solicitar"
   - ❌ Erro esperado:
     ```
     Você já possui uma aula particular agendada neste horário.
     ```

3. **Tente criar aula 3 com SOBREPOSIÇÃO**
   - Data: Amanhã
   - Hora: 14:30-15:30
   - Clique "Solicitar"
   - ❌ Erro esperado (mesmo acima)

4. **Crie aula 4 APÓS primeira (SEM conflito)**
   - Data: Amanhã
   - Hora: 15:00-16:00 (exatamente quando primeira termina)
   - Clique "Solicitar"
   - ✅ Sucesso (não há sobreposição)

---

## 🟡 TESTE 9: Listar Minhas Aulas

### Objetivo
Validar que cada role vê apenas suas aulas.

### Passos

1. **Login como Aluno 1**
   - Crie 2 aulas (onde é aluno)
   - Acesse `/aulas-particulares`
   - ✓ Vê apenas as 2 aulas

2. **Login como Aluno 2**
   - Acesse `/aulas-particulares`
   - ✓ Vê apenas suas aulas (0 se não criou)
   - ✗ Não vê aulas de Aluno 1

3. **Login como Professor 1**
   - Acesse `/aulas-particulares`
   - ✓ Vê aulas onde é PROFESSOR
   - ✗ Não vê aulas onde é ALUNO

---

## 🟢 TESTE 10: Integração Completa

### Objetivo
Fluxo end-to-end realista.

### Cenário
Um aluno deseja agendar uma aula de dança latino com o professor. O professor aceita, mas depois o aluno precisa reagendar. O professor aceita o novo horário.

### Passos

1. **Aluno solicita**
   - Login como Aluno
   - Solicita aula (Latino, Prof1, amanhã 14:00-15:00)
   - ✓ Aula criada (Pendente)

2. **Professor vê notificação**
   - Login como Professor
   - Vê aula em `/aulas-particulares`
   - ✓ Status = "Pendente"
   - ✓ Botão "Aceitar" disponível

3. **Professor aceita**
   - Clique "Aceitar"
   - ✓ Status = "Aceita"
   - ✓ Aluno notificado (simular com refresh)

4. **Aluno reagenda (com ≥12h)**
   - Login como Aluno
   - Clique "Reagendar"
   - Nova data: +3 dias
   - Nova hora: 16:00-17:00
   - ✓ Sucesso

5. **Professor vê novo horário**
   - Refresh `/aulas-particulares`
   - ✓ Aula anterior: "Cancelada"
   - ✓ Nova aula: "Pendente", 16:00-17:00

6. **Professor aceita novo horário**
   - Clique "Aceitar"
   - ✓ Status = "Aceita"

7. **Validação Final**
   - Aluno vê aula confirmada em novo horário
   - Aluno pode comentar ou preparar

---

## ⚠️ TESTES DE ERRO

### Erro 1: Data no Passado

```
Ação: Solicite aula com data anterior a hoje
Esperado: "A data da aula deve ser no futuro."
```

### Erro 2: Hora Fim ≤ Hora Início

```
Ação: Defina fim = início (ex: 14:00-14:00)
Esperado: "O horário de fim deve ser maior que o horário de início."
```

### Erro 3: Observação Muito Longa

```
Ação: Observação com >500 caracteres
Esperado: "A observação deve ter no máximo 500 caracteres."
```

### Erro 4: Professor Inválido

```
Ação: Forge request com professorIdHash inválido
Esperado: 400 Bad Request ou "Professor não encontrado"
```

### Erro 5: Reagendamento de Aula Recusada

```
Ação: Reagendar aula com status "Recusada"
Esperado: "Só é possível reagendar aulas com status Pendente ou Aceita."
```

---

## 📊 MATRIZ DE TESTES

| # | Teste | Aluno | Bolsista | Professor | Status |
|---|-------|-------|----------|-----------|--------|
| 1 | Acesso à página | ✓ | ✓ | N/A | 🟢 |
| 2 | Solicitar aula | ✓ | ✓ | - | 🟢 |
| 3 | Desconto bolsista | - | ✓ | - | 🟢 |
| 4 | Restrição solo | - | ✓ | - | 🟢 |
| 5 | Responder | - | - | ✓ | 🟢 |
| 6 | Cancelamento 12h | ✓ | ✓ | ✓ | 🟢 |
| 7 | Reagendamento | ✓ | ✓ | - | 🟢 |
| 8 | Conflito horários | ✓ | ✓ | - | 🟢 |
| 9 | Listar (filtro) | ✓ | ✓ | ✓ | 🟢 |
| 10 | Fluxo completo | ✓ | ✓ | ✓ | 🟢 |

---

## ✅ CHECKLIST FINAL

Antes de considerar o sistema pronto:

- [ ] Teste 1 passou (BUG #1 corrigido)
- [ ] Teste 2 passou (Solicitar como Aluno)
- [ ] Teste 3 passou (Desconto Bolsista)
- [ ] Teste 4 passou (Restrição Solo)
- [ ] Teste 5 passou (Professor responde)
- [ ] Teste 6 passou (Regra 12h)
- [ ] Teste 7 passou (Reagendamento)
- [ ] Teste 8 passou (Conflito horários)
- [ ] Teste 9 passou (Listar filtrado)
- [ ] Teste 10 passou (Fluxo E2E)
- [ ] Banco de dados consistente
- [ ] Performance aceitável
- [ ] Sem erro 500 no servidor
- [ ] Sem erro 404 em recursos
- [ ] Validações funcionam
- [ ] Mensagens de erro claras

---

**Documento gerado em:** 26/03/2026
**Versão:** 1.0
**Status:** Pronto para execução dos testes
