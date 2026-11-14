using MySqlConnector;

namespace FIGO.backend;

public sealed class ParticipantesService
{
    public List<Participante> ListarPessoas()
    {
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("SELECT Id, NomeCompleto, Cpf FROM Participante ORDER BY NomeCompleto, Id", c);
        using var r = cmd.ExecuteReader(); var lista = new List<Participante>();
        while (r.Read()) lista.Add(new() { Id = r.GetInt32(0), NomeCompleto = r.GetString(1), Cpf = r.IsDBNull(2) ? null : r.GetString(2) });
        return lista;
    }
    public List<OpcaoFinanceira> ListarFuncoes()
    {
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("SELECT Id, Nome FROM FuncaoParticipante ORDER BY Nome", c);
        using var r = cmd.ExecuteReader(); var lista = new List<OpcaoFinanceira>();
        while (r.Read()) lista.Add(new(r.GetInt32(0), r.GetString(1)));
        return lista;
    }
    public int AdicionarFuncao(string nome)
    {
        nome = ValidacaoFinanceira.Nome(nome, "Função", 100);
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("INSERT INTO FuncaoParticipante (Nome) VALUES (@nome)", c);
        cmd.Parameters.AddWithValue("@nome", nome);
        try { cmd.ExecuteNonQuery(); return checked((int)cmd.LastInsertedId); }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new ArgumentException("Esta função já está cadastrada."); }
    }
    public List<ParticipacaoViagem> ListarPorViagem(int viagemId)
    {
        ValidacaoParticipantes.Vinculo(viagemId, null, null);
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("""
            SELECT vp.Id, vp.ViagemId, p.Id, p.NomeCompleto, p.Cpf, vp.Idade, vp.FuncaoParticipanteId, f.Nome
            FROM ViagemParticipante vp JOIN Participante p ON p.Id = vp.ParticipanteId
            LEFT JOIN FuncaoParticipante f ON f.Id = vp.FuncaoParticipanteId
            WHERE vp.ViagemId = @viagem ORDER BY p.NomeCompleto, p.Id
            """, c);
        cmd.Parameters.AddWithValue("@viagem", viagemId);
        using var r = cmd.ExecuteReader(); var lista = new List<ParticipacaoViagem>();
        while (r.Read()) lista.Add(new() { Id = r.GetInt32(0), ViagemId = r.GetInt32(1), ParticipanteId = r.GetInt32(2),
            NomeCompleto = r.GetString(3), Cpf = r.IsDBNull(4) ? null : r.GetString(4), Idade = r.IsDBNull(5) ? null : r.GetInt32(5),
            FuncaoParticipanteId = r.IsDBNull(6) ? null : r.GetInt32(6), Funcao = r.IsDBNull(7) ? null : r.GetString(7) });
        return lista;
    }
    // Pessoa, vínculo e logs participam da mesma transação.
    public int Adicionar(int viagemId, Participante pessoa, int? idade, int? funcaoId)
    {
        ValidacaoParticipantes.Vinculo(viagemId, idade, funcaoId);
        if (pessoa.Id < 0) throw new ArgumentException("Participante inválido.");
        if (pessoa.Id == 0) ValidacaoParticipantes.Pessoa(pessoa);
        using var c = Conexao.Abrir(); using var tx = c.BeginTransaction();
        try
        {
            int pessoaId = pessoa.Id;
            if (pessoaId == 0)
            {
                using var nova = new MySqlCommand("INSERT INTO Participante (NomeCompleto, Cpf) VALUES (@nome, @cpf)", c, tx);
                nova.Parameters.AddWithValue("@nome", pessoa.NomeCompleto); nova.Parameters.AddWithValue("@cpf", pessoa.Cpf);
                nova.ExecuteNonQuery(); pessoaId = checked((int)nova.LastInsertedId);
            }
            using var cmd = new MySqlCommand("INSERT INTO ViagemParticipante (ViagemId, ParticipanteId, Idade, FuncaoParticipanteId) VALUES (@viagem, @pessoa, @idade, @funcao)", c, tx);
            cmd.Parameters.AddWithValue("@viagem", viagemId); cmd.Parameters.AddWithValue("@pessoa", pessoaId);
            cmd.Parameters.AddWithValue("@idade", idade); cmd.Parameters.AddWithValue("@funcao", funcaoId);
            cmd.ExecuteNonQuery(); int id = checked((int)cmd.LastInsertedId);
            tx.Commit(); pessoa.Id = pessoaId; return id;
        }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new ArgumentException("CPF já cadastrado ou pessoa já vinculada à viagem. Utilize o cadastro existente e confira a lista."); }
        catch (MySqlException ex) when (ex.Number == 1452) { throw new ArgumentException("A viagem, a pessoa ou a função não existe mais. Atualize a lista."); }
    }
    public void Editar(ParticipacaoViagem participacao)
    {
        ValidacaoParticipantes.Vinculo(participacao.ViagemId, participacao.Idade, participacao.FuncaoParticipanteId);
        if (participacao.Id <= 0 || participacao.ParticipanteId <= 0) throw new ArgumentException("Selecione uma participação válida.");
        var pessoa = new Participante { Id = participacao.ParticipanteId, NomeCompleto = participacao.NomeCompleto, Cpf = participacao.Cpf };
        ValidacaoParticipantes.Pessoa(pessoa);
        using var c = Conexao.Abrir(); using var tx = c.BeginTransaction();
        try
        {
            using (var verificar = new MySqlCommand("SELECT Id FROM ViagemParticipante WHERE Id = @id AND ViagemId = @viagem AND ParticipanteId = @pessoa FOR UPDATE", c, tx))
            {
                verificar.Parameters.AddWithValue("@id", participacao.Id); verificar.Parameters.AddWithValue("@viagem", participacao.ViagemId); verificar.Parameters.AddWithValue("@pessoa", pessoa.Id);
                if (verificar.ExecuteScalar() == null) throw new ArgumentException("A participação não existe mais. Atualize a lista.");
            }
            using (var cmd = new MySqlCommand("UPDATE Participante SET NomeCompleto = @nome, Cpf = @cpf WHERE Id = @id", c, tx))
            {
                cmd.Parameters.AddWithValue("@nome", pessoa.NomeCompleto); cmd.Parameters.AddWithValue("@cpf", pessoa.Cpf); cmd.Parameters.AddWithValue("@id", pessoa.Id); cmd.ExecuteNonQuery();
            }
            using (var cmd = new MySqlCommand("UPDATE ViagemParticipante SET Idade = @idade, FuncaoParticipanteId = @funcao WHERE Id = @id", c, tx))
            {
                cmd.Parameters.AddWithValue("@idade", participacao.Idade); cmd.Parameters.AddWithValue("@funcao", participacao.FuncaoParticipanteId); cmd.Parameters.AddWithValue("@id", participacao.Id); cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new ArgumentException("Este CPF pertence a outro cadastro. Confira a pessoa selecionada."); }
        catch (MySqlException ex) when (ex.Number == 1452) { throw new ArgumentException("A função selecionada não existe mais. Atualize a lista."); }
    }
    public void RemoverDaViagem(int participacaoId, int viagemId)
    {
        ValidacaoParticipantes.Vinculo(viagemId, null, null);
        if (participacaoId <= 0) throw new ArgumentException("Selecione uma participação.");
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("DELETE FROM ViagemParticipante WHERE Id = @id AND ViagemId = @viagem", c);
        cmd.Parameters.AddWithValue("@id", participacaoId); cmd.Parameters.AddWithValue("@viagem", viagemId);
        if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("A participação não existe mais. Atualize a lista.");
    }
}
