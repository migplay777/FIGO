using FIGO.backend;
using MySqlConnector;

internal static class ParticipantesAuditoriaTests
{
    private static void Confere(bool ok, string erro) { if (!ok) throw new Exception(erro); }
    private static void Rejeita(Action acao)
    {
        try { acao(); } catch (ArgumentException) { return; }
        throw new Exception("Dados inválidos de participante foram aceitos.");
    }
    public static void Validacoes()
    {
        var p = new Participante { NomeCompleto = "  Maria de Teste  " };
        ValidacaoParticipantes.Pessoa(p);
        Confere(p.NomeCompleto == "Maria de Teste" && p.Cpf == null, "Nome/CPF opcional inválido.");
        p.Cpf = "529.982.247-25"; ValidacaoParticipantes.Pessoa(p);
        Confere(p.Cpf == "52998224725" && p.CpfMascarado == "***.***.***-25", "CPF não normalizado ou máscara incorreta.");
        Rejeita(() => ValidacaoParticipantes.Pessoa(new() { NomeCompleto = " " }));
        Rejeita(() => ValidacaoParticipantes.Pessoa(new() { NomeCompleto = new string('a', 151) }));
        Rejeita(() => ValidacaoParticipantes.Pessoa(new() { NomeCompleto = "Nome", Cpf = "11111111111" }));
        Rejeita(() => ValidacaoParticipantes.Pessoa(new() { NomeCompleto = "Nome", Cpf = "52998224724" }));
        ValidacaoParticipantes.Vinculo(1, null, null); ValidacaoParticipantes.Vinculo(1, 0, 1); ValidacaoParticipantes.Vinculo(1, 130, null);
        Rejeita(() => ValidacaoParticipantes.Vinculo(0, null, null));
        Rejeita(() => ValidacaoParticipantes.Vinculo(1, -1, null));
        Rejeita(() => ValidacaoParticipantes.Vinculo(1, 131, null));
        Rejeita(() => ValidacaoParticipantes.Vinculo(1, null, 0));
        var audit = new AuditoriaService();
        Rejeita(() => audit.Listar(DateTime.Today, DateTime.Today.AddDays(1)));
        Rejeita(() => audit.Listar(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "Usuarios; DROP TABLE Viagem"));
        Console.WriteLine("Participantes: campos opcionais, CPF, idade e filtros de auditoria passaram.");
    }

    public static async Task IntegracaoAsync()
    {
        var builder = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION"));
        if (builder.Database != "figo_test") throw new Exception("Execute estes testes somente em figo_test.");
        Confere(Numero("SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'") == 15, "O banco deve conter exatamente 15 tabelas.");
        Confere(Numero("SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME LIKE 'Figo_Audit_%'") == 36, "Cobertura de triggers incompleta.");
        var s = new ParticipantesService(); var v = new ViagemServices(); var audit = new AuditoriaService();
        string tag = Guid.NewGuid().ToString("N")[..10];
        await new UsuarioService().CadastrarAsync("audit_" + tag, "Teste123!");
        var usuario = await new UsuarioService().EntrarAsync("audit_" + tag, "Teste123!") ?? throw new Exception("Login do autor falhou.");
        ContextoAuditoria.DefinirUsuario(usuario);
        var viagem1 = new Viagem("Missão " + tag, "Campinas", DateTime.Today, DateTime.Today, 25);
        var viagem2 = new Viagem("Retorno " + tag, "Campinas", DateTime.Today, DateTime.Today, 0);
        v.Adicionar(viagem1); v.Adicionar(viagem2);
        Confere(v.BuscarPorId(viagem1.Id)?.Nome == viagem1.Nome, "Trigger alterou LAST_INSERT_ID da viagem.");
        Confere(s.ListarPorViagem(viagem1.Id).Count == 0, "Viagem exigiu participantes nominais.");
        int funcao = s.AdicionarFuncao("Apoio " + tag);
        Rejeita(() => s.AdicionarFuncao("APOIO " + tag));
        var pessoa = new Participante { NomeCompleto = "Maria de Teste " + tag, Cpf = "529.982.247-25" };
        int vinculo = s.Adicionar(viagem1.Id, pessoa, null, null);
        Confere(pessoa.Id > 0 && s.ListarPorViagem(viagem1.Id).Single().Id == vinculo, "Identificador incorreto após trigger.");
        var primeira = s.ListarPorViagem(viagem1.Id).Single();
        Confere(primeira.Cpf == "52998224725" && primeira.Idade == null && primeira.FuncaoParticipanteId == null, "Opcionais não persistiram.");
        s.Adicionar(viagem2.Id, new() { Id = pessoa.Id }, 32, funcao);
        Rejeita(() => s.Adicionar(viagem1.Id, new() { Id = pessoa.Id }, 30, null));
        Rejeita(() => s.Adicionar(viagem1.Id, new() { NomeCompleto = "Duplicada", Cpf = pessoa.Cpf }, null, null));
        long pessoasAntes = Numero("SELECT COUNT(*) FROM Participante"); long logsAntes = Numero("SELECT COUNT(*) FROM LogAuditoria");
        Rejeita(() => s.Adicionar(viagem1.Id, new() { NomeCompleto = "Pessoa sem vínculo " + tag }, 20, int.MaxValue));
        Confere(Numero("SELECT COUNT(*) FROM Participante") == pessoasAntes && Numero("SELECT COUNT(*) FROM LogAuditoria") == logsAntes, "Falha deixou pessoa ou log parcial.");
        primeira.NomeCompleto = "Maria atualizada " + tag; primeira.Idade = 31; primeira.FuncaoParticipanteId = funcao; s.Editar(primeira);
        var segunda = s.ListarPorViagem(viagem2.Id).Single();
        Confere(segunda.NomeCompleto == primeira.NomeCompleto && segunda.Idade == 32, "Identidade compartilhada ou idade por viagem incorreta.");
        Confere(v.BuscarPorId(viagem1.Id)?.QuantidadeParticipantes == 25, "Lista nominal sobrescreveu quantidade planejada.");
        logsAntes = Numero("SELECT COUNT(*) FROM LogAuditoria"); s.Editar(primeira);
        Confere(Numero("SELECT COUNT(*) FROM LogAuditoria") == logsAntes, "Edição sem mudanças gerou log.");
        var registros = audit.Listar(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "Participante", 2, usuario.Nome, tamanhoPagina: 100);
        var alteracao = registros.Registros.Single(x => x.RegistroId == pessoa.Id);
        Confere(audit.Detalhes(alteracao.Id).Any(x => x.Campo == "NomeCompleto" && x.ValorAnterior == "Maria de Teste " + tag && x.ValorNovo == primeira.NomeCompleto), "Diferença anterior/nova não registrada.");
        Confere(Numero("SELECT COUNT(*) FROM DetalheLogAuditoria WHERE Campo IN ('SenhaHash','Salt','Iteracoes') OR ValorAnterior LIKE '%52998224725%' OR ValorNovo LIKE '%52998224725%'") == 0, "Auditoria vazou credenciais ou CPF completo.");
        Confere(Numero("SELECT COUNT(*) FROM DetalheLogAuditoria WHERE Campo='Cpf' AND ValorNovo='***25'") > 0, "CPF não mascarado no banco de logs.");
        // Mesmo CPF mascarado: o trigger deve comparar o conteúdo original.
        Executar($"UPDATE Participante SET Cpf='00000000025' WHERE Id={pessoa.Id}");
        Confere(Numero($"SELECT COUNT(*) FROM LogAuditoria WHERE Entidade='Participante' AND RegistroId={pessoa.Id} AND TipoEventoAuditoriaId=2") == 2, "Alteração com mesmo final do CPF foi ignorada.");
        s.Editar(primeira); // Restaura o CPF válido pelos serviços.
        try { v.Excluir(viagem1.Id); throw new Exception("Viagem vinculada foi excluída."); } catch (MySqlException ex) when (ex.Number == 1451) { }
        logsAntes = Numero("SELECT COUNT(*) FROM LogAuditoria");
        using (var c = Conexao.Abrir())
        using (var tx = c.BeginTransaction())
        {
            using var cmd = new MySqlCommand("UPDATE Participante SET NomeCompleto='A descartar' WHERE Id=@id", c, tx);
            cmd.Parameters.AddWithValue("@id", pessoa.Id); cmd.ExecuteNonQuery(); tx.Rollback();
        }
        Confere(Numero("SELECT COUNT(*) FROM LogAuditoria") == logsAntes && s.ListarPorViagem(viagem1.Id).Single().NomeCompleto == primeira.NomeCompleto, "Rollback não desfez dados e logs juntos.");
        // Consulta paginada sem duplicações em um conjunto estável.
        var pagina1 = audit.Listar(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), autor: usuario.Nome, tamanhoPagina: 2);
        var pagina2 = audit.Listar(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), autor: usuario.Nome, pagina: 2, tamanhoPagina: 2);
        Confere(pagina1.Total >= 4 && !pagina1.Registros.Select(x => x.Id).Intersect(pagina2.Registros.Select(x => x.Id)).Any(), "Paginação repetiu eventos.");
        // Alterações financeiras existentes também passam pelos triggers.
        var despesa = new Despesa { ViagemId = viagem1.Id, Descricao = "Despesa auditada", Data = DateTime.Today, Valor = 10,
            CategoriaId = new CadastrosFinanceirosService().Listar(TipoCadastroFinanceiro.CategoriaDespesa).First().Id };
        var ds = new DespesaService(); ds.Adicionar(despesa); despesa.Valor = 12; ds.Editar(despesa); ds.Excluir(despesa.Id);
        Confere(Numero($"SELECT COUNT(*) FROM LogAuditoria WHERE Entidade='Despesa' AND RegistroId={despesa.Id}") == 3, "CRUD financeiro sem trilha completa.");
        s.RemoverDaViagem(vinculo, viagem1.Id);
        Confere(s.ListarPessoas().Any(x => x.Id == pessoa.Id) && s.ListarPorViagem(viagem2.Id).Count == 1, "Remoção apagou pessoa ou outro vínculo.");
        s.RemoverDaViagem(segunda.Id, viagem2.Id);
        Executar($"DELETE FROM Participante WHERE Id={pessoa.Id}");
        v.Excluir(viagem1.Id); v.Excluir(viagem2.Id);
        // Reutilização do pool não pode manter o autor anterior.
        ContextoAuditoria.DefinirUsuario(null);
        int semAutor = s.AdicionarFuncao("Sem autor " + tag);
        Confere(Numero($"SELECT COUNT(*) FROM LogAuditoria WHERE Entidade='FuncaoParticipante' AND RegistroId={semAutor} AND UsuarioId IS NULL") == 1, "Pool herdou identidade antiga.");
        using (var direto = new MySqlConnection(builder.ConnectionString))
        {
            direto.Open(); using var cmd = new MySqlCommand("INSERT INTO FuncaoParticipante(Nome) VALUES(@nome)", direto);
            cmd.Parameters.AddWithValue("@nome", "SQL direto " + tag); cmd.ExecuteNonQuery();
            Confere(Numero($"SELECT COUNT(*) FROM LogAuditoria WHERE Entidade='FuncaoParticipante' AND RegistroId={cmd.LastInsertedId} AND UsuarioId IS NULL AND Origem='SQL direto'") == 1, "SQL externo não foi auditado.");
        }
        Executar($"DELETE FROM Usuarios WHERE Id={usuario.Id}");
        Confere(Numero($"SELECT COUNT(*) FROM LogAuditoria WHERE NomeUsuario='audit_{tag}' AND UsuarioId IS NULL") > 0, "Exclusão de usuário apagou identificação histórica.");
        Console.WriteLine("MySQL: 15 tabelas, 36 triggers, participantes opcionais, reutilização, CPF, transações, auditoria financeira, privacidade, pool e paginação passaram.");
    }

    public static void InstalacaoVazia()
    {
        var b = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION"));
        if (b.Database != "figo") throw new Exception("Smoke test de instalação exige banco figo isolado do CI.");
        Confere(Numero("SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE'") == 15, "Instalador não criou as 15 tabelas.");
        Confere(Numero("SELECT (SELECT COUNT(*) FROM Viagem)+(SELECT COUNT(*) FROM Receita)+(SELECT COUNT(*) FROM Despesa)+(SELECT COUNT(*) FROM Participante)") == 0, "Instalador inseriu dados fictícios.");
        var v = new Viagem("Instalação teste", "Campinas", DateTime.Today, DateTime.Today, 0); var vs = new ViagemServices(); vs.Adicionar(v);
        Confere(Numero($"SELECT COUNT(*) FROM LogAuditoria WHERE Entidade='Viagem' AND RegistroId={v.Id}") == 1, "Instalação não habilitou auditoria.");
        vs.Excluir(v.Id);
        Console.WriteLine("Instalação completa vazia e repetível passou.");
    }
    private static long Numero(string sql)
    {
        using var c = Conexao.Abrir(); using var cmd = new MySqlCommand(sql, c); return Convert.ToInt64(cmd.ExecuteScalar());
    }
    private static void Executar(string sql)
    {
        using var c = Conexao.Abrir(); using var cmd = new MySqlCommand(sql, c); cmd.ExecuteNonQuery();
    }
}
