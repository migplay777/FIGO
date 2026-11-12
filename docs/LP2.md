# Desenvolvimento do FIGO para Linguagem de Programação 2

Projeto de extensão da Igreja Família da Fé

IFSP Campus Campinas · Análise e Desenvolvimento de Sistemas

Equipe: Miguel Roberto da Silva, Leonardo Aparecido e Henrique Teles.

Data de referência: 12 de novembro de 2026.

Este documento explica como o FIGO utiliza os conceitos de LP2 para registrar e consultar as finanças das viagens missionárias. A descrição acompanha as classes, os formulários, as validações e os serviços implementados no repositório. A Meta 7 é demonstrada pela combinação das regras de negócio em C# com a interface Windows Forms e a persistência no MySQL.

## 1 Conferência dos requisitos mínimos

O PDF da disciplina estabelece cinco requisitos. A versão anterior já possuía as funcionalidades principais e conceitos distribuídos entre modelos, serviços, formulários e records. A atualização torna a orientação a objetos explícita na classe de domínio Viagem, reforça a validação antes da persistência e corrige problemas de edição.

| Requisito do PDF | Evidência na versão atualizada |
| --- | --- |
| Windows Forms com pelo menos duas janelas | FormLogin, FormPrincipal, FormNovaViagem, FormEditarViagem e FormLancamento são janelas distintas. |
| Classe própria com campos e modificadores public e private | Viagem contém campos privados nome e destino, propriedades públicas e a constante pública MaximoParticipantes. |
| Construtor e getters e setters | Viagem possui construtor sem parâmetros, construtor com parâmetros e acessores get e set explícitos em Nome e Destino. |
| Método com passagem e retorno de valores | CalcularDuracaoEmDias recebe um bool, retorna um int e é utilizado pelo DashboardView. |
| Uso de referências | FormEditarViagem recebe a instância selecionada de Viagem e mantém essa referência em original. |
| Exceção gerada por biblioteca do .NET | RelatoriosView trata IOException e UnauthorizedAccessException na gravação do CSV. |
| Exceção disparada pelas regras do programa | Viagem.Validar e ValidacaoFinanceira lançam ArgumentException, tratada pelos formulários. |
| MySQL com inclusão atualização e remoção por ADO.NET | ViagemServices, ReceitaService e DespesaService executam INSERT, UPDATE e DELETE usando MySqlConnection e MySqlCommand. |

Os cinco requisitos possuem exemplos identificáveis no código atualizado. A análise de conformidade não substitui a demonstração no computador de apresentação nem a avaliação do professor. Os testes automatizados são descritos na seção de verificação.

## 2 Problema atendido e organização do código

O FIGO associa receitas e despesas a viagens da Família da Fé. O responsável financeiro seleciona uma viagem, registra os recebimentos e gastos e consulta o saldo. A identificação de pagadores, formas de pagamento e contas de destino ajuda a explicar a origem dos recebimentos.

O saldo exibido corresponde aos lançamentos cadastrados. A aplicação não confirma créditos em extratos bancários nem realiza conciliação automática. Os benefícios esperados são facilitar a conferência das movimentações e a prestação de contas; não foram informadas medições de uso real.

| Camada ou projeto | Responsabilidade e exemplos |
| --- | --- |
| FIGO.Desktop | Janelas, controles, eventos e mensagens. Exemplos: FormLogin, FormPrincipal e FormLancamento. |
| Models no backend | Objetos que representam os dados. Exemplos: Viagem, Receita, Despesa, Pagador e ContaFinanceira. |
| Validation no backend | Regras dos lançamentos, CPF, CNPJ, seleção de cadastros e pesquisa textual. |
| Services no backend | Operações de persistência e autenticação. Exemplos: ViagemServices, ReceitaService e UsuarioService. |
| Data no backend | Conexao centraliza a abertura das conexões usando FIGO_DB_CONNECTION. |
| FIGO.Tests | Validações e integração com o banco isolado figo_test. |
| FIGO.Desktop.Tests | Verificação dos seletores e da abertura do editor no Windows. |

A interface envia dados aos modelos e serviços. O backend valida as informações antes da gravação. As consultas retornam objetos que os controles exibem. Dessa forma, a regra não depende apenas de uma mensagem ou de um limite visual de um campo.

## 3 Classe própria campos propriedades e construtores

Viagem é uma classe de domínio, independente de Form e UserControl. Ela representa os dados necessários para cadastrar uma viagem e validar seu período. Os campos privados ficam acessíveis por propriedades públicas, que retiram espaços no início e no fim dos textos.

Trecho de FIGO.backend/Models/Viagem.cs:

```csharp
public const int MaximoParticipantes = 10_000;
private string nome = string.Empty;
private string destino = string.Empty;

public string Nome
{
    get => nome;
    set => nome = (value ?? string.Empty).Trim();
}

public string Destino
{
    get => destino;
    set => destino = (value ?? string.Empty).Trim();
}

public Viagem() { }

public Viagem(string nome, string destino, DateTime dataInicio,
    DateTime dataFim, int quantidadeParticipantes)
{
    Nome = nome;
    Destino = destino;
    DataInicio = dataInicio.Date;
    DataFim = dataFim.Date;
    QuantidadeParticipantes = quantidadeParticipantes;
    Validar();
}
```

O construtor com parâmetros é utilizado pelos formulários de criação e edição. O construtor sem parâmetros mantém a leitura por ADO.NET e os inicializadores já usados pelo projeto. A validação do objeto completo também é chamada nos serviços antes de inserir ou atualizar.

As propriedades get e set são os acessores de leitura e escrita do C#. Os campos privados evitam o acesso direto ao armazenamento interno da classe. Não é necessário criar métodos duplicados como GetNome e SetNome para demonstrar esse recurso.

## 4 Métodos e regras da viagem

CalcularDuracaoEmDias recebe incluirUltimoDia e devolve a duração como número inteiro. Com true, uma viagem que começa e termina no mesmo dia tem duração de um dia. Com false, o retorno representa o intervalo entre as datas, sem acrescentar o dia final.

Trecho do método utilizado no painel:

```csharp
public int CalcularDuracaoEmDias(bool incluirUltimoDia = true)
{
    Validar();
    return (DataFim.Date - DataInicio.Date).Days
        + (incluirUltimoDia ? 1 : 0);
}
```

Chamada efetiva em DashboardView:

```csharp
int duracao = viagem.CalcularDuracaoEmDias(incluirUltimoDia: true);
```

O painel apresenta a quantidade de participantes e a duração da viagem. O método integra uma funcionalidade real da interface, além de demonstrar passagem e retorno de valores.

| Regra da viagem | Motivo |
| --- | --- |
| Nome e destino entre 1 e 255 caracteres | Evita textos vazios e respeita os campos do banco. |
| Datas preenchidas e compatíveis com o domínio DATE do MySQL | Impede que objetos sem datas válidas cheguem à gravação. |
| Data final igual ou posterior à inicial | Mantém o período coerente. |
| Participantes entre 0 e 10000 | Usa o mesmo limite na criação e na edição; zero permanece permitido no planejamento. |
| Id positivo para editar | Exige a seleção de uma viagem cadastrada. |

Como o objeto pode ser alterado após sua criação, ViagemServices.Adicionar e Editar repetem a validação imediatamente antes de abrir a conexão. Assim, as regras continuam aplicadas quando o serviço é chamado fora do formulário.

## 5 Referências compartilhadas entre as janelas

Na lista de viagens, o registro selecionado é um objeto Viagem. A ação Editar envia esse objeto ao construtor de FormEditarViagem. O formulário guarda a mesma referência no campo original.

```csharp
private readonly Viagem original;

public FormEditarViagem(Viagem v)
{
    original = v;
    // Os controles são preenchidos a partir de v.
}
```

Viagem é um tipo de referência. A chamada copia a referência para o parâmetro v, que continua apontando para a mesma instância. Esse compartilhamento não exige a palavra-chave ref. readonly impede trocar a referência armazenada no campo, mas não torna o objeto imutável.

Ao salvar, o editor cria uma candidata com os novos valores. Primeiro o serviço valida e grava essa candidata no MySQL. Depois o formulário atualiza os dados de original e fecha com DialogResult.OK. Cancelar ou falhar na gravação não deve antecipar a alteração do objeto usado pela lista.

```csharp
var alterada = new Viagem(nome.Text, destino.Text,
    inicio.Value, fim.Value, (int)participantes.Value)
    { Id = original.Id };

service.Editar(alterada);
original.Nome = alterada.Nome;
original.Destino = alterada.Destino;
original.DataInicio = alterada.DataInicio;
original.DataFim = alterada.DataFim;
original.QuantidadeParticipantes = alterada.QuantidadeParticipantes;
```

Outro uso de referências aparece no login. FormLogin devolve um UsuarioAutenticado e Program o entrega ao construtor de FormPrincipal. A janela principal é aberta somente depois do retorno de autenticação bem-sucedido.

## 6 Interface gráfica e eventos

O projeto FIGO.Desktop declara UseWindowsForms e tem como alvo net10.0-windows. A inicialização chama ApplicationConfiguration.Initialize e executa o formulário principal após o login. As telas usam eventos de botões para validar, consultar e gravar.

| Janela ou controle | Participação no fluxo |
| --- | --- |
| FormLogin | Alterna entre entrar e criar conta; recebe a confirmação de senha e trata erros. |
| FormPrincipal | Organiza a navegação depois da autenticação. |
| FormNovaViagem | Recebe os dados, usa o construtor de Viagem e solicita a inclusão. |
| FormEditarViagem | Recebe a referência da viagem e solicita a atualização. |
| FormLancamento | Permite incluir ou editar receita e despesa da viagem selecionada. |
| FormCadastroFinanceiro e suas especializações | Cadastram pagador, conta, origem, forma de pagamento ou categoria. |
| DashboardView e RelatoriosView | Exibem totais, duração, gráficos e exportação CSV. São UserControls hospedados nas janelas. |

Form e UserControl têm funções diferentes. O requisito de duas janelas é atendido pelos formulários, sem depender de contar as abas ou os controles como janelas adicionais.

O editor agora define Maximum do NumericUpDown antes de receber a quantidade de participantes. A versão anterior atribuía o valor antes de alterar o limite padrão de 100, o que podia impedir a abertura de uma viagem com 150 participantes.

Os seletores financeiros carregam opções com ID. A busca por trecho ocorre no C#, ignorando acentos e diferenças entre maiúsculas e minúsculas. Texto digitado sem uma opção confirmada não produz um ID de cadastro válido.

## 7 Exceções geradas pelo programa e por bibliotecas

As regras de negócio usam throw para rejeitar dados inconsistentes. O formulário captura a exceção e informa o problema, mantendo a janela aberta para correção.

Exemplo de regra do programa:

```csharp
if (DataFim.Date < DataInicio.Date)
    throw new ArgumentException(
        "A data fim não pode ser antes da data início.");
```

Exemplo de tratamento no formulário:

```csharp
catch (ArgumentException ex)
{
    lblErro.Text = ex.Message;
    return;
}
```

Um exemplo de exceção gerada pela biblioteca do .NET ocorre na gravação do arquivo CSV com File.WriteAllLines. O tratamento distingue falta de permissão e falha de entrada e saída.

```csharp
try
{
    File.WriteAllLines(dialogo.FileName, linhas, new UTF8Encoding(true));
    MessageBox.Show("Relatório exportado com sucesso.");
}
catch (UnauthorizedAccessException)
{
    // Informa a falta de permissão e orienta escolher outro local.
}
catch (IOException)
{
    // Orienta verificar o arquivo aberto ou o local de gravação.
}
```

O exemplo resume as mensagens; as cláusulas completas estão em RelatoriosView.cs. As falhas do provedor MySQL são representadas por MySqlException. Na exclusão de viagem, o código 1451 é tratado como vínculo existente com receitas ou despesas, sem prometer uma exclusão em cascata.

## 8 Persistência MySQL com ADO NET

MySqlConnector é o provedor ADO.NET usado pelo projeto. Suas classes MySqlConnection, MySqlCommand e MySqlDataReader executam conexão, comandos e leitura. O provedor está referenciado no backend na versão 2.3.7.

Conexao lê a variável de ambiente FIGO_DB_CONNECTION. Os serviços obtêm a conexão aberta e usam using para liberar os objetos ao sair do escopo, inclusive em caso de exceção. As credenciais não precisam ser gravadas no código-fonte.

Trecho de ViagemServices.Adicionar:

```csharp
ArgumentNullException.ThrowIfNull(viagem);
viagem.Validar();
using var conexao = Conexao.Abrir();

const string sql = @"
    INSERT INTO Viagem
        (Nome, Destino, DataInicio, DataFim, QuantidadeParticipantes)
    VALUES (@Nome, @Destino, @DataInicio, @DataFim,
        @QuantidadeParticipantes);
    SELECT LAST_INSERT_ID();";
using var comando = new MySqlCommand(sql, conexao);
comando.Parameters.AddWithValue("@Nome", viagem.Nome);
comando.Parameters.AddWithValue("@Destino", viagem.Destino);
comando.Parameters.AddWithValue("@DataInicio", viagem.DataInicio);
comando.Parameters.AddWithValue("@DataFim", viagem.DataFim);
comando.Parameters.AddWithValue("@QuantidadeParticipantes",
    viagem.QuantidadeParticipantes);
viagem.Id = Convert.ToInt32(comando.ExecuteScalar());
```

A consulta recupera o identificador gerado na mesma conexão. Os valores são vinculados a parâmetros; não são concatenados ao comando SQL como texto digitado pelo usuário. O trecho foi reformatado para leitura, mantendo as operações do serviço.

O PDF exige os recursos de ADO.NET apresentados na disciplina, mas não identifica um nome de pacote específico. A implementação utiliza o padrão de conexão, comando, leitor e parâmetros de ADO.NET, conforme a documentação do provedor.

## 9 Inclusão consulta atualização e remoção

O requisito mínimo pede uma tabela e três operações de alteração. O FIGO possui essas operações para Viagem, Receita e Despesa. As consultas retornam dados para listas, gráficos e relatórios.

| Operação | Método de ViagemServices | Resultado |
| --- | --- | --- |
| Inserir | Adicionar | Executa INSERT e atribui o Id gerado ao objeto. |
| Consultar | Listar e BuscarPorId | Usa ExecuteReader para preencher objetos Viagem. |
| Atualizar | Editar | Executa UPDATE com os dados e o Id da viagem. |
| Remover | Excluir | Executa DELETE pelo Id quando as FKs permitem. |

Exemplos das instruções utilizadas:

```sql
UPDATE Viagem
SET Nome = @Nome,
    Destino = @Destino,
    DataInicio = @DataInicio,
    DataFim = @DataFim,
    QuantidadeParticipantes = @QuantidadeParticipantes
WHERE Id = @Id;

DELETE FROM Viagem WHERE Id = @Id;
```

As duas operações usam MySqlCommand, AddWithValue e ExecuteNonQuery. O serviço valida a viagem antes do UPDATE. As receitas e despesas possuem serviços próprios com inclusão, edição e exclusão por parâmetros.

O esquema com chaves estrangeiras impede excluir uma viagem enquanto houver lançamentos vinculados. O texto de confirmação e o tratamento de erro foram alinhados a essa regra. A atualização de LP2 não altera o esquema nem remove movimentações em cascata.

Os totais financeiros são consultados em vw_ResumoViagem. O cálculo agrega receitas e despesas separadamente antes de associar os valores à viagem. O CSV usa os resultados apresentados no relatório, sem armazenar um segundo saldo editável.

## 10 Regras financeiras e autenticação

As regras financeiras permanecem concentradas em ValidacaoFinanceira e nos serviços. Essa organização permite reaproveitá-las nas telas e nos testes.

| Informação | Regra implementada |
| --- | --- |
| Receita nova | Exige viagem, descrição, valor, data, origem, pagador, forma e conta. |
| Despesa nova | Exige viagem, descrição, valor, data e categoria cadastrada. |
| Valor | Positivo, com até duas casas decimais e limite da aplicação de R$ 999.999.999,99. |
| Pessoa jurídica | CNPJ obrigatório e validado pelo programa. |
| Pessoa física | CPF opcional e validado quando informado. |
| Conta bancária | Exige banco, agência, número e titular; Caixa não exige dados bancários. |
| Usuário | Nome entre 3 e 50 caracteres. |
| Senha | Entre 8 e 128 caracteres, com letra, número e símbolo, além de confirmação na interface. |

UsuarioService gera salt individual e hash com PBKDF2 SHA256. A autenticação lê os dados de verificação no MySQL e compara os hashes. A senha em texto puro não é armazenada na tabela Usuarios.

O login identifica uma conta cadastrada. A aprovação administrativa de novos usuários e a autoria individual dos lançamentos ainda não estão implementadas. Esses pontos devem ser decididos com Samuel para a implantação na igreja.

As regras de 3FN e os materiais específicos de Banco de Dados são tratados na documentação do banco. O atendimento dos requisitos de LP2, isoladamente, não representa a conclusão de todas as entregas das disciplinas integradas.

## 11 Testes automatizados e limites da verificação

A automação em .github/workflows/build.yml executa duas frentes. No Windows, compila o projeto Desktop e roda os testes de backend e controles. No Linux, cria um MySQL isolado com a base figo_test, prepara as tabelas e executa os cenários de integração.

| Verificação | O que demonstra |
| --- | --- |
| Compilação do Desktop no Windows | Compatibilidade das alterações com o projeto WinForms e o backend. |
| ViagemTests.Validacoes | Datas coerentes, duração inclusiva, ano bissexto, textos e limites de participantes. |
| Rejeição nos serviços | Objeto inválido não chega à abertura de conexão para INSERT ou UPDATE. |
| Teste do editor WinForms | Abre uma viagem com 150 participantes e não altera o objeto ao cancelar. |
| ViagemTests.Integracao | Insere, lê, atualiza e exclui uma viagem no MySQL isolado. |
| Testes financeiros existentes | Cadastros, seletores, duplicatas, preservação do legado e totais financeiros. |
| Testes de autenticação | Cadastro, login válido, senha inválida, usuário ausente e duplicidade de nome. |
| Migração repetida | O script de cadastros financeiros é executado duas vezes na base de teste. |

Os testes de autenticação foram habilitados também na etapa MySQL da automação, que agora prepara database/001_usuarios.sql. Os resultados desta atualização ficam disponíveis na aba Checks do pull request e em Actions no GitHub.

O ambiente local desta elaboração não executa WinForms ou .NET. A execução automatizada no Windows é distinta de uma validação completa, com uso da interface pela equipe ou pela igreja. A gravação do CSV em um arquivo bloqueado deve ser conferida manualmente no computador de apresentação.

## 12 Roteiro de execução e demonstração

1. Incorpore o pull request da atualização de LP2 e faça Git Pull no Visual Studio.
2. Use um ambiente Windows com o SDK .NET 10 e os componentes de desenvolvimento Desktop necessários ao projeto.
3. Confirme o banco do FIGO e a variável FIGO_DB_CONNECTION. As migrações 001 e 002 precisam estar aplicadas; esta atualização de código não exige uma migração adicional.
4. Abra FIGO.Desktop.csproj e execute a aplicação. Cadastre uma conta ou entre com uma existente.
5. Cadastre uma viagem com nome, destino, datas e 150 participantes. Abra a edição e confirme os dados.
6. Altere a viagem e salve. Abra o painel e confira a duração calculada. Depois teste cancelar uma edição sem salvar.
7. Inclua uma receita e uma despesa. Edite um lançamento e remova um registro fictício para demonstrar o CRUD.
8. Exporte o relatório CSV e compare o saldo. Em ambiente de teste, tente um local sem permissão ou um arquivo bloqueado para demonstrar o tratamento de falhas de gravação.
9. Mostre Viagem.cs, FormEditarViagem, RelatoriosView e ViagemServices para relacionar o comportamento aos conceitos exigidos.

Os exemplos de cadastro e remoção devem usar dados de demonstração. O novo método de duração não altera os valores de receitas ou despesas. As regras e o código preservam os nomes das tabelas utilizados na versão principal.

Para repetir os testes de regras, execute:

```text
dotnet run --project FIGO.Tests/FIGO.Tests.csproj --configuration Release
```

Os testes de MySQL dependem das variáveis e da base figo_test preparadas pela automação. Não habilite as variáveis de integração apontando para o banco de uso real.

## 13 Organização e entregas parciais

A Meta 7 do curso está relacionada ao desenvolvimento das regras e da interface utilizando os conhecimentos de LP2. A evolução deve manter os formulários, as regras de negócio e as operações do banco coerentes entre si.

| Integrante | Responsabilidade informada pela equipe |
| --- | --- |
| Miguel Roberto da Silva | Levantamento de requisitos, entrevista com o stakeholder, atribuição de tarefas e desenvolvimento do sistema. |
| Leonardo Aparecido | Documentação do sistema, atributos e funcionalidades. |
| Henrique Teles | Apresentação e marketing do projeto. |

| Entrega parcial sugerida | Material ou evidência |
| --- | --- |
| Regras e modelos | Classes próprias, validações e exemplos dos conceitos de LP2. |
| Interface integrada | Login, viagem, lançamentos e referências entre objetos e janelas. |
| Persistência e regressões | CRUD com MySQL e resultados de testes em Windows e banco isolado. |
| Apresentação e validação | Demonstração do fluxo completo, explicação do código e retorno do stakeholder. |

Essa sequência não substitui o cronograma de cada disciplina. A previsão informada para o projeto é o fim de novembro de 2026, mas a data acadêmica exata deve ser conferida no Moodle. O desenvolvimento contínuo deve registrar alterações no GitHub e atualizar a documentação quando o comportamento mudar.

O sistema pretende atender uma necessidade real da igreja. A robustez é demonstrada por validação antes da gravação, parâmetros SQL, restrições do banco, preservação da referência após falha ou cancelamento e testes das operações. Indicadores de impacto social e de uso dependem da validação com a Família da Fé.

## 14 Fontes e localização dos exemplos

| Conceito | Arquivo principal |
| --- | --- |
| Campos construtores propriedades e método | FIGO.backend/Models/Viagem.cs |
| Validação e CRUD de viagens | FIGO.backend/Services/ViagemServices.cs |
| Referência compartilhada e edição | FIGO.Desktop/Views/ViagensView.cs |
| Criação por construtor | FIGO.Desktop/Views/FormNovaViagem.cs |
| Chamada do método de duração | FIGO.Desktop/Views/DashboardView.cs |
| Exceções de gravação de arquivo | FIGO.Desktop/Views/RelatoriosView.cs |
| Autenticação e hash | FIGO.backend/Services/UsuarioService.cs |
| Regras financeiras | FIGO.backend/Validation/ValidacaoFinanceira.cs |
| Regressões de viagens | FIGO.Tests/ViagemTests.cs |
| Regressão da interface | FIGO.Desktop.Tests/Program.cs |
| Automação de compilação e integração | .github/workflows/build.yml |

Referência acadêmica: Projeto de Extensão Requisitos Mínimos, PDF fornecido pela equipe. O documento exige Windows Forms, classe própria, referências, exceções e operações MySQL por ADO.NET.

Código do projeto: https://github.com/migplay777/FIGO

Documentação técnica de apoio:

- Microsoft Learn, constructors: https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/constructors
- Microsoft Learn, properties: https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/properties
- MySqlConnector, API de ADO.NET: https://mysqlconnector.net/tutorials/basic-api/

Os exemplos de código foram abreviados ou reformatados quando indicado. Os arquivos do pull request permanecem a referência executável para conferir instruções completas, mensagens e resultados de testes.
