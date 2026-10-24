using FIGO.backend;

namespace FIGO.Desktop;

internal abstract class FormCadastroFinanceiro : Form
{
    protected readonly TableLayoutPanel Campos = new() { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(24, 16, 24, 10) };
    private readonly Label erro = new() { Dock = DockStyle.Top, Height = 52, ForeColor = Color.FromArgb(171, 42, 53), Padding = new Padding(24, 4, 24, 0) };
    private readonly Button salvar = UiTheme.PrimaryButton("Salvar cadastro");
    private readonly Button cancelar = ViewHelpers.SecondaryButton("Cancelar");
    private bool salvando;
    public int NovoId { get; private set; }
    protected FormCadastroFinanceiro(string titulo, int altura)
    {
        Text = titulo; ClientSize = new Size(600, altura); MinimumSize = Size;
        StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; BackColor = UiTheme.Background; Font = UiTheme.Font(10);
        AutoScaleMode = AutoScaleMode.Dpi;
        Campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        Campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 110 };
        var botoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(20, 0, 20, 0) };
        salvar.Width = 150; cancelar.DialogResult = DialogResult.Cancel;
        salvar.Click += async (_, _) => await SalvarAsync();
        botoes.Controls.Add(salvar); botoes.Controls.Add(cancelar);
        footer.Controls.Add(erro); footer.Controls.Add(botoes);
        Controls.Add(Campos); Controls.Add(footer);
        AcceptButton = salvar; CancelButton = cancelar;
        FormClosing += (_, e) => { if (salvando) e.Cancel = true; };
    }
    protected Label Campo(string titulo, Control controle)
    {
        int linha = Campos.RowCount++;
        Campos.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        var label = new Label { Text = titulo, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        controle.Dock = DockStyle.Top; controle.Margin = new Padding(0, 10, 0, 0); controle.AccessibleName = titulo;
        Campos.Controls.Add(label, 0, linha); Campos.Controls.Add(controle, 1, linha);
        return label;
    }
    protected void Nota(string texto)
    {
        int linha = Campos.RowCount++;
        Campos.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        var label = new Label { Text = texto, ForeColor = UiTheme.Muted, Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), Font = UiTheme.Font(9) };
        Campos.Controls.Add(label, 0, linha); Campos.SetColumnSpan(label, 2);
    }
    protected abstract Task<int> GravarAsync();
    private async Task SalvarAsync()
    {
        if (salvando) return;
        salvando = true; salvar.Enabled = cancelar.Enabled = Campos.Enabled = false; erro.Text = "";
        try
        {
            NovoId = await GravarAsync();
            salvando = false; DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { erro.Text = ErroFinanceiro.Mensagem(ex); }
        finally { salvando = false; salvar.Enabled = cancelar.Enabled = Campos.Enabled = true; }
    }
}

internal sealed class FormNovoPagador : FormCadastroFinanceiro
{
    private readonly ComboBox tipo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox nome = new() { MaxLength = 150 };
    private readonly TextBox documento = new() { MaxLength = 18 };
    public FormNovoPagador() : base("Cadastrar pagador", 360)
    {
        tipo.Items.AddRange(["Pessoa física", "Empresa"]); tipo.SelectedIndex = 0;
        Campo("Tipo de pagador *", tipo);
        var nomeLabel = Campo("Nome completo *", nome);
        var docLabel = Campo("CPF (opcional)", documento);
        tipo.SelectedIndexChanged += (_, _) =>
        {
            nomeLabel.Text = tipo.SelectedIndex == 1 ? "Razão social *" : "Nome completo *";
            docLabel.Text = tipo.SelectedIndex == 1 ? "CNPJ *" : "CPF (opcional)";
            documento.Clear();
        };
        Nota("O CNPJ é obrigatório para empresas. O CPF é opcional para pessoas físicas. Os dados ficam disponíveis nos próximos lançamentos.");
    }
    protected override Task<int> GravarAsync()
    {
        var pagador = new Pagador { Tipo = tipo.SelectedIndex == 1 ? "PJ" : "PF", Nome = nome.Text, Documento = documento.Text };
        return Task.Run(() => new CadastrosFinanceirosService().AdicionarPagador(pagador));
    }
}

internal sealed class FormNovaConta : FormCadastroFinanceiro
{
    private readonly ComboBox tipo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox nome = new() { MaxLength = 100 };
    private readonly TextBox banco = new() { MaxLength = 100 };
    private readonly TextBox agencia = new() { MaxLength = 20 };
    private readonly TextBox numero = new() { MaxLength = 30 };
    private readonly TextBox titular = new() { MaxLength = 150 };
    public FormNovaConta() : base("Cadastrar conta de destino", 500)
    {
        tipo.Items.AddRange(["Conta bancária", "Caixa em espécie"]); tipo.SelectedIndex = 0;
        Campo("Tipo *", tipo); Campo("Nome da conta *", nome); Campo("Banco *", banco);
        Campo("Agência *", agencia); Campo("Número / dígito *", numero); Campo("Titular *", titular);
        tipo.SelectedIndexChanged += (_, _) => banco.Enabled = agencia.Enabled = numero.Enabled = titular.Enabled = tipo.SelectedIndex == 0;
        Nota("Use um nome fácil de encontrar, por exemplo: Conta da igreja. Para recebimentos em dinheiro, selecione um caixa.");
    }
    protected override Task<int> GravarAsync()
    {
        var conta = new ContaFinanceira { Tipo = tipo.SelectedIndex == 0 ? "Bancaria" : "Caixa", Nome = nome.Text,
            Banco = banco.Text, Agencia = agencia.Text, Numero = numero.Text, Titular = titular.Text };
        return Task.Run(() => new CadastrosFinanceirosService().AdicionarConta(conta));
    }
}

internal sealed class FormNovoNomeFinanceiro : FormCadastroFinanceiro
{
    private readonly TextBox nome = new() { MaxLength = 100 };
    private readonly TipoCadastroFinanceiro tipo;
    public FormNovoNomeFinanceiro(TipoCadastroFinanceiro tipo) : base(tipo switch
    {
        TipoCadastroFinanceiro.CategoriaDespesa => "Nova categoria de despesa",
        TipoCadastroFinanceiro.FormaPagamento => "Nova forma de pagamento",
        _ => "Nova origem de receita"
    }, 230)
    {
        this.tipo = tipo; Campo("Nome *", nome);
        Nota("A opção será salva para seleção rápida nos próximos lançamentos.");
    }
    protected override Task<int> GravarAsync()
    {
        string texto = nome.Text;
        return Task.Run(() => new CadastrosFinanceirosService().Adicionar(tipo, texto));
    }
}
