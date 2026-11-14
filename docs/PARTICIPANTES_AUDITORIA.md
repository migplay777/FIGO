# FIGO — Participantes opcionais e histórico de alterações

Versão 1.2.0 · 14/11/2026 · C# / .NET 10 / Windows Forms / MySQL 8.0.16+ (CI: MySQL 8.4).

Conector MySQL atualizado para **MySqlConnector 2.6.1**, versão corrigida para o aviso GHSA-473q-m89c-ghf8 identificado pelo NuGet durante a compilação. Referência do mantenedor: https://github.com/mysql-net/MySqlConnector/security/advisories/GHSA-473q-m89c-ghf8.

## Entrega e contagem do banco

A base anterior tem **9 tabelas**. Esta entrega acrescenta **6 tabelas**, totalizando **15 tabelas físicas**. A view `vw_ResumoViagem`, triggers, índices e procedimentos não contam como tabelas. As dez funções ao final são propostas futuras, ainda não implementadas.

| Nº | Tabela | Responsabilidade |
|---|---|---|
| 1 | Viagem | Viagem, datas, destino e quantidade planejada |
| 2 | Receita | Entradas financeiras da viagem |
| 3 | Despesa | Saídas financeiras da viagem |
| 4 | Usuarios | Contas de acesso e credenciais derivadas |
| 5 | Pagador | Pessoas/empresas que efetuam pagamentos |
| 6 | OrigemReceita | Classificação das entradas |
| 7 | FormaPagamento | Meios de pagamento |
| 8 | ContaFinanceira | Contas e caixas de destino |
| 9 | CategoriaDespesa | Classificação das saídas |
| 10 | Participante | Nome completo e CPF opcional da pessoa |
| 11 | FuncaoParticipante | Catálogo reutilizável de funções |
| 12 | ViagemParticipante | Relação entre pessoa e viagem, idade e função nessa viagem |
| 13 | TipoEventoAuditoria | Criação, alteração e exclusão |
| 14 | LogAuditoria | Data UTC, autor, entidade, ID, ação e origem |
| 15 | DetalheLogAuditoria | Campo alterado, valor anterior e novo |

## Instalação e atualização

1. Faça backup do banco e feche o aplicativo durante a aplicação do SQL. As migrações contêm DDL e não são uma única transação reversível.
2. **Banco existente:** selecione o banco correto no MySQL Workbench. Execute `database/001_usuarios.sql` e `database/002_cadastros_financeiros.sql` se ainda não foram aplicados; execute **`database/003_participantes_auditoria.sql` por inteiro**. Os delimitadores precisam ser interpretados pelo cliente SQL.
3. **Instalação vazia:** execute `database/000_instalacao_completa.sql`. Ele cria/seleciona o banco `figo`, as 15 tabelas, a view e os catálogos iniciais. Não inclui viagens, valores financeiros, participantes ou usuários fictícios.
4. Configure `FIGO_DB_CONNECTION` com a conexão do ambiente. Inicie `FIGO.Desktop`. Cadastre uma conta pela tela de acesso, se necessário.
5. Para conferir: `SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE';` deve resultar em **15**, considerando somente as tabelas do projeto.

Execute com conta que tenha privilégios para criar tabelas, FKs, rotinas e triggers. A conta usada pelo aplicativo precisa executar as operações normais e a rotina de auditoria; os triggers usam o definidor da instalação. Mantenha esse usuário de banco válido. Os scripts suportam reexecução e preservam os dados, mas não corrigem automaticamente tabelas preexistentes com definições incompatíveis. Todas as tabelas de dados e auditoria devem usar InnoDB para a atomicidade.

A alteração no repositório não executa o SQL no computador do usuário. A ativação exige integrar esta versão e aplicar a migração no ambiente em uso.

## Regras de negócio de participantes

- Uma viagem pode existir sem nenhuma pessoa identificada. Os registros anteriores continuam válidos.
- Ao incluir uma pessoa, o nome é obrigatório, com até 150 caracteres. CPF, idade e função são opcionais.
- CPF informado é normalizado para 11 dígitos e validado pelos dígitos verificadores no código. CPF repetido é bloqueado por índice único; diversos cadastros sem CPF são permitidos. O banco verifica formato, não os dígitos verificadores de um SQL externo.
- Idade aceita números inteiros de 0 a 130 ou NULL. Campo vazio significa não informado, diferente de idade zero.
- Função é selecionada no catálogo. Há opções iniciais de participante, coordenador, motorista e apoio, além de cadastro de novas funções.
- A mesma pessoa pode participar de diversas viagens; um índice único impede a duplicação na mesma viagem.
- Nome e CPF pertencem à pessoa e são compartilhados entre viagens. **Idade e função pertencem à participação na viagem**, permitindo preservar informações de viagens diferentes.
- A inclusão conjunta de pessoa/vínculo e a edição de identidade/participação são transacionais. Falha no vínculo ou no log desfaz toda a operação.
- Remover da viagem exclui somente o vínculo. O cadastro pessoal e os outros vínculos permanecem.
- Viagens com pessoas vinculadas têm exclusão bloqueada por FK. Não há exclusões em cascata de pessoas ou histórico.
- A quantidade planejada, já usada nos relatórios financeiros, continua independente da lista nominal opcional.
- Pessoas sem CPF não são deduplicadas pelo nome. Para reutilizar um cadastro, escolha “Usar pessoa já cadastrada”.

## Como usar

Em **Participantes**, selecione a viagem e clique em **Adicionar**. Preencha o nome ou escolha uma pessoa existente. Informe idade, função e CPF somente quando disponíveis. A lista mostra CPF mascarado; o formulário de edição permite revisar o CPF completo. **Editar** informa que nome e CPF afetam as outras viagens da pessoa. **Remover da viagem** exige confirmação e preserva o cadastro pessoal. Também há um atalho **Participantes** na lista de viagens.

Em **Histórico**, filtre por período, entidade, ação e nome do autor. As datas da tela são locais; no banco os instantes são UTC. Navegue em páginas de 50 eventos e selecione uma linha para visualizar os valores anterior/novo de cada campo. A tela é somente de consulta.

## Funcionamento dos logs

São **36 triggers**: criação, alteração e exclusão em 12 tabelas operacionais (as nove anteriores mais as três de participantes). As três tabelas de auditoria não geram novos logs sobre si mesmas. A rotina `Figo_RegistrarAuditoria` compara campos explícitos e registra apenas mudanças efetivas; gravar os mesmos valores não cria uma alteração fictícia.

`ContextoAuditoria` transporta a identidade autenticada. `Conexao` reinicializa o contexto em toda conexão, síncrona ou assíncrona, inclusive quando não há usuário, evitando identidade residual do pool. O banco resolve o nome a partir do ID existente. Uma operação externa sem contexto tem autor não identificado, origem `SQL direto` e identificação da conta de banco. A origem informada pelo cliente não é uma prova de identidade independente.

Cada log contém ID do registro original, entidade, ação, instante UTC, ID do usuário quando disponível, nome histórico, origem e conta de banco. O ID de registro é uma referência histórica polimórfica, sem FK ao objeto operacional, para preservar o histórico após exclusões. A exclusão de um usuário mantém o nome histórico e coloca sua FK como NULL.

CPF de participante e documento de pagador são mascarados **antes de persistir nos detalhes do log**. A comparação usa o valor real antes da máscara, para detectar mudanças mesmo quando o final do CPF coincide. Senha, hash, salt, parâmetros de derivação e strings de conexão não são serializados; alterações de credenciais registram somente um indicador. Nome, dados financeiros e outros campos da lista explícita podem conter dados pessoais e exigem controle de acesso e backup adequado.

O histórico começa após a instalação: não reconstrói autores nem valores antigos. Não registra consultas, tentativas de login, alterações de esquema, ações em tabelas novas sem triggers, nem alterações feitas nas próprias tabelas de auditoria por administradores. Não é um mecanismo de inviolabilidade: a conta administrativa do MySQL pode alterar os registros. A conta operacional deve ter somente os privilégios necessários, sem permissão direta para editar/apagar logs ou mudar triggers. O aplicativo ainda não possui perfis de acesso; os usuários autenticados podem consultar o histórico.

## Organização do código e verificação

| Camada | Arquivos principais | Responsabilidade |
|---|---|---|
| Modelos | `Models/Participantes.cs` | Identidade e participação, campos opcionais, máscara |
| Validação | `Validation/ValidacaoParticipantes.cs` | Nome, CPF, idade e referências |
| Serviços | `Services/ParticipantesService.cs` | CRUD de participação, reutilização, transações e parâmetros SQL |
| Auditoria | `Services/AuditoriaService.cs`, `Data/ContextoAuditoria.cs`, `Data/Conexao.cs` | Contexto de autor e consulta paginada de eventos/detalhes |
| Interface | `Views/ParticipantesView.cs`, `FormParticipante.cs`, `AuditoriaView.cs` | Fluxos de participantes e histórico sem SQL na interface |
| Persistência | `database/003_participantes_auditoria.sql` | Integridade referencial, índices, restrições e auditoria atômica |
| Testes | `FIGO.Tests/ParticipantesAuditoriaTests.cs`, `FIGO.Desktop.Tests/Program.cs` | Validações, integração MySQL e comportamento de controles |

Os caminhos da tabela são relativos aos projetos correspondentes. Os scripts distribuídos são reproduzidos por `python3 database/tools/gerar_participantes_auditoria.py`. O gerador mantém a lista de campos auditados explícita e não inclui credenciais.

O workflow do GitHub compila o desktop no Windows, executa testes de interface e validação, e testa MySQL 8.4 em banco isolado. A integração verifica as 15 tabelas, 36 triggers, dados financeiros legados, opcionais, CPF e duplicidade, reutilização em viagens, idade por viagem, rollback de dados/logs, alteração sem mudanças, autor, proteção de documentos/credenciais, CRUD financeiro auditado, paginação e redefinição de contexto do pool. A migração é reaplicada após a criação de dados/logs; os testes repetem o cenário. O instalador completo também é executado duas vezes em banco separado e verificado sem dados fictícios. Testes não acessam banco de produção.

Comandos: `dotnet build FIGO.Desktop/FIGO.Desktop.csproj -c Release`; `dotnet run --project FIGO.Tests -c Release`; `dotnet run --project FIGO.Desktop.Tests -c Release`. Integração exige as variáveis explicitadas no workflow e banco isolado `figo_test`. A interface gráfica completa exige Windows; a execução dos testes de controles não substitui a revisão visual em todas as resoluções/DPI.

## Relação com as disciplinas

**Banco de Dados — Meta 5:** seis novas entidades com responsabilidades distintas, relacionamento N:N pessoa/viagem, catálogo de funções, integridade referencial, campos opcionais, índices, migração preservando o legado e auditoria transacional. Os textos legados de origem/categoria financeira continuam existindo por compatibilidade com a versão anterior; esta entrega não elimina essa redundância.

**LP2 — Meta 7:** modelos e serviços separados da interface, validação centralizada, tipos anuláveis, reutilização de validação de CPF, tratamento de exceções, consultas parametrizadas, transações, tarefas assíncronas na interface e testes. A conformidade com todos os requisitos da disciplina ainda depende da rubrica oficial do professor, não apresentada nesta solicitação.

**Desenvolvimento contínuo:** entregas técnicas desta alteração: (1) modelo/migração, (2) participantes/regras/tela, (3) logs/consulta, (4) testes/documentação. As datas acadêmicas devem ser preenchidas conforme o cronograma de cada disciplina. A solução resolve identificação opcional de viajantes e rastreabilidade das alterações financeiras, evitando tabelas sem uso apenas para cumprir contagem.

## Dez opções futuras para chegar a 20 tabelas

As estimativas abaixo consideram o desenho proposto e partem das **15 tabelas desta entrega**. Cada função ainda precisa de detalhamento e implementação; não somar todas se o objetivo for exatamente 20.

| Opção | Nova função | Tabelas propostas | Acréscimo |
|---|---|---|---|
| 1 | Comprovantes anexados a receitas/despesas | `AnexoLancamento` | +1 |
| 2 | Orçamento previsto e comparação com realizado | `OrcamentoViagem`, `ItemOrcamento` | +2 |
| 3 | Metas e acompanhamento de arrecadação | `MetaArrecadacao` | +1 |
| 4 | Solicitação e aprovação de despesas | `SolicitacaoAprovacao`, `AprovacaoDespesa` | +2 |
| 5 | Centros de custo para agrupar despesas | `CentroCusto` | +1 |
| 6 | Fornecedores e dados de contato | `Fornecedor` | +1 |
| 7 | Contas a pagar e pagamentos parciais | `ContaPagar`, `PagamentoConta` | +2 |
| 8 | Importação de extratos e conciliação bancária | `ExtratoBancario`, `ItemExtrato`, `Conciliacao` | +3 |
| 9 | Checklist de preparação e prestação de contas | `ChecklistViagem`, `ItemChecklistViagem` | +2 |
| 10 | Perfis e permissões de usuários | `PerfilAcesso`, `PermissaoAcesso`, `UsuarioPerfil`, `PerfilPermissao` | +4 |

**Combinação sugerida para exatamente 20:** anexos (+1), orçamento (+2), metas (+1) e centros de custo (+1): **15 + 1 + 2 + 1 + 1 = 20**. Anexos devem ter FK validada para o lançamento, armazenamento seguro e limite de tamanho. Fornecedores devem evitar duplicar documentos já existentes em cadastros quando representarem a mesma pessoa/empresa. Todas as funções devem estender a auditoria e os testes para suas novas entidades.

## Referências técnicas

- MySQL, triggers e rollback em tabelas transacionais: https://dev.mysql.com/doc/refman/8.4/en/trigger-syntax.html
- MySQL, preservação de LAST_INSERT_ID em triggers: https://dev.mysql.com/doc/refman/8.4/en/stored-routines-last-insert-id.html
- MySqlConnector, variáveis de sessão e opções de conexão: https://mysqlconnector.net/connection-options/
