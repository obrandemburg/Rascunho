# **1\. Planejamento do MVP**

## **1.1 MVP**

### **Perfis do sistema**

* Aluno — dança de salão gratuita não se aplica; paga mensalidade por turma.

* Bolsista — dança de salão gratuita (sem mensalidade); 50% de desconto em turmas de dança solo e em aulas particulares (salão ou solo).

* Professor — gerencia turmas, chamada e aulas particulares.

* Recepção — administra turmas, usuários, avisos e bolsistas.

* Gerente — todas as funções da recepção \+ quadro de desempenho de bolsistas.

* Líder — visualização de faturamento (detalhamento na fase 1.2).

### **Funcionalidades transversais**

* Sistema de bolsistas: dias obrigatórios, frequência, sugestão automática e desempenho.

* Notificações push: aula chegando, solicitação aceita/recusada, vaga liberada, novo aviso, chamada registrada.

* Segurança, autenticação e controle de acesso por perfil.

* Reagendamento de aulas (reposição de faltas elegíveis).

### **Interações MVP**

**Usuário anônimo entra no site:**

O site é exibido com:

* Avisos / quadro de eventos públicos.

* Opção para realizar login (usuário e senha).

* Botão "Baixar o app": exibido somente quando o sistema detecta que o PWA ainda não está instalado.

**👤  Aluno**   *—   paga mensalidade por turma de dança de salão*

**Dashboard principal após login. Acesso rápido:**

* Minhas aulas  •  Aulas particulares  •  Quadro de avisos

**Barra lateral:**

* Início  •  Quadro de turmas  •  Meu perfil  •  Minhas aulas  •  Aulas particulares  •  Quadro de avisos  •  Reagendar aula

**Funcionalidade de cada tela:**

1. **Início**

Dashboard exibido logo após o login. Cartões de acesso rápido para Minhas aulas, Aulas particulares e Quadro de avisos.

2. **Quadro de turmas**

Lista todas as turmas ativas (com vagas ou sem vagas) em cards contendo: nome, professor, data de início, dia e horário, sala, nível técnico, ritmo e número de alunos.

Filtros: ritmo, professor, dia da semana, horário, nível e data de início. Cada filtro exibe as opções existentes no sistema como lista selecionável.

Ações nos cards:

* Matricular na turma: o sistema explica o funcionamento, solicita confirmação e ajusta a mensalidade do aluno.

* Marcar interesse (turma sem vagas): o aluno entra na fila de espera e recebe notificação push quando uma vaga for liberada.

3. **Meu perfil**

Exibe: foto, nome, biografia e tipo de plano. Permite editar foto e biografia.

4. **Minhas aulas**

Cards de cada turma em que o aluno está matriculado. Cada card contém:

* Acessar grupo da turma: link para o WhatsApp da turma.

* Ver avisos da turma: abre os avisos específicos daquela turma.

5. **Aulas particulares**

Botão "Agendar aula particular":

* Filtros por professor, ritmo, dia e horário. Exibe cards com horários disponíveis e botão "Agendar aula".

* Ao confirmar: professor recebe notificação push com a solicitação.

* Ao aceitar: aluno recebe notificação push e a aula aparece em Aulas particulares e Minhas aulas.

⚠  *RN-AP01: o sistema só exibe horários que não conflitem com turmas do professor.*

Cards de aulas aceitas:

* Botão "WhatsApp do professor": abre conversa no WhatsApp.

* Botão "Reagendar aula": exibe próximos horários disponíveis na agenda do professor, sem conflitos com turmas dele.

* Botão "Cancelar aula": bloqueado se faltarem menos de 12 horas. Exibe: "Não é possível cancelar com menos de 12 horas de antecedência. Converse diretamente com o professor."

6. **Quadro de avisos**

Lista os avisos de público geral dentro do intervalo de exibição vigente. Avisos expirados não aparecem.

7. **Reagendar aula**

O sistema exibe apenas faltas elegíveis para reposição (ver RN-REP). O aluno seleciona a falta e escolhe uma nova data disponível.

**🎓  Bolsista**   *—   salão gratuito · 50% em solo e particulares*

**Dashboard principal após login. Acesso rápido:**

* Turmas — dias obrigatórios  •  Minhas aulas  •  Quadro de avisos

**Barra lateral:**

* Início  •  Turmas — dias obrigatórios  •  Minhas aulas  •  Desempenho  •  Aulas particulares  •  Quadro de avisos

**Funcionalidade de cada tela:**

8. **Início**

Dashboard exibido após o login. Cartões de acesso rápido: Turmas — dias obrigatórios, Minhas aulas e Quadro de avisos.

9. **Turmas — dias obrigatórios**

Exibe as turmas de dança de salão ativas nos dois dias obrigatórios semanais do bolsista. Os cards são ordenados por necessidade da turma:

* Turmas que precisam do papel do bolsista (condutor ou conduzido) aparecem no topo, com destaque visual — cor diferente e etiqueta "Precisando de condutores" ou "Precisando de conduzidos".

* Turmas sem necessidade urgente aparecem abaixo, sem destaque.

O bolsista não faz matrícula formal em turmas de salão. Sua presença é registrada pelo professor na chamada. Esta tela é um guia para ele saber onde sua presença agrega mais valor no dia.

⚠  *RN-BOL04: o bolsista não pode se matricular em turmas de dança solo nem agendar aulas particulares de solo nos seus dias obrigatórios.*

10. **Minhas aulas**

Lista as aulas em que o bolsista está formalmente inscrito: turmas de dança solo (com 50% de desconto) e aulas particulares agendadas.

11. **Desempenho**

Exibe o histórico de presenças separado por dias obrigatórios e dias extras, com o indicador de situação baseado na frequência nos dias obrigatórios:

* 85%+ → "Excelente\! Continue assim."

* 75% a 84% → "Vamos melhorar\!"

* 60% a 74% → "Está tudo bem? Converse com o líder sobre sua frequência."

* Abaixo de 60% → "Converse imediatamente com o líder sobre sua frequência."

Presenças em dias extras aparecem separadas e não impactam o indicador principal.

**Confirmação de presenças — segurança do bolsista:**

Cada vez que o professor finaliza uma chamada e o bolsista foi marcado como presente, o bolsista recebe uma notificação push: "Sua presença na turma \[Nome da Turma\] do dia \[Data\] foi registrada."

Além da notificação, a tela Desempenho exibe a lista completa de presenças registradas, aula a aula, com data, turma e professor responsável pelo registro, permitindo ao bolsista auditar seu próprio histórico.

12. **Aulas particulares**

Funcionamento idêntico ao do Aluno, com as seguintes diferenças:

* O valor exibido no card de agendamento já aparece com 50% de desconto (salão ou solo).

* O sistema bloqueia o agendamento de aulas particulares de dança solo nos dias obrigatórios do bolsista (RN-BOL04).

⚠  *RN-AP01 também se aplica: horários com conflito de turma do professor não são exibidos.*

13. **Quadro de avisos**

Lista avisos gerais e avisos direcionados à equipe dentro do período vigente.

**🏫  Professor**   *—   turmas · chamada · aulas particulares*

**Dashboard principal após login. Acesso rápido:**

* Minhas turmas  •  Fazer chamada  •  Aulas particulares

**Barra lateral:**

* Início  •  Minhas turmas  •  Fazer chamada  •  Aulas particulares  •  Meu perfil  •  Quadro de avisos

**Funcionalidade de cada tela:**

14. **Início**

Dashboard após login. Cards com a agenda do dia: turmas que ocorrem hoje e aulas particulares agendadas.

15. **Minhas turmas**

Lista as turmas vinculadas ao professor em cards: nome, ritmo, sala, dia e horário, nível e número de alunos matriculados.

Cada card contém:

* Ver lista de alunos: nome e foto dos matriculados.

* Fazer chamada: atalho direto para a tela de chamada da turma.

* Ver / publicar avisos da turma: professor e recepção podem publicar avisos de turma.

16. **Fazer chamada**

**Fluxo da tela de chamada:**

1\. O professor seleciona a turma (card da turma ou busca) e confirma a data.

2\. A tela exibe duas seções visuais distintas:

**Seção A — Alunos matriculados:**

Lista automática de todos os alunos da turma com foto e nome. Para cada aluno, dois botões grandes e bem espaçados:

* ✅  Presente  (verde)

* ❌  Falta       (vermelho)

Campo de observação opcional abaixo de cada aluno, colapsado por padrão — o professor toca para expandir e digitar (ex.: "chegou atrasado", "dificuldade no passo X", "evolução notável").

**Seção B — Adicionar participante extra:**

Barra de pesquisa com placeholder "Buscar bolsista, aluno em reposição ou aula experimental...". O professor digita o nome e o sistema retorna resultados mostrando, para cada resultado:

* Nome e foto do participante.

* Etiqueta automática identificando o tipo: 🎓 Bolsista · 🔄 Reposição · 🆕 Experimental.

Ao selecionar, o participante é adicionado à chamada com os mesmos botões Presente / Falta e campo de observação opcional.

⚠  *A etiqueta de tipo é gerada automaticamente pelo sistema com base no perfil do usuário e no histórico de faltas elegíveis para reposição.*

**Finalizar chamada:**

Botão "Finalizar chamada" fixo na parte inferior da tela. Ao tocar:

* O sistema exibe um resumo: X presentes, Y faltas, Z participantes extras.

* O professor confirma. O sistema salva, atualiza históricos e envia notificação push de confirmação a cada bolsista presente.

Regras da chamada:

* Só pode ser registrada no dia da aula ou com até 24 horas de atraso (RN-CHA01).

* Ao finalizar, o sistema recalcula a elegibilidade de reposição dos alunos faltantes.

17. **Aulas particulares**

Três abas:

* Solicitações pendentes: cards com nome, ritmo, dia e horário solicitado. Ações: Aceitar (notifica aluno via push e entra na agenda de ambos) ou Recusar (notifica aluno via push).

* Aulas agendadas: lista confirmadas com data, horário, aluno e ritmo. Cada card: botão WhatsApp do aluno e botão Cancelar aula (notifica aluno via push; regra de 12 horas se aplica ao professor também).

* Minha disponibilidade: grade de dias e horários em que o professor aceita particulares. Base usada pelo sistema para exibir opções ao agendar.

⚠  *RN-AP01: o sistema nunca exibe como disponíveis horários em que o professor já tem turma em andamento.*

18. **Meu perfil**

Exibe foto, nome, biografia e ritmos que leciona. Permite editar foto e biografia.

19. **Quadro de avisos**

Lista avisos gerais e avisos de equipe dentro do período vigente.

**🗂️  Recepção**   *—   turmas · usuários · avisos · bolsistas*

**Dashboard principal após login. Acesso rápido:**

* Gerenciar turmas  •  Gerenciar usuários  •  Publicar aviso

**Barra lateral:**

* Início  •  Gerenciar turmas  •  Gerenciar usuários  •  Quadro de avisos  •  Sistema de bolsistas

**Funcionalidade de cada tela:**

20. **Início**

Painel com: total de alunos ativos, total de bolsistas ativos, turmas em andamento no dia e avisos vigentes.

21. **Gerenciar turmas**

Exibe todas as turmas em cards/lista: nome, professor, ritmo, sala, dia e horário, nível, alunos e vagas restantes.

Ações:

* Criar turma: formulário com nome, professor, ritmo, sala, dia/horário, nível e vagas máximas. Campos com placeholders (lista de professores, salas). Ao clicar em "Criar", o sistema valida conflitos (RN-TUR01 a RN-TUR03) e lista os impedimentos caso existam.

* Editar turma: alterar sala, vagas, professor, dia/horário ou nível.

* Encerrar turma: desativa a turma e notifica alunos matriculados.

* Ver lista de alunos: nome e foto dos matriculados.

* Adicionar aluno manualmente.

* Remover aluno da turma.

22. **Gerenciar usuários**

Lista pesquisável de alunos, bolsistas e professores.

Ações:

* Cadastrar aluno: nome, e-mail, telefone, tipo de plano e foto.

* Cadastrar bolsista: nome, e-mail, telefone, papel (condutor / conduzido), dois dias obrigatórios semanais (6 horas / 6 aulas por semana) e foto.

* Cadastrar professor: nome, e-mail, telefone, ritmos que leciona e foto.

* Editar dados de qualquer usuário.

* Desativar usuário: bloqueia acesso sem excluir histórico.

* Ver perfil completo: dados pessoais, turmas, histórico de presença e aulas particulares.

23. **Quadro de avisos**

Ações:

* Publicar aviso: título, conteúdo, período de exibição e público-alvo (geral, equipe ou turma específica).

* Editar aviso: conteúdo ou período.

* Remover aviso antes do término.

* Listar avisos ativos e expirados.

24. **Sistema de bolsistas**

Ações:

* Visualizar sugestões automáticas de bolsistas (critérios: frequência, tempo de casa, número de turmas).

* Confirmar ou rejeitar sugestão.

* Visualizar lista de bolsistas ativos com tipo de desconto concedido.

* Acompanhar frequência geral nos dias obrigatórios (leitura apenas — painel detalhado é exclusivo do Gerente).

**📊  Gerente**   *—   tudo da recepção \+ quadro de desempenho de bolsistas*

O Gerente possui acesso a todas as telas da Recepção. A única diferença na navegação é um item adicional na barra lateral:

* Quadro de desempenho — bolsistas

**Funcionalidade exclusiva do Gerente:**

25. **Quadro de desempenho — bolsistas**

Visão gerencial contínua sobre o desempenho de todos os bolsistas ativos.

**Painel resumido:**

* Total de bolsistas ativos.

* Bolsistas em situação crítica (\< 60%).

* Bolsistas em alerta (60%–74%).

* Bolsistas em situação saudável (75%+).

**Lista de bolsistas:**

* Card por bolsista: foto, nome, papel (condutor/conduzido), dias obrigatórios, frequência percentual e indicador de situação.

* Ordenados por prioridade: crítico no topo.

* Clique no card: perfil completo com histórico aula a aula.

**Ações por bolsista:**

* Editar dias obrigatórios: alterar os dois dias semanais e carga mínima.

* Registrar conversa: campo de texto para registrar observações ou conversas realizadas (ex.: "15/03 — bolsista relatou problema pessoal, retorno esperado 01/04").

* Desativar bolsa: converte o bolsista em aluno comum com notificação push.

* Renovar bolsa: confirma continuidade para o próximo período.

**Filtros:**

* Por situação (Excelente / Vamos melhorar / Atenção / Crítico).

* Por papel (condutor / conduzido).

* Por dia obrigatório (ex.: todos que têm segunda como dia obrigatório).

## **Regras de negócio e verificações do sistema**

Cada regra possui um código único usado como referência no documento e no desenvolvimento.

### **Bolsistas**

1. **\[BOL01\]**  O bolsista não paga mensalidade em turmas de dança de salão. Seu cadastro de perfil define o tipo como "bolsista" e o sistema nunca gera cobrança para turmas de salão nesse perfil.

2. **\[BOL02\]**  O bolsista paga 50% do valor de mensalidade ao se matricular em turmas de dança solo. O desconto é aplicado automaticamente no momento da matrícula com base no perfil.

3. **\[BOL03\]**  O bolsista paga 50% do valor de aulas particulares (salão ou solo). O desconto é exibido no card de agendamento antes da confirmação.

4. **\[BOL04\]**  O bolsista não pode se matricular em turmas regulares de dança solo nos seus dias obrigatórios. O sistema bloqueia a matrícula e exibe: "Você já possui um dia obrigatório nesse dia. Não é possível se matricular em dança solo no mesmo horário."

5. **\[BOL05\]**  O bolsista não pode agendar aulas particulares de dança solo (nem de salão) nos seus dias obrigatórios. O sistema filtra e não exibe horários que coincidam com os dias obrigatórios ao buscar vagas para particular.

6. **\[BOL06\]**  Cada bolsista possui exatamente dois dias obrigatórios semanais, totalizando carga mínima de 6 horas (6 aulas) por semana. Esse valor é configurado pela recepção no cadastro e pode ser editado pelo gerente.

7. **\[BOL07\]**  A frequência nos dias obrigatórios é calculada exclusivamente com base nas presenças registradas pelo professor na chamada. Presença em dias extras não conta para o indicador de frequência obrigatória, mas é registrada e exibida separadamente.

8. **\[BOL08\]**  A cada chamada finalizada com o bolsista presente, o sistema envia notificação push ao bolsista: "Sua presença na turma \[Nome\] do dia \[Data\] foi registrada pelo professor \[Nome do professor\]." Isso garante ao bolsista segurança sobre o registro correto de sua frequência.

9. **\[BOL09\]**  O bolsista não faz matrícula formal em turmas de dança de salão. Sua presença é registrada pelo professor na chamada por meio da barra de pesquisa de participantes extras.

### **Aulas particulares**

10. **\[AP01\]**  O sistema nunca exibe, como horários disponíveis para aula particular, horários em que o professor já possui uma turma em andamento. A grade de disponibilidade do professor (configurada por ele) é filtrada automaticamente contra o calendário de turmas antes de ser exibida ao aluno.

11. **\[AP02\]**  Ao aceitar uma solicitação de aula particular, o sistema verifica novamente se não houve criação de nova turma ou outro agendamento conflitante desde que a solicitação foi enviada. Em caso de conflito identificado pós-aceitação, o sistema alerta o professor antes de confirmar.

12. **\[AP03\]**  Não é possível cancelar uma aula particular com menos de 12 horas de antecedência pelo sistema — para aluno ou bolsista. O sistema exibe: "Não é possível cancelar com menos de 12 horas de antecedência. Converse diretamente com o professor."

13. **\[AP04\]**  O professor pode cancelar a qualquer momento pelo sistema. Ao cancelar, o aluno recebe notificação push imediata.

14. **\[AP05\]**  Um professor não pode ter duas aulas particulares no mesmo horário. O sistema valida no momento da aceitação da solicitação.

15. **\[AP06\]**  Um aluno ou bolsista não pode agendar duas aulas particulares no mesmo horário. O sistema valida no momento do agendamento.

### **Chamada**

16. **\[CHA01\]**  A chamada só pode ser registrada no dia da aula ou com até 24 horas de atraso. Chamadas fora dessa janela estão bloqueadas no sistema.

17. **\[CHA02\]**  Ao finalizar a chamada, o sistema atualiza automaticamente o histórico de presença de cada participante e recalcula a elegibilidade para reposição dos alunos que faltaram.

18. **\[CHA03\]**  O sistema identifica automaticamente o tipo do participante adicionado via pesquisa na chamada: 🎓 Bolsista (perfil bolsista), 🔄 Reposição (aluno com falta elegível para aquela turma) ou 🆕 Experimental (perfil experimental ou indicação manual).

19. **\[CHA04\]**  O professor não pode registrar chamada de uma turma que não lhe pertence.

20. **\[CHA05\]**  Não é possível lançar duas chamadas para a mesma turma na mesma data. O sistema bloqueia e exibe a chamada já registrada para edição, caso ainda esteja dentro da janela de 24 horas.

### **Turmas**

21. **\[TUR01\]**  Não é possível criar uma turma se o professor selecionado já possui outra turma no mesmo dia e horário. O sistema exibe: "\[Nome do professor\] já possui a turma \[Nome da turma\] nesse dia e horário."

22. **\[TUR02\]**  Não é possível criar uma turma se a sala selecionada já está ocupada naquele dia e horário por outra turma. O sistema exibe: "A sala \[Nome da sala\] já está ocupada por \[Nome da turma\] nesse horário."

23. **\[TUR03\]**  Ao editar uma turma (professor, sala, dia ou horário), as mesmas validações de RN-TUR01 e RN-TUR02 são executadas. Conflitos bloqueiam a edição e são listados na tela.

24. **\[TUR04\]**  Ao encerrar uma turma, todos os alunos matriculados recebem notificação push: "A turma \[Nome da turma\] foi encerrada."

25. **\[TUR05\]**  Um aluno não pode se matricular na mesma turma duas vezes. O sistema valida no momento da matrícula (solicitada pelo aluno ou inserida manualmente pela recepção).

26. **\[TUR06\]**  Um aluno não pode se matricular em duas turmas com o mesmo dia e horário. O sistema valida e exibe os conflitos antes de confirmar a matrícula.

### **Reposição de aulas**

27. **\[REP01\]**  Apenas faltas registradas em chamada dentro de um período máximo (a definir) são elegíveis para reposição.

28. **\[REP02\]**  Uma falta perde elegibilidade se: (a) for mais antiga que o período máximo; (b) já foi reposta; (c) já possui reposição agendada; (d) a reposição foi marcada mas o aluno não compareceu.

29. **\[REP03\]**  O sistema bloqueia o agendamento de reposição para faltas não elegíveis e exibe o motivo específico de inelegibilidade.

30. **\[REP04\]**  O aluno não pode reagendar uma reposição já agendada sem antes cancelar a anterior (cancelamento de reposição não afeta a elegibilidade da falta original, que retorna à fila).

### **Controle de acesso por perfil**

31. **\[ACE01\]**  Aluno: acesso exclusivo às telas de aluno.

32. **\[ACE02\]**  Bolsista: acesso exclusivo às telas de bolsista.

33. **\[ACE03\]**  Professor: acesso exclusivo às telas de professor. Pode publicar avisos apenas nas turmas às quais está vinculado.

34. **\[ACE04\]**  Recepção: acesso exclusivo às telas de recepção.

35. **\[ACE05\]**  Gerente: acesso a todas as telas de recepção \+ quadro de desempenho de bolsistas.

36. **\[ACE06\]**  Líder: perfil a detalhar na fase 1.2.

37. **\[ACE07\]**  Nenhum perfil pode acessar telas de outro perfil via manipulação de URL ou chamada direta de API. Toda rota deve ser validada no backend com base no token de sessão.

## **1.2 Futuro (Confirmado)**

* Eventos e ingressos

* Financeiro básico

* Dashboard administrativo

* Marcar aula experimental

* Perfil e telas do Líder (visualização de faturamento)

## **1.3 Funcionalidades Opcionais**

* Gamificação

* Programa de fidelidade

* Indicação de amigos

* Relatórios avançados

## **1.4 Ideias Interessantes**

* IA para atendimento

* Integração WhatsApp

* Sugestão de turmas automática

* Ranking de alunos

