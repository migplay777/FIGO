using System.Security.Cryptography;
using MySqlConnector;

namespace FIGO.backend;

public sealed record UsuarioAutenticado(int Id, string Nome);

public sealed class UsuarioService
{
    private const int Iteracoes = 600000;
    public static void ValidarCadastro(string usuario, string senha)
    {
        if (string.IsNullOrWhiteSpace(usuario) || usuario.Trim().Length < 3 || usuario.Trim().Length > 50)
            throw new ArgumentException("O usuário deve ter entre 3 e 50 caracteres.");
        if (senha.Length < 8 || senha.Length > 128 || !senha.Any(char.IsLetter) ||
            !senha.Any(char.IsDigit) || !senha.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            throw new ArgumentException("A senha deve ter de 8 a 128 caracteres, com letra, número e sinal especial.");
    }

    public async Task CadastrarAsync(string usuario, string senha)
    {
        ValidarCadastro(usuario, senha);
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = await Task.Run(() => Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, 32));
        using var conexao = await Conexao.AbrirAsync();
        using var cmd = new MySqlCommand("INSERT INTO Usuarios (NomeUsuario, SenhaHash, Salt, Iteracoes) VALUES (@nome, @hash, @salt, @iteracoes)", conexao);
        cmd.Parameters.AddWithValue("@nome", usuario.Trim());
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@salt", salt);
        cmd.Parameters.AddWithValue("@iteracoes", Iteracoes);
        try { await cmd.ExecuteNonQueryAsync(); }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new ArgumentException("Este usuário já existe. Escolha outro nome."); }
    }

    public async Task<UsuarioAutenticado?> EntrarAsync(string usuario, string senha)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(senha))
            throw new ArgumentException("Informe usuário e senha.");
        if (usuario.Trim().Length > 50 || senha.Length > 128) return null;
        using var conexao = await Conexao.AbrirAsync();
        using var cmd = new MySqlCommand("SELECT Id, NomeUsuario, SenhaHash, Salt, Iteracoes FROM Usuarios WHERE NomeUsuario = @nome", conexao);
        cmd.Parameters.AddWithValue("@nome", usuario.Trim());
        using var reader = await cmd.ExecuteReaderAsync();
        bool existe = await reader.ReadAsync();
        int id = existe ? reader.GetInt32("Id") : 0;
        string nome = existe ? reader.GetString("NomeUsuario") : "";
        byte[] hash = existe ? (byte[])reader["SenhaHash"] : new byte[32];
        byte[] salt = existe ? (byte[])reader["Salt"] : new byte[16];
        int iteracoes = existe ? reader.GetInt32("Iteracoes") : Iteracoes;
        if (iteracoes < 100000 || iteracoes > 2000000) throw new InvalidOperationException("Cadastro de usuário inválido.");
        byte[] informado = await Task.Run(() => Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, 32));
        bool confere = CryptographicOperations.FixedTimeEquals(hash, informado);
        return existe && confere ? new UsuarioAutenticado(id, nome) : null;
    }
}
