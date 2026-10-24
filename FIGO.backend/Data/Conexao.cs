using MySqlConnector;

namespace FIGO.backend;

public static class Conexao
{
    private static string ConnectionString => Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION")
        ?? throw new InvalidOperationException("Configure a variável de ambiente FIGO_DB_CONNECTION.");

    public static MySqlConnection Abrir()
    {
        var conexao = new MySqlConnection(ConnectionString);
        try { conexao.Open(); return conexao; }
        catch { conexao.Dispose(); throw; }
    }
        // TODO: ajuste Server, Uid e Pwd para o seu ambiente.
       private const string ConnectionString =
    "Server = 'localhost'; Database= 'figo'; Uid='root'; Pwd='mi330guel';";

    public static async Task<MySqlConnection> AbrirAsync()
    {
        var conexao = new MySqlConnection(ConnectionString);
        try { await conexao.OpenAsync(); return conexao; }
        catch { conexao.Dispose(); throw; }
    }
}
