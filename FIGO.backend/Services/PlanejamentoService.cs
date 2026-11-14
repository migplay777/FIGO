using MySqlConnector;

namespace FIGO.backend;

public sealed class PlanejamentoService
{
    public List<LinhaOrcamento> Orcamento(int viagemId)
    {
        ValidacaoPlanejamento.Id(viagemId, "uma viagem");
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("""
            SELECT COALESCE(i.Id,0), cat.Id, cat.Nome, COALESCE(i.ValorPrevisto,0), COALESCE(d.Total,0)
            FROM CategoriaDespesa cat
            LEFT JOIN OrcamentoViagem o ON o.ViagemId=@viagem
            LEFT JOIN ItemOrcamento i ON i.OrcamentoViagemId=o.Id AND i.CategoriaDespesaId=cat.Id
            LEFT JOIN (SELECT CategoriaId, SUM(Valor) Total FROM Despesa WHERE ViagemId=@viagem GROUP BY CategoriaId) d ON d.CategoriaId=cat.Id
            WHERE i.Id IS NOT NULL OR d.Total IS NOT NULL ORDER BY cat.Nome
            """, c);
        cmd.Parameters.AddWithValue("@viagem", viagemId);
        using var r = cmd.ExecuteReader(); var lista = new List<LinhaOrcamento>();
        while (r.Read()) lista.Add(new(r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetDecimal(3), r.GetDecimal(4)));
        // Despesas legadas sem categoria estruturada também entram no total realizado.
        r.Close(); cmd.CommandText = "SELECT COALESCE(SUM(Valor),0) FROM Despesa WHERE ViagemId=@viagem AND CategoriaId IS NULL";
        decimal legado = Convert.ToDecimal(cmd.ExecuteScalar());
        if (legado != 0) lista.Add(new(0, 0, "Sem categoria estruturada (legado)", 0, legado));
        return lista;
    }
    public int SalvarItem(int viagemId, int categoriaId, decimal previsto)
    {
        ValidacaoPlanejamento.Id(viagemId, "uma viagem"); ValidacaoPlanejamento.Id(categoriaId, "uma categoria"); ValidacaoPlanejamento.Valor(previsto);
        using var c = Conexao.Abrir(); using var tx = c.BeginTransaction();
        using var cmd = new MySqlCommand("INSERT INTO OrcamentoViagem (ViagemId) VALUES (@viagem) ON DUPLICATE KEY UPDATE ViagemId=@viagem", c, tx);
        cmd.Parameters.AddWithValue("@viagem", viagemId); cmd.ExecuteNonQuery();
        // No-op upserts com UseAffectedRows não fornecem necessariamente LastInsertedId.
        cmd.CommandText = "SELECT Id FROM OrcamentoViagem WHERE ViagemId=@viagem FOR UPDATE";
        long orcamento = Convert.ToInt64(cmd.ExecuteScalar());
        cmd.CommandText = "INSERT INTO ItemOrcamento (OrcamentoViagemId,CategoriaDespesaId,ValorPrevisto) VALUES (@orcamento,@categoria,@valor) ON DUPLICATE KEY UPDATE ValorPrevisto=@valor";
        cmd.Parameters.AddWithValue("@orcamento", orcamento); cmd.Parameters.AddWithValue("@categoria", categoriaId); cmd.Parameters.AddWithValue("@valor", previsto);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT Id FROM ItemOrcamento WHERE OrcamentoViagemId=@orcamento AND CategoriaDespesaId=@categoria";
        int id = Convert.ToInt32(cmd.ExecuteScalar()); tx.Commit(); return id;
    }
    public void ExcluirItem(int viagemId, int itemId)
    {
        ValidacaoPlanejamento.Id(viagemId, "uma viagem"); ValidacaoPlanejamento.Id(itemId, "um item previsto");
        using var c = Conexao.Abrir(); using var cmd = new MySqlCommand("DELETE i FROM ItemOrcamento i JOIN OrcamentoViagem o ON o.Id=i.OrcamentoViagemId WHERE i.Id=@id AND o.ViagemId=@viagem", c);
        cmd.Parameters.AddWithValue("@id", itemId); cmd.Parameters.AddWithValue("@viagem", viagemId); cmd.ExecuteNonQuery();
    }
    public ResumoMeta? Meta(int viagemId)
    {
        ValidacaoPlanejamento.Id(viagemId, "uma viagem");
        using var c = Conexao.Abrir(); using var cmd = new MySqlCommand("SELECT m.Id,m.Descricao,m.ValorMeta,m.DataLimite, (SELECT COALESCE(SUM(Valor),0) FROM Receita WHERE ViagemId=m.ViagemId) FROM MetaArrecadacao m WHERE m.ViagemId=@viagem", c);
        cmd.Parameters.AddWithValue("@viagem", viagemId); using var r = cmd.ExecuteReader();
        return r.Read() ? new(r.GetInt32(0), r.GetString(1), r.GetDecimal(2), r.IsDBNull(3) ? null : r.GetDateTime(3), r.GetDecimal(4)) : null;
    }
    public int SalvarMeta(int viagemId, string descricao, decimal valor, DateTime? prazo)
    {
        ValidacaoPlanejamento.Id(viagemId, "uma viagem"); ValidacaoPlanejamento.Valor(valor);
        descricao = ValidacaoFinanceira.Nome(descricao, "Descrição da meta", 150); if (prazo != null) ValidacaoPlanejamento.Data(prazo.Value);
        using var c = Conexao.Abrir(); using var tx = c.BeginTransaction();
        using var cmd = new MySqlCommand("INSERT INTO MetaArrecadacao (ViagemId,Descricao,ValorMeta,DataLimite) VALUES (@viagem,@nome,@valor,@prazo) ON DUPLICATE KEY UPDATE Descricao=@nome,ValorMeta=@valor,DataLimite=@prazo", c, tx);
        cmd.Parameters.AddWithValue("@viagem", viagemId); cmd.Parameters.AddWithValue("@nome", descricao); cmd.Parameters.AddWithValue("@valor", valor); cmd.Parameters.AddWithValue("@prazo", prazo?.Date);
        cmd.ExecuteNonQuery(); cmd.CommandText = "SELECT Id FROM MetaArrecadacao WHERE ViagemId=@viagem";
        int id = Convert.ToInt32(cmd.ExecuteScalar()); tx.Commit(); return id;
    }
    public void ExcluirMeta(int viagemId)
    {
        ValidacaoPlanejamento.Id(viagemId, "uma viagem"); using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("DELETE FROM MetaArrecadacao WHERE ViagemId=@viagem", c); cmd.Parameters.AddWithValue("@viagem", viagemId); cmd.ExecuteNonQuery();
    }
}
