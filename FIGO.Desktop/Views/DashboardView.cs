using FIGO.backend;
using System.Drawing.Drawing2D;

namespace FIGO.Desktop;

public class DashboardView : UserControl
{
    private readonly ResumoService resumoService = new();
    private readonly DespesaService despesaService = new();
    private readonly ViagemServices viagemService = new();

    private readonly ComboBox cmbViagem = new();
    private readonly Label lblData = new();
    private readonly FlowLayoutPanel cards = new();
    private readonly DoughnutPanel graficoCategorias = new();
    private readonly BarsPanel graficoComparativo = new();
    private readonly DataGridView gridViagens = new();
    private readonly Label lblTripTitulo = new();
    private readonly Label lblTripPeriodo = new();
    private readonly Label lblTripParticipantes = new();
    private readonly Label lblErro = new();

    private readonly Dictionary<int, Viagem> viagensPorId = new();
    private bool carregando;

    public DashboardView()
    {
        BackColor = UiTheme.Background;
        Dock = DockStyle.Fill;
        ConstruirTela();
        Load += (_, _) => CarregarDados();
    }

    private void ConstruirTela()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 77));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));

        var principal = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.Background, Padding = new Padding(0, 0, 8, 0) };
        var direita = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.Background, Padding = new Padding(8, 0, 0, 0) };

        root.Controls.Add(principal, 0, 0);
        root.Controls.Add(direita, 1, 0);
        Controls.Add(root);

        ConstruirPrincipal(principal);
        ConstruirDireita(direita);
    }

    private void ConstruirPrincipal(Panel principal)
    {
        var cabecalho = new Panel { Dock = DockStyle.Top, Height = 94, BackColor = UiTheme.Background };
        cabecalho.Controls.Add(new Label
        {
            Text = "Bem-vindo, Miguel!",
            Font = UiTheme.Font(18, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Left = 2,
            Top = 7
        });
        cabecalho.Controls.Add(new Label
        {
            Text = "Aqui você acompanha o resumo financeiro das viagens missionárias.",
            Font = UiTheme.Font(9.5f),
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Left = 2,
            Top = 39
        });

        lblData.Text = $"▣  {DateTime.Today:ddd, dd 'de' MMMM 'de' yyyy}";
        lblData.ForeColor = UiTheme.Muted;
        lblData.Font = UiTheme.Font(8.5f);
        lblData.AutoSize = true;
        lblData.Top = 7;
        cabecalho.Controls.Add(lblData);

        var lblViagem = new Label { Text = "Viagem atual", ForeColor = UiTheme.Text, Font = UiTheme.Font(8.2f), AutoSize = true, Top = 34 };
        cabecalho.Controls.Add(lblViagem);
        cmbViagem.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbViagem.Font = UiTheme.Font(9f);
        cmbViagem.Height = 30;
        cmbViagem.Top = 53;
        cmbViagem.Width = 285;
        cmbViagem.SelectedIndexChanged += (_, _) => { if (!carregando) AtualizarViagemSelecionada(); };
        cabecalho.Controls.Add(cmbViagem);
        cabecalho.Resize += (_, _) =>
        {
            lblData.Left = Math.Max(500, cabecalho.ClientSize.Width - lblData.Width - 6);
            cmbViagem.Left = Math.Max(500, cabecalho.ClientSize.Width - cmbViagem.Width - 6);
            lblViagem.Left = cmbViagem.Left;
        };

        cards.Dock = DockStyle.Top;
        cards.Height = 148;
        cards.FlowDirection = FlowDirection.LeftToRight;
        cards.WrapContents = false;
        cards.BackColor = UiTheme.Background;
        cards.Padding = new Padding(0, 2, 0, 7);

        var graficos = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 255,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 4, 0, 8)
        };
        graficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        graficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var cardCat = UiTheme.CardPanel(12);
        cardCat.Dock = DockStyle.Fill;
        cardCat.Margin = new Padding(0, 0, 7, 0);
        cardCat.Controls.Add(new Label { Text = "Despesas por categoria", Dock = DockStyle.Top, Height = 28, Font = UiTheme.Font(10f, FontStyle.Bold), ForeColor = UiTheme.Text });
        graficoCategorias.Dock = DockStyle.Fill;
        cardCat.Controls.Add(graficoCategorias);
        graficoCategorias.BringToFront();

        var cardComp = UiTheme.CardPanel(12);
        cardComp.Dock = DockStyle.Fill;
        cardComp.Margin = new Padding(7, 0, 0, 0);
        cardComp.Controls.Add(new Label { Text = "Receitas x Despesas", Dock = DockStyle.Top, Height = 28, Font = UiTheme.Font(10f, FontStyle.Bold), ForeColor = UiTheme.Text });
        graficoComparativo.Dock = DockStyle.Fill;
        cardComp.Controls.Add(graficoComparativo);
        graficoComparativo.BringToFront();

        graficos.Controls.Add(cardCat, 0, 0);
        graficos.Controls.Add(cardComp, 1, 0);

        var cardTabela = UiTheme.CardPanel(12);
        cardTabela.Dock = DockStyle.Top;
        cardTabela.Height = 290;
        cardTabela.Margin = new Padding(0);

        var tabelaTopo = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.White };
        tabelaTopo.Controls.Add(new Label { Text = "Últimas viagens cadastradas", Left = 0, Top = 9, AutoSize = true, Font = UiTheme.Font(10.5f, FontStyle.Bold), ForeColor = UiTheme.Text });
        var btnNova = UiTheme.PrimaryButton("+  Nova viagem");
        btnNova.Width = 112;
        btnNova.Top = 2;
        btnNova.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnNova.Click += (_, _) => AbrirNovaViagem();
        tabelaTopo.Controls.Add(btnNova);
        tabelaTopo.Resize += (_, _) => btnNova.Left = tabelaTopo.ClientSize.Width - btnNova.Width;

        ConfigurarGrid();
        cardTabela.Controls.Add(gridViagens);
        cardTabela.Controls.Add(tabelaTopo);

        lblErro.Dock = DockStyle.Top;
        lblErro.Height = 28;
        lblErro.ForeColor = Color.FromArgb(178, 83, 35);
        lblErro.Font = UiTheme.Font(8.5f);
        lblErro.Visible = false;

        principal.Controls.Add(cardTabela);
        principal.Controls.Add(graficos);
        principal.Controls.Add(cards);
        principal.Controls.Add(lblErro);
        principal.Controls.Add(cabecalho);
    }

    private void ConstruirDireita(Panel direita)
    {
        var cardViagem = UiTheme.CardPanel(0);
        cardViagem.Dock = DockStyle.Top;
        cardViagem.Height = 210;
        cardViagem.BackColor = Color.FromArgb(24, 61, 91);
        cardViagem.BorderColor = Color.FromArgb(34, 71, 102);

        var gradiente = new GradientTripPanel { Dock = DockStyle.Fill, Padding = new Padding(18), Radius = 12 };
        lblTripTitulo.Text = "Viagem atual";
        lblTripTitulo.ForeColor = Color.White;
        lblTripTitulo.Font = UiTheme.Font(13.5f, FontStyle.Bold);
        lblTripTitulo.AutoSize = true;
        lblTripTitulo.Left = 16;
        lblTripTitulo.Top = 88;
        lblTripPeriodo.ForeColor = Color.White;
        lblTripPeriodo.Font = UiTheme.Font(9.5f);
        lblTripPeriodo.AutoSize = true;
        lblTripPeriodo.Left = 16;
        lblTripPeriodo.Top = 122;
        lblTripParticipantes.ForeColor = Color.White;
        lblTripParticipantes.Font = UiTheme.Font(9f);
        lblTripParticipantes.AutoSize = true;
        lblTripParticipantes.Left = 16;
        lblTripParticipantes.Top = 157;
        gradiente.Controls.Add(lblTripTitulo);
        gradiente.Controls.Add(lblTripPeriodo);
        gradiente.Controls.Add(lblTripParticipantes);
        cardViagem.Controls.Add(gradiente);

        var atalhos = UiTheme.CardPanel(0);
        atalhos.Dock = DockStyle.Top;
        atalhos.Height = 245;
        atalhos.Margin = new Padding(0, 10, 0, 0);
        string[] itens = ["▣  Resumo da viagem", "▤  Lançar despesa", "↓  Lançar receita", "▤  Ver todas as despesas", "$  Ver todas as receitas"];
        int top = 0;
        foreach (var texto in itens)
        {
            var b = new Button
            {
                Text = texto + "                         ›",
                Left = 0,
                Top = top,
                Height = 47,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UiTheme.Text,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 4, 0),
                Font = UiTheme.Font(9f),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => AcaoRapida(texto);
            atalhos.Controls.Add(b);
            b.BringToFront();
            top += 47;
        }

        var versiculo = UiTheme.CardPanel(15);
        versiculo.Dock = DockStyle.Top;
        versiculo.Height = 120;
        versiculo.Margin = new Padding(0, 10, 0, 0);
        versiculo.BackColor = Color.FromArgb(239, 245, 253);
        versiculo.Controls.Add(new Label
        {
            Text = "❝   “Ide por todo o mundo,\n       pregai o evangelho a toda criatura.”\n\n                                              Marcos 16:15",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(52, 78, 112),
            Font = UiTheme.Font(8.8f, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleLeft
        });

        direita.Controls.Add(versiculo);
        direita.Controls.Add(atalhos);
        direita.Controls.Add(cardViagem);
    }

    private void ConfigurarGrid()
    {
        gridViagens.Dock = DockStyle.Fill;
        gridViagens.AutoGenerateColumns = false;
        gridViagens.ReadOnly = true;
        gridViagens.AllowUserToAddRows = false;
        gridViagens.AllowUserToDeleteRows = false;
        gridViagens.AllowUserToResizeRows = false;
        gridViagens.RowHeadersVisible = false;
        gridViagens.BackgroundColor = Color.White;
        gridViagens.BorderStyle = BorderStyle.None;
        gridViagens.GridColor = Color.FromArgb(236, 240, 246);
        gridViagens.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridViagens.ColumnHeadersHeight = 38;
        gridViagens.RowTemplate.Height = 38;
        gridViagens.EnableHeadersVisualStyles = false;
        gridViagens.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(247, 249, 252),
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(8.2f, FontStyle.Bold),
            SelectionBackColor = Color.FromArgb(247, 249, 252)
        };
        gridViagens.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(8.1f),
            SelectionBackColor = Color.FromArgb(235, 244, 255),
            SelectionForeColor = UiTheme.Text
        };
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome da viagem", DataPropertyName = "Nome", FillWeight = 130 });
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Destino", DataPropertyName = "Destino", FillWeight = 115 });
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Período", DataPropertyName = "Periodo", FillWeight = 145 });
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Participantes", DataPropertyName = "Participantes", FillWeight = 75 });
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Receitas", DataPropertyName = "Receitas", FillWeight = 95 });
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Despesas", DataPropertyName = "Despesas", FillWeight = 95 });
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Saldo", DataPropertyName = "Saldo", FillWeight = 90 });
        gridViagens.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", FillWeight = 85 });
        gridViagens.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private void CarregarDados()
    {
        carregando = true;
        try
        {
            lblErro.Visible = false;
            viagensPorId.Clear();
            var viagens = viagemService.Listar();
            foreach (var v in viagens) viagensPorId[v.Id] = v;

            cmbViagem.DataSource = viagens;
            cmbViagem.DisplayMember = nameof(Viagem.Nome);
            cmbViagem.ValueMember = nameof(Viagem.Id);

            var resumos = resumoService.ListarResumos();
            gridViagens.DataSource = resumos.Take(6).Select(r => ParaLinha(r, viagensPorId.GetValueOrDefault(r.ViagemId))).ToList();

            if (viagens.Count > 0)
            {
                cmbViagem.SelectedIndex = 0;
                AtualizarViagem(viagens[0].Id);
            }
            else
            {
                MostrarVazio();
            }
        }
        catch (Exception ex)
        {
            CarregarDemonstracao();
            lblErro.Text = "Modo de demonstração: não foi possível acessar o MySQL. " + SimplificarErro(ex.Message);
            lblErro.Visible = true;
        }
        finally
        {
            carregando = false;
        }
    }

    private void AtualizarViagemSelecionada()
    {
        if (cmbViagem.SelectedValue is int id) AtualizarViagem(id);
        else if (cmbViagem.SelectedItem is Viagem v) AtualizarViagem(v.Id);
    }

    private void AtualizarViagem(int viagemId)
    {
        try
        {
            var viagem = viagensPorId.GetValueOrDefault(viagemId) ?? viagemService.BuscarPorId(viagemId);
            var resumo = resumoService.ObterResumo(viagemId);
            if (viagem is null || resumo is null) return;

            MontarCards(resumo);
            lblTripTitulo.Text = "✈  " + viagem.Destino;
            lblTripPeriodo.Text = $"{viagem.DataInicio:dd/MM/yyyy}   →   {viagem.DataFim:dd/MM/yyyy}";
            lblTripParticipantes.Text = $"♟  {viagem.QuantidadeParticipantes} participantes";

            graficoComparativo.Receitas = resumo.TotalReceitas;
            graficoComparativo.Despesas = resumo.TotalDespesas;
            graficoComparativo.Invalidate();

            var categorias = despesaService.ObterPorCategoria(viagemId);
            graficoCategorias.Definir(categorias, resumo.TotalDespesas);
        }
        catch (Exception ex)
        {
            lblErro.Text = "Não foi possível atualizar esta viagem: " + SimplificarErro(ex.Message);
            lblErro.Visible = true;
        }
    }

    private void MontarCards(ResumoViagem r)
    {
        cards.SuspendLayout();
        foreach (Control c in cards.Controls) c.Dispose();
        cards.Controls.Clear();
        cards.Controls.Add(CriarCard("↓", "Receitas", Dinheiro(r.TotalReceitas), "Entradas registradas", UiTheme.Green));
        cards.Controls.Add(CriarCard("▣", "Despesas", Dinheiro(r.TotalDespesas), "Saídas registradas", UiTheme.Red));
        cards.Controls.Add(CriarCard("●", "Saldo", Dinheiro(r.Saldo), r.Saldo >= 0 ? "Saldo positivo" : "Atenção ao orçamento", UiTheme.Blue));
        cards.Controls.Add(CriarCard("♟", "Participantes", r.QuantidadeParticipantes.ToString(), "Equipe da viagem", UiTheme.Purple));
        cards.ResumeLayout();
    }

    private static Control CriarCard(string icone, string titulo, string valor, string detalhe, Color cor)
    {
        var card = UiTheme.CardPanel(12);
        card.Width = 220;
        card.Height = 126;
        card.Margin = new Padding(0, 0, 10, 0);

        var bolha = new Label
        {
            Text = icone,
            Width = 45,
            Height = 45,
            Left = 12,
            Top = 12,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = cor,
            ForeColor = Color.White,
            Font = UiTheme.Font(17, FontStyle.Bold)
        };
        card.Controls.Add(bolha);
        card.Controls.Add(new Label { Text = titulo, Left = 70, Top = 17, AutoSize = true, ForeColor = cor, Font = UiTheme.Font(9f, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = valor, Left = 70, Top = 43, AutoSize = true, ForeColor = UiTheme.Text, Font = UiTheme.Font(14f, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = detalhe, Left = 14, Top = 91, AutoSize = true, ForeColor = UiTheme.Muted, Font = UiTheme.Font(8f) });
        return card;
    }

    private void MostrarVazio()
    {
        MontarCards(new ResumoViagem());
        graficoCategorias.Definir([], 0);
        graficoComparativo.Receitas = 0;
        graficoComparativo.Despesas = 0;
        lblTripTitulo.Text = "Nenhuma viagem cadastrada";
        lblTripPeriodo.Text = "Cadastre a primeira viagem para começar";
        lblTripParticipantes.Text = "0 participantes";
    }

    private void CarregarDemonstracao()
    {
        var hoje = DateTime.Today;
        var demos = new[]
        {
            new LinhaViagem("São Paulo - SP", "São Paulo - SP", "12/10/2026 - 15/10/2026", 18, "R$ 8.500,00", "R$ 6.230,00", "R$ 2.270,00", "Em andamento"),
            new LinhaViagem("Campinas - SP", "Campinas - SP", "20/11/2026 - 22/11/2026", 12, "R$ 5.200,00", "R$ 4.850,00", "R$ 350,00", "Planejada"),
            new LinhaViagem("Belo Horizonte - MG", "Belo Horizonte - MG", "15/02/2027 - 20/02/2027", 20, "R$ 12.000,00", "R$ 9.870,00", "R$ 2.130,00", "Planejada"),
            new LinhaViagem("Goiânia - GO", "Goiânia - GO", "10/05/2027 - 14/05/2027", 15, "R$ 7.800,00", "R$ 6.900,00", "R$ 900,00", "Planejada")
        };
        gridViagens.DataSource = demos;
        MontarCards(new ResumoViagem { TotalReceitas = 8500, TotalDespesas = 6230, Saldo = 2270, QuantidadeParticipantes = 18 });
        lblTripTitulo.Text = "✈  São Paulo - SP";
        lblTripPeriodo.Text = "12/10/2026   →   15/10/2026";
        lblTripParticipantes.Text = "♟  18 participantes";
        graficoComparativo.Receitas = 8500;
        graficoComparativo.Despesas = 6230;
        graficoCategorias.Definir(
        [
            new DespesaCategoria { Categoria = "Alimentação", Total = 2100 },
            new DespesaCategoria { Categoria = "Transporte", Total = 1500 },
            new DespesaCategoria { Categoria = "Hospedagem", Total = 1200 },
            new DespesaCategoria { Categoria = "Outros", Total = 1430 }
        ], 6230);
        cmbViagem.DataSource = new[] { "São Paulo - SP (demonstração)" };
    }

    private void AbrirNovaViagem()
    {
        using var form = new FormNovaViagem();
        if (form.ShowDialog(this) == DialogResult.OK) CarregarDados();
    }

    private void AcaoRapida(string acao)
    {
        MessageBox.Show($"Atalho '{acao.Replace("▣  ", "").Replace("▤  ", "").Replace("↓  ", "").Replace("$  ", "")}' disponível pelo menu lateral do FIGO.", "FIGO", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static LinhaViagem ParaLinha(ResumoViagem r, Viagem? v)
    {
        string periodo = v is null ? "—" : $"{v.DataInicio:dd/MM/yyyy} - {v.DataFim:dd/MM/yyyy}";
        string status = v is null ? "—" : (DateTime.Today < v.DataInicio.Date ? "Planejada" : DateTime.Today <= v.DataFim.Date ? "Em andamento" : "Concluída");
        return new LinhaViagem(r.Nome, r.Destino, periodo, r.QuantidadeParticipantes, Dinheiro(r.TotalReceitas), Dinheiro(r.TotalDespesas), Dinheiro(r.Saldo), status);
    }

    private static string Dinheiro(decimal valor) => $"R$ {valor:N2}";
    private static string SimplificarErro(string msg) => msg.Length > 140 ? msg[..140] + "…" : msg;

    private sealed record LinhaViagem(string Nome, string Destino, string Periodo, int Participantes, string Receitas, string Despesas, string Saldo, string Status);
}

internal sealed class DoughnutPanel : Panel
{
    private List<DespesaCategoria> itens = [];
    private decimal total;
    private readonly Color[] cores = [UiTheme.Blue, UiTheme.Green, UiTheme.Orange, UiTheme.Purple, Color.FromArgb(86, 182, 194)];

    public DoughnutPanel() { DoubleBuffered = true; BackColor = Color.White; }

    public void Definir(IEnumerable<DespesaCategoria> categorias, decimal totalDespesas)
    {
        itens = categorias.Take(5).ToList();
        total = totalDespesas;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int tamanho = Math.Min(150, Math.Min(Height - 18, Width / 2 - 10));
        if (tamanho < 60) return;
        var rect = new Rectangle(10, 16, tamanho, tamanho);

        decimal soma = itens.Sum(x => x.Total);
        if (soma <= 0)
        {
            using var p = new Pen(UiTheme.Border, 25);
            e.Graphics.DrawEllipse(p, rect);
        }
        else
        {
            float inicio = -90;
            for (int i = 0; i < itens.Count; i++)
            {
                float angulo = (float)(itens[i].Total / soma * 360m);
                using var p = new Pen(cores[i % cores.Length], 28);
                e.Graphics.DrawArc(p, rect, inicio, angulo);
                inicio += angulo;
            }
        }

        string valor = $"R$ {total:N2}";
        var fonte = UiTheme.Font(9f, FontStyle.Bold);
        var sz = e.Graphics.MeasureString(valor, fonte);
        e.Graphics.DrawString(valor, fonte, new SolidBrush(UiTheme.Text), rect.X + (rect.Width - sz.Width) / 2, rect.Y + rect.Height / 2 - 13);
        e.Graphics.DrawString("Total de despesas", UiTheme.Font(7.5f), new SolidBrush(UiTheme.Muted), rect.X + 29, rect.Y + rect.Height / 2 + 6);

        int x = rect.Right + 25;
        int y = 22;
        for (int i = 0; i < itens.Count; i++)
        {
            using var b = new SolidBrush(cores[i % cores.Length]);
            e.Graphics.FillEllipse(b, x, y + 4, 10, 10);
            e.Graphics.DrawString(itens[i].Categoria, UiTheme.Font(8f), new SolidBrush(UiTheme.Text), x + 18, y);
            decimal pct = total == 0 ? 0 : itens[i].Total / total * 100m;
            e.Graphics.DrawString($"R$ {itens[i].Total:N2}   {pct:N1}%", UiTheme.Font(7.7f), new SolidBrush(UiTheme.Muted), x + 18, y + 18);
            y += 40;
        }
    }
}

internal sealed class BarsPanel : Panel
{
    public decimal Receitas { get; set; }
    public decimal Despesas { get; set; }
    public BarsPanel() { DoubleBuffered = true; BackColor = Color.White; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        decimal max = Math.Max(1m, Math.Max(Receitas, Despesas));
        int baseY = Height - 35;
        int maxH = Math.Max(50, Height - 75);
        int barW = Math.Max(45, Math.Min(85, Width / 6));
        int center = Width / 2;
        int x1 = center - barW - 35;
        int x2 = center + 35;
        int h1 = (int)(Receitas / max * maxH);
        int h2 = (int)(Despesas / max * maxH);

        using var gridPen = new Pen(Color.FromArgb(236, 240, 246));
        for (int i = 0; i <= 4; i++)
        {
            int y = baseY - i * maxH / 4;
            e.Graphics.DrawLine(gridPen, 24, y, Width - 18, y);
        }
        using var green = new SolidBrush(UiTheme.Green);
        using var blue = new SolidBrush(UiTheme.Blue);
        e.Graphics.FillRectangle(green, x1, baseY - h1, barW, h1);
        e.Graphics.FillRectangle(blue, x2, baseY - h2, barW, h2);
        e.Graphics.DrawString($"R$ {Receitas:N2}", UiTheme.Font(7.8f), new SolidBrush(UiTheme.Text), x1 - 2, baseY - h1 - 20);
        e.Graphics.DrawString($"R$ {Despesas:N2}", UiTheme.Font(7.8f), new SolidBrush(UiTheme.Text), x2 - 2, baseY - h2 - 20);
        e.Graphics.DrawString("Receitas", UiTheme.Font(8f), new SolidBrush(UiTheme.Muted), x1, baseY + 7);
        e.Graphics.DrawString("Despesas", UiTheme.Font(8f), new SolidBrush(UiTheme.Muted), x2, baseY + 7);
    }
}

internal sealed class GradientTripPanel : RoundedPanel
{
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(43, 91, 120), Color.FromArgb(17, 41, 61), 35f);
        e.Graphics.FillRectangle(brush, ClientRectangle);
        using var glow = new SolidBrush(Color.FromArgb(35, 250, 168, 116));
        e.Graphics.FillEllipse(glow, Width / 2 - 40, -60, Width, 150);
    }
}
