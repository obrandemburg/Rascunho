**Ponto da Dança**

Documentação Completa do Sistema

MVP Versão 1.1  ·  18/05/2026

https://pontodadanca.trindaflow.com.br

# **Sumário**

# **Visão Geral do Sistema**

O Ponto da Dança é uma plataforma de gestão para escola de dança com 6 perfis de usuário: Aluno, Bolsista, Professor, Líder, Recepção e Gerente. O sistema gerencia turmas, presenças, aulas particulares, reposições, bolsistas e comunicados.

**Stack Tecnológica:**

* Backend: ASP.NET Core 10 Minimal API (sem controllers) — pasta Rascunho/

* Frontend: Blazor WebAssembly 10 \+ MudBlazor 9.1.0 (PWA) — pasta Rascunho.Client/

* Banco de dados: PostgreSQL \+ Entity Framework Core 10 (Code-First, Migrations)

* Autenticação: JWT Bearer \+ BCrypt \+ Hashids (IDs ofuscados)

* Deploy: Docker \+ Traefik v2 \+ GitHub Actions → VPS Hetzner

**Padrões Arquiteturais (regras imutáveis):**

* Minimal API com extension methods por domínio (SEM controllers)

* SEM AutoMapper — mapeamentos manuais em Rascunho/Mappers/

* SEM Repository Pattern — EF Core acessado diretamente nos Services

* TPH (Table-Per-Hierarchy) para usuários — todos os perfis em uma tabela com discriminador "Tipo"

* IDs públicos ofuscados via Hashids em todos os endpoints (nunca expõe ID interno do banco)

* Código 100% em português: classes, métodos, variáveis, comentários

* Tratamento centralizado: RegraNegocioException → 422, ArgumentException → 400, demais → 500

* FluentValidation para validação de DTOs de entrada

| Perfil | Role JWT | Descrição |
| :---- | :---- | :---- |
| Aluno | Aluno | Aluno regular matriculado em turmas |
| Bolsista | Bolsista | Aluno com benefício que apoia as aulas |
| Professor | Professor | Leciona turmas e ministra aulas particulares |
| Líder | Líder | Papel de liderança (faturamento — Fase 1.2) |
| Recepção | Recepção | Gerencia matrículas, usuários e turmas |
| Gerente | Gerente | Acesso total: configurações e desempenho estratégico |

# **1\. Autenticação e Segurança**

**✅ Funcionando**

## **1.1 Login e Geração de Token JWT**

O sistema usa JWT Bearer para autenticação. O fluxo completo:

1. Frontend envia POST para /api/auth/login com email e senha

2. Backend valida credenciais via BCrypt (TokenService.VerificarSenha)

3. Verifica se o usuário está ativo (usuario.Ativo \== true)

4. Gera JWT com claims: NameIdentifier (ID), Email, Name, Role (Tipo)

5. Retorna token \+ nome \+ tipo \+ fotoUrl para o frontend

6. Frontend armazena no LocalStorage: authToken, userName, userType, userFotoUrl

7. CustomAuthStateProvider.NotifyUserAuthentication() atualiza o estado reativo da UI

Expiração: 8 horas. ClockSkew de 5 minutos configurado.

**Arquivos responsáveis:**

* Backend: Rascunho/Endpoints/AuthEndpoints.cs — POST /api/auth/login

* Backend: Rascunho/Services/TokenService.cs — GerarToken(), hash e claims

* Frontend: Rascunho.Client/Services/AuthService.cs — LoginAsync(), LogoutAsync()

* Frontend: Rascunho.Client/Security/CustomAuthStateProvider.cs — ParseClaimsFromJwt(), TokenEstaExpirado()

* Frontend: Rascunho.Client/Pages/Auth/Login.razor — formulário de login

## **1.2 Logout com Revogação Server-Side**

O logout invalida tokens no servidor via campo UltimoLogoutEmUtc na entidade Usuario. Todo token emitido antes desse timestamp é considerado inválido, mesmo que o JWT ainda não tenha expirado — protege contra tokens roubados após logout.

**Fluxo de logout:**

8. Frontend chama AuthService.LogoutAsync()

9. POST fire-and-forget para /api/auth/logout (atualiza UltimoLogoutEmUtc no banco)

10. LocalStorage limpo (authToken, userName, userType, userFotoUrl)

11. Header Authorization do HttpClient removido

12. CustomAuthStateProvider.NotifyUserLogout() atualiza a UI

**Arquivos responsáveis:**

* Backend: Rascunho/Endpoints/AuthEndpoints.cs — POST /api/auth/logout

* Backend: Rascunho/Entities/Usuario.cs — RegistrarLogout(), UltimoLogoutEmUtc

* Frontend: Rascunho.Client/Services/AuthService.cs — LogoutAsync()

## **1.3 Roles e Autorização**

Seis roles implementados: Aluno, Bolsista, Professor, Líder, Gerente, Recepção.

* Backend: políticas de autorização via .RequireRole() nos endpoints

* Frontend: \<AuthorizeView Roles="..."\> para mostrar/ocultar UI por perfil

* NavMenu.razor filtra a navegação por role automaticamente

**Arquivos:**

* Rascunho/Program.cs — configuração JWT e políticas

* Rascunho.Client/Layouts/NavMenu.razor — navegação por role

* Rascunho.Client/App.razor — AuthorizeRouteView com redirecionamento para /login

## **1.4 Parse do JWT no Frontend**

CustomAuthStateProvider decodifica o JWT sem biblioteca externa: divide o token em 3 partes, Base64Url decode do payload, deserializa o JSON de claims, mapeia claims JWT para ClaimTypes .NET, e verifica o claim "exp" (UNIX timestamp) para detectar expiração antes de qualquer chamada à API.

**Métodos principais:**

* GetAuthenticationStateAsync() — retorna ClaimsPrincipal ou anônimo

* TokenEstaExpirado() — decodifica payload, verifica campo "exp"

* ParseClaimsFromJwt() — mapeia claims JWT → .NET ClaimTypes

# **2\. Gestão de Usuários**

**✅ Funcionando**

## **2.1 Cadastro Individual**

Endpoint: POST /api/usuarios/cadastrar — restrito a Recepção e Gerente.

**Campos obrigatórios:**

* Nome (3–100 caracteres)

* Email (único no sistema)

* Senha (mínimo 8 caracteres)

* DataNascimento (passada, usuário com 5+ anos)

* Tipo (Aluno / Professor / Bolsista / etc.)

* Gênero

* CPF (validação matemática — algoritmo Receita Federal)

**Campos específicos por tipo:**

* Professor: lista de ritmos que leciona (mínimo 1\)

* Bolsista: PapelDominante (Condutor/Conduzido/Ambos) \+ 2 dias obrigatórios distintos

**Arquivos:**

* Rascunho/Endpoints/UsuarioEndpoints.cs — POST /api/usuarios/cadastrar

* Rascunho/Services/UsuarioService.cs — CriarUsuarioAsync()

* Rascunho/Validations/CriarUsuarioRequestValidator.cs — FluentValidation

* Rascunho.Client/Pages/Admin/CriarUsuario.razor — formulário \+ upload de foto

## **2.2 Cadastro em Massa**

Endpoint: POST /api/usuarios/cadastrar/lista — envia lista de usuários em um único request. Cada usuário é validado individualmente. Um erro em um usuário não interrompe os demais (retorna lista de erros por email).

**Arquivos:**

* Rascunho/Services/UsuarioService.cs — CadastrarEmMassaAsync()

## **2.3 Edição de Perfil e Alteração de Senha**

* PUT /api/usuarios/meu-perfil/atualizar — NomeSocial, Biografia (próprio usuário)

* PUT /api/usuarios/meu-perfil/alterar-senha — requer senha atual \+ nova senha (8+ chars) \+ confirmação

**Arquivos:**

* Rascunho/Entities/Usuario.cs — EditarPerfil(), AlterarSenha()

* Rascunho.Client/Pages/Perfil.razor

## **2.4 Listagem, Busca e Ativação/Desativação**

* GET /api/usuarios/listar — todos (paginação pendente — PERF-03)

* GET /api/usuarios/listar-paginado — com paginação

* GET /api/usuarios/tipo/{tipo}/ativos — por tipo e status

* GET /api/usuarios/buscar?q=... — busca por nome, CPF ou email (mínimo 2 chars)

* PUT /api/usuarios/status/{idHash} — ativar/desativar — Gerente

* DELETE /api/usuarios/deletar/{idHash} — exclusão permanente — Gerente

Todos os IDs retornados são Hashids (nunca o ID interno do banco).

**Arquivos:**

* Rascunho/Mappers/UsuarioMapper.cs — ToResponse() com Hashids \+ CPF formatado

* Rascunho.Client/Pages/Gerencia/GestaoUsuarios.razor

# **3\. Turmas**

**✅ Funcionando**

## **3.1 Criação de Turma**

Endpoint: POST /api/turmas/criar — restrito a Recepção e Gerente.

Campos: Ritmo, Professor(es), Sala, DiaDaSemana, HorarioInicio, HorarioFim, Nível, LimiteAlunos, DataInicio, LinkWhatsApp.

**Validações implementadas:**

* RN-TUR01: Professor não pode ter duas turmas no mesmo dia e horário (choque de professor)

* RN-TUR02: Sala não pode ser usada em dois horários sobrepostos (choque de sala)

* RN-TUR03: Limite de alunos deve ser positivo

**Arquivos:**

* Rascunho/Services/TurmaService.cs — CriarTurmaAsync()

* Rascunho.Client/Pages/Admin/GerenciarTurmas.razor

## **3.2 Listagem de Turmas**

* GET /api/turmas/listar-ativas — turmas ativas (público, com filtros)

* GET /api/turmas/minhas-turmas — aluno: matriculadas; professor: lecionadas

* GET /api/turmas/ — listagem completa para admin (com filtros)

* GET /api/turmas/{turmaIdHash}/alunos — lista alunos matriculados em uma turma

## **3.3 Encerramento de Turma**

Endpoint: PUT /api/turmas/{turmaIdHash}/encerrar — Recepção/Gerente.

RN-TUR04: Ao encerrar turma, cancela automaticamente aulas experimentais pendentes e reposições agendadas nessa turma como destino.

**Arquivo: Rascunho/Services/TurmaService.cs — EncerrarTurmaAsync()**

# **4\. Mecanismo de Chamada e Presença**

**✅ Funcionando**

Este é o mecanismo central do sistema. Ao final de cada aula, o professor (ou recepção/gerente) abre o app, seleciona a turma e a data, e registra a presença. A chamada é dividida em duas seções:

* Seção A: Alunos regularmente matriculados na turma

* Seção B: Participantes extras — bolsistas de apoio, alunos em reposição, alunos em aula experimental

## **4.1 Carregamento da Lista para Chamada**

Endpoint: GET /api/turmas/{turmaIdHash}/chamada?dataAula=YYYY-MM-DD

O serviço ChamadaService.ObterListaParaChamadaAsync() executa:

13. Busca matrículas ativas com Include(m \=\> m.Aluno) para ter dados do aluno (Seção A)

14. Para cada aluno, verifica se já existe RegistroPresenca para a data — se existir, carrega o estado salvo

15. Retorna o campo Observacao de cada aluno (ex: "chegou atrasado")

16. Extras: busca RegistroPresenca com AlunoId fora das matrículas (chamada já feita antes para essa data)

Resposta: ObterChamadaResponse(TurmaIdHash, DataAula, List\<AlunoChamadaResponse\> Alunos, List\<AlunoChamadaResponse\> Extras)

**Arquivos:**

* Rascunho/Endpoints/ChamadaEndpoints.cs — GET /api/turmas/{id}/chamada

* Rascunho/Services/ChamadaService.cs — ObterListaParaChamadaAsync()

* Rascunho/Entities/RegistroPresenca.cs — TurmaId, AlunoId, DataAula, Presente, Observacao

* Rascunho.Client/Pages/Professor/FazerChamada.razor — CarregarChamada()

## **4.2 Busca de Participantes Extras (Seção B)**

Endpoint: GET /api/turmas/{turmaIdHash}/chamada/buscar-extras?termo=NOME\_OU\_CPF

O serviço ChamadaService.BuscarParticipantesExtrasAsync() busca:

17. Bolsistas ativos cujo nome ou CPF contém o termo (qualquer bolsista pode apoiar qualquer turma)

18. Alunos com AulaExperimental agendada para essa turma (status Pendente ou Confirmada)

19. Alunos com Reposicao agendada nessa turma como destino (status Agendada)

Retorna List\<ParticipanteExtraResponse\> com TipoParticipante \= "Bolsista" | "Experimental" | "Reposicao".

No frontend (FazerChamada.razor): caixa de busca com debounce de 400ms. Resultados aparecem com chips coloridos:

* Bolsista — chip roxo (Color.Secondary)

* Reposicao — chip laranja (Color.Warning)

* Experimental — chip azul (Color.Info)

**Arquivos:**

* Rascunho/Endpoints/ChamadaEndpoints.cs — GET /api/turmas/{id}/chamada/buscar-extras

* Rascunho/Services/ChamadaService.cs — BuscarParticipantesExtrasAsync()

* Rascunho.Client/Pages/Professor/FazerChamada.razor — BuscarExtras(), AdicionarExtra()

## **4.3 Registro da Chamada**

Endpoint: POST /api/turmas/{turmaIdHash}/chamada

Request: RegistrarChamadaRequest(DataAula, List\<AlunoPresencaRequest\> Presencas, List\<AlunoPresencaRequest\> ExtrasPresencas)

O serviço ChamadaService.RegistrarChamadaAsync() executa:

20. Valida que o usuário logado é Professor desta turma (ou tem role Recepção/Gerente)

21. Upsert de RegistroPresenca para cada aluno em Presencas (cria ou atualiza)

22. Upsert de RegistroPresenca para cada aluno em ExtrasPresencas

23. Automação de Reposição: se extra tem Reposicao Agendada nessa turma/data → marca como "Realizada"

Antes de salvar, o professor vê modal de resumo: total de presentes, faltas, extras e observações.

**Arquivos:**

* Rascunho/Services/ChamadaService.cs — RegistrarChamadaAsync()

* Rascunho/Entities/RegistroPresenca.cs — AtualizarPresenca()

* Rascunho.Client/Pages/Professor/FazerChamada.razor — SalvarChamada(), mostrarResumo

## **4.4 Observações por Aluno**

Cada aluno pode ter uma observação textual (máximo 500 caracteres). Fica recolhida por padrão (ícone de lápis). Ao clicar, expande o campo de texto.

Exemplos: "chegou atrasado", "fez grande evolucao hoje", "saiu mais cedo por motivo de saude".

**Arquivo: Rascunho/Entities/RegistroPresenca.cs — campo Observacao (adicionado em Sprint 2\)**

## **4.5 DTOs Compartilhados (Chamada)**

Definidos em Rascunho.Shared/DTOs/ChamadaDTOs.cs:

* AlunoPresencaRequest(AlunoIdHash, Presente, Observacao) — entrada

* RegistrarChamadaRequest(DataAula, Presencas, ExtrasPresencas) — request completo

* AlunoChamadaResponse(AlunoIdHash, Nome, FotoUrl, Papel, Presente, Observacao) — item da lista

* ObterChamadaResponse(TurmaIdHash, DataAula, Alunos, Extras) — resposta do GET

* ParticipanteExtraResponse(UsuarioIdHash, Nome, FotoUrl, TipoParticipante) — resultado da busca

# **5\. Matrícula e Lista de Espera**

**✅ Funcionando**

## **5.1 Matrícula em Turma**

O aluno escolhe seu papel na dança (Condutor, Conduzido, ou Ambos). A matrícula registra ValorMensalidade e OrigemDesconto para rastreamento histórico.

* POST /api/turmas/{turmaIdHash}/matricular — aluno matricula a si mesmo

* POST /api/turmas/{turmaIdHash}/admin/matricular — recepção/gerente matricula qualquer aluno

**Regras:**

* Se turma cheia → entra automaticamente na fila de espera

* RN-BOL04: Bolsista nao pode se matricular em turmas de salao ou solo nos seus dias obrigatorios

* Aluno nao pode se matricular duas vezes na mesma turma

**Fluxo no frontend (MatricularAluno.razor):**

24. Seleciona turma (cards agrupados: Com Vaga / Cheia / Inativa)

25. Busca o aluno pelo nome (mínimo 2 chars, debounced)

26. Seleciona papel (pré-preenchido por gênero: Masculino→Condutor, Feminino→Conduzido)

27. Confirma via dialog

**Arquivos:**

* Rascunho/Services/TurmaService.cs — MatricularAlunoAsync()

* Rascunho/Entities/Matricula.cs — TurmaId, AlunoId, Papel, ValorMensalidade, OrigemDesconto

* Rascunho.Client/Pages/Admin/MatricularAluno.razor

## **5.2 Desmatriculação**

DELETE /api/turmas/{turmaIdHash}/admin/desmatricular/{alunoIdHash} — Recepção/Gerente.

RN-ACE01: Alunos nao podem se desmatricular sozinhos — devem solicitar a recepção.

Se havia aluno na lista de espera, o próximo é notificado (FCM — atualmente stub).

## **5.3 Lista de Espera**

Quando uma turma está lotada, o aluno pode entrar na fila. A fila mantém posição e status:

| Status | Descrição |
| :---- | :---- |
| Aguardando | Na fila, sem vaga disponível no momento |
| Notificado | Vaga liberada — aluno tem 48h para confirmar (configurável) |
| Expirado | Nao confirmou no prazo (lazy evaluation) |
| Convertido | Confirmou a vaga e foi matriculado |

* POST /api/turmas/{id}/lista-espera — entrar na fila

* DELETE /api/turmas/{id}/lista-espera — sair da fila (reordena posicoes — BUG-003 corrigido)

* GET /api/turmas/{id}/lista-espera — ver fila (recepção/gerente)

* GET /api/turmas/minhas-esperas — esperas do aluno logado

**Arquivos:**

* Rascunho/Services/ListaEsperaService.cs — EntrarNaFilaAsync(), SairDaFilaAsync()

* Rascunho/Entities/ListaEspera.cs — Posicao, StatusListaEspera, PrazoConfirmacao

* Rascunho.Client/Pages/Aluno/MinhasEsperas.razor

# **6\. Aulas Particulares**

**✅ Funcionando**

## **6.1 Solicitação pelo Aluno**

O aluno seleciona Professor → Ritmo (carregado dinamicamente via GET /api/professores/{id}/ritmos) → Data e hora → Observação.

**Cálculo do ValorCobrado:**

* Valor padrão: ConfiguracaoService.ObterPrecoAulaParticular() (padrão R$ 80,00)

* RN-BOL02: Bolsista paga 50% (desconto automático)

* O valor é persistido no momento da solicitação para rastreamento histórico

Endpoint: POST /api/aulas-particulares/solicitar → Status inicial: "Pendente"

**Arquivos:**

* Rascunho/Services/AulaParticularService.cs — SolicitarAulaAsync()

* Rascunho/Entities/AulaParticular.cs — ValorCobrado (persistido)

* Rascunho.Client/Pages/Aluno/AulasParticulares.razor — fluxo em etapas

## **6.2 Aceitação e Recusa pelo Professor**

PUT /api/aulas-particulares/{aulaIdHash}/responder com { Aceita: true/false, Justificativa: "..." }

* Aceitar → Status "Aceita"

* Recusar → Status "Recusada" com justificativa

* Notificação push enviada ao aluno (atualmente stub — FCM pendente)

**Arquivo: Rascunho.Client/Pages/Professor/AulasParticulares.razor — Aba Pendentes**

## **6.3 Reagendamento**

PUT /api/aulas-particulares/{aulaIdHash}/reagendar

* RN-AP03: Permitido somente se faltam mais de 12 horas para a aula (status "Aceita")

* RN-AP06: Novo horário nao pode conflitar com outras aulas do professor

**Arquivo: Rascunho/Services/AulaParticularService.cs — ReagendarAulaAsync()**

## **6.4 Cancelamento**

DELETE /api/aulas-particulares/{aulaIdHash}/cancelar — aluno ou professor podem cancelar. Status → "Cancelada".

## **6.5 Disponibilidade do Professor**

O professor define blocos de tempo disponíveis por dia da semana. O sistema filtra conflitos com turmas regulares.

* GET /api/professores/minha-disponibilidade — disponibilidade propria

* PUT /api/professores/minha-disponibilidade — atualizar (replace all)

* GET /api/professores/{professorIdHash}/disponibilidade — disponibilidade de um professor especifico

**Arquivos:**

* Rascunho/Services/ProfessorDisponibilidadeService.cs

* Rascunho.Client/Pages/Professor/AulasParticulares.razor — Aba Minha Disponibilidade (implementada)

## **6.6 Configuracao de Preco (Gerente)**

* GET /api/gerente/configuracoes — preco atual \+ janela reposicao

* PUT /api/gerente/configuracoes/preco-aula-particular — novo preco

* PUT /api/gerente/configuracoes/janela-reposicao — nova janela em dias

**BUG-004 (pendente): ConfiguracaoService mantem configuracoes em memoria. Alteracoes nao persistem apos restart/redeploy — o preco volta para R$ 80,00 a cada deploy.**

**Arquivo: Rascunho/Services/ConfiguracaoService.cs — ObterPrecoAulaParticular(), AtualizarPrecoAulaParticular()**

# **7\. Reposições de Aula**

**✅ Funcionando**

Quando um aluno falta, tem direito a repor a presença em outra turma ativa dentro de uma janela configurável (padrão: 30 dias).

## **7.1 Faltas Elegíveis**

Endpoint: GET /api/reposicoes/elegiveis

ReposicaoService.ObterFaltasElegiveisAsync() retorna faltas que:

* Estao dentro da janela (via ConfiguracaoService.ObterJanelaReposicaoDias())

* Ainda nao foram repostas (sem Reposicao com status Agendada ou Realizada para essa falta)

* Pertencem ao aluno logado

## **7.2 Agendamento**

Endpoint: POST /api/reposicoes/agendar

Request: AgendarReposicaoRequest(TurmaOrigemIdHash, DataFalta, TurmaDestinoIdHash, DataReposicaoAgendada)

**Regras:**

* Turma destino deve estar ativa (qualquer ritmo — sem restricao de ritmo na implementacao atual)

* Aluno nao pode ter outra reposicao agendada para o mesmo horario/turma

* Status inicial: "Agendada"

* Automacao: quando professor marca presenca na Secao B, a reposicao vira "Realizada"

## **7.3 Cancelamento**

DELETE /api/reposicoes/{idHash}/cancelar

RN-REP04: Ao cancelar, a falta volta a ser elegível — o aluno pode agendar em outra turma/data.

**Arquivo: Rascunho/Services/ReposicaoService.cs — CancelarReposicaoAsync()**

## **7.4 Historico**

GET /api/reposicoes/minhas — retorna todas as reposicoes do aluno: agendadas, realizadas e canceladas.

**Frontend: Rascunho.Client/Pages/Aluno/Reagendar.razor**

# **8\. Aulas Experimentais**

**⚠️ Parcial / Em desenvolvimento**

Permitem que um novo aluno experimente uma aula antes de se matricular. Backend implementado, UX em desenvolvimento.

## **8.1 Solicitacao**

POST /api/experimentais/solicitar

* Aluno seleciona turma \+ data (deve ser mesmo DiaDaSemana da turma)

* Turma deve ter vagas

* Status inicial: "Pendente"

**Arquivos: Rascunho/Services/AulaExperimentalService.cs, Rascunho.Client/Pages/Aluno/AulaExperimental.razor**

## **8.2 Gestao de Status (Admin)**

PUT /api/experimentais/admin/{idHash}/status — recepção confirma ou cancela.

Status possíveis: Pendente, Confirmada, Cancelada, Realizada (marcada automaticamente na chamada).

## **8.3 Integracao com a Chamada**

Alunos com AulaExperimental Pendente ou Confirmada aparecem na busca da Secao B (tipo "Experimental"). Ao ser marcado presente, a experimental vira "Realizada".

Nota: link removido do NavMenu (BUG-014 corrigido). A pagina existe com aviso de desenvolvimento.

# **9\. Sistema de Bolsistas**

**✅ Funcionando**

Bolsistas sao alunos com papel especial que apoiam as aulas em troca de beneficios (desconto em mensalidade e aulas particulares).

## **9.1 Turmas Recomendadas (Turmas do Dia)**

GET /api/bolsistas/turmas-recomendadas?diaDaSemana=N

Retorna turmas de danca de salao mais desbalanceadas do dia filtrado (padrao \= dia atual). Turmas de "Danca solo" sao excluidas (BUG-023 corrigido).

Cada item: SugestaoBalanceamentoResponse com TotalCondutores, TotalConduzidos, Status, QuantidadeFaltante e sugestoes de bolsistas.

**Arquivos: Rascunho/Services/BolsistaService.cs — TurmasRecomendadasParaBolsistaAsync(), Rascunho.Client/Pages/Bolsista/TurmasRecomendadas.razor**

## **9.2 Desempenho do Bolsista**

GET /api/bolsistas/meu-desempenho?periodo=30dias|mes|semana|Nmeses|tudo

**O servico calcula:**

* Frequencia nos dias obrigatorios (percentual de presencas)

* Frequencia em dias extras

* Indicador de situacao: Excelente (\>=90%), Bom (\>=70%), Atencao (\>=50%), Critico (\<50%)

* Historico por aula com data, turma, tipo (obrigatorio/extra)

**Arquivos: Rascunho/Services/BolsistaService.cs — MeuDesempenhoAsync(), Rascunho.Client/Pages/Bolsista/Desempenho.razor**

## **9.3 Habilidades por Ritmo**

* GET /api/bolsistas/minhas-habilidades — lista habilidades

* POST /api/bolsistas/minhas-habilidades — adicionar (ritmo \+ papel dominante \+ nivel)

* DELETE /api/bolsistas/minhas-habilidades/{ritmoIdHash} — remover

**Frontend: Rascunho.Client/Pages/Bolsista/MinhasHabilidades.razor — CRUD completo com chips**

## **9.4 Relatorio de Horas Semanais**

GET /api/bolsistas/meu-relatorio-horas — calcula horas cumpridas vs meta semanal baseada nos dias obrigatorios.

## **9.5 Analise de Turma (Gerente)**

GET /api/bolsistas/analisar-turma/{idHash} — avalia saude de uma turma: ratio condutores/conduzidos, recomendacoes.

## **9.6 Desativacao da Bolsa (Gerente)**

PUT /api/gerente/bolsistas/{idHash}/desativar-bolsa

28. Busca matriculas solo com desconto de bolsista

29. Remove o desconto (OrigemDesconto \= null)

30. Converte Bolsista para Aluno via SQL direto (UPDATE Usuarios SET Tipo \= Aluno)

31. No proximo login, o token refletira a nova role

**Arquivo: Rascunho/Endpoints/GerenteEndpoints.cs**

# **10\. Ritmos e Salas**

**✅ Funcionando**

## **10.1 Gestao de Ritmos**

Estilos de danca (Forro, Samba, Salsa, Zouk, etc.) com Modalidade (Danca solo / Danca de salao).

* POST /criar — criar ritmo (Gerente)

* GET /listar — todos os ritmos

* PUT /atualizar/{idHash} — editar (Gerente)

* PUT /{idHash}/status — ativar/desativar com validacao de turmas ativas (Gerente)

* DELETE /excluir/{idHash} — exclusao (Gerente)

**Arquivos: Rascunho/Endpoints/RitmoEndpoints.cs, Rascunho/Services/RitmoService.cs, Rascunho.Client/Pages/Admin/GerenciarRitmos.razor**

## **10.2 Gestao de Salas**

Salas de aula com nome e capacidade.

* POST /criar, GET /listar, GET /ativas, PUT /atualizar/{idHash}, PUT /{idHash}/status

**Arquivos: Rascunho/Endpoints/SalaEndpoints.cs, Rascunho/Services/SalaService.cs, Rascunho.Client/Pages/Admin/GerenciarSalas.razor**

# **11\. Avisos e Comunicados**

**✅ Funcionando**

## **11.1 Tipos de Aviso**

* Geral: visivel para todos (alunos, bolsistas, professores, publico)

* Equipe: visivel apenas para staff (professores, bolsistas, recepcao, gerente)

## **11.2 Endpoints**

* GET /api/avisos/geral — avisos gerais ativos (publico)

* GET /api/avisos/equipe — avisos de equipe (autenticado)

* POST /api/avisos/admin/criar — criar (Recepcao/Gerente)

* PUT /api/avisos/admin/atualizar/aviso/{idHash} — editar (Recepcao/Gerente)

* DELETE /api/avisos/admin/excluir/aviso/{idHash} — excluir (Recepcao/Gerente)

Cada aviso tem DataPublicacao e DataExpiracao — a listagem retorna apenas avisos ainda vigentes.

**Arquivos: Rascunho/Endpoints/AvisoEndpoints.cs, Rascunho/Services/AvisoService.cs, Rascunho.Client/Pages/QuadroAvisos.razor, AvisosEquipe.razor**

# **12\. Gerencia Estrategica**

**⚠️ Parcial / Em desenvolvimento**

## **12.1 Configuracoes do Sistema**

* GET /api/gerente/configuracoes — preco aula particular \+ janela reposicao

* PUT /api/gerente/configuracoes/preco-aula-particular — atualizar preco (R$)

* PUT /api/gerente/configuracoes/janela-reposicao — atualizar janela em dias

**BUG-004 PENDENTE: ConfiguracaoService armazena em memoria. Apos restart/redeploy, valores voltam ao padrao (preco: R$ 80,00, janela: 30 dias).**

**Arquivo: Rascunho/Services/ConfiguracaoService.cs**

## **12.2 Quadro de Desempenho de Bolsistas**

GET /api/gerente/desempenho-bolsistas?periodo=... — desempenho de todos os bolsistas ativos com indicadores visuais. Filtros: periodo, dia obrigatorio, papel dominante.

Acoes disponiveis: Desativar Bolsa (endpoint existe). Demais botoes (editar dias, registrar conversa) precisam ser conectados no frontend.

**Arquivo: Rascunho.Client/Pages/Gerencia/QuadroDesempenho.razor**

## **12.3 Registrar Conversa com Bolsista**

POST /api/gerente/bolsistas/{idHash}/registrar-conversa

**Estado: endpoint existe mas NAO PERSISTE — o registro e perdido. Issue SEC-16. Implementacao completa planejada para Sprint 13\.**

## **12.4 Gestao de Usuarios (Gerente)**

Tela GestaoUsuarios.razor: listar com filtros, ativar/desativar, excluir permanentemente.

# **13\. Upload e Fotos de Perfil**

**✅ Funcionando**

## **13.1 Upload de Foto**

POST /api/upload/foto — multipart/form-data

**Validacoes de seguranca:**

* Tamanho maximo: 5 MB

* Validacao de Content-Type do cliente

* Nome do arquivo gerado com GUID para evitar colisoes e path traversal

* SEC-05 pendente: validacao de magic bytes ainda incompleta

URL publica sempre relativa: /api/fotos/{nomeArquivo} — independente do host.

## **13.2 Servir a Foto**

GET /api/fotos/{nomeArquivo} — sem autenticacao, Content-Type correto por extensao. Servido diretamente pelo backend (Traefik roteia /api/\* para o container backend).

BUG-022 (corrigido): antes as URLs eram absolutas com IP da VPS, causando Mixed Content bloqueado pelo browser em HTTPS.

## **13.3 Normalizacao de URLs (Retrocompatibilidade)**

UserAvatar.razor normaliza qualquer formato para /api/fotos/{nomeArquivo}:

* http://5.161.202.169:8080/uploads/fotos/uuid.jpg  →  /api/fotos/uuid.jpg

* /uploads/fotos/uuid.jpg  →  /api/fotos/uuid.jpg

* /api/fotos/uuid.jpg  →  sem alteracao

Fotos antigas no banco continuam funcionando sem migration de dados.

**Arquivos: Rascunho/Endpoints/UploadEndpoints.cs, Rascunho.Client/Shared/UserAvatar.razor**

# **14\. Eventos e Ingressos**

**⚠️ Parcial / Em desenvolvimento**

Estrutura de dados implementada para eventos (Bailes, Workshops) e ingressos. UX completa na Fase 1.2.

* GET /api/eventos/futuros — eventos com data futura

* GET /api/eventos/historico — eventos passados

* POST /api/eventos/{eventoIdHash}/comprar — comprar ingresso (valida capacidade)

* POST /api/eventos/admin/criar — criar evento (Gerente)

Pagina Ingressos.razor existe mas esta fora do NavMenu. Fase 1.2.

**Arquivos: Rascunho/Endpoints/EventoEndpoints.cs, Rascunho/Services/EventoService.cs, Rascunho/Entities/Evento.cs, Ingresso.cs**

# **15\. Dashboard e Paineis por Perfil**

**⚠️ Parcial / Em desenvolvimento**

## **15.1 Home.razor — Landing e Dashboard Unificado**

A pagina inicial serve como landing publica e como dashboard por role:

| Role | Conteudo exibido |
| :---- | :---- |
| Anonimo | Hero section, cards de funcionalidades, estatísticas, avisos gerais |
| Aluno/Bolsista | Cards: Minhas Aulas, Aulas Particulares |
| Professor | Cards: Minhas Turmas, Fazer Chamada, Aulas Particulares |
| Bolsista (adicional) | Cards: Desempenho, Turmas Recomendadas, Habilidades |
| Recepcao/Gerente | Cards: Criar Usuario, Gerir Turmas, Salas, Ritmos, Matricular, Avisos |
| Gerente (adicional) | Cards: Gestao Usuarios, Desempenho Bolsistas, Dashboard |

**Arquivo: Rascunho.Client/Pages/Home.razor**

## **15.2 Painel do Aluno**

PainelAluno.razor: proxima aula desta semana \+ atalhos. Usa ObterTurmaResponse compartilhado (BUG-008 corrigido).

## **15.3 NavMenu — Navegacao por Role**

| Role | Links disponíveis |
| :---- | :---- |
| Publico (todos) | Home, Ritmos, Turmas |
| Qualquer logado | Meu Perfil |
| Aluno | Minhas Aulas, Aulas Particulares, Quadro Avisos, Reagendar, Minhas Esperas |
| Professor | Minhas Turmas, Fazer Chamada, Aulas Particulares, Quadro Avisos |
| Bolsista | Minhas Aulas, Desempenho, Aulas Particulares, Reagendar, Turmas do Dia, Habilidades, Esperas, Avisos |
| Recepcao/Gerente | Usuarios, Turmas, Salas, Ritmos, Avisos, Matricular |
| Gerente | Gerencia Estrategica (Dashboard, Gestao Usuarios, Desempenho Bolsistas) |

**Arquivo: Rascunho.Client/Layouts/NavMenu.razor**

# **16\. Notificacoes Push**

**❌ Não implementado**

Interface INotificacaoService definida. Implementacao atual: NotificacaoServiceStub que nao envia nada.

| Gatilho | Destinatario |
| :---- | :---- |
| Professor finaliza chamada com bolsista presente (BOL08) | Bolsista |
| Recepcao encerra turma (TUR04) | Todos os matriculados |
| Professor aceita aula particular | Aluno solicitante |
| Professor recusa aula particular | Aluno solicitante |
| Professor cancela aula particular | Aluno |
| Vaga liberada na lista de espera | Proximo da fila |

Pendente: Firebase Cloud Messaging (FCM) — Sprint 15\.

**Arquivos: Rascunho/Services/INotificacaoService.cs, Rascunho/Services/NotificacaoServiceStub.cs**

# **17\. Bugs Pendentes e Divida Tecnica**

## **17.1 Bugs Ativos**

| ID | Descricao | Severidade | Arquivo |
| :---- | :---- | :---- | :---- |
| BUG-004 | ConfiguracaoService perde dados no restart | Alto | Rascunho/Services/ConfiguracaoService.cs |
| BUG-013 | IP da VPS hardcoded (resolvido em prod via Traefik, pendente no codigo) | Alto | Rascunho.Client/Program.cs |

## **17.2 Issues de Seguranca Pendentes**

| ID | Descricao | Severidade |
| :---- | :---- | :---- |
| SEC-04 | AllowedHosts revertido — causa 400 atras do Traefik | Critico (revertido) |
| SEC-05 | Upload valida apenas Content-Type do cliente (falsificavel) | Alto |
| SEC-06 | Desmatriculacao sem restricao de role em alguns fluxos | Alto |
| SEC-08 | Politica de senha inconsistente (6 chars criacao vs 8 chars alteracao) | Alto |
| SEC-09 | int.TryParse sem verificacao em BolsistaEndpoints | Alto |
| SEC-10 | ConfiguracaoService sem persistencia (= BUG-004) | Alto |
| SEC-12 | Headers HTTP de seguranca ausentes (CSP, X-Frame-Options) | Medio |
| SEC-13 | Sem rate limiting no POST /api/auth/login | Medio |
| SEC-16 | registrar-conversa nao persiste dados | Medio |

## **17.3 Funcionalidades Pendentes no MVP**

| Funcionalidade | Status | Onde implementar |
| :---- | :---- | :---- |
| InicioAdmin.razor (dashboard recepcao) | Nao existe | Rascunho.Client/Pages/Admin/InicioAdmin.razor |
| SistemaBolsistas.razor (recepcao) | Nao existe | Rascunho.Client/Pages/Admin/SistemaBolsistas.razor |
| QuadroTurmas.razor (aluno autenticado) | Nao existe | Rascunho.Client/Pages/Aluno/QuadroTurmas.razor |
| Acoes do Gerente no QuadroDesempenho | Parcial | Rascunho.Client/Pages/Gerencia/QuadroDesempenho.razor |
| Editar turma existente (frontend) | Nao existe | GerenciarTurmas.razor \+ PUT /turmas/{id}/editar |
| FCM (Notificacoes Push) | Stub | Sprint 15 |
| Persistencia de ConfiguracaoService | Em memoria | Sprint — tabela Configuracoes |
| Endpoint dashboard-resumo gerencia | Nao existe | GerenteEndpoints.cs |

# **18\. Infraestrutura e Deploy**

**✅ Funcionando**

## **18.1 Stack de Deploy**

* VPS: Hetzner (5.161.202.169)

* Dominio: https://pontodadanca.trindaflow.com.br

* Reverse Proxy: Traefik v2 com SSL Let's Encrypt automatico

* Containers: Docker Compose em modo Swarm

* Imagens: ghcr.io/obrandemburg/rascunho:latest (backend) e ghcr.io/obrandemburg/rascunho-client:latest (frontend)

* CI/CD: GitHub Actions → push em main → build paralelo → SSH na VPS → docker compose pull && up \-d

## **18.2 Configuracao Sensivel**

Gerenciada via .env na VPS (nao versionado): DB\_USER, DB\_PASSWORD, JWT\_KEY, JWT\_ISSUER, JWT\_AUDIENCE, HASHIDS\_SALT, CORS\_ALLOWED\_ORIGINS, ASPNETCORE\_ALLOWEDHOSTS.

## **18.3 Migrations Automaticas**

Program.cs aplica migrations na inicializacao com retry policy: 5 tentativas com 3 segundos de espera. Permite o container backend aguardar o PostgreSQL.

## **18.4 Volumes e Storage**

pd\_uploads — volume Docker persistente para fotos de perfil em producao.

Documentacao da API: Scalar UI em /scalar, OpenAPI JSON em /openapi/v1.json (apenas em desenvolvimento).

# **19\. Componentes Compartilhados do Frontend**

## **UserAvatar.razor**

Avatar com foto de perfil e fallback para iniciais. Normaliza URLs antigas. Trata erro de carregamento (BUG-018 corrigido — nao exibe mais texto "Foto de X"). Usado em toda a aplicacao: AppBar, listas de alunos, chamada, desempenho.

**Arquivo: Rascunho.Client/Shared/UserAvatar.razor**

## **ConfirmDialog.razor**

Dialog generico de confirmacao (sim/nao) com texto e cor configuaveis.

**Arquivo: Rascunho.Client/Shared/ConfirmDialog.razor**

## **PwaBaixarApp.razor**

Banner de instalacao do PWA — aparece apenas em mobile e apenas quando o app nao esta instalado. Suporta Android (prompt nativo) e iOS (instrucoes manuais via Safari).

**Arquivo: Rascunho.Client/Shared/PwaBaixarApp.razor**

## **HttpInterceptorHandler.cs**

Middleware que intercepta todas as respostas HTTP e trata erros globalmente:

* 401: Limpa sessao, exibe "sessao expirou", redireciona para /login

* 400: Exibe primeiro erro de validacao (FluentValidation)

* 422: Exibe mensagem de regra de negocio violada

* 500+: Mensagem generica \+ console.log em dev

**Arquivo: Rascunho.Client/Infraestrutura/HttpInterceptorHandler.cs**

# **20\. Mapa de Entidades do Banco de Dados**

Banco: PostgreSQL. ORM: Entity Framework Core 10\. Padrao: Code-First \+ Migrations.

| Entidade | Tabela | Campos principais | Relacionamentos |
| :---- | :---- | :---- | :---- |
| Usuario (TPH) | Usuarios | Id, Nome, Email, SenhaHash, Tipo, Ativo, DataNascimento, CPF, Genero, FotoUrl, UltimoLogoutEmUtc | Matriculas, RegistroPresenca, AulasParticulares |
| Ritmo | Ritmos | Id, Nome, Descricao, Modalidade, Ativo | Turmas, HabilidadesUsuario |
| Sala | Salas | Id, Nome, CapacidadeMaxima, Ativo | Turmas |
| Turma | Turmas | Id, RitmoId, SalaId, DiaDaSemana, HorarioInicio/Fim, Nivel, LimiteAlunos, DataInicio, LinkWhatsApp | Professores (M:N), Matriculas, RegistroPresenca, ListaEspera |
| Matricula | Matriculas | TurmaId+AlunoId (PK composta), Papel, ValorMensalidade, OrigemDesconto | Turma, Aluno |
| RegistroPresenca | RegistrosPresencas | TurmaId+AlunoId+DataAula (PK composta), Presente, Observacao | Turma, Aluno |
| AulaParticular | AulasParticulares | Id, AlunoId, ProfessorId, RitmoId, DataHora, Status, ValorCobrado, ObservacaoAluno | Aluno, Professor, Ritmo |
| AulaExperimental | AulasExperimentais | Id, AlunoId, TurmaId, DataAula, Status | Aluno, Turma |
| Reposicao | Reposicoes | Id, AlunoId, TurmaOrigemId, DataFalta, TurmaDestinoId, DataReposicaoAgendada, Status | Aluno, TurmaOrigem, TurmaDestino |
| ListaEspera | ListasEspera | Id, TurmaId, AlunoId, Posicao, StatusListaEspera, PrazoConfirmacao | Turma, Aluno |
| Aviso | Avisos | Id, AutorId, Titulo, Mensagem, DataPublicacao, DataExpiracao, TipoVisibilidade | Autor (Usuario) |
| ProfessorDisponibilidade | ProfessorDisponibilidades | Id, ProfessorId, DiaDaSemana, HorarioInicio, HorarioFim, Ativo | Professor |
| HabilidadeUsuario | HabilidadesUsuario | UsuarioId+RitmoId (PK composta), PapelDominante, Nivel | Usuario, Ritmo |
| Evento | Eventos | Id, Nome, Descricao, DataHora, Tipo, Capacidade, Preco | Ingressos |
| Ingresso | Ingressos | Id, EventoId, UsuarioId, DataCompra, ValorPago, Status | Evento, Usuario |

Documentacao gerada automaticamente a partir da analise completa do codigo-fonte.

Data: 18/05/2026  —  Projeto: Ponto da Danca MVP 1.1

Repositorio: github.com/obrandemburg/rascunho