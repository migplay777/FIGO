using MySqlConnector;

namespace FIGO.backend
{
    public static class Conexao
    {
        // TODO: ajuste Server, Uid e Pwd para o seu ambiente.
        private const string ConnectionString =
            Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION") ?? throw new InvalidOperationException("Configure a variável de ambiente FIGO_DB_CONNECTION.");

        public static MySqlConnection Abrir()
        {
            var conexao = new MySqlConnection(ConnectionString);
            conexao.Open();
            return conexao;
        }
    }
}
