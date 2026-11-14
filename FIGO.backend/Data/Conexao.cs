using MySqlConnector;

namespace FIGO.backend;

public static class Conexao
{
    private static string ConnectionString => Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION")
        ?? throw new InvalidOperationException("Configure a variável de ambiente FIGO_DB_CONNECTION.");

    public static MySqlConnection Abrir()
    {
        var conexao = Nova();
        try { conexao.Open(); using var contexto = CriarContexto(conexao); contexto.ExecuteNonQuery(); return conexao; }
        catch { conexao.Dispose(); throw; }
    }
    public static async Task<MySqlConnection> AbrirAsync()
    {
        var conexao = Nova();
        try { await conexao.OpenAsync(); using var contexto = CriarContexto(conexao); await contexto.ExecuteNonQueryAsync(); return conexao; }
        catch { conexao.Dispose(); throw; }
    }
    private static MySqlConnection Nova() => new(new MySqlConnectionStringBuilder(ConnectionString) { AllowUserVariables = true }.ConnectionString);
    private static MySqlCommand CriarContexto(MySqlConnection conexao)
    {
        // Reinicializa inclusive sem login, evitando herdar o autor do pool.
        var cmd = new MySqlCommand("SET @figo_usuario_id = @autor, @figo_origem = 'FIGO Desktop'", conexao);
        cmd.Parameters.AddWithValue("@autor", ContextoAuditoria.Usuario?.Id);
        return cmd;
    }
}
