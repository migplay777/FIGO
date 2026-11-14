# FIGO 1.3 — Anexos, orçamento, metas e contas a pagar

Entrega de 14/11/2026. Implementa as opções **1, 2, 3 e 7** escolhidas pelo usuário, sobre a versão com participantes e logs. As seis tabelas novas levam o banco de **15 a 21 tabelas**; a opção 7 exige duas tabelas, por isso esta seleção ultrapassa a meta mínima de 20.

| Opção | Função entregue | Novas tabelas |
|---|---|---|
| 1 | Comprovantes de receitas/despesas | AnexoLancamento |
| 2 | Orçamento por categoria e comparação com realizado | OrcamentoViagem, ItemOrcamento |
| 3 | Meta por viagem, prazo opcional e progresso | MetaArrecadacao |
| 7 | Compromissos, pagamentos parciais e estorno | ContaPagar, PagamentoConta |

As outras 15 tabelas permanecem: Viagem, Receita, Despesa, Usuarios, Pagador, OrigemReceita, FormaPagamento, ContaFinanceira, CategoriaDespesa, Participante, FuncaoParticipante, ViagemParticipante, TipoEventoAuditoria, LogAuditoria e DetalheLogAuditoria. Views, rotinas e triggers não entram na contagem.

## Atualizar ou instalar

1. Faça backup do MySQL e feche o aplicativo.
2. Banco existente: selecione o banco correto e execute as migrações **001, 002, 003 e 004**, nessa ordem, quando ainda não aplicadas. A nova migração é `database/004_planejamento_anexos_contas.sql`; execute o arquivo completo, incluindo `DELIMITER`.
3. Banco vazio: `database/000_instalacao_completa.sql` cria/seleciona `figo` com as 21 tabelas e todas as migrações. Não cria lançamentos financeiros, contas a pagar ou anexos fictícios.
4. Use o código desta versão e configure `FIGO_DB_CONNECTION`. O desktop requer Windows e .NET 10; o banco requer MySQL 8.0.16+ (CI: 8.4) e tabelas InnoDB.
5. Confirme `SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE';`: o projeto completo tem 21 tabelas.

A migração é reexecutável e preserva dados e histórico. DDL faz commit implícito: use janela de manutenção e backup; não conte com rollback único de toda a instalação. A conta de instalação precisa criar tabelas, índices, FKs, rotinas e triggers. A conta do aplicativo precisa dos privilégios operacionais, sem edição direta do histórico. Para arquivos de 5 MB, o limite `max_allowed_packet` do servidor deve comportar o arquivo e os metadados; 8 MB ou mais atende esse tamanho. Os arquivos fazem parte do backup do banco. Esta entrega no repositório não executa migrações no computador do usuário.

Para reproduzir os SQL distribuídos: `python3 database/tools/gerar_planejamento.py`. O gerador recompõe a migração 004 e o instalador 000, reutilizando o gerador da versão anterior.

## Comprovantes e anexos

Em **Receitas** ou **Despesas**, selecione um lançamento salvo e clique em **Comprovantes**. É possível adicionar, listar, salvar uma cópia e remover anexos. Pagamentos de contas a pagar também têm esse botão no seu histórico.

- PDF, PNG ou JPEG, com até **5 MiB (5.242.880 bytes)** por arquivo. A extensão deve coincidir com a assinatura inicial dos bytes; isso não equivale a análise antivírus ou validação completa do documento.
- O conteúdo fica em MEDIUMBLOB no MySQL, junto de nome, tipo, tamanho e SHA-256. Isso evita depender de uma pasta específica em cada computador.
- Cada anexo tem uma FK para receita **ou** despesa, nunca as duas. CHECK assegura destino único e tamanho; triggers verificam o hash.
- A mesma sequência de bytes não pode ser anexada duas vezes ao mesmo lançamento; pode ser usada em lançamentos diferentes quando necessário.
- O nome original é reduzido ao nome do arquivo e saneado. Não se persiste caminho local. A leitura é limitada para evitar carregar um arquivo maior que o permitido.
- Salvar cópia verifica integridade e solicita o destino. O aplicativo não executa ou abre automaticamente arquivos anexados.
- Excluir lançamento com anexo é bloqueado. Remova conscientemente os anexos antes; a exclusão fica registrada no histórico.
- Logs contêm metadados e hash, sem copiar o documento binário. Os documentos podem conter dados pessoais e permanecem acessíveis aos usuários autenticados, como os demais dados financeiros desta versão.

## Orçamento previsto versus realizado

No menu **Planejamento**, escolha a viagem e abra **Orçamento por categoria**. **Definir previsto** cadastra ou substitui o valor da categoria. **Excluir previsto** remove somente a previsão. Os gastos permanecem.

Existe um cabeçalho de orçamento por viagem e um item por categoria. O previsto é positivo, com duas casas decimais. O realizado é calculado a partir de Despesa, incluindo despesas geradas por pagamentos. Categorias que receberam gastos sem previsão aparecem com previsto zero; gastos legados sem CategoriaId aparecem em uma linha própria. Assim, os totais não ocultam despesas fora do plano. Diferença negativa indica excesso de gasto.

O orçamento não cria despesas e não altera saldo. O cabeçalho é criado na primeira previsão e pode permanecer vazio depois de excluir seus itens, preservando a identidade do orçamento.

## Meta de arrecadação

Em **Planejamento → Meta de arrecadação**, use **Definir meta**. Há uma meta por viagem, com descrição, valor positivo e prazo opcional. Desmarque o seletor de data para não informar prazo. É possível atualizar ou remover a meta.

Arrecadado é a soma atual de todas as receitas da viagem, inclusive as anteriores à criação da meta. Alterar ou excluir uma receita atualiza o progresso na próxima consulta. O percentual pode ultrapassar 100%; a barra limita apenas sua representação visual a 100. Falta nunca fica negativa. Situações: concluída, prazo encerrado ou em andamento. Meta não cria receita e não representa saldo líquido (despesas não são subtraídas da arrecadação).

## Contas a pagar e pagamentos parciais

No menu **Contas a pagar**, selecione uma viagem e registre descrição, credor, categoria, total e vencimento. A conta é um compromisso previsto e não gera saída de caixa por si só. Consulte todas, em aberto, vencidas ou pagas.

**Pagar** aceita parte do saldo ou o total restante, com data e referência opcional. Uma transação:

1. Bloqueia a conta e verifica novamente seu saldo atual.
2. Confere a chave única da operação para tratar repetição da mesma tentativa sem duplicar o pagamento.
3. Cria uma única Despesa, na viagem e categoria da conta.
4. Vincula a despesa em PagamentoConta e confirma os dados junto com os logs.

Valor e data ficam na Despesa; PagamentoConta guarda o vínculo, a referência e a chave da operação. Não há outra cópia do valor a somar nos relatórios. **Não cadastre manualmente uma segunda despesa para o mesmo pagamento.**

Saldo da conta = valor contratado menos despesas vinculadas pelos pagamentos. Situações são calculadas: paga quando quitada; vencida quando ainda há saldo depois do vencimento; parcial quando há pagamento sem vencimento; aberta nas demais situações. Contas vencidas e parcialmente pagas continuam identificadas como vencidas, com as colunas pago/saldo mostrando a parcialidade.

O código usa transação READ COMMITTED e bloqueio da conta; os triggers também conferem viagem/categoria e limite do pagamento com leitura atual bloqueada. A chave evita repetir a mesma tentativa, mas dois pagamentos diferentes, com chaves diferentes e saldo suficiente, são operações distintas. Um pagamento em data futura segue a política já existente dos lançamentos do FIGO: a data é informada pelo usuário e o relatório atual inclui os registros da viagem.

**Pagamentos** abre o histórico da conta. **Estornar** remove o vínculo e a despesa gerada na mesma operação. Se a despesa tiver anexos, a FK impede o estorno e toda a operação é desfeita; remova os anexos antes. O histórico de auditoria é mantido. Para corrigir um pagamento, estorne e registre novamente. A despesa vinculada é protegida contra edição/exclusão pela tela geral e por SQL comum.

Com pagamentos, não é permitido mudar a categoria nem reduzir o total abaixo do pago. A viagem da conta é imutável. Descrição, credor, vencimento e um total compatível podem ser corrigidos. Para excluir a conta, estorne todos os seus pagamentos antes. Os campos de entrada são parametrizados e as validações também são aplicadas nos serviços, independentemente dos limites da tela.

## Auditoria e arquitetura

As seis novas tabelas recebem 18 triggers de auditoria. O total passa a **54 triggers de auditoria em 18 tabelas operacionais**, além de nove triggers de integridade para pagamento, estorno, edição de conta/despesa e hash dos anexos. Os eventos continuam disponíveis em **Histórico**, inclusive com filtros para as novas entidades.

As três tabelas do histórico não auditam a si próprias. A identificação do autor continua vindo do contexto autenticado, inicializado em cada conexão. SQL externo sem contexto permanece com autor desconhecido. Arquivos completos não são serializados no log; alterações em seu conteúdo precisam de hash compatível e a mudança de hash entra na auditoria. Permanecem os limites da versão anterior: sem reconstrução de alterações anteriores à instalação, sem logs de SELECT/DDL e sem garantia de inviolabilidade contra administradores do MySQL.

| Camada | Implementação |
|---|---|
| Modelos e regras | `FIGO.backend/Models/PlanejamentoFinanceiro.cs` |
| Planejamento | `FIGO.backend/Services/PlanejamentoService.cs` |
| Pagamentos e transações | `FIGO.backend/Services/ContasPagarService.cs` |
| Arquivos e integridade | `FIGO.backend/Services/AnexosService.cs` |
| Interface | `PlanejamentoView`, `ContasPagarView`, `FormAnexos` e formulários específicos |
| Migração | `database/004_planejamento_anexos_contas.sql` |
| Testes | `FIGO.Tests/PlanejamentoTests.cs`, `FIGO.Desktop.Tests/Program.cs` |

Isso amplia a camada de dados da Meta 5 e as regras/interface da Meta 7 de LP2. As entregas parciais são esquema/migração, serviços/regras, interface, integração e documentação. Datas acadêmicas continuam dependendo do cronograma de cada disciplina; conformidade integral depende da rubrica oficial.

## Verificação

O workflow compila o desktop no Windows e executa testes de controles. No MySQL 8.4 isolado, aplica migrações repetidas, preserva o cenário legado e verifica as 21 tabelas e 54 triggers de auditoria. Testes cobrem previsto/realizado, gasto sem previsão, progresso da meta, comprovantes e duplicatas, bloqueio por anexos, pagamento parcial, repetição da mesma operação, limite de saldo, solicitações concorrentes, proteção da despesa e estorno atômico. A migração é reaplicada depois de criar dados/logs e o instalador vazio é executado duas vezes em outro banco.

Comandos: `dotnet build FIGO.Desktop -c Release`; `dotnet run --project FIGO.Tests -c Release`; `dotnet run --project FIGO.Desktop.Tests -c Release` (Windows). Integração exige banco isolado `figo_test` e as variáveis do workflow; não execute testes destrutivos de integração no banco em uso. Testes de controles não substituem revisão visual completa de todas as resoluções e escalas.

Referências: [leituras com bloqueio](https://dev.mysql.com/doc/refman/8.4/en/innodb-locking-reads.html) e [isolamento de transações](https://dev.mysql.com/doc/refman/8.4/en/innodb-transaction-isolation-levels.html), documentação oficial MySQL.
