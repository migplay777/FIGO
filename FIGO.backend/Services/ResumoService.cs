using System.Collections.Generic;
using MySqlConnector;

namespace FIGO.backend
{
    public class ResumoService
    {
        public ResumoViagem? ObterResumo(int viagemId)
        {
            using var conexao = Conexao.Abrir();
            const string sql = @"
                SELECT ViagemId, Nome, Destino, QuantidadeParticipantes,
                       TotalReceitas, TotalDespesas, Saldo
                FROM vw_ResumoViagem
                WHERE ViagemId = @ViagemId;";
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ViagemId", viagemId);
            using var leitor = comando.ExecuteReader();
            return leitor.Read() ? LerResumo(leitor) : null;
        }

        public List<ResumoViagem> ListarResumos()
        {
            var resumos = new List<ResumoViagem>();
            using var conexao = Conexao.Abrir();
            const string sql = @"
                SELECT ViagemId, Nome, Destino, QuantidadeParticipantes,
                       TotalReceitas, TotalDespesas, Saldo
                FROM vw_ResumoViagem
                ORDER BY ViagemId DESC;";
            using var comando = new MySqlCommand(sql, conexao);
            using var leitor = comando.ExecuteReader();
            while (leitor.Read()) resumos.Add(LerResumo(leitor));
            return resumos;
        }

        private static ResumoViagem LerResumo(MySqlDataReader leitor)
        {
            return new ResumoViagem
            {
                ViagemId = leitor.GetInt32("ViagemId"),
                Nome = leitor.GetString("Nome"),
                Destino = leitor.GetString("Destino"),
                QuantidadeParticipantes = leitor.GetInt32("QuantidadeParticipantes"),
                TotalReceitas = leitor.GetDecimal("TotalReceitas"),
                TotalDespesas = leitor.GetDecimal("TotalDespesas"),
                Saldo = leitor.GetDecimal("Saldo")
            };
        }
    }
}
