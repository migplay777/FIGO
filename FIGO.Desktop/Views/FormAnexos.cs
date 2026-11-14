using FIGO.backend;

namespace FIGO.Desktop;

internal sealed class FormAnexos : Form
{
    private readonly bool receita;private readonly int lancamento;private readonly AnexosService service=new();
    private readonly DataGridView grid=new() { Dock=DockStyle.Fill,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
    private readonly Label aviso=ViewHelpers.EmptyOrError("");private readonly FlowLayoutPanel barra=new() { Dock=DockStyle.Top,Height=52 };private bool ocupado;
    public FormAnexos(bool receita,int lancamento)
    {
        this.receita=receita;this.lancamento=lancamento;Text="Comprovantes e anexos • FIGO";Size=new Size(850,530);MinimumSize=new Size(700,430);StartPosition=FormStartPosition.CenterParent;Font=UiTheme.Font(10);BackColor=Color.White;
        ViewHelpers.EstilizarGrid(grid);foreach(var c in new[]{("Arquivo","NomeOriginal"),("Tipo","TipoArquivo"),("Bytes","TamanhoBytes")})grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText=c.Item1,DataPropertyName=c.Item2 });
        Botao("Adicionar",AdicionarAsync);Botao("Salvar cópia",BaixarAsync);Botao("Remover",ExcluirAsync);Botao("Atualizar",AtualizarAsync);
        Controls.Add(grid);Controls.Add(aviso);Controls.Add(barra);Controls.Add(ViewHelpers.Header("Comprovantes","PDF, PNG ou JPEG, até 5 MB por arquivo. O arquivo fica vinculado ao lançamento."));
        Load+=async(_,_)=>await Executar(AtualizarAsync);FormClosing+=(_,e)=>{if(ocupado)e.Cancel=true;};
    }
    private void Botao(string texto,Func<Task> acao){var b=ViewHelpers.SecondaryButton(texto);b.Width=120;b.Click+=async(_,_)=>await Executar(acao);barra.Controls.Add(b);}
    private async Task AtualizarAsync(){var lista=await Task.Run(()=>service.Listar(receita,lancamento));if(!IsDisposed)grid.DataSource=lista;}
    private AnexoResumo Selecionado()=>grid.CurrentRow?.DataBoundItem as AnexoResumo??throw new ArgumentException("Selecione um arquivo.");
    private async Task AdicionarAsync()
    {
        using var abrir=new OpenFileDialog { Filter="Comprovantes|*.pdf;*.png;*.jpg;*.jpeg",CheckFileExists=true,Multiselect=false };
        if(abrir.ShowDialog(this)!=DialogResult.OK)return;
        byte[] bytes=await AnexosService.LerArquivoAsync(abrir.FileName);string nome=Path.GetFileName(abrir.FileName);
        await Task.Run(()=>service.Adicionar(receita,lancamento,nome,bytes));await AtualizarAsync();
    }
    private async Task BaixarAsync()
    {
        var a=Selecionado();var arquivo=await Task.Run(()=>service.Baixar(a.Id));
        using var salvar=new SaveFileDialog { FileName=arquivo.Metadados.NomeOriginal,Filter="Arquivo original|*.*",OverwritePrompt=true,AddExtension=false };
        if(salvar.ShowDialog(this)!=DialogResult.OK)return;await File.WriteAllBytesAsync(salvar.FileName,arquivo.Conteudo);aviso.Text="Cópia salva.";
    }
    private async Task ExcluirAsync()
    {
        var a=Selecionado();if(MessageBox.Show(this,$"Remover o anexo {a.NomeOriginal}?","FIGO",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        await Task.Run(()=>service.Excluir(a.Id,receita,lancamento));await AtualizarAsync();
    }
    private async Task Executar(Func<Task> acao)
    {
        if(ocupado)return;ocupado=true;barra.Enabled=false;aviso.Text="";
        try{await acao();}catch(Exception ex){aviso.Text=ErroFinanceiro.Mensagem(ex);}finally{ocupado=false;barra.Enabled=true;}
    }
}
