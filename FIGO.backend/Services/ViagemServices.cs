using System.Collections.Generic;
using MySqlConnector;

namespace FIGO.backend
{
    public class ViagemServices
    {
        public void Adicionar(Viagem viagem)
        {
            using var conexao = Conexao.Abrir();
            const string sql = @"
                INSERT INTO Viagem (Nome, Destino, DataInicio, DataFim, QuantidadeParticipantes)
                VALUES (@Nome, @Destino, @DataInicio, @DataFim, @QuantidadeParticipantes);
                SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Nome", viagem.Nome);
            comando.Parameters.AddWithValue("@Destino", viagem.Destino);
            comando.Parameters.AddWithValue("@DataInicio", viagem.DataInicio);
            comando.Parameters.AddWithValue("@DataFim", viagem.DataFim);
            comando.Parameters.AddWithValue("@QuantidadeParticipantes", viagem.QuantidadeParticipantes);
            viagem.Id = System.Convert.ToInt32(comando.ExecuteScalar());
        }

        public List<Viagem> Listar()
        {
            var viagens = new List<Viagem>();
            using var conexao = Conexao.Abrir();
            const string sql = "SELECT Id, Nome, Destino, DataInicio, DataFim, QuantidadeParticipantes FROM Viagem ORDER BY DataInicio DESC;";
            using var comando = new MySqlCommand(sql, conexao);
            using var leitor = comando.ExecuteReader();
            while (leitor.Read()) viagens.Add(LerViagem(leitor));
            return viagens;
        }

        public Viagem? BuscarPorId(int id)
        {
            using var conexao = Conexao.Abrir();
            const string sql = "SELECT Id, Nome, Destino, DataInicio, DataFim, QuantidadeParticipantes FROM Viagem WHERE Id = @Id;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Id", id);
            using var leitor = comando.ExecuteReader();
            return leitor.Read() ? LerViagem(leitor) : null;
        }

        public void Editar(Viagem viagem)
        {
            using var conexao = Conexao.Abrir();
            const string sql = @"
                UPDATE Viagem
                SET Nome = @Nome,
                    Destino = @Destino,
                    DataInicio = @DataInicio,
                    DataFim = @DataFim,
                    QuantidadeParticipantes = @QuantidadeParticipantes
                WHERE Id = @Id;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Nome", viagem.Nome);
            comando.Parameters.AddWithValue("@Destino", viagem.Destino);
            comando.Parameters.AddWithValue("@DataInicio", viagem.DataInicio);
            comando.Parameters.AddWithValue("@DataFim", viagem.DataFim);
            comando.Parameters.AddWithValue("@QuantidadeParticipantes", viagem.QuantidadeParticipantes);
            comando.Parameters.AddWithValue("@Id", viagem.Id);
            comando.ExecuteNonQuery();
        }

        public void Excluir(int id)
        {
            using var conexao = Conexao.Abrir();
            const string sql = "DELETE FROM Viagem WHERE Id = @Id;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Id", id);
            comando.ExecuteNonQuery();
        }

        private static Viagem LerViagem(MySqlDataReader leitor)
        {
            return new Viagem
            {
                Id = leitor.GetInt32("Id"),
                Nome = leitor.GetString("Nome"),
                Destino = leitor.GetString("Destino"),
                DataInicio = leitor.GetDateTime("DataInicio"),
                DataFim = leitor.GetDateTime("DataFim"),
                QuantidadeParticipantes = leitor.GetInt32("QuantidadeParticipantes")
            };
        }
    }
}
