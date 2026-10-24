using MySqlConnector;

namespace FIGO.backend;

public class ReceitaService
{
    public void Adicionar(Receita receita) => Salvar(receita, false);
    public void Editar(Receita receita) => Salvar(receita, true);

    private static void Salvar(Receita receita, bool editar)
    {
        ValidacaoFinanceira.Receita(receita);
        using var conexao = Conexao.Abrir();
        const string cadastros = "OrigemReceita o, Pagador p, FormaPagamento f, ContaFinanceira c";
        const string selecao = "o.Id = @origem AND p.Id = @pagador AND f.Id = @forma AND c.Id = @conta";
        string sql = editar ? $@"
            UPDATE Receita r, {cadastros}
            SET r.Descricao = @descricao, r.Valor = @valor, r.Data = @data, r.Origem = o.Nome,
                r.OrigemId = o.Id, r.PagadorId = p.Id, r.FormaPagamentoId = f.Id,
                r.ContaFinanceiraId = c.Id, r.ReferenciaPagamento = @referencia
            WHERE r.Id = @id AND r.ViagemId = @viagem AND {selecao};" : $@"
            INSERT INTO Receita (ViagemId, Descricao, Valor, Data, Origem, OrigemId, PagadorId, FormaPagamentoId, ContaFinanceiraId, ReferenciaPagamento)
            SELECT @viagem, @descricao, @valor, @data, o.Nome, o.Id, p.Id, f.Id, c.Id, @referencia
            FROM {cadastros} WHERE {selecao};";
        using var cmd = new MySqlCommand(sql, conexao);
        cmd.Parameters.AddWithValue("@viagem", receita.ViagemId); cmd.Parameters.AddWithValue("@id", receita.Id);
        cmd.Parameters.AddWithValue("@descricao", receita.Descricao.Trim()); cmd.Parameters.AddWithValue("@valor", receita.Valor);
        cmd.Parameters.AddWithValue("@data", receita.Data.Date); cmd.Parameters.AddWithValue("@origem", receita.OrigemId);
        cmd.Parameters.AddWithValue("@pagador", receita.PagadorId); cmd.Parameters.AddWithValue("@forma", receita.FormaPagamentoId);
        cmd.Parameters.AddWithValue("@conta", receita.ContaFinanceiraId); cmd.Parameters.AddWithValue("@referencia", receita.ReferenciaPagamento.Trim());
        int afetados = cmd.ExecuteNonQuery();
        if (afetados == 0)
        {
            // UPDATE sem mudanças também pode devolver zero com UseAffectedRows=true.
            if (!editar) throw new ArgumentException("Um dos cadastros selecionados não está disponível. Atualize a seleção.");
            cmd.CommandText = $"SELECT COUNT(*) FROM Receita r, {cadastros} WHERE r.Id = @id AND r.ViagemId = @viagem AND {selecao}";
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) throw new ArgumentException("Receita ou cadastro não encontrado. Atualize a listagem.");
        }
        if (!editar) receita.Id = checked((int)cmd.LastInsertedId);
    }

    public List<Receita> ListarPorViagem(int viagemId)
    {
        using var conexao = Conexao.Abrir();
        const string sql = @"
            SELECT r.*, COALESCE(p.Nome, 'Não informado (registro antigo)') AS PagadorNome,
                COALESCE(f.Nome, 'Não informado') AS FormaPagamentoNome,
                CASE WHEN c.Id IS NULL THEN 'Não informada'
                    WHEN c.Tipo = 'Caixa' THEN CONCAT(c.Nome, ' • Caixa')
                    ELSE CONCAT(c.Nome, ' • ', c.Banco, ' • Ag. ', c.Agencia, ' / Conta ', c.Numero) END AS ContaDestino
            FROM Receita r
            LEFT JOIN Pagador p ON p.Id = r.PagadorId
            LEFT JOIN FormaPagamento f ON f.Id = r.FormaPagamentoId
            LEFT JOIN ContaFinanceira c ON c.Id = r.ContaFinanceiraId
            WHERE r.ViagemId = @viagem ORDER BY r.Data DESC, r.Id DESC;";
        using var cmd = new MySqlCommand(sql, conexao); cmd.Parameters.AddWithValue("@viagem", viagemId);
        using var reader = cmd.ExecuteReader();
        var lista = new List<Receita>();
        while (reader.Read()) lista.Add(new Receita
        {
            Id = reader.GetInt32("Id"), ViagemId = reader.GetInt32("ViagemId"), Descricao = reader.GetString("Descricao"),
            Valor = reader.GetDecimal("Valor"), Data = reader.GetDateTime("Data"), Origem = reader.GetString("Origem"),
            OrigemId = IdOuNulo(reader, "OrigemId"), PagadorId = IdOuNulo(reader, "PagadorId"),
            FormaPagamentoId = IdOuNulo(reader, "FormaPagamentoId"), ContaFinanceiraId = IdOuNulo(reader, "ContaFinanceiraId"),
            PagadorNome = reader.GetString("PagadorNome"), FormaPagamentoNome = reader.GetString("FormaPagamentoNome"),
            ContaDestino = reader.GetString("ContaDestino"), ReferenciaPagamento = reader.GetString("ReferenciaPagamento")
        });
        return lista;
    }
    private static int? IdOuNulo(MySqlDataReader reader, string campo) => reader.IsDBNull(reader.GetOrdinal(campo)) ? null : reader.GetInt32(campo);

    public void Excluir(int id)
    {
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlCommand("DELETE FROM Receita WHERE Id = @id", conexao);
        cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
    }
}
