using FIGO.backend;

namespace FIGO.Desktop;

public sealed class ParticipantesView : UserControl
{
    private readonly ParticipantesService service = new();
    private readonly ComboBox viagens = new() { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Nome", ValueMember = "Id" };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly Label aviso = ViewHelpers.EmptyOrError("");
    private readonly Label contagem = new() { Dock = DockStyle.Bottom, Height = 38, ForeColor = UiTheme.Muted, TextAlign = ContentAlignment.MiddleLeft };
    private readonly FlowLayoutPanel barra = new() { Dock = DockStyle.Top, Height = 96, AutoScroll = true, Padding = new Padding(0, 6, 0, 0) };
    private bool ocupado;

    public ParticipantesView(int? viagemInicial = null)
    {
        BackColor = UiTheme.Background;
        var card = UiTheme.CardPanel(14); card.Dock = DockStyle.Fill;
        ViewHelpers.EstilizarGrid(grid);
        foreach (var coluna in new[] { ("Nome completo", "NomeCompleto"), ("Idade", "Idade"), ("Função", "Funcao"), ("CPF", "CpfMascarado") })
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = coluna.Item1, DataPropertyName = coluna.Item2 });
        barra.Controls.Add(viagens);
        Botao("Adicionar", async () => await EditarAsync(null));
        Botao("Editar", async () => { if (Selecionada() is { } p) await EditarAsync(p); else aviso.Text = "Selecione um participante."; });
        Botao("Remover da viagem", RemoverAsync, 156);
        Botao("Nova função", () => { using var f = new FormNovaFuncaoParticipante(); f.ShowDialog(this); return Task.CompletedTask; }, 125);
        Botao("Atualizar", CarregarAsync);
        viagens.SelectedIndexChanged += async (_, _) => { if (!ocupado) await ExecutarAsync(CarregarAsync); };
        card.Controls.Add(grid); card.Controls.Add(contagem); card.Controls.Add(aviso); card.Controls.Add(barra);
        Controls.Add(card); Controls.Add(ViewHelpers.Header("Participantes", "Cadastro opcional por viagem. CPF aparece mascarado na lista."));
        Load += async (_, _) => await ExecutarAsync(async () =>
        {
            var lista = await Task.Run(() => new ViagemServices().Listar());
            if (IsDisposed) return;
            viagens.DataSource = lista;
            if (viagemInicial != null) viagens.SelectedValue = viagemInicial.Value;
            await CarregarAsync();
        });
    }
    private void Botao(string texto, Func<Task> acao, int largura = 105)
    {
        var b = ViewHelpers.SecondaryButton(texto); b.Width = largura;
        b.Click += async (_, _) => await ExecutarAsync(acao); barra.Controls.Add(b);
    }
    private int? ViagemId => viagens.SelectedValue is int id && id > 0 ? id : null;
    private ParticipacaoViagem? Selecionada() => grid.CurrentRow?.DataBoundItem as ParticipacaoViagem;
    private async Task CarregarAsync()
    {
        if (ViagemId is not { } id) { grid.DataSource = null; contagem.Text = "Cadastre ou selecione uma viagem para começar."; return; }
        var lista = await Task.Run(() => service.ListarPorViagem(id));
        if (IsDisposed) return;
        grid.DataSource = lista;
        contagem.Text = $"{lista.Count} participante(s) identificado(s). A quantidade planejada da viagem é mantida separadamente.";
    }
    private async Task EditarAsync(ParticipacaoViagem? original)
    {
        if (ViagemId is not { } id) throw new ArgumentException("Selecione uma viagem.");
        var dados = await Task.Run(() => (service.ListarPessoas(), service.ListarFuncoes()));
        if (IsDisposed) return;
        using var f = new FormParticipante(id, dados.Item1, dados.Item2, original);
        if (f.ShowDialog(this) == DialogResult.OK) await CarregarAsync();
    }
    private async Task RemoverAsync()
    {
        var p = Selecionada();
        if (p == null) throw new ArgumentException("Selecione um participante.");
        if (MessageBox.Show(this, $"Remover {p.NomeCompleto} desta viagem? O cadastro da pessoa será preservado.", "FIGO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await Task.Run(() => service.RemoverDaViagem(p.Id, p.ViagemId));
        await CarregarAsync();
    }
    private async Task ExecutarAsync(Func<Task> acao)
    {
        if (ocupado) return;
        ocupado = true; barra.Enabled = false; aviso.Text = "";
        try { await acao(); }
        catch (Exception ex) { if (!IsDisposed) aviso.Text = ErroFinanceiro.Mensagem(ex); }
        finally { ocupado = false; if (!IsDisposed) barra.Enabled = true; }
    }
}
