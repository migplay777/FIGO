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
