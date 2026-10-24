using System.Drawing.Drawing2D;

namespace FIGO.Desktop;

// Marca vetorial: desenhada na resolução da tela, sem emojis ou arquivos externos.
internal sealed class FigoLogo : Control
{
    public FigoLogo() { DoubleBuffered = true; SetStyle(ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 1 || Height < 1) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var state = g.Save();
        float escala = Math.Min(Width / 100f, Height / 110f);
        g.TranslateTransform((Width - 100 * escala) / 2, (Height - 110 * escala) / 2);
        g.ScaleTransform(escala, escala);
        using var fruto = new GraphicsPath();
        fruto.AddBezier(49, 21, 48, 42, 13, 43, 13, 73);
        fruto.AddBezier(13, 73, 13, 108, 88, 109, 88, 73);
        fruto.AddBezier(88, 73, 88, 44, 62, 40, 59, 21);
        fruto.CloseFigure();
        using var roxo = new LinearGradientBrush(new Rectangle(12, 20, 78, 85), Color.FromArgb(163, 104, 190), Color.FromArgb(76, 38, 106), 55f);
        g.FillPath(roxo, fruto);
        using var folha = new GraphicsPath();
        folha.AddBezier(54, 25, 56, 3, 79, 3, 89, 8);
        folha.AddBezier(89, 8, 82, 27, 63, 32, 54, 25);
        using var verde = new SolidBrush(Color.FromArgb(119, 173, 116));
        g.FillPath(verde, folha);
        using var veio = new Pen(Color.FromArgb(202, 225, 174), 1.5f);
        g.DrawBezier(veio, 55, 25, 63, 18, 72, 14, 82, 11);
        using var brilho = new Pen(Color.FromArgb(110, 244, 215, 250), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawBezier(brilho, 36, 49, 22, 63, 22, 78, 31, 85);
        g.Restore(state);
    }
}
