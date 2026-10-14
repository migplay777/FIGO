using FIGO.backend;
using System.Globalization;

namespace FIGO.Desktop;

public class LancamentosView : UserControl
{
    private readonly bool receita;
    private readonly ViagemServices viagens = new();
    private readonly ReceitaService receitas = new();
    private readonly DespesaService despesas = new();
    private readonly ComboBox seletor = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 330 };
    private readonly DataGridView tabela = new();
    private readonly Label aviso = ViewHelpers.EmptyOrError("");
    private readonly Label total = new() { AutoSize = true, Font = UiTheme.Font(12, FontStyle.Bold), ForeColor = UiTheme.Text };
    private List<Viagem> lista = new();

    public LancamentosView(bool isReceita)
    {
        receita = isReceita;
        BackColor = UiTheme.Background;
        var header = ViewHelpers.Header(receita ? "Receitas" : "Despesas", "Gerencie os lançamentos financeiros de cada viagem.");
        var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Color.White };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, WrapContents = false };
        toolbar.Controls.Add(seletor);
        foreach (var (nome, acao) in new (string, Action)[] { ("Novo", Novo), ("Editar", Editar), ("Excluir", Excluir), ("Atualizar", CarregarLancamentos) })
        {
            var botao = ViewHelpers.SecondaryButton(nome);
            botao.Click += (_, _) => acao();
            toolbar.Controls.Add(botao);
        }
        seletor.SelectedIndexChanged += (_, _) => CarregarLancamentos();
        ViewHelpers.EstilizarGrid(tabela);
        tabela.Dock = DockStyle.Fill;
        foreach (var (titulo, campo) in new[] { ("Descrição", "Descricao"), ("Valor", "Valor"), ("Data", "Data"), (receita ? "Origem" : "Categoria", receita ? "Origem" : "Categoria") })
            tabela.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, DataPropertyName = campo });
        tabela.Columns[1].DefaultCellStyle.Format = "C2";
        tabela.Columns[2].DefaultCellStyle.Format = "dd/MM/yyyy";
        tabela.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        total.Location = new Point(12, 15);
        footer.Controls.Add(total);
        card.Controls.Add(tabela);
        card.Controls.Add(footer);
        card.Controls.Add(aviso);
        card.Controls.Add(toolbar);
        Controls.Add(card);
        Controls.Add(header);
        Load += (_, _) => CarregarViagens();
    }
    private int ViagemId => seletor.SelectedItem is ViagemItem v ? v.Id : 0;
    private void CarregarViagens()
    {
        try
        {
            lista = viagens.Listar();
            seletor.DataSource = lista.Select(v => new ViagemItem(v.Id, v.Nome + " — " + v.Destino)).ToList();
            aviso.Text = lista.Count == 0 ? "Cadastre uma viagem antes de registrar lançamentos." : "";
            CarregarLancamentos();
        }
        catch (Exception ex) { aviso.Text = "Erro ao carregar viagens: " + ex.Message; }
    }
    private void CarregarLancamentos()
    {
        if (ViagemId == 0) { tabela.DataSource = null; total.Text = "Total: R$ 0,00"; return; }
        try
        {
            if (receita)
            {
                var dados = receitas.ListarPorViagem(ViagemId);
                tabela.DataSource = dados;
                total.Text = $"Total de receitas: {dados.Sum(x => x.Valor):C2}";
            }
            else
            {
                var dados = despesas.ListarPorViagem(ViagemId);
                tabela.DataSource = dados;
                total.Text = $"Total de despesas: {dados.Sum(x => x.Valor):C2}";
            }
            aviso.Text = "";
        }
        catch (Exception ex) { aviso.Text = "Erro ao consultar lançamentos: " + ex.Message; tabela.DataSource = null; }
    }
    private void Novo() => AbrirFormulario(false);
    private void Editar() => AbrirFormulario(true);
    private void AbrirFormulario(bool editar)
    {
        if (ViagemId == 0) { MessageBox.Show("Selecione uma viagem."); return; }
        var selecionado = tabela.CurrentRow?.DataBoundItem;
        if (editar && selecionado == null) { MessageBox.Show("Selecione um lançamento."); return; }
        using var form = new FormLancamento(receita, ViagemId, editar ? selecionado : null);
        if (form.ShowDialog(this) == DialogResult.OK) CarregarLancamentos();
    }
    private void Excluir()
    {
        var item = tabela.CurrentRow?.DataBoundItem;
        if (item == null) { MessageBox.Show("Selecione um lançamento."); return; }
        if (MessageBox.Show("Excluir este lançamento?", "FIGO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { if (receita) receitas.Excluir(((Receita)item).Id); else despesas.Excluir(((Despesa)item).Id); CarregarLancamentos(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private record ViagemItem(int Id, string Nome) { public override string ToString() => Nome; }
}

internal sealed class FormLancamento : Form
{
    private readonly bool receita;
    private readonly int viagemId;
    private readonly object? original;
    private readonly TextBox descricao = new();
    private readonly NumericUpDown valor = new() { DecimalPlaces = 2, Maximum = 1000000000, ThousandsSeparator = true };
    private readonly DateTimePicker data = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox categoria = new();
    public FormLancamento(bool isReceita, int id, object? item)
    {
        receita = isReceita; viagemId = id; original = item;
        Text = (item == null ? "Novo lançamento — " : "Editar lançamento — ") + (receita ? "Receita" : "Despesa");
        Size = new Size(490, 335); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        BackColor = UiTheme.Background;
        if (item is Receita r) { descricao.Text = r.Descricao; valor.Value = Math.Clamp(r.Valor, 0, valor.Maximum); data.Value = r.Data; categoria.Text = r.Origem; }
        if (item is Despesa d) { descricao.Text = d.Descricao; valor.Value = Math.Clamp(d.Valor, 0, valor.Maximum); data.Value = d.Data; categoria.Text = d.Categoria; }
        var campos = new (string, Control)[] { ("Descrição", descricao), ("Valor (R$)", valor), ("Data", data), (receita ? "Origem" : "Categoria", categoria) };
        for (int i = 0; i < campos.Length; i++)
        {
            Controls.Add(new Label { Text = campos[i].Item1, Left = 24, Top = 30 + i * 48, Width = 120 });
            var c = campos[i].Item2; c.Left = 150; c.Top = 25 + i * 48; c.Width = 290; Controls.Add(c);
        }
        var salvar = UiTheme.PrimaryButton("Salvar"); salvar.Left = 325; salvar.Top = 235; salvar.Width = 115;
        salvar.Click += (_, _) => Salvar(); Controls.Add(salvar);
    }
    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(descricao.Text) || string.IsNullOrWhiteSpace(categoria.Text) || valor.Value <= 0)
        { MessageBox.Show("Preencha descrição, valor positivo e origem/categoria."); return; }
        try
        {
            if (receita)
            {
                var r = original as Receita ?? new Receita { ViagemId = viagemId };
                r.Descricao = descricao.Text.Trim(); r.Valor = valor.Value; r.Data = data.Value.Date; r.Origem = categoria.Text.Trim();
                if (original == null) new ReceitaService().Adicionar(r); else new ReceitaService().Editar(r);
            }
            else
            {
                var d = original as Despesa ?? new Despesa { ViagemId = viagemId };
                d.Descricao = descricao.Text.Trim(); d.Valor = valor.Value; d.Data = data.Value.Date; d.Categoria = categoria.Text.Trim();
                if (original == null) new DespesaService().Adicionar(d); else new DespesaService().Editar(d);
            }
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { MessageBox.Show("Erro ao salvar: " + ex.Message, "FIGO", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
