using FIGO.backend;

namespace FIGO.Desktop;

internal sealed class FormLancamento : Form
{
    private readonly bool receita;
    private readonly int viagemId;
    private readonly object? original;
    private readonly CadastrosFinanceirosService cadastros = new();
    private readonly TextBox descricao = new() { MaxLength = 255 };
    private readonly NumericUpDown valor = new() { DecimalPlaces = 2, Maximum = 999999999.99m, ThousandsSeparator = true };
    private readonly DateTimePicker data = new() { Format = DateTimePickerFormat.Short, MinDate = new DateTime(1900, 1, 1) };
    private readonly TextBox referencia = new() { MaxLength = 100 };
    private readonly PesquisaComboBox origem = new(), pagador = new(), forma = new(), conta = new(), categoria = new();
    private readonly Panel corpo = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24, 10, 24, 12) };
    private readonly TableLayoutPanel campos = new() { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
    private readonly Label erro = new() { Dock = DockStyle.Top, Height = 48, ForeColor = Color.FromArgb(171, 42, 53), Padding = new Padding(24, 4, 24, 0) };
    private readonly Button salvar = UiTheme.PrimaryButton("Salvar lançamento");
    private readonly Button recarregar = ViewHelpers.SecondaryButton("Recarregar opções");
    private readonly Button cancelar = ViewHelpers.SecondaryButton("Cancelar");
    private bool ocupado;
    private bool carregado;

    public FormLancamento(bool isReceita, int id, object? item)
    {
        receita = isReceita; viagemId = id; original = item;
        Text = (item == null ? "Nova " : "Editar ") + (receita ? "receita" : "despesa") + " • FIGO";
        ClientSize = new Size(760, receita ? 690 : 450);
        MinimumSize = new Size(680, receita ? 580 : 420);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UiTheme.Background; Font = UiTheme.Font(10);
        MaximizeBox = false; MinimizeBox = false; AutoScaleMode = AutoScaleMode.Dpi;

        var header = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(24, 16, 24, 0) };
        header.Controls.Add(new Label { Text = receita ? "Registrar recebimento" : "Registrar despesa", Left = 24, Top = 16,
            AutoSize = true, Font = UiTheme.Font(19, FontStyle.Bold), ForeColor = UiTheme.Text });
        header.Controls.Add(new Label { Text = "Digite nos campos de seleção para pesquisar. Use + Novo para cadastrar uma opção.",
            Left = 24, Top = 58, Width = 690, Height = 36, ForeColor = UiTheme.Muted, Font = UiTheme.Font(9) });
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        corpo.Controls.Add(campos);
        Campo("Descrição *", descricao);
        Campo("Valor recebido (R$) *".Replace("recebido", receita ? "recebido" : "pago"), valor);
        Campo("Data *", data);
        if (receita)
        {
            Campo("Origem da receita *", origem, () => NovoNome(TipoCadastroFinanceiro.OrigemReceita, origem));
            Campo("Quem pagou *", pagador, NovoPagador);
            Campo("Forma de pagamento *", forma, () => NovoNome(TipoCadastroFinanceiro.FormaPagamento, forma));
            Campo("Conta de destino *", conta, NovaConta);
            Campo("Referência (opcional)", referencia);
        }
        else Campo("Categoria *", categoria, () => NovoNome(TipoCadastroFinanceiro.CategoriaDespesa, categoria));
        var detalhe = new Label { Text = item is Receita r && (r.PagadorId == null || r.FormaPagamentoId == null || r.ContaFinanceiraId == null)
                ? "Registro antigo: complete o pagador, a forma e a conta antes de salvar."
                : "* Campos obrigatórios. Todos os cadastros ficam disponíveis nas próximas viagens.",
            ForeColor = UiTheme.Muted, Dock = DockStyle.Bottom, Height = 36, Font = UiTheme.Font(9) };
        corpo.Controls.Add(detalhe);
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 110 };
        var botoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(20, 0, 20, 0) };
        salvar.Width = 165; recarregar.Width = 155; cancelar.DialogResult = DialogResult.Cancel;
        salvar.Click += async (_, _) => await SalvarAsync();
        recarregar.Click += async (_, _) => await CarregarAsync();
        botoes.Controls.Add(salvar); botoes.Controls.Add(cancelar); botoes.Controls.Add(recarregar);
        footer.Controls.Add(erro); footer.Controls.Add(botoes);
        Controls.Add(corpo); Controls.Add(footer); Controls.Add(header);
        AcceptButton = salvar; CancelButton = cancelar;
        if (item is Receita rec) { descricao.Text = rec.Descricao; valor.Value = Math.Clamp(rec.Valor, 0, valor.Maximum); data.Value = rec.Data; referencia.Text = rec.ReferenciaPagamento; }
        if (item is Despesa des) { descricao.Text = des.Descricao; valor.Value = Math.Clamp(des.Valor, 0, valor.Maximum); data.Value = des.Data; }
        salvar.Enabled = false;
        Shown += async (_, _) => await CarregarAsync();
        FormClosing += (_, e) => { if (ocupado) e.Cancel = true; };
    }
    private void Campo(string titulo, Control controle, Func<Task>? novo = null)
    {
        int linha = campos.RowCount++;
        campos.RowStyles.Add(new RowStyle(SizeType.Absolute, 53));
        campos.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, linha);
        controle.Dock = DockStyle.Top; controle.Margin = new Padding(0, 10, 0, 0); controle.AccessibleName = titulo;
        campos.Controls.Add(controle, 1, linha);
        if (novo is null) campos.SetColumnSpan(controle, 2);
        else
        {
            var botao = ViewHelpers.SecondaryButton("+ Novo");
            botao.Dock = DockStyle.Top; botao.Margin = new Padding(8, 6, 0, 0);
            botao.AccessibleName = "Cadastrar: " + titulo.Replace(" *", "");
            botao.Click += async (_, _) => await novo();
            campos.Controls.Add(botao, 2, linha);
        }
    }
    private async Task CarregarAsync()
    {
        if (ocupado) return;
        ocupado = true; corpo.Enabled = salvar.Enabled = recarregar.Enabled = cancelar.Enabled = false; erro.Text = "Carregando opções…";
        // Preserva escolhas ao atualizar, sem preencher automaticamente campos ainda desconhecidos.
        int? origemId = carregado ? origem.IdSelecionado : (original as Receita)?.OrigemId;
        int? pagadorId = carregado ? pagador.IdSelecionado : (original as Receita)?.PagadorId;
        int? formaId = carregado ? forma.IdSelecionado : (original as Receita)?.FormaPagamentoId;
        int? contaId = carregado ? conta.IdSelecionado : (original as Receita)?.ContaFinanceiraId;
        int? categoriaId = carregado ? categoria.IdSelecionado : (original as Despesa)?.CategoriaId;
        try
        {
            if (receita)
            {
                var dados = await Task.Run(() => (Origens: cadastros.Listar(TipoCadastroFinanceiro.OrigemReceita),
                    Pagadores: cadastros.ListarPagadores(), Formas: cadastros.Listar(TipoCadastroFinanceiro.FormaPagamento), Contas: cadastros.ListarContas()));
                origem.DefinirOpcoes(dados.Origens.Select(Item), origemId);
                pagador.DefinirOpcoes(dados.Pagadores.Select(Item), pagadorId);
                forma.DefinirOpcoes(dados.Formas.Select(Item), formaId);
                conta.DefinirOpcoes(dados.Contas.Select(Item), contaId);
            }
            else
            {
                var itens = await Task.Run(() => cadastros.Listar(TipoCadastroFinanceiro.CategoriaDespesa));
                categoria.DefinirOpcoes(itens.Select(Item), categoriaId);
            }
            carregado = true; erro.Text = "";
        }
        catch (Exception ex) { carregado = false; erro.Text = ErroFinanceiro.Mensagem(ex); }
        finally { ocupado = false; corpo.Enabled = recarregar.Enabled = cancelar.Enabled = true; salvar.Enabled = carregado; }
    }
    private static ItemSelecao Item(OpcaoFinanceira o) => new(o.Id, o.Nome);
    private static ItemSelecao Item(ContaFinanceira c) => new(c.Id, c.Identificacao, c.Titular);
    private static ItemSelecao Item(Pagador p)
    {
        string doc = p.Documento switch
        {
            { Length: 11 } cpf => $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}",
            { Length: 14 } cnpj => $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}",
            _ => "CPF não informado"
        };
        string tipo = p.Tipo == "PJ" ? "Empresa • CNPJ " : "Pessoa física • ";
        return new(p.Id, $"{p.Nome} • {tipo}{doc}", p.Documento ?? "");
    }
    private async Task NovoNome(TipoCadastroFinanceiro tipo, PesquisaComboBox seletor)
    {
        using var form = new FormNovoNomeFinanceiro(tipo);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await AtualizarSeletorAsync(async () =>
        {
            var itens = await Task.Run(() => cadastros.Listar(tipo));
            seletor.DefinirOpcoes(itens.Select(Item), form.NovoId);
        });
    }
    private async Task NovoPagador()
    {
        using var form = new FormNovoPagador();
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await AtualizarSeletorAsync(async () =>
        {
            var itens = await Task.Run(cadastros.ListarPagadores);
            pagador.DefinirOpcoes(itens.Select(Item), form.NovoId);
        });
    }
    private async Task NovaConta()
    {
        using var form = new FormNovaConta();
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await AtualizarSeletorAsync(async () =>
        {
            var itens = await Task.Run(cadastros.ListarContas);
            conta.DefinirOpcoes(itens.Select(Item), form.NovoId);
        });
    }
    private async Task AtualizarSeletorAsync(Func<Task> atualizar)
    {
        ocupado = true; corpo.Enabled = salvar.Enabled = cancelar.Enabled = recarregar.Enabled = false;
        try { await atualizar(); erro.Text = "Cadastro salvo e selecionado."; }
        catch (Exception ex) { erro.Text = "O cadastro foi salvo. Recarregue as opções. " + ErroFinanceiro.Mensagem(ex); }
        finally { ocupado = false; corpo.Enabled = cancelar.Enabled = recarregar.Enabled = true; salvar.Enabled = carregado; }
    }
    private async Task SalvarAsync()
    {
        if (ocupado || !carregado) return;
        ocupado = true; corpo.Enabled = salvar.Enabled = cancelar.Enabled = recarregar.Enabled = false; erro.Text = "";
        try
        {
            if (receita)
            {
                var r = new Receita { Id = (original as Receita)?.Id ?? 0, ViagemId = viagemId, Descricao = descricao.Text.Trim(),
                    Valor = valor.Value, Data = data.Value.Date, OrigemId = origem.IdSelecionado, PagadorId = pagador.IdSelecionado,
                    FormaPagamentoId = forma.IdSelecionado, ContaFinanceiraId = conta.IdSelecionado, ReferenciaPagamento = referencia.Text.Trim() };
                ValidacaoFinanceira.Receita(r);
                await Task.Run(() => { if (original == null) new ReceitaService().Adicionar(r); else new ReceitaService().Editar(r); });
            }
            else
            {
                var d = new Despesa { Id = (original as Despesa)?.Id ?? 0, ViagemId = viagemId, Descricao = descricao.Text.Trim(),
                    Valor = valor.Value, Data = data.Value.Date, CategoriaId = categoria.IdSelecionado };
                ValidacaoFinanceira.Despesa(d);
                await Task.Run(() => { if (original == null) new DespesaService().Adicionar(d); else new DespesaService().Editar(d); });
            }
            ocupado = false; DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { erro.Text = ErroFinanceiro.Mensagem(ex); }
        finally { ocupado = false; corpo.Enabled = cancelar.Enabled = recarregar.Enabled = true; salvar.Enabled = carregado; }
    }
}
