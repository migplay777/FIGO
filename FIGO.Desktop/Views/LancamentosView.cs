using FIGO.backend;

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

    private readonly int? viagemInicial;

    public LancamentosView(bool isReceita, int? viagemId = null)
    {
        viagemInicial = viagemId;
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
        if (receita)
            foreach (var (titulo, campo) in new[] { ("Pagador", "PagadorNome"), ("Pagamento", "FormaPagamentoNome"), ("Conta de destino", "ContaDestino"), ("Referência", "ReferenciaPagamento") })
                tabela.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, DataPropertyName = campo, MinimumWidth = campo == "ContaDestino" ? 210 : 115 });
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
            if (viagemInicial is int id)
                seletor.SelectedItem = seletor.Items.Cast<ViagemItem>().FirstOrDefault(v => v.Id == id);
            aviso.Text = lista.Count == 0 ? "Cadastre uma viagem antes de registrar lançamentos." : "";
            CarregarLancamentos();
        }
        catch (Exception ex) { aviso.Text = ErroFinanceiro.Mensagem(ex); }
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
        catch (Exception ex) { aviso.Text = ErroFinanceiro.Mensagem(ex); tabela.DataSource = null; }
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
        catch (Exception ex) { MessageBox.Show(ErroFinanceiro.Mensagem(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private record ViagemItem(int Id, string Nome) { public override string ToString() => Nome; }
}
