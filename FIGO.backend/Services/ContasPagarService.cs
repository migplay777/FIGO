using MySqlConnector;

namespace FIGO.backend;

public sealed class ContasPagarService
{
    public List<ContaPagar> Listar(int viagemId)
    {
        ValidacaoPlanejamento.Id(viagemId, "uma viagem"); using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("""
            SELECT cp.Id,cp.ViagemId,cp.Descricao,cp.Credor,cp.CategoriaDespesaId,cat.Nome,cp.ValorTotal,cp.Vencimento,COALESCE(p.Pago,0)
            FROM ContaPagar cp JOIN CategoriaDespesa cat ON cat.Id=cp.CategoriaDespesaId
            LEFT JOIN (SELECT p.ContaPagarId,SUM(d.Valor) Pago FROM PagamentoConta p JOIN Despesa d ON d.Id=p.DespesaId GROUP BY p.ContaPagarId) p ON p.ContaPagarId=cp.Id
            WHERE cp.ViagemId=@viagem ORDER BY cp.Vencimento,cp.Id
            """, c); cmd.Parameters.AddWithValue("@viagem", viagemId);
        using var r = cmd.ExecuteReader(); var lista = new List<ContaPagar>();
        while (r.Read()) lista.Add(new() { Id=r.GetInt32(0),ViagemId=r.GetInt32(1),Descricao=r.GetString(2),Credor=r.GetString(3),CategoriaId=r.GetInt32(4),Categoria=r.GetString(5),ValorTotal=r.GetDecimal(6),Vencimento=r.GetDateTime(7),Pago=r.GetDecimal(8) });
        return lista;
    }
    public int Salvar(ContaPagar conta)
    {
        ValidacaoPlanejamento.Conta(conta);
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand(conta.Id == 0 ? "INSERT INTO ContaPagar (ViagemId,Descricao,Credor,CategoriaDespesaId,ValorTotal,Vencimento) VALUES (@viagem,@descricao,@credor,@categoria,@valor,@vencimento)" : "UPDATE ContaPagar SET Descricao=@descricao,Credor=@credor,CategoriaDespesaId=@categoria,ValorTotal=@valor,Vencimento=@vencimento WHERE Id=@id AND ViagemId=@viagem", c);
        cmd.Parameters.AddWithValue("@viagem", conta.ViagemId); cmd.Parameters.AddWithValue("@descricao", conta.Descricao); cmd.Parameters.AddWithValue("@credor", conta.Credor); cmd.Parameters.AddWithValue("@categoria", conta.CategoriaId); cmd.Parameters.AddWithValue("@valor", conta.ValorTotal); cmd.Parameters.AddWithValue("@vencimento", conta.Vencimento.Date); cmd.Parameters.AddWithValue("@id", conta.Id);
        int afetadas = cmd.ExecuteNonQuery();
        if (conta.Id == 0) conta.Id = checked((int)cmd.LastInsertedId);
        else if (afetadas == 0)
        {
            cmd.CommandText="SELECT COUNT(*) FROM ContaPagar WHERE Id=@id AND ViagemId=@viagem";
            if (Convert.ToInt32(cmd.ExecuteScalar())==0) throw new ArgumentException("Esta conta não existe mais. Atualize a lista.");
        }
        return conta.Id;
    }
    public void Excluir(int contaId, int viagemId)
    {
        ValidacaoPlanejamento.Id(contaId, "uma conta"); ValidacaoPlanejamento.Id(viagemId, "uma viagem");
        using var c=Conexao.Abrir(); using var cmd=new MySqlCommand("DELETE FROM ContaPagar WHERE Id=@id AND ViagemId=@viagem",c);
        cmd.Parameters.AddWithValue("@id",contaId);cmd.Parameters.AddWithValue("@viagem",viagemId);cmd.ExecuteNonQuery();
    }
    public List<PagamentoRegistrado> Pagamentos(int contaId)
    {
        ValidacaoPlanejamento.Id(contaId, "uma conta");using var c=Conexao.Abrir();
        using var cmd=new MySqlCommand("SELECT p.Id,p.ContaPagarId,p.DespesaId,d.Valor,d.Data,p.Referencia FROM PagamentoConta p JOIN Despesa d ON d.Id=p.DespesaId WHERE p.ContaPagarId=@id ORDER BY d.Data,p.Id",c);
        cmd.Parameters.AddWithValue("@id",contaId);using var r=cmd.ExecuteReader();var lista=new List<PagamentoRegistrado>();
        while(r.Read())lista.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetDecimal(3),r.GetDateTime(4),r.GetString(5)));return lista;
    }
    public int Pagar(int contaId, decimal valor, DateTime data, string referencia, string chaveOperacao)
    {
        ValidacaoPlanejamento.Id(contaId,"uma conta");ValidacaoPlanejamento.Valor(valor);ValidacaoPlanejamento.Data(data);
        if(!Guid.TryParseExact(chaveOperacao,"N",out _))throw new ArgumentException("Identificador da operação inválido.");
        if(referencia.Length>100)throw new ArgumentException("A referência permite até 100 caracteres.");
        using var c=Conexao.Abrir();using var tx=c.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        using var cmd=new MySqlCommand("SELECT ViagemId,CategoriaDespesaId,Descricao,ValorTotal FROM ContaPagar WHERE Id=@conta FOR UPDATE",c,tx);
        cmd.Parameters.AddWithValue("@conta",contaId);
        int viagem,categoria;string descricao;decimal total;
        using(var r=cmd.ExecuteReader())
        {
            if(!r.Read())throw new ArgumentException("A conta não existe mais.");
            viagem=r.GetInt32(0);categoria=r.GetInt32(1);descricao=r.GetString(2);total=r.GetDecimal(3);
        }
        cmd.CommandText="SELECT p.Id,p.ContaPagarId,d.Valor,d.Data,p.Referencia FROM PagamentoConta p JOIN Despesa d ON d.Id=p.DespesaId WHERE p.ChaveOperacao=@chave FOR UPDATE";
        cmd.Parameters.AddWithValue("@chave",chaveOperacao);cmd.Parameters.AddWithValue("@referencia",referencia.Trim());
        using(var r=cmd.ExecuteReader())
        {
            if(r.Read())
            {
                if(r.GetInt32(1)!=contaId||r.GetDecimal(2)!=valor||r.GetDateTime(3).Date!=data.Date||r.GetString(4)!=referencia.Trim())throw new ArgumentException("Esta operação já foi usada para outro pagamento.");
                int id=r.GetInt32(0);r.Close();tx.Commit();return id;
            }
        }
        cmd.CommandText="SELECT COALESCE(SUM(d.Valor),0) FROM PagamentoConta p JOIN Despesa d ON d.Id=p.DespesaId WHERE p.ContaPagarId=@conta FOR UPDATE";
        decimal pago=Convert.ToDecimal(cmd.ExecuteScalar());
        if(valor>total-pago)throw new ArgumentException("O pagamento ultrapassa o saldo da conta. Atualize a listagem.");
        cmd.CommandText="INSERT INTO Despesa (ViagemId,Descricao,Valor,Data,Categoria,CategoriaId) SELECT @viagem,@descricao,@valor,@data,Nome,Id FROM CategoriaDespesa WHERE Id=@categoria";
        cmd.Parameters.AddWithValue("@viagem",viagem);cmd.Parameters.AddWithValue("@descricao",descricao);cmd.Parameters.AddWithValue("@valor",valor);cmd.Parameters.AddWithValue("@data",data.Date);cmd.Parameters.AddWithValue("@categoria",categoria);
        if(cmd.ExecuteNonQuery()!=1)throw new ArgumentException("Categoria da conta não encontrada.");long despesa=cmd.LastInsertedId;
        cmd.CommandText="INSERT INTO PagamentoConta (ContaPagarId,DespesaId,Referencia,ChaveOperacao) VALUES (@conta,@despesa,@referencia,@chave)";
        cmd.Parameters.AddWithValue("@despesa",despesa);cmd.ExecuteNonQuery();int novoId=checked((int)cmd.LastInsertedId);tx.Commit();return novoId;
    }
    public void Estornar(int contaId, int pagamentoId)
    {
        ValidacaoPlanejamento.Id(contaId,"uma conta");ValidacaoPlanejamento.Id(pagamentoId,"um pagamento");
        using var c=Conexao.Abrir();using var tx=c.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        using var cmd=new MySqlCommand("SELECT Id FROM ContaPagar WHERE Id=@conta FOR UPDATE",c,tx);cmd.Parameters.AddWithValue("@conta",contaId);
        if(cmd.ExecuteScalar()==null)throw new ArgumentException("A conta não existe mais.");
        cmd.CommandText="DELETE FROM PagamentoConta WHERE Id=@id AND ContaPagarId=@conta";cmd.Parameters.AddWithValue("@id",pagamentoId);
        if(cmd.ExecuteNonQuery()==0)throw new ArgumentException("O pagamento já foi removido. Atualize a listagem.");
        // O trigger exclui a despesa vinculada; uma FK de anexo bloqueia e reverte tudo.
        tx.Commit();
    }
}
