using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace VolumePlus;

/// <summary>Palette sombre, accent indigo #5562EA (identique a MuteBind pour la coherence).</summary>
public static class Palette
{
    public static Color Window     = Hex("#1F1F1F");
    public static Color TitleBar   = Hex("#191919");
    public static Color TitleRule  = Hex("#262626");
    public static Color Footer     = Hex("#1B1B1B");
    public static Color Rule       = Hex("#2A2A2A");

    public static Color BtnFill    = Hex("#2B2B2B");
    public static Color BtnHover   = Hex("#343434");
    public static Color BtnPress   = Hex("#262626");
    public static Color BtnBorder  = Hex("#3A3A3A");

    public static Color Accent     = Hex("#5562EA");
    public static Color AccentHover= Hex("#6570EE");
    public static Color AccentPress= Hex("#4650C9");
    public static Color Track      = Hex("#333333");

    public static Color TextTitle  = Hex("#F5F5F5");
    public static Color TextBody   = Hex("#E6E6E6");
    public static Color TextMuted  = Hex("#9A9A9A");
    public static Color TextFaint  = Hex("#7A7A7A");
    public static Color Warn       = Hex("#C9A227");

    public static Color SwitchOff       = Hex("#2E2E2E");
    public static Color SwitchOffBorder = Hex("#4A4A4A");
    public static Color Knob            = Hex("#FFFFFF");
    public static Color KnobOff         = Hex("#C8C8C8");

    public static Color CaptionFg    = Hex("#B0B0B0");
    public static Color CaptionHover = Hex("#2A2A2A");
    public static Color CloseHover   = Hex("#C42B1C");

    public static Color Hex(string h)
    {
        h = h.TrimStart('#');
        return Color.FromArgb(
            Convert.ToInt32(h.Substring(0, 2), 16),
            Convert.ToInt32(h.Substring(2, 2), 16),
            Convert.ToInt32(h.Substring(4, 2), 16));
    }
}

/// <summary>Choix des polices (avec repli si non installees) — tailles en PIXELS.</summary>
public static class UiFont
{
    private static readonly HashSet<string> Installed = LoadFamilies();

    private static HashSet<string> LoadFamilies()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var ifc = new InstalledFontCollection();
            foreach (var f in ifc.Families) set.Add(f.Name);
        }
        catch { }
        return set;
    }

    private static string Pick(params string[] names)
    {
        foreach (var n in names)
            if (Installed.Contains(n)) return n;
        return names[^1];
    }

    public static readonly string Family     = Pick("Segoe UI Variable Text", "Segoe UI", "Tahoma");
    public static readonly string SemiFamily = Pick("Segoe UI Semibold", "Segoe UI Variable Display", "Segoe UI");
    public static readonly string MonoFamily = Pick("Cascadia Mono", "Consolas", "Courier New");

    public static Font Ui(float px, FontStyle style = FontStyle.Regular) => new(Family, px, style, GraphicsUnit.Pixel);
    public static Font Semi(float px) => new(SemiFamily, px, FontStyle.Regular, GraphicsUnit.Pixel);
    public static Font Mono(float px, FontStyle style = FontStyle.Regular) => new(MonoFamily, px, style, GraphicsUnit.Pixel);
}

/// <summary>Aides de dessin (chemins arrondis).</summary>
public static class Draw
{
    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        if (radius <= 0.5f) { p.AddRectangle(r); p.CloseFigure(); return p; }
        float d = radius * 2f;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

/// <summary>Controle de base double-bufferise peint entierement a la main.</summary>
public abstract class DrawControl : Control
{
    protected DrawControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
    }

    protected float DpiScale => DeviceDpi / 96f;
}

/// <summary>Bouton plat arrondi avec etats normal / survol / presse.</summary>
public sealed class HoverButton : DrawControl
{
    public Color FillNormal = Palette.BtnFill;
    public Color FillHover = Palette.BtnHover;
    public Color FillPress = Palette.BtnPress;
    public Color BorderColor = Palette.BtnBorder;
    public Color LabelColor = Palette.TextBody;
    public float Radius = 6f;

    private bool _hover, _press;

    public HoverButton() { Cursor = Cursors.Hand; }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _press = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _press = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _press = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Draw.Rounded(r, Radius * DpiScale);
        Color fill = _press ? FillPress : (_hover ? FillHover : FillNormal);
        using (var b = new SolidBrush(fill)) g.FillPath(b, path);
        if (BorderColor.A > 0) { using var pen = new Pen(BorderColor, 1f); g.DrawPath(pen, path); }

        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), LabelColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

/// <summary>Interrupteur (toggle) style WinUI : 40x20, pastille 12.</summary>
public sealed class ToggleSwitch : DrawControl
{
    private bool _on;
    public event EventHandler? Toggled;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool On
    {
        get => _on;
        set { if (_on != value) { _on = value; Invalidate(); } }
    }

    public ToggleSwitch() { Cursor = Cursors.Hand; Size = new Size(40, 20); }

    protected override void OnClick(EventArgs e)
    {
        _on = !_on; Invalidate();
        Toggled?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Draw.Rounded(r, r.Height / 2f);
        using (var b = new SolidBrush(_on ? Palette.Accent : Palette.SwitchOff)) g.FillPath(b, path);
        using (var pen = new Pen(_on ? Palette.Accent : Palette.SwitchOffBorder, 1f)) g.DrawPath(pen, path);

        float knob = Height * 0.6f;
        float pad = (Height - knob) / 2f;
        float x = _on ? (Width - knob - pad) : pad;
        using var kb = new SolidBrush(_on ? Palette.Knob : Palette.KnobOff);
        g.FillEllipse(kb, x, pad, knob, knob);
    }
}

/// <summary>Bouton de la barre de titre (reduire / fermer), dessine en vectoriel.</summary>
public sealed class CaptionButton : DrawControl
{
    public enum Kind { Minimize, Close, Info }

    public Kind Type = Kind.Close;
    public Color HoverBg = Palette.CaptionHover;
    public Color HoverFg = Palette.TextBody;

    private bool _hover;

    public CaptionButton() { Cursor = Cursors.Hand; Size = new Size(44, 31); }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        if (_hover)
        {
            using var b = new SolidBrush(HoverBg);
            g.FillRectangle(b, 0, 0, Width, Height);
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color fg = _hover ? HoverFg : Palette.CaptionFg;
        float cx = Width / 2f, cy = Height / 2f;
        float s = 5f * DpiScale;
        using var pen = new Pen(fg, 1.3f * DpiScale) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        if (Type == Kind.Minimize)
        {
            g.DrawLine(pen, cx - s, cy, cx + s, cy);
        }
        else if (Type == Kind.Info)
        {
            float r = 6.5f * DpiScale;
            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            using var fb = new SolidBrush(fg);
            float dot = 1.15f * DpiScale;
            g.FillEllipse(fb, cx - dot, cy - r * 0.52f - dot, dot * 2, dot * 2);
            g.DrawLine(pen, cx, cy - r * 0.1f, cx, cy + r * 0.5f);
        }
        else
        {
            g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
            g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
        }
    }
}
