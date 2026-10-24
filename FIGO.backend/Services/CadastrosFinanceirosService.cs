using MySqlConnector;

namespace FIGO.backend;

public sealed class CadastrosFinanceirosService
{
    private static string Tabela(TipoCadastroFinanceiro tipo) => tipo switch
    {
        TipoCadastroFinanceiro.OrigemReceita => "OrigemReceita",
        TipoCadastroFinanceiro.FormaPagamento => "FormaPagamento",
        TipoCadastroFinanceiro.CategoriaDespesa => "CategoriaDespesa",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public List<OpcaoFinanceira> Listar(TipoCadastroFinanceiro tipo)
    {
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlCommand($"SELECT Id, Nome FROM {Tabela(tipo)} ORDER BY Nome", conexao);
        using var reader = cmd.ExecuteReader();
        var lista = new List<OpcaoFinanceira>();
        while (reader.Read()) lista.Add(new(reader.GetInt32("Id"), reader.GetString("Nome")));
        return lista;
    }

    public int Adicionar(TipoCadastroFinanceiro tipo, string nome)
    {
        nome = ValidacaoFinanceira.Nome(nome, "Nome");
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlCommand($"INSERT INTO {Tabela(tipo)} (Nome) VALUES (@nome)", conexao);
        cmd.Parameters.AddWithValue("@nome", nome);
        return Inserir(cmd, "Este nome já está cadastrado. Pesquise e selecione a opção existente.");
    }

    public List<Pagador> ListarPagadores()
    {
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlCommand("SELECT Id, Tipo, Nome, Documento FROM Pagador ORDER BY Nome, Tipo, Documento", conexao);
        using var reader = cmd.ExecuteReader();
        var lista = new List<Pagador>();
        while (reader.Read()) lista.Add(new Pagador { Id = reader.GetInt32("Id"), Tipo = reader.GetString("Tipo"),
            Nome = reader.GetString("Nome"), Documento = reader.IsDBNull(reader.GetOrdinal("Documento")) ? null : reader.GetString("Documento") });
        return lista;
    }

    public int AdicionarPagador(Pagador pagador)
    {
        ValidacaoFinanceira.Pagador(pagador);
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlCommand("INSERT INTO Pagador (Tipo, Nome, Documento) VALUES (@tipo, @nome, @documento)", conexao);
        cmd.Parameters.AddWithValue("@tipo", pagador.Tipo);
        cmd.Parameters.AddWithValue("@nome", pagador.Nome);
        cmd.Parameters.AddWithValue("@documento", (object?)pagador.Documento ?? DBNull.Value);
        int id = Inserir(cmd, "Este CPF/CNPJ já está cadastrado. Pesquise o pagador existente.");
        pagador.Id = id;
        return id;
    }

    public List<ContaFinanceira> ListarContas()
    {
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlCommand("SELECT Id, Nome, Tipo, Banco, Agencia, Numero, Titular FROM ContaFinanceira ORDER BY Nome", conexao);
        using var reader = cmd.ExecuteReader();
        var lista = new List<ContaFinanceira>();
        while (reader.Read()) lista.Add(new ContaFinanceira { Id = reader.GetInt32("Id"), Nome = reader.GetString("Nome"),
            Tipo = reader.GetString("Tipo"), Banco = reader.GetString("Banco"), Agencia = reader.GetString("Agencia"),
            Numero = reader.GetString("Numero"), Titular = reader.GetString("Titular") });
        return lista;
    }

    public int AdicionarConta(ContaFinanceira conta)
    {
        ValidacaoFinanceira.Conta(conta);
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlCommand("INSERT INTO ContaFinanceira (Nome, Tipo, Banco, Agencia, Numero, Titular) VALUES (@nome, @tipo, @banco, @agencia, @numero, @titular)", conexao);
        cmd.Parameters.AddWithValue("@nome", conta.Nome); cmd.Parameters.AddWithValue("@tipo", conta.Tipo);
        cmd.Parameters.AddWithValue("@banco", conta.Banco); cmd.Parameters.AddWithValue("@agencia", conta.Agencia);
        cmd.Parameters.AddWithValue("@numero", conta.Numero); cmd.Parameters.AddWithValue("@titular", conta.Titular);
        int id = Inserir(cmd, "Já existe uma conta com esse nome ou os mesmos dados bancários. Selecione a conta cadastrada.");
        conta.Id = id;
        return id;
    }

    private static int Inserir(MySqlCommand cmd, string duplicado)
    {
        try { cmd.ExecuteNonQuery(); return checked((int)cmd.LastInsertedId); }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new ArgumentException(duplicado); }
    }
}
