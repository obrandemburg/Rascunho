# Estratégia Comercial — Ponto da Dança como SaaS

**Adicionado em:** 06/04/2026
**Contexto:** Matheus está preparando reunião com o dono da escola para apresentar o sistema como produto comercial.

---

## Situação Atual

- A escola tem **800 alunos** (mensalidade R$ 250) e **200 bolsistas**
- O dono revelou que a escola está em **má fase financeira** — gargalo é retenção de alunos
- Matheus desenvolveu o **Ponto da Dança** do zero e quer vender como SaaS
- Concorrentes cobram entre **R$ 99 e R$ 299/mês** mas são genéricos (Tecnofit, EVO, Pacto, WebDança)

---

## Modelo de Precificação Definido

### Estratégia "Piloto de Impacto" (para o contexto de crise da escola)

| Fase | Período | Valor |
|---|---|---|
| Trial Guiado | Mês 1–2 | R$ 0 |
| Validação | Mês 3 | R$ 199 |
| Contrato Formal | Mês 4+ | R$ 399/mês |

- Ideia original (R$ 3.000 implantação + R$ 699/mês) foi descartada para o contexto de crise
- Preço alvo de R$ 399/mês gera ROI de 4x a 6x para o cliente

---

## Diferenciais Competitivos (que concorrentes não têm)

1. **Gestão de Bolsistas Nativa** — nenhum sistema do mercado trata bolsistas como força de trabalho com carga horária, rodízios e renovação
2. **Reposição em 1 clique** — sem burocracia de criar aulas novas
3. **Chamada granular** — presença, atraso, saída antecipada, ausência justificada (impacta elegibilidade de reposição)
4. **Aluguel de salas integrado** — nenhum concorrente oferece
5. **Construído por bolsista da própria escola** — credibilidade e proximidade únicas

---

## ROI para o Cliente (números da reunião)

- **R$ 15.000/ano** perdidos com 5 cancelamentos/mês (R$ 250 × 5 × 12)
- **R$ 1.500–3.000/mês** em bolsas pagas sem contrapartida (estimativa 15% irregular)
- **R$ 600/mês** em custo de mão de obra para tarefas manuais (WhatsApp, chamadas, reposições)
- **Total de valor gerado:** R$ 1.795–2.545/mês
- **Custo do sistema:** R$ 399/mês → **ROI de 4x a 6x**

---

## Principais Concorrentes

| Sistema | Lacuna principal |
|---|---|
| Tecnofit | Genérico (musculação). Sem bolsistas, sem reposição de dança |
| EVO/W12 | Foco em hardware (catracas). Irrelevante para dança |
| Pacto | CRM robusto mas sem contexto de dança |
| WebDança | Especializado em dança, mas não resolve bolsistas nem reposição automática |

---

## Posicionamento

- **Não é:** "Um Tecnofit para dança"
- **É:** "A plataforma construída por quem vive a dança"
- **Headline:** "Dança merecia um sistema à altura"
- **Proposta:** "30 dias de piloto sem custo. Se não ver valor, encerramos."

---

## Status da Reunião com o Dono

- [ ] Reunião ainda não realizada (em preparação)
- [ ] Roteiro de perguntas de descoberta definido (ver documento completo)
- [ ] Pitch de ROI preparado
- [ ] Scripts de objeção preparados

---

## Arquivos Relacionados

- `ESTRATEGIA_COMPLETA_PONTO_DA_DANCA.md` — documento mestre com tudo
- `analise-mercado-academias-danca.xlsx` — matriz de concorrentes e lacunas
- `análise-posicionamento-estratégico.docx` — narrativa de posicionamento

---

## Recomendações de Infraestrutura (para escalar como SaaS)

- **Hospedagem recomendada:** Hetzner VPS CPX21 — 4 GB RAM, 3 vCPU, 80 GB NVMe (~R$ 90/mês)
- **Custo total de infra:** ~R$ 104/mês (VPS + domínio + backups + Cloudflare free)
- **Margem de infra com R$ 399/mês:** R$ 295/mês de lucro líquido de infra

---

## Próximos Passos (P0 antes da reunião)

1. Dashboard executivo com KPIs visíveis
2. Módulo de bolsistas com relatório de irregularidade
3. Chamada digital por turma
4. Autenticação com roles funcionando
5. Teste de carga com 20+ usuários simultâneos
6. UI polida sem erros de console
