using MySqlConnector;

namespace FIGO.backend
{
    public static class Conexao
    {
        // TODO: ajuste Server, Uid e Pwd para o seu ambiente.
       private const string ConnectionString =
    "Server = 'localhost'; Database= 'figo'; Uid='root'; Pwd='mi330guel';";

        public static MySqlConnection Abrir()
        {
            var conexao = new MySqlConnection(ConnectionString);
            conexao.Open();
            return conexao;
        }
    }
}
