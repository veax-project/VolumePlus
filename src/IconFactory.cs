using System.Drawing;
using System.Drawing.Drawing2D;

namespace VolumePlus;

/// <summary>Genere l'icone (barre des taches / fenetre) : haut-parleur blanc + ondes indigo sur pastille sombre.</summary>
public static class IconFactory
{
    private static readonly Color Bg = Color.FromArgb(24, 26, 27);
    private static readonly Color Accent = Palette.Accent;
    private static readonly Color Glyph = Color.White;

    public static Icon CreateAppIcon(int size = 32)
    {
        using var bmp = Draw(size);
        IntPtr hIcon = bmp.GetHicon();
        using var tmp = Icon.FromHandle(hIcon);
        var icon = (Icon)tmp.Clone();
        Native.DestroyIconSafe(hIcon);
        return icon;
    }

    public static Bitmap Draw(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        float pad = size * 0.06f;
        var rect = new RectangleF(pad, pad, size - 2 * pad, size - 2 * pad);

        // Pastille sombre + lisere indigo.
        using (var bg = new SolidBrush(Bg))
            g.FillEllipse(bg, rect);
        using (var pen = new Pen(Accent, MathF.Max(1f, size * 0.06f)))
            g.DrawEllipse(pen, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);

        // Haut-parleur (corps + cone).
        float bx = size * 0.28f;
        float by = size * 0.40f;
        float bw = size * 0.12f;
        float bh = size * 0.20f;
        using (var gb = new SolidBrush(Glyph))
        {
            g.FillRectangle(gb, bx, by, bw, bh);
            var cone = new[]
            {
                new PointF(bx + bw, by + bh / 2f - size * 0.16f),
                new PointF(bx + bw + size * 0.14f, by + bh / 2f - size * 0.16f),
                new PointF(bx + bw + size * 0.14f, by + bh / 2f + size * 0.16f),
                new PointF(bx + bw, by + bh / 2f + size * 0.16f),
            };
            g.FillPolygon(gb, cone);
        }

        // Ondes indigo.
        float wcx = size * 0.56f;
        float wcy = size * 0.50f;
        using (var pen = new Pen(Accent, MathF.Max(1.4f, size * 0.055f))
        { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawArc(pen, wcx, wcy - size * 0.10f, size * 0.14f, size * 0.20f, -55, 110);
            g.DrawArc(pen, wcx - size * 0.02f, wcy - size * 0.17f, size * 0.24f, size * 0.34f, -55, 110);
        }

        return bmp;
    }
}
