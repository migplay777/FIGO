using FIGO.backend;

static void Rejeita(string nome, string senha)
{
    try { UsuarioService.ValidarCadastro(nome, senha); }
    catch (ArgumentException) { return; }
    throw new Exception("Cadastro inválido foi aceito: " + nome);
}
UsuarioService.ValidarCadastro("miguel", "Abc1234!");
UsuarioService.ValidarCadastro("Miguel Silva", "senha123#");
Rejeita("", "Abc1234!");
Rejeita("ab", "Abc1234!");
Rejeita(new string('a', 51), "Abc1234!");
Rejeita("miguel", "Abc12!");
Rejeita("miguel", "abcdefgh!");
Rejeita("miguel", "1234567!");
Rejeita("miguel", "Abcd1234");
Rejeita("miguel", "Abcd123 ");
Rejeita("miguel", new string('a', 129) + "1!");
Console.WriteLine("Validações de usuário e senha passaram.");

// Execute apenas em um banco de testes preparado com database/001_usuarios.sql.
if (Environment.GetEnvironmentVariable("FIGO_RUN_DB_TESTS") == "1")
{
    var service = new UsuarioService();
    string nome = "teste_" + Guid.NewGuid().ToString("N");
    const string senha = "Teste123!";
    try
    {
        await service.CadastrarAsync(nome, senha);
        var autenticado = await service.EntrarAsync(nome, senha);
        if (autenticado?.Nome != nome) throw new Exception("Login válido falhou.");
        if (await service.EntrarAsync(nome, "Errada123!") != null) throw new Exception("Senha incorreta aceita.");
        if (await service.EntrarAsync("ausente_" + Guid.NewGuid().ToString("N"), senha) != null) throw new Exception("Usuário inexistente aceito.");
        if (await service.EntrarAsync("' OR 1=1 --", senha) != null) throw new Exception("Entrada SQL aceita.");
        bool duplicado = false;
        try { await service.CadastrarAsync(nome.ToUpperInvariant(), senha); }
        catch (ArgumentException) { duplicado = true; }
        if (!duplicado) throw new Exception("Usuário duplicado aceito.");
        Console.WriteLine("Cadastro e autenticação MySQL passaram.");
    }
    finally
    {
        using var conexao = Conexao.Abrir();
        using var cmd = new MySqlConnector.MySqlCommand("DELETE FROM Usuarios WHERE NomeUsuario = @nome", conexao);
        cmd.Parameters.AddWithValue("@nome", nome);
        cmd.ExecuteNonQuery();
    }
}
