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
