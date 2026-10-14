namespace FIGO.Desktop;

internal static class ViewHelpers
{
    public static Panel Header(string titulo, string subtitulo)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = UiTheme.Background };
        p.Controls.Add(new Label { Text = titulo, Left = 2, Top = 5, AutoSize = true, Font = UiTheme.Font(18, FontStyle.Bold), ForeColor = UiTheme.Text });
        p.Controls.Add(new Label { Text = subtitulo, Left = 2, Top = 40, AutoSize = true, Font = UiTheme.Font(9.5f), ForeColor = UiTheme.Muted });
        return p;
    }

    public static void EstilizarGrid(DataGridView grid)
    {
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Color.FromArgb(236, 240, 246);
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 40;
        grid.RowTemplate.Height = 38;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(247, 249, 252),
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(8.5f, FontStyle.Bold),
            SelectionBackColor = Color.FromArgb(247, 249, 252)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(8.5f),
            SelectionBackColor = Color.FromArgb(235, 244, 255),
            SelectionForeColor = UiTheme.Text
        };
    }

    public static Button SecondaryButton(string text)
    {
        var b = new RoundedButton
        {
            Text = text,
            Width = 105,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(9f, FontStyle.Bold),
            Radius = 8,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = UiTheme.Border;
        return b;
    }

    public static Label EmptyOrError(string texto) => new()
    {
        Text = texto,
        Dock = DockStyle.Top,
        Height = 32,
        ForeColor = Color.FromArgb(178, 83, 35),
        Font = UiTheme.Font(8.5f),
        TextAlign = ContentAlignment.MiddleLeft
    };
}
