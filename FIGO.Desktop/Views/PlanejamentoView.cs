using FIGO.backend;

namespace FIGO.Desktop;

internal sealed class PlanejamentoView : TelaViagemFinanceira
{
    private readonly PlanejamentoService service=new();
    private readonly DataGridView itens=Tabela(("Categoria","Categoria",null),("Previsto","Previsto","C2"),("Realizado","Realizado","C2"),("Disponível / excesso","Diferenca","C2"),("Situação","Situacao",null));
    private readonly Label metaTexto=new() { Dock=DockStyle.Top,Height=210,Padding=new Padding(20),Font=UiTheme.Font(12),ForeColor=UiTheme.Text };
    private readonly ProgressBar progresso=new() { Dock=DockStyle.Top,Height=28,Maximum=100 };
    private ResumoMeta? meta;
    public PlanejamentoView():base("Planejamento financeiro","Compare o orçamento com os gastos e acompanhe a arrecadação da viagem.")
    {
        Botao("Definir previsto",PrevistoAsync,135);Botao("Excluir previsto",ExcluirPrevistoAsync,135);Botao("Definir meta",MetaAsync);Botao("Remover meta",ExcluirMetaAsync);Botao("Atualizar",AtualizarAsync,105);
        var tabs=new TabControl { Dock=DockStyle.Fill };var orc=new TabPage("Orçamento por categoria");orc.Controls.Add(itens);
        var arrecadacao=new TabPage("Meta de arrecadação");arrecadacao.Controls.Add(progresso);arrecadacao.Controls.Add(metaTexto);tabs.TabPages.AddRange([orc,arrecadacao]);Area.Controls.Add(tabs);
    }
    protected override async Task AtualizarAsync()
    {
        if(ViagemAtual is not {} id){itens.DataSource=null;meta=null;metaTexto.Text="Selecione uma viagem.";Rodape.Text="";progresso.Value=0;return;}
        var dados=await Task.Run(()=>(service.Orcamento(id),service.Meta(id)));if(IsDisposed)return;
        itens.DataSource=dados.Item1;meta=dados.Item2;
        decimal previsto=dados.Item1.Sum(x=>x.Previsto),real=dados.Item1.Sum(x=>x.Realizado);
        Rodape.Text=$"Previsto: {previsto:C2}   |   Realizado: {real:C2}   |   Diferença: {previsto-real:C2}. Valores previstos não alteram o saldo financeiro.";
        metaTexto.Text=meta==null?"Nenhuma meta cadastrada. Clique em Definir meta.":$"{meta.Descricao}\n\nArrecadado: {meta.Arrecadado:C2} de {meta.ValorMeta:C2} ({meta.Percentual:N2}%)\nFalta: {meta.Falta:C2}\nPrazo: {meta.DataLimite?.ToString("dd/MM/yyyy")??"Não informado"}\nSituação: {meta.Situacao}";
        progresso.Value=meta==null?0:(int)Math.Clamp(meta.Percentual,0,100);
    }
    private async Task PrevistoAsync()
    {
        int id=ExigirViagem();var categorias=await Task.Run(()=>new CadastrosFinanceirosService().Listar(TipoCadastroFinanceiro.CategoriaDespesa));if(IsDisposed)return;
        using var f=new FormItemOrcamento(id,categorias,itens.CurrentRow?.DataBoundItem as LinhaOrcamento);
        if(f.ShowDialog(this)==DialogResult.OK)await AtualizarAsync();
    }
    private async Task ExcluirPrevistoAsync()
    {
        int id=ExigirViagem();if(itens.CurrentRow?.DataBoundItem is not LinhaOrcamento { ItemId:>0 } item)throw new ArgumentException("Selecione uma categoria com valor previsto.");
        if(MessageBox.Show(this,"Excluir o valor previsto desta categoria? As despesas realizadas serão mantidas.","FIGO",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        await Task.Run(()=>service.ExcluirItem(id,item.ItemId));await AtualizarAsync();
    }
    private async Task MetaAsync(){using var f=new FormMetaArrecadacao(ExigirViagem(),meta);if(f.ShowDialog(this)==DialogResult.OK)await AtualizarAsync();}
    private async Task ExcluirMetaAsync()
    {
        int id=ExigirViagem();if(meta==null)throw new ArgumentException("Esta viagem não possui meta.");
        if(MessageBox.Show(this,"Remover a meta? As receitas serão mantidas.","FIGO",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        await Task.Run(()=>service.ExcluirMeta(id));await AtualizarAsync();
    }
}

internal sealed class FormItemOrcamento : FormCadastroFinanceiro
{
    private readonly ComboBox categoria=new() { DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Nome",ValueMember="Id" };
    private readonly NumericUpDown valor=new() { DecimalPlaces=2,Maximum=999999999.99m,ThousandsSeparator=true };
    private readonly int viagem;
    public FormItemOrcamento(int viagem,IReadOnlyList<OpcaoFinanceira> categorias,LinhaOrcamento? item=null):base("Definir orçamento da categoria",300)
    {
        this.viagem=viagem;categoria.DataSource=new[]{new OpcaoFinanceira(0,"Selecione uma categoria")}.Concat(categorias).ToList();
        categoria.SelectedValue=item?.CategoriaId??0;valor.Value=item?.Previsto??0;Campo("Categoria *",categoria);Campo("Previsto (R$) *",valor);
        Nota("Uma previsão por categoria em cada viagem. Salvar substitui o valor previsto dessa categoria, preservando os gastos realizados.");
    }
    protected override Task<int> GravarAsync(){int cat=(int?)categoria.SelectedValue??0;decimal v=valor.Value;return Task.Run(()=>new PlanejamentoService().SalvarItem(viagem,cat,v));}
}
internal sealed class FormMetaArrecadacao : FormCadastroFinanceiro
{
    private readonly TextBox nome=new() { MaxLength=150 };
    private readonly NumericUpDown valor=new() { DecimalPlaces=2,Maximum=999999999.99m,ThousandsSeparator=true };
    private readonly DateTimePicker prazo=new() { Format=DateTimePickerFormat.Short,ShowCheckBox=true,Checked=false,MinDate=new DateTime(1900,1,1) };
    private readonly int viagem;
    public FormMetaArrecadacao(int viagem,ResumoMeta? meta=null):base("Meta de arrecadação",350)
    {
        this.viagem=viagem;nome.Text=meta?.Descricao??"Arrecadação da viagem";valor.Value=meta?.ValorMeta??0;if(meta?.DataLimite is {} data){prazo.Value=data;prazo.Checked=true;}
        Campo("Descrição *",nome);Campo("Meta (R$) *",valor);Campo("Prazo opcional",prazo);Nota("O progresso usa todas as receitas registradas para esta viagem. Desmarque a data para deixar o prazo em branco.");
    }
    protected override Task<int> GravarAsync(){string n=nome.Text;decimal v=valor.Value;DateTime? p=prazo.Checked?prazo.Value.Date:null;return Task.Run(()=>new PlanejamentoService().SalvarMeta(viagem,n,v,p));}
}
