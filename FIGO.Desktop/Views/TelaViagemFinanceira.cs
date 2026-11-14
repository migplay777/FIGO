using FIGO.backend;

namespace FIGO.Desktop;

internal abstract class TelaViagemFinanceira : UserControl
{
    private readonly ComboBox viagens = new() { Width=260,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Nome",ValueMember="Id" };
    protected readonly FlowLayoutPanel Barra=new() { Dock=DockStyle.Top,Height=96,AutoScroll=true };
    protected readonly Panel Area=new() { Dock=DockStyle.Fill };
    protected readonly Label Aviso=ViewHelpers.EmptyOrError("");
    protected readonly Label Rodape=new() { Dock=DockStyle.Bottom,Height=48,ForeColor=UiTheme.Muted,TextAlign=ContentAlignment.MiddleLeft };
    private bool ocupado;
    protected int? ViagemAtual => viagens.SelectedValue is int id && id>0?id:null;
    protected int ExigirViagem() => ViagemAtual??throw new ArgumentException("Selecione uma viagem.");
    protected TelaViagemFinanceira(string titulo,string subtitulo)
    {
        BackColor=UiTheme.Background;var card=UiTheme.CardPanel(14);card.Dock=DockStyle.Fill;
        Barra.Controls.Add(viagens);card.Controls.Add(Area);card.Controls.Add(Rodape);card.Controls.Add(Aviso);card.Controls.Add(Barra);
        Controls.Add(card);Controls.Add(ViewHelpers.Header(titulo,subtitulo));
        viagens.SelectedIndexChanged+=async(_,_)=>{if(!ocupado)await Operar(AtualizarAsync);};
        Load+=async(_,_)=>await Operar(async()=>
        {
            var lista=await Task.Run(()=>new ViagemServices().Listar());if(IsDisposed)return;
            viagens.DataSource=lista;await AtualizarAsync();
        });
    }
    protected void Botao(string texto,Func<Task> acao,int largura=120)
    {
        var b=ViewHelpers.SecondaryButton(texto);b.Width=largura;b.Click+=async(_,_)=>await Operar(acao);Barra.Controls.Add(b);
    }
    protected async Task Operar(Func<Task> acao)
    {
        if(ocupado)return;ocupado=true;Barra.Enabled=false;Aviso.Text="";
        try{await acao();}catch(Exception ex){if(!IsDisposed)Aviso.Text=ErroFinanceiro.Mensagem(ex);}
        finally{ocupado=false;if(!IsDisposed)Barra.Enabled=true;}
    }
    protected abstract Task AtualizarAsync();
    internal static DataGridView Tabela(params (string Titulo,string Campo,string? Formato)[] colunas)
    {
        var g=new DataGridView { Dock=DockStyle.Fill,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
        ViewHelpers.EstilizarGrid(g);foreach(var c in colunas)g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText=c.Titulo,DataPropertyName=c.Campo,DefaultCellStyle=new DataGridViewCellStyle { Format=c.Formato??"" } });return g;
    }
}
