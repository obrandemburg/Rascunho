# Ponto da Dança — Camada 1: Visão Geral

## O que é o projeto?

O **Ponto da Dança** é um sistema de gestão escolar especializado para uma academia de dança, desenvolvido como Progressive Web App (PWA). O sistema digitaliza e automatiza todas as operações internas da escola: controle de turmas, chamadas, aulas particulares, sistema de bolsistas, avisos, e lista de espera — eliminando planilhas e comunicação informal por WhatsApp.

O produto é acessado via navegador (com instalação como app no celular/desktop via PWA). Não há app nativo nas lojas. O nome de código do repositório é **Rascunho**, mas o produto final é o **Ponto da Dança**.

---

## Perfis de Usuário

O sistema possui 6 perfis com permissões distintas e isoladas. Nenhum perfil pode acessar telas de outro via URL ou API.

| Perfil | Descrição curta |
|---|---|
| **Aluno** | Paga mensalidade por turma. Acesso às próprias aulas, aulas particulares e avisos. |
| **Bolsista** | Dança de salão gratuita. 50% de desconto em solo e particulares. Possui dias obrigatórios semanais. |
| **Professor** | Gerencia suas turmas, realiza chamadas e administra aulas particulares. |
| **Recepção** | Administra turmas, usuários, avisos e o sistema de bolsistas. |
| **Gerente** | Tudo da recepção + quadro de desempenho detalhado de bolsistas. |
| **Líder** | Perfil a ser detalhado na Fase 1.2 (visualização de faturamento). |

---

## Funcionalidades do MVP (Fase 1.1)

### Módulo de Turmas
- Listagem pública e filtrada de turmas ativas (ritmo, professor, dia, horário, nível, data de início)
- Matrícula com validação de conflitos de horário e vagas
- Lista de espera com notificação automática quando vaga é liberada
- Encerramento de turma com notificação push a todos os matriculados

### Módulo de Chamada
- Registro de presença no dia da aula ou com até 24h de atraso
- Seções separadas: alunos matriculados e participantes extras (bolsistas, reposições, experimentais)
- Identificação automática do tipo de participante extra
- Notificação push ao bolsista confirmando presença registrada

### Módulo de Aulas Particulares
- Agendamento por alunos e bolsistas filtrado por professor, ritmo, dia e horário
- Grade de disponibilidade configurada pelo professor, nunca conflitante com turmas
- Aceite/recusa pelo professor com notificação push ao aluno
- Regra de cancelamento: mínimo 12 horas de antecedência para aluno/bolsista (professor pode cancelar a qualquer momento)

### Sistema de Bolsistas
- Dois dias obrigatórios semanais com mínimo de 6 horas/aulas por semana
- Indicador de frequência calculado exclusivamente pelos dias obrigatórios
- Tela de guia de turmas — mostra onde a presença do bolsista agrega mais valor (balanceamento condutor/conduzido)
- Desempenho com 4 faixas de situação (Excelente, Vamos melhorar, Atenção, Crítico)
- Sugestão automática de novos bolsistas baseada em critérios de frequência, tempo de casa e número de turmas
- Auditoria: bolsista pode ver todas as presenças registradas, aula a aula

### Módulo de Avisos
- Avisos com período de exibição, público-alvo (geral, equipe, turma específica) e data de expiração
- Publicação por recepção, gerente e professor (apenas para suas próprias turmas)

### Módulo de Reposição de Aulas
- Faltas elegíveis para reposição com janela de tempo configurável
- Reagendamento dentro das vagas disponíveis da turma
- Controle de elegibilidade: falta reposta, com reposição agendada ou não comparecimento perdem elegibilidade

### Módulo Público
- Página pública com avisos e listagem de turmas (sem login)
- Botão "Baixar o app" exibido apenas quando o PWA ainda não está instalado

---

## Funcionalidades Futuras Confirmadas (Fase 1.2)

- Eventos e ingressos
- Financeiro básico
- Dashboard administrativo
- Marcar aula experimental
- Perfil e telas do Líder (faturamento)

---

## Regras de Negócio Críticas (resumo)

Cada regra possui um código único para referência no desenvolvimento:

- **[BOL01–09]** — Regras do sistema de bolsistas (gratuidade, desconto, frequência, dias obrigatórios)
- **[AP01–06]** — Regras de aulas particulares (conflito de horário, cancelamento, duplo agendamento)
- **[CHA01–05]** — Regras de chamada (janela de 24h, bloqueio de turma alheia, chamada duplicada)
- **[TUR01–06]** — Regras de turmas (conflito professor/sala, matrícula duplicada, encerramento)
- **[REP01–04]** — Regras de reposição (elegibilidade, janela de tempo, re-agendamento)
- **[ACE01–07]** — Controle de acesso por perfil (isolamento total entre perfis via token JWT)
