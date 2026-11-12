using FIGO.backend;
using MySqlConnector;

internal static class ViagemTests
{
    private static void Confere(bool condicao, string mensagem)
    {
        if (!condicao) throw new Exception(mensagem);
    }

    private static void Rejeita(Action acao)
    {
        try { acao(); }
        catch (ArgumentException) { return; }
        throw new Exception("Uma viagem inválida foi aceita.");
    }

    public static void Validacoes()
    {
        var viagem = new Viagem("  Missão de teste  ", "  Campinas  ",
            new DateTime(2028, 2, 28, 12, 0, 0), new DateTime(2028, 3, 1, 8, 0, 0), 150);
        Confere(viagem.Nome == "Missão de teste" && viagem.Destino == "Campinas", "Espaços externos não foram normalizados.");
        Confere(viagem.CalcularDuracaoEmDias(true) == 3 && viagem.CalcularDuracaoEmDias(false) == 2,
            "Duração com ano bissexto e contagem inclusiva incorreta.");
        viagem.DataFim = viagem.DataInicio;
        Confere(viagem.CalcularDuracaoEmDias(true) == 1 && viagem.CalcularDuracaoEmDias(false) == 0,
            "Duração de uma viagem de um dia incorreta.");
        viagem.QuantidadeParticipantes = 0; viagem.Validar();
        viagem.QuantidadeParticipantes = Viagem.MaximoParticipantes; viagem.Validar();
        viagem.QuantidadeParticipantes++; Rejeita(viagem.Validar);
        viagem.QuantidadeParticipantes = -1; Rejeita(viagem.Validar);
        viagem.QuantidadeParticipantes = 150;
        viagem.DataFim = viagem.DataInicio.AddDays(-1);
        Rejeita(() => viagem.CalcularDuracaoEmDias());
        // O serviço deve rejeitar o objeto antes de abrir uma conexão.
        Rejeita(() => new ViagemServices().Adicionar(viagem));
        Rejeita(() => new ViagemServices().Editar(viagem));
        Rejeita(() => new Viagem("  ", "Campinas", DateTime.Today, DateTime.Today, 0));
        Rejeita(() => new Viagem("Teste", new string('x', 256), DateTime.Today, DateTime.Today, 0));
        Rejeita(() => new Viagem("Teste", "Campinas", default, default, 0));
        var semId = new Viagem("Teste", "Campinas", DateTime.Today, DateTime.Today, 0);
        Rejeita(() => new ViagemServices().Editar(semId));
        Console.WriteLine("Viagens: período, duração, limites e validação antes da persistência passaram.");
    }

    public static void Integracao()
    {
        var config = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION"));
        if (config.Database != "figo_test") throw new Exception("Execute testes de viagem somente em figo_test.");
        var service = new ViagemServices();
        var viagem = new Viagem("LP2 " + Guid.NewGuid().ToString("N")[..8], "Campinas",
            DateTime.Today, DateTime.Today.AddDays(1), 150);
        try
        {
            service.Adicionar(viagem);
            var salva = service.BuscarPorId(viagem.Id);
            Confere(salva?.Nome == viagem.Nome && salva.QuantidadeParticipantes == 150,
                "A inclusão da viagem não preservou os dados.");
            viagem.Destino = "Valinhos";
            viagem.DataFim = viagem.DataInicio.AddDays(2);
            viagem.QuantidadeParticipantes = 200;
            service.Editar(viagem);
            var editada = service.BuscarPorId(viagem.Id);
            Confere(editada?.Destino == "Valinhos" && editada.QuantidadeParticipantes == 200 && editada.CalcularDuracaoEmDias(true) == 3,
                "A edição da viagem não foi persistida.");
            service.Excluir(viagem.Id);
            Confere(service.BuscarPorId(viagem.Id) is null, "A exclusão da viagem não foi persistida.");
            Console.WriteLine("Viagens MySQL: inclusão, leitura, atualização e exclusão passaram.");
        }
        finally
        {
            if (viagem.Id > 0) service.Excluir(viagem.Id);
        }
    }
}
