using FIGO.backend;

namespace FIGO.Desktop;

internal sealed class ContasPagarView : TelaViagemFinanceira
{
    private readonly ContasPagarService service=new();
    private readonly DataGridView grid=Tabela(("Descrição","Descricao",null),("Credor","Credor",null),("Vencimento","Vencimento","dd/MM/yyyy"),("Total","ValorTotal","C2"),("Pago","Pago","C2"),("Saldo","Saldo","C2"),("Situação","Situacao",null));
    private readonly ComboBox filtro=new() { Width=140,DropDownStyle=ComboBoxStyle.DropDownList };
    private List<ContaPagar> contas=[];
    public ContasPagarView():base("Contas a pagar","Cada pagamento gera uma despesa. A conta planejada só afeta o saldo quando há pagamentos.")
    {
        filtro.Items.AddRange(["Todas","Em aberto","Vencidas","Pagas"]);filtro.SelectedIndex=0;filtro.SelectedIndexChanged+=(_,_)=>Exibir();Barra.Controls.Add(filtro);
        Botao("Nova conta",()=>EditarAsync(null));Botao("Editar",async()=>await EditarAsync(Selecionada()),90);
        Botao("Excluir",ExcluirAsync,90);Botao("Pagar",PagarAsync,90);Botao("Pagamentos",HistoricoAsync);Botao("Atualizar",AtualizarAsync,105);Area.Controls.Add(grid);
    }
    private ContaPagar Selecionada()=>grid.CurrentRow?.DataBoundItem as ContaPagar??throw new ArgumentException("Selecione uma conta.");
    protected override async Task AtualizarAsync()
    {
        if(ViagemAtual is not {} id){contas=[];Exibir();return;}
        var dados=await Task.Run(()=>service.Listar(id));if(IsDisposed)return;contas=dados;Exibir();
    }
    private void Exibir()
    {
        grid.DataSource=contas.Where(c=>filtro.SelectedIndex switch { 1=>c.Saldo>0,2=>c.Situacao=="Vencida",3=>c.Saldo<=0,_=>true }).ToList();
        Rodape.Text=$"Total contratado: {contas.Sum(c=>c.ValorTotal):C2}   |   Pago: {contas.Sum(c=>c.Pago):C2}   |   Em aberto: {contas.Sum(c=>c.Saldo):C2}";
    }
    private async Task EditarAsync(ContaPagar? conta)
    {
        int id=ExigirViagem();var categorias=await Task.Run(()=>new CadastrosFinanceirosService().Listar(TipoCadastroFinanceiro.CategoriaDespesa));if(IsDisposed)return;
        using var f=new FormContaPagar(id,categorias,conta);if(f.ShowDialog(this)==DialogResult.OK)await AtualizarAsync();
    }
    private async Task ExcluirAsync()
    {
        var c=Selecionada();if(MessageBox.Show(this,$"Excluir a conta '{c.Descricao}'? Contas com pagamentos exigem estorno antes da exclusão.","FIGO",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        await Task.Run(()=>service.Excluir(c.Id,c.ViagemId));await AtualizarAsync();
    }
    private async Task PagarAsync()
    {
        var c=Selecionada();if(c.Saldo<=0)throw new ArgumentException("Esta conta já está paga.");
        using var f=new FormPagamentoConta(c);if(f.ShowDialog(this)==DialogResult.OK)await AtualizarAsync();
    }
    private async Task HistoricoAsync(){using var f=new FormPagamentosConta(Selecionada());f.ShowDialog(this);await AtualizarAsync();}
}

internal sealed class FormContaPagar : FormCadastroFinanceiro
{
    private readonly TextBox descricao=new() { MaxLength=255 },credor=new() { MaxLength=150 };
    private readonly ComboBox categoria=new() { DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Nome",ValueMember="Id" };
    private readonly NumericUpDown valor=new() { DecimalPlaces=2,Maximum=999999999.99m,ThousandsSeparator=true };
    private readonly DateTimePicker vencimento=new() { Format=DateTimePickerFormat.Short,MinDate=new DateTime(1900,1,1) };
    private readonly int viagem,id;
    public FormContaPagar(int viagem,IReadOnlyList<OpcaoFinanceira> categorias,ContaPagar? original=null):base(original==null?"Nova conta a pagar":"Editar conta a pagar",470)
    {
        this.viagem=viagem;id=original?.Id??0;descricao.Text=original?.Descricao??"";credor.Text=original?.Credor??"";valor.Value=original?.ValorTotal??0;vencimento.Value=original?.Vencimento??DateTime.Today;
        categoria.DataSource=new[]{new OpcaoFinanceira(0,"Selecione uma categoria")}.Concat(categorias).ToList();categoria.SelectedValue=original?.CategoriaId??0;categoria.Enabled=original?.Pago is not >0;
        Campo("Descrição *",descricao);Campo("Credor *",credor);Campo("Categoria *",categoria);Campo("Valor total (R$) *",valor);Campo("Vencimento *",vencimento);
        Nota("Registrar a conta não gera despesa. Com pagamentos, a categoria fica protegida e o total não pode ficar abaixo do que já foi pago.");
    }
    protected override Task<int> GravarAsync()
    {
        var c=new ContaPagar { Id=id,ViagemId=viagem,Descricao=descricao.Text,Credor=credor.Text,CategoriaId=(int?)categoria.SelectedValue??0,ValorTotal=valor.Value,Vencimento=vencimento.Value.Date };
        return Task.Run(()=>new ContasPagarService().Salvar(c));
    }
}
internal sealed class FormPagamentoConta : FormCadastroFinanceiro
{
    private readonly NumericUpDown valor=new() { DecimalPlaces=2,Maximum=999999999.99m,ThousandsSeparator=true };
    private readonly DateTimePicker data=new() { Format=DateTimePickerFormat.Short,MinDate=new DateTime(1900,1,1) };
    private readonly TextBox referencia=new() { MaxLength=100 };
    private readonly int conta;private readonly string operacao=Guid.NewGuid().ToString("N");
    public FormPagamentoConta(ContaPagar c):base("Registrar pagamento",350)
    {
        conta=c.Id;valor.Maximum=Math.Max(0,c.Saldo);valor.Value=Math.Max(0,c.Saldo);Campo("Valor pago (R$) *",valor);Campo("Data *",data);Campo("Referência opcional",referencia);
        Nota($"{c.Descricao}. Saldo informado: {c.Saldo:C2}. Pode pagar parte do saldo. A despesa é gerada automaticamente; não registre outra despesa para este pagamento.");
    }
    protected override Task<int> GravarAsync(){decimal v=valor.Value;DateTime d=data.Value.Date;string r=referencia.Text;return Task.Run(()=>new ContasPagarService().Pagar(conta,v,d,r,operacao));}
}
internal sealed class FormPagamentosConta : Form
{
    private readonly ContasPagarService service=new();private readonly ContaPagar conta;
    private readonly DataGridView grid=TelaViagemFinanceira.Tabela(("Data","Data","dd/MM/yyyy"),("Valor","Valor","C2"),("Referência","Referencia",null),("Despesa vinculada","DespesaId",null));
    private readonly Label aviso=ViewHelpers.EmptyOrError("");private readonly FlowLayoutPanel barra=new() { Dock=DockStyle.Top,Height=52 };private bool ocupado;
    public FormPagamentosConta(ContaPagar conta)
    {
        this.conta=conta;Text="Pagamentos — "+conta.Descricao;Size=new Size(900,550);MinimumSize=new Size(740,450);StartPosition=FormStartPosition.CenterParent;Font=UiTheme.Font(10);BackColor=Color.White;
        Botao("Comprovantes",AnexosAsync);Botao("Estornar",EstornarAsync);Botao("Atualizar",AtualizarAsync);
        Controls.Add(grid);Controls.Add(aviso);Controls.Add(barra);Controls.Add(ViewHelpers.Header("Pagamentos da conta","Estornar remove o pagamento e a despesa vinculada. O histórico de alterações é preservado."));
        Load+=async(_,_)=>await Executar(AtualizarAsync);FormClosing+=(_,e)=>{if(ocupado)e.Cancel=true;};
    }
    private void Botao(string texto,Func<Task> acao){var b=ViewHelpers.SecondaryButton(texto);b.Width=140;b.Click+=async(_,_)=>await Executar(acao);barra.Controls.Add(b);}
    private PagamentoRegistrado Selecionado()=>grid.CurrentRow?.DataBoundItem as PagamentoRegistrado??throw new ArgumentException("Selecione um pagamento.");
    private async Task AtualizarAsync(){var lista=await Task.Run(()=>service.Pagamentos(conta.Id));if(!IsDisposed)grid.DataSource=lista;}
    private Task AnexosAsync(){using var f=new FormAnexos(false,Selecionado().DespesaId);f.ShowDialog(this);return Task.CompletedTask;}
    private async Task EstornarAsync()
    {
        var p=Selecionado();if(MessageBox.Show(this,$"Estornar {p.Valor:C2}? O saldo da conta e o financeiro da viagem serão atualizados. Se houver comprovantes, remova-os antes.","FIGO",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        await Task.Run(()=>service.Estornar(conta.Id,p.Id));await AtualizarAsync();
    }
    private async Task Executar(Func<Task> acao)
    {
        if(ocupado)return;ocupado=true;barra.Enabled=false;aviso.Text="";
        try{await acao();}catch(Exception ex){aviso.Text=ErroFinanceiro.Mensagem(ex);}finally{ocupado=false;barra.Enabled=true;}
    }
}
