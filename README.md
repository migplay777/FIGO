# FIGO

Sistema financeiro de viagens missionárias da Igreja Família da Fé, desenvolvido em **C#/.NET**, **Windows Forms** e **MySQL**.

## Estrutura
- `FIGO.backend/` — modelos, serviços e acesso ao MySQL.
- `FIGO.Desktop/` — interface Windows Forms com Dashboard, Viagens, Receitas, Despesas, Relatórios e Configurações.

## Configuração do banco
Defina a variável de ambiente `FIGO_DB_CONNECTION` antes de executar.

Exemplo:

```text
Server=localhost;Database=figo;Uid=usuario;Pwd=SUA_SENHA;
```

Não publique senhas do banco no repositório.

## Executar
Abra `FIGO.backend/FIGO.backend.slnx` no Visual Studio. A solução referencia o backend e o projeto `FIGO.Desktop`.

O Dashboard possui modo de demonstração caso o MySQL ainda não esteja disponível.

## Frontend
A interface segue o layout de referência do FIGO:
- menu lateral;
- cartões de receitas, despesas, saldo e participantes;
- gráficos de despesas por categoria e receitas x despesas;
- tabela de viagens;
- cadastro/edição/exclusão de viagens;
- cadastro/edição/exclusão de receitas e despesas;
- relatório financeiro com exportação CSV.

> O repositório ignora `.vs/`, `bin/`, `obj/`, arquivos de usuário e arquivos `.env`.

## Login e criação de conta
1. Execute `database/001_usuarios.sql` no banco MySQL já utilizado pelo FIGO.
2. Configure `FIGO_DB_CONNECTION` e reinicie o Visual Studio para carregar a variável.
3. Inicie **FIGO.Desktop**. Clique em **Criar uma conta**, informe um usuário (3–50 caracteres) e confirme a senha.
4. A senha exige 8–128 caracteres, letra, número e sinal especial. Exemplo de formato: `MinhaSenha1!` (crie sua própria senha).
5. Após criar a conta, entre com usuário e senha. A janela principal só abre após autenticação.

As senhas são armazenadas com PBKDF2-SHA256 (600.000 iterações) e salt aleatório por usuário. Não há conta ou senha padrão. O nome do usuário autenticado aparece no cabeçalho e no Dashboard. Nomes de usuário não diferenciam maiúsculas e minúsculas; senhas diferenciam.

O cadastro está disponível na tela inicial: todas as contas têm acesso à mesma gestão financeira. Controle de permissões/aprovação de novos usuários não faz parte desta versão. O bloqueio de 30 segundos após 5 falhas vale para a janela de login aberta.

Os atalhos da direita usam a viagem selecionada: lançar receita/despesa abre o formulário e atualiza o Dashboard ao salvar; consultar receitas/despesas abre a listagem com a viagem selecionada; resumo abre o relatório filtrado. Em demonstração, os atalhos solicitam uma viagem real.

## Verificação
- `dotnet build FIGO.Desktop/FIGO.Desktop.csproj --configuration Release` (Windows, SDK .NET 10).
- `dotnet run --project FIGO.Tests/FIGO.Tests.csproj` valida as regras de cadastro.
- Para testar cadastro/login no MySQL, use um banco de testes preparado e defina `FIGO_RUN_DB_TESTS=1`, além da conexão. O teste remove a própria conta ao terminar.
- No Windows, confira: cadastro com senhas divergentes, usuário repetido, senha incorreta, login válido, fechamento da tela de login, ausência de conexão e os cinco atalhos com duas viagens distintas.

## Receitas detalhadas e cadastros rápidos
Execute **`database/002_cadastros_financeiros.sql` completo no banco do FIGO**, após a migração de usuários. Selecione primeiro o banco no Workbench e execute o arquivo inteiro, inclusive os trechos `DELIMITER` e `CALL`. O script precisa de permissão para criar tabelas, alterar tabelas e criar/executar/remover a procedure temporária. Ele pode ser executado novamente. Faça uma cópia de segurança antes de atualizar um banco em uso; os comandos DDL do MySQL não são desfeitos como uma transação comum.

Em **Receitas → Novo**, preencha descrição, valor, data e selecione:

- **Origem:** doação, oferta, patrocínio, venda, evento etc. É possível cadastrar outras origens.
- **Quem pagou:** pessoa física ou empresa. Empresas exigem razão social e CNPJ válido. Pessoas físicas exigem nome e podem deixar o CPF em branco; quando informado, o CPF é validado. CPF/CNPJ repetidos não geram outro cadastro.
- **Forma de pagamento:** Pix, transferência, boleto, cartão, dinheiro etc. Outras formas também podem ser cadastradas.
- **Conta de destino:** conta bancária com nome, banco, agência, número/dígito e titular. Para dinheiro em espécie, use um caixa. O script disponibiliza `Caixa em espécie`, sem atribuí-lo automaticamente a nenhuma receita.
- **Referência opcional:** identificador do Pix, recibo ou outra referência do pagamento.

Os campos de seleção pesquisam **dentro do próprio campo**, por qualquer trecho, ignorando diferenças de maiúsculas/minúsculas e acentos. Pagadores também podem ser encontrados pelo CPF/CNPJ, com ou sem máscara. Selecione um resultado na lista (mouse ou teclado); digitar um nome sem selecioná-lo não grava um cadastro. Os botões **+ Novo** abrem o cadastro correspondente, salvam no MySQL e selecionam o novo registro sem apagar o restante do lançamento. **Recarregar opções** busca cadastros adicionados em outra janela/computador.

Em **Despesas → Novo**, **Categoria** usa a mesma seleção com busca e botão **+ Novo**. Categorias já utilizadas nas despesas anteriores são importadas para a seleção. As novas opções ficam disponíveis para todas as viagens e usuários, seguindo o compartilhamento já existente no FIGO.

Receitas anteriores mantêm valor, data, descrição e origem. Pagador, forma e conta antigos permanecem desconhecidos, exibidos como não informados. Ao editar um registro antigo, complete esses campos antes de salvar. Não são criados pagadores nem vínculos bancários presumidos. Os relatórios e gráficos continuam usando os valores e categorias dos lançamentos.

A identificação bancária registra onde o valor foi recebido; não faz transações, conciliação ou consulta de saldo ao banco. A validação de CPF/CNPJ verifica formato e dígitos, sem consulta à Receita Federal. O CNPJ aceita os formatos numérico e alfanumérico, conforme o [manual oficial Receita Federal/Serpro](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf).

### Testes financeiros
O GitHub Actions compila o WinForms no Windows, testa as seleções e valida as regras financeiras. Outro job prepara um MySQL 8.4 isolado, aplica a migração duas vezes sobre registros antigos e verifica cadastro, duplicidades, criação/edição/exclusão de lançamentos e os totais dos relatórios. `database/tests/base_legada.sql` é uma fixture exclusiva de testes: não execute esse arquivo no banco em uso.

Para repetir a integração, prepare o banco **figo_test**, defina `FIGO_DB_CONNECTION` apontando para ele e `FIGO_RUN_FINANCE_DB_TESTS=1`, e execute `dotnet run --project FIGO.Tests`. A aplicação deve continuar sendo iniciada pelo projeto **FIGO.Desktop**; o executável do backend apenas apresenta as instruções e não insere dados de demonstração.
