using System.Collections.Generic;
using MySqlConnector;

namespace FIGO.backend
{
    public class DespesaService
    {
        public void Adicionar(Despesa despesa) => Salvar(despesa, false);
        public void Editar(Despesa despesa) => Salvar(despesa, true);

        private static void Salvar(Despesa despesa, bool editar)
        {
            ValidacaoFinanceira.Despesa(despesa);
            using var conexao = Conexao.Abrir();
            string sql = editar ? @"
                UPDATE Despesa d JOIN CategoriaDespesa c ON c.Id = @categoria
                SET d.Descricao = @descricao, d.Valor = @valor, d.Data = @data, d.Categoria = c.Nome, d.CategoriaId = c.Id
                WHERE d.Id = @id AND d.ViagemId = @viagem;" : @"
                INSERT INTO Despesa (ViagemId, Descricao, Valor, Data, Categoria, CategoriaId)
                SELECT @viagem, @descricao, @valor, @data, c.Nome, c.Id FROM CategoriaDespesa c WHERE c.Id = @categoria;";
            using var cmd = new MySqlCommand(sql, conexao);
            cmd.Parameters.AddWithValue("@id", despesa.Id); cmd.Parameters.AddWithValue("@viagem", despesa.ViagemId);
            cmd.Parameters.AddWithValue("@descricao", despesa.Descricao.Trim()); cmd.Parameters.AddWithValue("@valor", despesa.Valor);
            cmd.Parameters.AddWithValue("@data", despesa.Data.Date); cmd.Parameters.AddWithValue("@categoria", despesa.CategoriaId);
            if (cmd.ExecuteNonQuery() == 0)
            {
                if (!editar) throw new ArgumentException("Categoria não encontrada. Atualize a seleção.");
                cmd.CommandText = "SELECT COUNT(*) FROM Despesa d JOIN CategoriaDespesa c ON c.Id = @categoria WHERE d.Id = @id AND d.ViagemId = @viagem";
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) throw new ArgumentException("Despesa ou categoria não encontrada. Atualize a listagem.");
            }
            if (!editar) despesa.Id = checked((int)cmd.LastInsertedId);
        }

        public List<Despesa> ListarPorViagem(int viagemId)
        {
            var despesas = new List<Despesa>();
            using var conexao = Conexao.Abrir();
            const string sql = "SELECT Id, ViagemId, Descricao, Valor, Data, Categoria, CategoriaId FROM Despesa WHERE ViagemId = @ViagemId ORDER BY Data;";
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
                    Categoria = leitor.GetString("Categoria"),
                    CategoriaId = leitor.IsDBNull(leitor.GetOrdinal("CategoriaId")) ? null : leitor.GetInt32("CategoriaId")
                });
            }
            return despesas;
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
