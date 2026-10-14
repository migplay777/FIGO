# FIGO

Sistema financeiro de viagens missionárias, com backend C#/.NET, Windows Forms e MySQL.

## Configuração
Defina a variável de ambiente `FIGO_DB_CONNECTION` com a string de conexão MySQL antes de executar. Exemplo: `Server=localhost;Database=figo;Uid=usuario;Pwd=SUA_SENHA;`. Não publique senhas no GitHub.

Abra o projeto `FIGO.Desktop/FIGO.Desktop.csproj` no Visual Studio e restaure as dependências.

## Atualização do frontend
- Receitas e Despesas: cadastro, edição, exclusão e listagem por viagem.
- Relatórios: consolidação por viagem e exportação CSV.
- Configurações: informações sobre a conexão com o banco.
- Dashboard e Viagens: preservados da versão anterior.

**Atenção:** a interface ainda precisa ser compilada/testada em Windows com .NET 10 e um banco MySQL configurado. A exportação atual é CSV, não PDF.
