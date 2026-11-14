using FIGO.backend;
using MySqlConnector;

internal static class PlanejamentoTests
{
    private static readonly byte[] pdf="%PDF-1.7\nCONTEUDO_SENTINELA_PRIVADO\n%%EOF"u8.ToArray();
    private static void Confere(bool ok,string erro){if(!ok)throw new Exception(erro);}
    private static void Rejeita(Action acao)
    {
        try{acao();}catch(ArgumentException){return;}catch(MySqlException ex) when(ex.Number is 1062 or 1451 or 1452 or 1644 or 3819){return;}
        throw new Exception("Operação inválida foi aceita.");
    }
    public static void Validacoes()
    {
        Rejeita(()=>ValidacaoPlanejamento.Valor(0));Rejeita(()=>ValidacaoPlanejamento.Valor(-1));Rejeita(()=>ValidacaoPlanejamento.Valor(1.234m));
        Rejeita(()=>ValidacaoPlanejamento.Valor(1000000000m));Rejeita(()=>ValidacaoPlanejamento.Conta(new()));
        Confere(AnexosService.Validar("../../comprovante.pdf",pdf).Nome=="comprovante.pdf","Anexo preservou caminho externo.");
        Rejeita(()=>AnexosService.Validar("arquivo.exe",pdf));Rejeita(()=>AnexosService.Validar("arquivo.jpg",pdf));
        Rejeita(()=>AnexosService.Validar("arquivo.pdf",[]));Rejeita(()=>AnexosService.Validar("grande.pdf",new byte[AnexosService.LimiteBytes+1]));
        var meta=new ResumoMeta(1,"Meta",100,null,150);Confere(meta.Falta==0&&meta.Percentual==150&&meta.Situacao=="Concluída","Meta acima de 100% incorreta.");
        Confere(new LinhaOrcamento(1,1,"Transporte",100,125).Diferenca==-25,"Orçamento ocultou excesso de gasto.");
        Console.WriteLine("Planejamento: validações monetárias, metas, orçamento, anexos/tipo/tamanho/caminho passaram.");
    }
    public static async Task IntegracaoAsync()
    {
        var b=new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FIGO_DB_CONNECTION"));
        if(b.Database!="figo_test")throw new Exception("Execute somente no banco isolado figo_test.");
        var vs=new ViagemServices();var cp=new ContasPagarService();var plano=new PlanejamentoService();var anexos=new AnexosService();var ds=new DespesaService();var rs=new ReceitaService();var cad=new CadastrosFinanceirosService();
        string tag=Guid.NewGuid().ToString("N")[..8];var v=new Viagem("Planejamento "+tag,"Campinas",DateTime.Today,DateTime.Today,10);vs.Adicionar(v);
        int categoria=cad.Listar(TipoCadastroFinanceiro.CategoriaDespesa).First().Id;
        int item=plano.SalvarItem(v.Id,categoria,500);Confere(plano.SalvarItem(v.Id,categoria,600)==item,"Orçamento duplicou item ao atualizar.");
        Confere(plano.Orcamento(v.Id).Single().Previsto==600,"Orçamento não atualizou previsão.");
        Confere(new ResumoService().ObterResumo(v.Id)?.TotalDespesas==0,"Orçamento criou despesa.");
        var conta=new ContaPagar { ViagemId=v.Id,Descricao="Hospedagem "+tag,Credor="Hotel de teste",CategoriaId=categoria,ValorTotal=100,Vencimento=DateTime.Today.AddDays(-1) };cp.Salvar(conta);
        Confere(cp.Listar(v.Id).Single().Situacao=="Vencida"&&new ResumoService().ObterResumo(v.Id)?.TotalDespesas==0,"Conta aberta afetou caixa ou perdeu vencimento.");
        string chave=Guid.NewGuid().ToString("N");int pago=cp.Pagar(conta.Id,30,DateTime.Today,"PIX teste",chave);
        Confere(cp.Pagar(conta.Id,30,DateTime.Today,"PIX teste",chave)==pago,"Repetir operação duplicou pagamento.");
        Rejeita(()=>cp.Pagar(conta.Id,31,DateTime.Today,"PIX teste",chave));
        Confere(cp.Listar(v.Id).Single().Pago==30&&cp.Listar(v.Id).Single().Saldo==70,"Pagamento parcial calculado incorretamente.");
        Confere(new ResumoService().ObterResumo(v.Id)?.TotalDespesas==30&&plano.Orcamento(v.Id).Single().Realizado==30,"Pagamento não entrou uma vez no financeiro/orçamento.");
        long despesasAntes=Numero("SELECT COUNT(*) FROM Despesa"),logsAntes=Numero("SELECT COUNT(*) FROM LogAuditoria");
        Rejeita(()=>cp.Pagar(conta.Id,71,DateTime.Today,"Excesso",Guid.NewGuid().ToString("N")));
        Confere(Numero("SELECT COUNT(*) FROM Despesa")==despesasAntes&&Numero("SELECT COUNT(*) FROM LogAuditoria")==logsAntes,"Pagamento recusado deixou despesa ou log parcial.");
        var d=ds.ListarPorViagem(v.Id).Single();d.Valor=40;Rejeita(()=>ds.Editar(d));Rejeita(()=>ds.Excluir(d.Id));
        conta.ValorTotal=20;Rejeita(()=>cp.Salvar(conta));conta.ValorTotal=100;
        int segundaCategoria=cad.Listar(TipoCadastroFinanceiro.CategoriaDespesa).First(x=>x.Id!=categoria).Id;
        conta.CategoriaId=segundaCategoria;Rejeita(()=>cp.Salvar(conta));conta.CategoriaId=categoria;
        int anexo=anexos.Adicionar(false,d.Id,"comprovante.pdf",pdf);
        Confere(anexos.Baixar(anexo).Conteudo.SequenceEqual(pdf),"Comprovante não preservou bytes/integridade.");
        Rejeita(()=>anexos.Adicionar(false,d.Id,"copia.pdf",pdf));Rejeita(()=>cp.Estornar(conta.Id,pago));
        Confere(cp.Pagamentos(conta.Id).Count==1&&ds.ListarPorViagem(v.Id).Count==1,"Estorno bloqueado por anexo apagou parcialmente os dados.");
        Confere(Numero("SELECT COUNT(*) FROM DetalheLogAuditoria WHERE Campo='Conteudo' OR ValorNovo LIKE '%CONTEUDO_SENTINELA_PRIVADO%'")==0,"Auditoria copiou conteúdo de documento.");
        anexos.Excluir(anexo,false,d.Id);cp.Estornar(conta.Id,pago);
        Confere(cp.Pagamentos(conta.Id).Count==0&&ds.ListarPorViagem(v.Id).Count==0&&cp.Listar(v.Id).Single().Saldo==100,"Estorno não desfez pagamento e despesa.");
        // Duas solicitações simultâneas de 60 para saldo 100: apenas uma pode vencer.
        var tentativas=await Task.WhenAll(Enumerable.Range(0,2).Select(_=>Task.Run(()=>
        {
            try{cp.Pagar(conta.Id,60,DateTime.Today,"Concorrência",Guid.NewGuid().ToString("N"));return true;}
            catch(ArgumentException){return false;}
        })));
        Confere(tentativas.Count(x=>x)==1&&cp.Listar(v.Id).Single().Pago==60,"Pagamentos concorrentes ultrapassaram saldo.");
        cp.Pagar(conta.Id,40,DateTime.Today,"Quitação",Guid.NewGuid().ToString("N"));Confere(cp.Listar(v.Id).Single().Situacao=="Paga","Quitação não fechou a conta.");
        Rejeita(()=>cp.Excluir(conta.Id,v.Id));
        int pagador=cad.AdicionarPagador(new Pagador { Nome="Doador "+tag,Tipo="PF" });
        var receita=new Receita { ViagemId=v.Id,Descricao="Arrecadação",Valor=200,Data=DateTime.Today,OrigemId=cad.Listar(TipoCadastroFinanceiro.OrigemReceita).First().Id,PagadorId=pagador,FormaPagamentoId=cad.Listar(TipoCadastroFinanceiro.FormaPagamento).First().Id,ContaFinanceiraId=cad.ListarContas().First().Id };rs.Adicionar(receita);
        int meta=plano.SalvarMeta(v.Id,"Meta da viagem",100,null);Confere(plano.Meta(v.Id) is { Percentual:200,Situacao:"Concluída" },"Meta não usou receitas existentes.");
        Confere(plano.SalvarMeta(v.Id,"Meta ampliada",1000,DateTime.Today.AddDays(5))==meta&&plano.Meta(v.Id)?.Falta==800,"Meta duplicada ou falta incorreta.");
        receita.Valor=250;rs.Editar(receita);Confere(plano.Meta(v.Id)?.Percentual==25,"Meta não refletiu alteração da receita.");
        int anexoReceita=anexos.Adicionar(true,receita.Id,"recibo.pdf",pdf);Rejeita(()=>rs.Excluir(receita.Id));anexos.Excluir(anexoReceita,true,receita.Id);
        // Gastos sem previsão continuam visíveis e não somem dos totais.
        var semPrevisao=new Despesa { ViagemId=v.Id,Descricao="Gasto não previsto",Valor=12,Data=DateTime.Today,CategoriaId=segundaCategoria };ds.Adicionar(semPrevisao);
        Confere(plano.Orcamento(v.Id).Single(x=>x.CategoriaId==segundaCategoria).Diferenca==-12,"Orçamento omitiu gasto sem previsão.");
        Confere(new ResumoService().ObterResumo(v.Id)?.Saldo==138,"Saldo duplicou contas, orçamento ou pagamentos.");
        foreach(string entidade in new[]{"OrcamentoViagem","ItemOrcamento","MetaArrecadacao","ContaPagar","PagamentoConta","AnexoLancamento"})
            Confere(Numero($"SELECT COUNT(*) FROM LogAuditoria WHERE Entidade='{entidade}'")>0,"Entidade sem auditoria: "+entidade);
        rs.Excluir(receita.Id);Confere(plano.Meta(v.Id)?.Arrecadado==0,"Meta manteve receita excluída.");plano.ExcluirMeta(v.Id);Confere(plano.Meta(v.Id)==null,"Meta não removida.");
        ds.Excluir(semPrevisao.Id);foreach(var p in cp.Pagamentos(conta.Id))cp.Estornar(conta.Id,p.Id);cp.Excluir(conta.Id,v.Id);plano.ExcluirItem(v.Id,item);
        Confere(plano.Orcamento(v.Id).Count==0,"Remoção deixou previsão ou despesa.");
        Executar($"DELETE FROM OrcamentoViagem WHERE ViagemId={v.Id}");vs.Excluir(v.Id);
        Console.WriteLine("MySQL: anexos, orçamento versus realizado, metas, pagamentos parciais, idempotência, concorrência, estorno atômico e auditoria passaram.");
    }
    private static long Numero(string sql){using var c=Conexao.Abrir();using var cmd=new MySqlCommand(sql,c);return Convert.ToInt64(cmd.ExecuteScalar());}
    private static void Executar(string sql){using var c=Conexao.Abrir();using var cmd=new MySqlCommand(sql,c);cmd.ExecuteNonQuery();}
}
