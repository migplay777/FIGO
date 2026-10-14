using System.Drawing.Drawing2D;

namespace FIGO.Desktop;

internal static class UiTheme
{
    public static readonly Color Navy = Color.FromArgb(13, 31, 55);
    public static readonly Color Navy2 = Color.FromArgb(25, 58, 96);
    public static readonly Color Blue = Color.FromArgb(46, 135, 231);
    public static readonly Color LightBlue = Color.FromArgb(232, 242, 255);
    public static readonly Color Background = Color.FromArgb(246, 249, 253);
    public static readonly Color Card = Color.White;
    public static readonly Color Border = Color.FromArgb(224, 231, 240);
    public static readonly Color Text = Color.FromArgb(23, 49, 82);
    public static readonly Color Muted = Color.FromArgb(93, 111, 137);
    public static readonly Color Green = Color.FromArgb(42, 174, 108);
    public static readonly Color Red = Color.FromArgb(244, 79, 93);
    public static readonly Color Purple = Color.FromArgb(120, 72, 193);
    public static readonly Color Orange = Color.FromArgb(246, 166, 38);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);

    public static RoundedPanel CardPanel(int padding = 14)
    {
        return new RoundedPanel
        {
            BackColor = Card,
            Padding = new Padding(padding),
            Margin = new Padding(6),
            BorderColor = Border,
            Radius = 12
        };
    }

    public static Button PrimaryButton(string text)
    {
        var button = new RoundedButton
        {
            Text = text,
            Height = 38,
            AutoSize = false,
            BackColor = Blue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = Font(9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Radius = 9
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}

internal class RoundedPanel : Panel
{
    public int Radius { get; set; } = 12;
    public Color BorderColor { get; set; } = Color.Transparent;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        Resize += (_, _) => AtualizarRegiao();
    }

    private void AtualizarRegiao()
    {
        if (Width <= 0 || Height <= 0) return;
        using var path = CriarCaminho(ClientRectangle, Radius);
        Region = new Region(path);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (BorderColor == Color.Transparent || Width < 2 || Height < 2) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(BorderColor, 1);
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CriarCaminho(rect, Radius);
        e.Graphics.DrawPath(pen, path);
    }

    protected static GraphicsPath CriarCaminho(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(1, radius * 2);
        var arc = new Rectangle(rect.X, rect.Y, d, d);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - d;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - d;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class RoundedButton : Button
{
    public int Radius { get; set; } = 9;
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width <= 0 || Height <= 0) return;
        using var path = new GraphicsPath();
        int d = Radius * 2;
        var r = new Rectangle(0, 0, Width, Height);
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }
}
