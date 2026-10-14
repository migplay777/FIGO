using FIGO.backend;

namespace FIGO.Desktop;

public class ViagensView : UserControl
{
    private readonly ViagemServices service = new();
    private readonly DataGridView grid = new();
    private readonly Label aviso = ViewHelpers.EmptyOrError("");

    public ViagensView()
    {
        BackColor = UiTheme.Background;
        var header = ViewHelpers.Header("Viagens", "Cadastre e acompanhe as viagens missionárias da igreja.");
        var card = UiTheme.CardPanel(14);
        card.Dock = DockStyle.Fill;

        var barra = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
        var nova = UiTheme.PrimaryButton("+  Nova viagem"); nova.Width = 120; nova.Left = 0; nova.Top = 5;
        var editar = ViewHelpers.SecondaryButton("Editar"); editar.Left = 130; editar.Top = 5;
        var excluir = ViewHelpers.SecondaryButton("Excluir"); excluir.Left = 245; excluir.Top = 5;
        nova.Click += (_, _) => Nova();
        editar.Click += (_, _) => Editar();
        excluir.Click += (_, _) => Excluir();
        barra.Controls.AddRange([nova, editar, excluir]);

        ViewHelpers.EstilizarGrid(grid);
        grid.Dock = DockStyle.Fill;
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome", DataPropertyName = "Nome", FillWeight = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Destino", DataPropertyName = "Destino", FillWeight = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Início", DataPropertyName = "DataInicio", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fim", DataPropertyName = "DataFim", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Participantes", DataPropertyName = "QuantidadeParticipantes" });
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        card.Controls.Add(grid);
        card.Controls.Add(aviso);
        card.Controls.Add(barra);
        Controls.Add(card);
        Controls.Add(header);
        Load += (_, _) => Carregar();
    }

    private void Carregar()
    {
        try { aviso.Visible = false; grid.DataSource = service.Listar(); }
        catch (Exception ex) { aviso.Text = "Não foi possível acessar as viagens: " + ex.Message; aviso.Visible = true; grid.DataSource = null; }
    }

    private Viagem? Selecionada() => grid.CurrentRow?.DataBoundItem as Viagem;

    private void Nova()
    {
        using var f = new FormNovaViagem();
        if (f.ShowDialog(this) == DialogResult.OK) Carregar();
    }

    private void Editar()
    {
        var v = Selecionada();
        if (v is null) { MessageBox.Show("Selecione uma viagem."); return; }
        using var f = new FormEditarViagem(v);
        if (f.ShowDialog(this) == DialogResult.OK) Carregar();
    }

    private void Excluir()
    {
        var v = Selecionada();
        if (v is null) { MessageBox.Show("Selecione uma viagem."); return; }
        if (MessageBox.Show($"Excluir a viagem '{v.Nome}' e seus lançamentos?", "FIGO", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { service.Excluir(v.Id); Carregar(); }
        catch (Exception ex) { MessageBox.Show("Erro ao excluir: " + ex.Message, "FIGO", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

internal sealed class FormEditarViagem : Form
{
    private readonly Viagem original;
    private readonly ViagemServices service = new();
    private readonly TextBox nome = new();
    private readonly TextBox destino = new();
    private readonly DateTimePicker inicio = new();
    private readonly DateTimePicker fim = new();
    private readonly NumericUpDown participantes = new();

    public FormEditarViagem(Viagem v)
    {
        original = v;
        Text = "Editar viagem"; Width = 470; Height = 360; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        BackColor = UiTheme.Background; Font = UiTheme.Font(9f);
        nome.Text = v.Nome; destino.Text = v.Destino; inicio.Value = v.DataInicio; fim.Value = v.DataFim; participantes.Value = v.QuantidadeParticipantes;
        Montar();
    }

    private void Montar()
    {
        int y = 26;
        Add("Nome da viagem", nome, ref y); Add("Destino", destino, ref y); Add("Data de início", inicio, ref y); Add("Data de fim", fim, ref y); Add("Participantes", participantes, ref y);
        inicio.Format = fim.Format = DateTimePickerFormat.Short; participantes.Maximum = 10000;
        var salvar = UiTheme.PrimaryButton("Salvar alterações"); salvar.Width = 145; salvar.Left = 274; salvar.Top = 270; salvar.Click += (_, _) => Salvar(); Controls.Add(salvar);
    }
    private void Add(string label, Control c, ref int y) { Controls.Add(new Label { Text = label, Left = 28, Top = y + 7, Width = 120, ForeColor = UiTheme.Text }); c.Left = 155; c.Top = y; c.Width = 264; Controls.Add(c); y += 45; }
    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(nome.Text) || string.IsNullOrWhiteSpace(destino.Text) || fim.Value.Date < inicio.Value.Date) { MessageBox.Show("Revise os dados informados."); return; }
        original.Nome = nome.Text.Trim(); original.Destino = destino.Text.Trim(); original.DataInicio = inicio.Value.Date; original.DataFim = fim.Value.Date; original.QuantidadeParticipantes = (int)participantes.Value;
        try { service.Editar(original); DialogResult = DialogResult.OK; Close(); } catch (Exception ex) { MessageBox.Show("Erro ao salvar: " + ex.Message); }
    }
}
