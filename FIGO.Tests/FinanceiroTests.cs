using FIGO.backend;
using MySqlConnector;

internal static class FinanceiroTests
{
    private static void Confere(bool condicao, string mensagem) { if (!condicao) throw new Exception(mensagem); }
    private static void Rejeita(Action acao)
    {
        try { acao(); } catch (ArgumentException) { return; }
        throw new Exception("Operação financeira inválida foi aceita.");
    }
    public static void Validacoes()
    {
        Confere(ValidacaoFinanceira.CpfValido("529.982.247-25"), "CPF com máscara rejeitado.");
        Confere(!ValidacaoFinanceira.CpfValido("52998224724") && !ValidacaoFinanceira.CpfValido("11111111111"), "CPF inválido aceito.");
        Confere(ValidacaoFinanceira.CnpjValido("11.222.333/0001-81"), "CNPJ numérico rejeitado.");
        // Exemplo oficial Receita Federal / Serpro.
        Confere(ValidacaoFinanceira.CnpjValido("12.ABC.345/01DE-35"), "CNPJ alfanumérico rejeitado.");
        Confere(!ValidacaoFinanceira.CnpjValido("12.ABC.345/01DE-34") && !ValidacaoFinanceira.CnpjValido("00000000000000"), "CNPJ inválido aceito.");
        var pessoa = new Pagador { Nome = "Pessoa sem CPF", Tipo = "PF" };
        ValidacaoFinanceira.Pagador(pessoa);
        Confere(pessoa.Documento == null, "CPF opcional alterado.");
        var empresa = new Pagador { Nome = "Empresa teste", Tipo = "PJ", Documento = "12.abc.345/01de-35" };
        ValidacaoFinanceira.Pagador(empresa);
        Confere(empresa.Documento == "12ABC34501DE35", "CNPJ não normalizado.");
        Rejeita(() => ValidacaoFinanceira.Pagador(new Pagador { Nome = "Empresa", Tipo = "PJ" }));
        Rejeita(() => ValidacaoFinanceira.Pagador(new Pagador { Nome = "Pessoa", Tipo = "PF", Documento = "123" }));
        Rejeita(() => ValidacaoFinanceira.Conta(new ContaFinanceira { Nome = "Conta sem banco" }));
        ValidacaoFinanceira.Conta(new ContaFinanceira { Nome = "Caixa local", Tipo = "Caixa" });
        var receita = new Receita { ViagemId = 1, Descricao = "Teste", Data = DateTime.Today, Valor = 15.25m,
            OrigemId = 1, PagadorId = 1, FormaPagamentoId = 1, ContaFinanceiraId = 1 };
        ValidacaoFinanceira.Receita(receita);
        receita.PagadorId = null; Rejeita(() => ValidacaoFinanceira.Receita(receita));
        receita.PagadorId = 1; receita.Valor = 0; Rejeita(() => ValidacaoFinanceira.Receita(receita));
        receita.Valor = 1.234m; Rejeita(() => ValidacaoFinanceira.Receita(receita));
        Rejeita(() => ValidacaoFinanceira.Despesa(new Despesa { ViagemId = 1, Descricao = "Teste", Data = DateTime.Today, Valor = 10 }));
        Confere(BuscaFinanceira.Corresponde("Alimentação da equipe", "MENTA"), "Busca por trecho/sem acento falhou.");
        Confere(BuscaFinanceira.Corresponde("Empresa Exemplo 12ABC34501DE35", "empresa 12abc"), "Busca com nome e documento falhou.");
        Confere(!BuscaFinanceira.Corresponde("Transporte", "Hotel"), "Busca retornou item sem correspondência.");
        Console.WriteLine("Validação financeira, CPF/CNPJ e busca passaram.");
    }
    public static void Integracao()
    {
        var builder = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION"));
        if (builder.Database != "figo_test") throw new Exception("Execute testes financeiros somente em figo_test.");
        var cadastros = new CadastrosFinanceirosService();
        var receitas = new ReceitaService(); var despesas = new DespesaService();
        var antigo = receitas.ListarPorViagem(1).Single();
        var antiga = despesas.ListarPorViagem(1).Single();
        Confere(antigo.Origem == "Igreja antiga" && antigo.Valor == 100 && antigo.OrigemId > 0 && antigo.PagadorId == null, "Migração alterou receita legada.");
        Confere(antiga.Categoria == "Transporte legado" && antiga.Valor == 20 && antiga.CategoriaId > 0, "Migração alterou despesa legada.");
        Confere(cadastros.Listar(TipoCadastroFinanceiro.OrigemReceita).Count(x => x.Nome == "Doação") == 1, "Migração duplicou origens.");
        Confere(new ResumoService().ObterResumo(1)?.Saldo == 80, "Migração alterou saldo anterior.");
        string sufixo = Guid.NewGuid().ToString("N")[..8];
        var viagem = new Viagem { Nome = "Teste " + sufixo, Destino = "Campinas", DataInicio = DateTime.Today, DataFim = DateTime.Today, QuantidadeParticipantes = 1 };
        new ViagemServices().Adicionar(viagem);
        var pessoa = new Pagador { Nome = "Pessoa " + sufixo, Tipo = "PF" };
        int pessoaId = cadastros.AdicionarPagador(pessoa);
        int empresaId = cadastros.AdicionarPagador(new Pagador { Nome = "Empresa " + sufixo, Tipo = "PJ", Documento = "12.ABC.345/01DE-35" });
        Rejeita(() => cadastros.AdicionarPagador(new Pagador { Nome = "Duplicada", Tipo = "PJ", Documento = "12ABC34501DE35" }));
        Confere(cadastros.ListarPagadores().Single(p => p.Id == pessoaId).Documento == null, "CPF não opcional no banco.");
        int origemId = cadastros.Adicionar(TipoCadastroFinanceiro.OrigemReceita, "Campanha " + sufixo);
        int categoriaId = cadastros.Adicionar(TipoCadastroFinanceiro.CategoriaDespesa, "Materiais " + sufixo);
        Rejeita(() => cadastros.Adicionar(TipoCadastroFinanceiro.CategoriaDespesa, "MATERIAIS " + sufixo));
        int formaId = cadastros.Adicionar(TipoCadastroFinanceiro.FormaPagamento, "PIX " + sufixo);
        int contaId = cadastros.AdicionarConta(new ContaFinanceira { Nome = "Conta " + sufixo, Banco = "Banco de teste", Agencia = "0001", Numero = sufixo, Titular = "Igreja de teste" });
        Rejeita(() => cadastros.AdicionarConta(new ContaFinanceira { Nome = "Duplicada " + sufixo, Banco = "Banco de teste", Agencia = "0001", Numero = sufixo, Titular = "Outra" }));
        var r = new Receita { ViagemId = viagem.Id, Descricao = "Teste ' OR 1=1 --", Valor = 125.50m, Data = DateTime.Today,
            OrigemId = origemId, PagadorId = empresaId, FormaPagamentoId = formaId, ContaFinanceiraId = contaId, ReferenciaPagamento = "E123-teste" };
        receitas.Adicionar(r);
        var salvo = receitas.ListarPorViagem(viagem.Id).Single();
        Confere(salvo.PagadorId == empresaId && salvo.Origem == "Campanha " + sufixo && salvo.FormaPagamentoId == formaId && salvo.ContaFinanceiraId == contaId && salvo.ReferenciaPagamento == "E123-teste" && salvo.Descricao == r.Descricao, "Receita não preservou os dados estruturados.");
        r.PagadorId = pessoaId; r.Valor = 200; receitas.Editar(r); receitas.Editar(r);
        Confere(receitas.ListarPorViagem(viagem.Id).Single().PagadorId == pessoaId, "Edição da receita falhou.");
        r.PagadorId = int.MaxValue;
        Rejeita(() => receitas.Adicionar(r)); Rejeita(() => receitas.Editar(r));
        var d = new Despesa { ViagemId = viagem.Id, Descricao = "Materiais", Valor = 50, Data = DateTime.Today, CategoriaId = categoriaId };
        despesas.Adicionar(d);
        Confere(despesas.ListarPorViagem(viagem.Id).Single().CategoriaId == categoriaId, "Categoria não persistiu.");
        d.Valor = 60; despesas.Editar(d); despesas.Editar(d);
        Confere(despesas.ObterPorCategoria(viagem.Id).Single().Total == 60, "Gráfico por categoria não preservado.");
        Confere(new ResumoService().ObterResumo(viagem.Id)?.Saldo == 140, "Relatório financeiro diverge.");
        receitas.Excluir(salvo.Id); despesas.Excluir(d.Id);
        Confere(receitas.ListarPorViagem(viagem.Id).Count == 0 && despesas.ListarPorViagem(viagem.Id).Count == 0, "Exclusão de lançamentos falhou.");
        Console.WriteLine("Migração repetida, dados legados, cadastros, duplicatas, CRUD e relatórios MySQL passaram.");
    }
}
