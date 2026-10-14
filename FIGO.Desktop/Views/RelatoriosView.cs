using FIGO.backend;
using System.Text;

namespace FIGO.Desktop;
public class RelatoriosView : UserControl
{
    private readonly ResumoService service = new();
    private readonly DataGridView grid = new();
    private readonly Label aviso = ViewHelpers.EmptyOrError("");
    public RelatoriosView()
    {
        BackColor = UiTheme.Background;
        var header = ViewHelpers.Header("Relatórios financeiros", "Consulte o saldo consolidado por viagem e exporte os resultados.");
        var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Color.White };
        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
        var atualizar = ViewHelpers.SecondaryButton("Atualizar"); atualizar.Click += (_, _) => Carregar();
        var exportar = ViewHelpers.SecondaryButton("Exportar CSV"); exportar.Width = 135; exportar.Click += (_, _) => Exportar();
        barra.Controls.Add(atualizar); barra.Controls.Add(exportar);
        ViewHelpers.EstilizarGrid(grid); grid.Dock = DockStyle.Fill;
        foreach (var (titulo, campo) in new[] { ("Viagem", "Nome"), ("Destino", "Destino"), ("Participantes", "QuantidadeParticipantes"), ("Receitas", "TotalReceitas"), ("Despesas", "TotalDespesas"), ("Saldo", "Saldo") })
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, DataPropertyName = campo });
        foreach (var i in new[] { 3, 4, 5 }) grid.Columns[i].DefaultCellStyle.Format = "C2";
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        card.Controls.Add(grid); card.Controls.Add(aviso); card.Controls.Add(barra);
        Controls.Add(card); Controls.Add(header);
        Load += (_, _) => Carregar();
    }
    private void Carregar()
    {
        try { grid.DataSource = service.ListarResumos(); aviso.Text = ""; }
        catch (Exception ex) { aviso.Text = "Não foi possível carregar o relatório: " + ex.Message; grid.DataSource = null; }
    }
    private void Exportar()
    {
        if (grid.DataSource is not List<ResumoViagem> itens || itens.Count == 0) { MessageBox.Show("Não há dados para exportar."); return; }
        using var dialogo = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "FIGO_relatorio.csv" };
        if (dialogo.ShowDialog() != DialogResult.OK) return;
        static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
        var linhas = new List<string> { "Viagem;Destino;Participantes;Receitas;Despesas;Saldo" };
        foreach (var v in itens) linhas.Add(string.Join(";", Csv(v.Nome), Csv(v.Destino), v.QuantidadeParticipantes, v.TotalReceitas.ToString(System.Globalization.CultureInfo.InvariantCulture), v.TotalDespesas.ToString(System.Globalization.CultureInfo.InvariantCulture), v.Saldo.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        try { File.WriteAllLines(dialogo.FileName, linhas, new UTF8Encoding(true)); MessageBox.Show("Relatório exportado com sucesso."); }
        catch (Exception ex) { MessageBox.Show("Erro ao exportar: " + ex.Message); }
    }
}
