using System.Collections.Generic;
using MySqlConnector;

namespace FIGO.backend
{
    public class ReceitaService
    {
        public void Adicionar(Receita receita)
        {
            using var conexao = Conexao.Abrir();
            const string sql = @"
                INSERT INTO Receita (ViagemId, Descricao, Valor, Data, Origem)
                VALUES (@ViagemId, @Descricao, @Valor, @Data, @Origem);
                SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ViagemId", receita.ViagemId);
            comando.Parameters.AddWithValue("@Descricao", receita.Descricao);
            comando.Parameters.AddWithValue("@Valor", receita.Valor);
            comando.Parameters.AddWithValue("@Data", receita.Data);
            comando.Parameters.AddWithValue("@Origem", receita.Origem);
            receita.Id = System.Convert.ToInt32(comando.ExecuteScalar());
        }

        public List<Receita> ListarPorViagem(int viagemId)
        {
            var receitas = new List<Receita>();
            using var conexao = Conexao.Abrir();
            const string sql = "SELECT Id, ViagemId, Descricao, Valor, Data, Origem FROM Receita WHERE ViagemId = @ViagemId ORDER BY Data;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ViagemId", viagemId);
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                receitas.Add(new Receita
                {
                    Id = leitor.GetInt32("Id"),
                    ViagemId = leitor.GetInt32("ViagemId"),
                    Descricao = leitor.GetString("Descricao"),
                    Valor = leitor.GetDecimal("Valor"),
                    Data = leitor.GetDateTime("Data"),
                    Origem = leitor.GetString("Origem")
                });
            }
            return receitas;
        }

        public void Editar(Receita receita)
        {
            using var conexao = Conexao.Abrir();
            const string sql = @"
                UPDATE Receita
                SET Descricao = @Descricao,
                    Valor = @Valor,
                    Data = @Data,
                    Origem = @Origem
                WHERE Id = @Id;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Descricao", receita.Descricao);
            comando.Parameters.AddWithValue("@Valor", receita.Valor);
            comando.Parameters.AddWithValue("@Data", receita.Data);
            comando.Parameters.AddWithValue("@Origem", receita.Origem);
            comando.Parameters.AddWithValue("@Id", receita.Id);
            comando.ExecuteNonQuery();
        }

        public void Excluir(int id)
        {
            using var conexao = Conexao.Abrir();
            const string sql = "DELETE FROM Receita WHERE Id = @Id;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Id", id);
            comando.ExecuteNonQuery();
        }
    }
}
