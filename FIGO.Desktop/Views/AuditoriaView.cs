using FIGO.backend;

namespace FIGO.Desktop;

public sealed class AuditoriaView : UserControl
{
    private readonly AuditoriaService service = new();
    private readonly DateTimePicker inicio = new() { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today.AddDays(-30) };
    private readonly DateTimePicker fim = new() { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
    private readonly ComboBox entidade = new() { Width = 185, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox tipo = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Nome", ValueMember = "Id" };
    private readonly TextBox autor = new() { Width = 150, MaxLength = 50, PlaceholderText = "Nome do autor" };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView detalhes = new() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly Label aviso = ViewHelpers.EmptyOrError("");
    private readonly Label paginacao = new() { AutoSize = true, Margin = new Padding(8, 12, 8, 0) };
    private readonly Button anterior = ViewHelpers.SecondaryButton("Anterior");
    private readonly Button proxima = ViewHelpers.SecondaryButton("Próxima");
    private readonly FlowLayoutPanel filtros = new() { Dock = DockStyle.Top, Height = 95, AutoScroll = true };
    private int pagina = 1;
    private long total;
    private bool ocupado;
    private int versaoSelecao;

    public AuditoriaView()
    {
        BackColor = UiTheme.Background;
        var card = UiTheme.CardPanel(14); card.Dock = DockStyle.Fill;
        entidade.Items.Add("Todas as entidades"); entidade.Items.AddRange(AuditoriaService.Entidades.Cast<object>().ToArray()); entidade.SelectedIndex = 0;
        tipo.DataSource = new[] { new OpcaoFinanceira(0, "Todas as ações"), new(1, "Criação"), new(2, "Alteração"), new(3, "Exclusão") };
        filtros.Controls.AddRange([Legenda("De"), inicio, Legenda("Até"), fim, entidade, tipo, autor]);
        var buscar = ViewHelpers.SecondaryButton("Filtrar");
        buscar.Click += async (_, _) => { if (!ocupado) { pagina = 1; await CarregarAsync(); } };
        filtros.Controls.Add(buscar);
        ViewHelpers.EstilizarGrid(grid); ViewHelpers.EstilizarGrid(detalhes);
        foreach (var c in new[] { ("Data / hora", "DataLocal"), ("Autor", "Autor"), ("Ação", "Acao"), ("Entidade", "Entidade"), ("Registro", "RegistroId"), ("Origem", "Origem") })
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = c.Item1, DataPropertyName = c.Item2 });
        grid.Columns[0].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
        foreach (var c in new[] { ("Campo", "Campo"), ("Valor anterior", "ValorAnterior"), ("Valor novo", "ValorNovo") })
            detalhes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = c.Item1, DataPropertyName = c.Item2 });
        detalhes.DefaultCellStyle.NullValue = "(não informado)";
        var conteudo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        conteudo.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); conteudo.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        conteudo.Controls.Add(grid, 0, 0); conteudo.Controls.Add(detalhes, 0, 1);
        var rodape = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46 };
        rodape.Controls.AddRange([anterior, proxima, paginacao]);
        anterior.Click += async (_, _) => { if (!ocupado && pagina > 1) { pagina--; await CarregarAsync(); } };
        proxima.Click += async (_, _) => { if (!ocupado && (long)pagina * 50 < total) { pagina++; await CarregarAsync(); } };
        grid.SelectionChanged += async (_, _) => await CarregarDetalhesAsync();
        card.Controls.Add(conteudo); card.Controls.Add(rodape); card.Controls.Add(aviso); card.Controls.Add(filtros);
        Controls.Add(card); Controls.Add(ViewHelpers.Header("Histórico de alterações", "Selecione um evento para comparar os campos. Horário local; CPF mascarado; consulta de 50 eventos por página."));
        Load += async (_, _) => await CarregarAsync();
    }
    private static Label Legenda(string texto) => new() { Text = texto, AutoSize = true, Margin = new Padding(4, 8, 2, 0) };
    private async Task CarregarAsync()
    {
        if (ocupado) return;
        ocupado = true; filtros.Enabled = anterior.Enabled = proxima.Enabled = false; aviso.Text = "";
        try
        {
            DateTime de = inicio.Value.Date.ToUniversalTime();
            DateTime ate = fim.Value.Date.AddDays(1).ToUniversalTime();
            if (fim.Value.Date < inicio.Value.Date) throw new ArgumentException("A data final deve ser igual ou posterior à inicial.");
            string? tabela = entidade.SelectedIndex == 0 ? null : entidade.SelectedItem?.ToString();
            int? acao = (int?)tipo.SelectedValue; if (acao == 0) acao = null;
            string nome = autor.Text; int atual = pagina;
            var resultado = await Task.Run(() => service.Listar(de, ate, tabela, acao, nome, atual));
            if (IsDisposed) return;
            total = resultado.Total;
            grid.DataSource = resultado.Registros.ToList();
            paginacao.Text = $"Página {pagina} de {Math.Max(1, (total + 49) / 50)} — {total} evento(s)";
        }
        catch (Exception ex) { if (!IsDisposed) { aviso.Text = ErroFinanceiro.Mensagem(ex); grid.DataSource = null; detalhes.DataSource = null; total = 0; paginacao.Text = "Consulta indisponível"; } }
        finally
        {
            ocupado = false;
            if (!IsDisposed) { filtros.Enabled = true; anterior.Enabled = pagina > 1; proxima.Enabled = (long)pagina * 50 < total; }
        }
    }
    private async Task CarregarDetalhesAsync()
    {
        int versao = ++versaoSelecao;
        detalhes.DataSource = null;
        if (grid.CurrentRow?.DataBoundItem is not RegistroAuditoria registro) return;
        try
        {
            var lista = await Task.Run(() => service.Detalhes(registro.Id));
            if (!IsDisposed && versao == versaoSelecao) detalhes.DataSource = lista;
        }
        catch (Exception ex) { if (!IsDisposed && versao == versaoSelecao) aviso.Text = ErroFinanceiro.Mensagem(ex); }
    }
}
