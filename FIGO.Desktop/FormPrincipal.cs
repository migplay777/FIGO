namespace FIGO.Desktop;

public class FormPrincipal : Form
{
    private readonly Panel painelConteudo = new();
    private readonly FlowLayoutPanel menu = new();
    private readonly Dictionary<Button, Func<UserControl>> telas = new();
    private Button? botaoAtivo;

    public FormPrincipal()
    {
        Text = "FIGO - Relatório Financeiro de Viagens Missionárias";
        MinimumSize = new Size(1180, 720);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.Background;
        Font = UiTheme.Font(9f);

        var header = CriarHeader();
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        var lateral = CriarMenuLateral();

        painelConteudo.Dock = DockStyle.Fill;
        painelConteudo.BackColor = UiTheme.Background;
        painelConteudo.Padding = new Padding(18, 14, 18, 18);

        corpo.Controls.Add(painelConteudo);
        corpo.Controls.Add(lateral);
        Controls.Add(corpo);
        Controls.Add(header);

        AbrirTela(telas.Keys.First());
    }

    private Control CriarHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 116, BackColor = UiTheme.Navy };

        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "figo_logo.png");
        if (File.Exists(logoPath))
        {
            var picture = new PictureBox
            {
                Left = 18,
                Top = 12,
                Width = 280,
                Height = 92,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Image.FromFile(logoPath),
                BackColor = UiTheme.Navy
            };
            header.Controls.Add(picture);
        }
        else
        {
            header.Controls.Add(new Label
            {
                Text = "🫐  FIGO\n      Viagens Missionárias · Relatórios Financeiros",
                ForeColor = Color.White,
                Font = UiTheme.Font(19, FontStyle.Bold),
                Left = 20,
                Top = 28,
                AutoSize = true
            });
        }

        var divisor = new Panel { Left = 315, Top = 16, Width = 1, Height = 82, BackColor = Color.FromArgb(70, 96, 125) };
        header.Controls.Add(divisor);

        header.Controls.Add(new Label
        {
            Text = "Juntos no propósito,\naté os confins da terra.",
            ForeColor = Color.FromArgb(205, 216, 231),
            Font = UiTheme.Font(11f, FontStyle.Italic),
            Left = 345,
            Top = 29,
            AutoSize = true
        });
        header.Controls.Add(new Label
        {
            Text = "Atos 1:8",
            ForeColor = Color.FromArgb(155, 174, 199),
            Font = UiTheme.Font(7.5f),
            Left = 345,
            Top = 77,
            AutoSize = true
        });

        var usuario = new Label
        {
            Text = "🔔      ●  Miguel   ⌄",
            ForeColor = Color.White,
            Font = UiTheme.Font(10.5f),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Top = 43
        };
        header.Controls.Add(usuario);
        header.Resize += (_, _) => usuario.Left = header.ClientSize.Width - usuario.Width - 28;
        return header;
    }

    private Control CriarMenuLateral()
    {
        var lateral = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = UiTheme.Navy };
        menu.Dock = DockStyle.Top;
        menu.FlowDirection = FlowDirection.TopDown;
        menu.WrapContents = false;
        menu.Height = 370;
        menu.Padding = new Padding(8, 8, 8, 0);
        menu.BackColor = UiTheme.Navy;
        lateral.Controls.Add(menu);

        AdicionarItem("⌂   Dashboard", () => new DashboardView());
        AdicionarItem("✈   Viagens", () => new ViagensView());
        AdicionarItem("$   Receitas", () => new ReceitasView());
        AdicionarItem("▣   Despesas", () => new DespesasView());
        AdicionarItem("▥   Relatórios", () => new RelatoriosView());
        AdicionarItem("⚙   Configurações", () => new ConfiguracoesView());

        var rodape = new Label
        {
            Text = "♜   Igreja Família da Fé\n\nv1.1.0",
            ForeColor = Color.FromArgb(220, 231, 244),
            Font = UiTheme.Font(9f),
            AutoSize = true,
            Left = 26,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        lateral.Controls.Add(rodape);
        lateral.Resize += (_, _) => rodape.Top = lateral.ClientSize.Height - 82;
        return lateral;
    }

    private void AdicionarItem(string texto, Func<UserControl> tela)
    {
        var botao = new RoundedButton
        {
            Text = texto,
            Width = 210,
            Height = 46,
            Margin = new Padding(0, 2, 0, 4),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(221, 232, 245),
            BackColor = UiTheme.Navy,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            Font = UiTheme.Font(10f),
            Cursor = Cursors.Hand,
            Radius = 8
        };
        botao.FlatAppearance.BorderSize = 0;
        botao.FlatAppearance.MouseOverBackColor = Color.FromArgb(31, 68, 109);
        botao.Click += (_, _) => AbrirTela(botao);
        menu.Controls.Add(botao);
        telas[botao] = tela;
    }

    private void AbrirTela(Button botao)
    {
        if (botaoAtivo is not null)
        {
            botaoAtivo.BackColor = UiTheme.Navy;
            botaoAtivo.Font = UiTheme.Font(10f);
        }

        botaoAtivo = botao;
        botaoAtivo.BackColor = Color.FromArgb(47, 93, 147);
        botaoAtivo.Font = UiTheme.Font(10f, FontStyle.Bold);

        painelConteudo.SuspendLayout();
        foreach (Control c in painelConteudo.Controls) c.Dispose();
        painelConteudo.Controls.Clear();
        var view = telas[botao]();
        view.Dock = DockStyle.Fill;
        painelConteudo.Controls.Add(view);
        painelConteudo.ResumeLayout();
    }
}
