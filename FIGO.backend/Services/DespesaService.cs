using System.Collections.Generic;
using MySqlConnector;

namespace FIGO.backend
{
    public class DespesaService
    {
        public void Adicionar(Despesa despesa)
        {
            using var conexao = Conexao.Abrir();
            const string sql = @"
                INSERT INTO Despesa (ViagemId, Descricao, Valor, Data, Categoria)
                VALUES (@ViagemId, @Descricao, @Valor, @Data, @Categoria);
                SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ViagemId", despesa.ViagemId);
            comando.Parameters.AddWithValue("@Descricao", despesa.Descricao);
            comando.Parameters.AddWithValue("@Valor", despesa.Valor);
            comando.Parameters.AddWithValue("@Data", despesa.Data);
            comando.Parameters.AddWithValue("@Categoria", despesa.Categoria);
            despesa.Id = System.Convert.ToInt32(comando.ExecuteScalar());
        }

        public List<Despesa> ListarPorViagem(int viagemId)
        {
            var despesas = new List<Despesa>();
            using var conexao = Conexao.Abrir();
            const string sql = "SELECT Id, ViagemId, Descricao, Valor, Data, Categoria FROM Despesa WHERE ViagemId = @ViagemId ORDER BY Data;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ViagemId", viagemId);
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                despesas.Add(new Despesa
                {
                    Id = leitor.GetInt32("Id"),
                    ViagemId = leitor.GetInt32("ViagemId"),
                    Descricao = leitor.GetString("Descricao"),
                    Valor = leitor.GetDecimal("Valor"),
                    Data = leitor.GetDateTime("Data"),
                    Categoria = leitor.GetString("Categoria")
                });
            }
            return despesas;
        }

        public void Editar(Despesa despesa)
        {
            using var conexao = Conexao.Abrir();
            const string sql = @"
                UPDATE Despesa
                SET Descricao = @Descricao,
                    Valor = @Valor,
                    Data = @Data,
                    Categoria = @Categoria
                WHERE Id = @Id;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Descricao", despesa.Descricao);
            comando.Parameters.AddWithValue("@Valor", despesa.Valor);
            comando.Parameters.AddWithValue("@Data", despesa.Data);
            comando.Parameters.AddWithValue("@Categoria", despesa.Categoria);
            comando.Parameters.AddWithValue("@Id", despesa.Id);
            comando.ExecuteNonQuery();
        }

        public void Excluir(int id)
        {
            using var conexao = Conexao.Abrir();
            const string sql = "DELETE FROM Despesa WHERE Id = @Id;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Id", id);
            comando.ExecuteNonQuery();
        }

        public List<DespesaCategoria> ObterPorCategoria(int viagemId)
        {
            var resultado = new List<DespesaCategoria>();
            using var conexao = Conexao.Abrir();
            const string sql = @"
                SELECT Categoria, SUM(Valor) AS Total
                FROM Despesa
                WHERE ViagemId = @ViagemId
                GROUP BY Categoria
                ORDER BY Total DESC;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ViagemId", viagemId);
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                resultado.Add(new DespesaCategoria
                {
                    Categoria = leitor.GetString("Categoria"),
                    Total = leitor.GetDecimal("Total")
                });
            }
            return resultado;
        }
    }
}
