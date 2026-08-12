using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VolumePlus;

/// <summary>Une entree du menu de la barre des taches.</summary>
public sealed class TrayMenuItem
{
    public enum Kind { Action, Check, Status, Separator }

    public Kind Type = Kind.Action;
    public string Label = "";
    public Action? OnClick;
    public bool Checked;
    public bool Bold;
    public bool Enabled = true;

    public bool IsClickable => Enabled && OnClick != null && Type is Kind.Action or Kind.Check;

    public static TrayMenuItem Sep() => new() { Type = Kind.Separator, Enabled = false };
    public static TrayMenuItem Act(string label, Action onClick, bool bold = false)
        => new() { Type = Kind.Action, Label = label, OnClick = onClick, Bold = bold };
    public static TrayMenuItem Chk(string label, bool on, Action onClick)
        => new() { Type = Kind.Check, Label = label, Checked = on, OnClick = onClick };
    public static TrayMenuItem Stat(string label)
        => new() { Type = Kind.Status, Label = label, Enabled = false };
}

/// <summary>Menu contextuel 100% dessine a la main (pop-up), charte indigo.</summary>
public sealed class TrayMenu : Form
{
    private static readonly Color Surface = Palette.Window;
    private static readonly Color Border = Palette.BtnBorder;
    private static readonly Color Hover = Color.FromArgb(46, 50, 69);

    private readonly List<TrayMenuItem> _items;
    private readonly Rectangle[] _bounds;
    private int _hover = -1;

    public bool AutoClose = true;

    public TrayMenu(List<TrayMenuItem> items)
    {
        _items = items;
        _bounds = new Rectangle[items.Count];

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Surface;
        TopMost = true;
        KeyPreview = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassStyle |= 0x00020000; return cp; } // ombre
    }

    private int S(float px) => (int)Math.Round(px * DeviceDpi / 96f);
    private float Sf(float px) => px * DeviceDpi / 96f;

    private Font ItemFont(bool bold) => bold ? UiFont.Semi(Sf(13)) : UiFont.Ui(Sf(13));
    private Font StatusFont() => UiFont.Ui(Sf(12));

    private void BuildLayout()
    {
        int pad = S(6);
        int textLeft = S(38);
        int rightPad = S(22);

        int maxText = S(150);
        foreach (var it in _items)
        {
            if (it.Type == TrayMenuItem.Kind.Separator) continue;
            var f = it.Type == TrayMenuItem.Kind.Status ? StatusFont() : ItemFont(it.Bold);
            int w = TextRenderer.MeasureText(it.Label, f, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            maxText = Math.Max(maxText, w);
        }
        int width = textLeft + maxText + rightPad;

        int y = pad;
        for (int i = 0; i < _items.Count; i++)
        {
            int h = _items[i].Type == TrayMenuItem.Kind.Separator ? S(11) : S(34);
            _bounds[i] = new Rectangle(0, y, width, h);
            y += h;
        }

        ClientSize = new Size(width, y + pad);
    }

    public void ShowAtCursor()
    {
        _ = Handle;
        Native.EnableRoundedCorners(Handle);
        BuildLayout();

        var p = Cursor.Position;
        var wa = Screen.FromPoint(p).WorkingArea;
        int x = p.X - Width;
        if (x < wa.Left) x = p.X;
        if (x + Width > wa.Right) x = wa.Right - Width;
        int yy = p.Y - Height;
        if (yy < wa.Top) yy = p.Y;
        if (yy + Height > wa.Bottom) yy = wa.Bottom - Height;
        Location = new Point(x, yy);

        Show();
        Native.SetForegroundWindow(Handle);
        Activate();
    }

    public void ShowDemoCentered()
    {
        _ = Handle;
        Native.EnableRoundedCorners(Handle);
        BuildLayout();
        var wa = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(wa.X + (wa.Width - Width) / 2, wa.Y + (wa.Height - Height) / 2);
        Show();
        Native.SetForegroundWindow(Handle);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (AutoClose) Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Close();
        base.OnKeyDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int h = HitTest(e.Y);
        if (h != _hover) { _hover = h; Invalidate(); }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_hover != -1) { _hover = -1; Invalidate(); }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        int i = HitTest(e.Y);
        if (i >= 0 && _items[i].IsClickable)
        {
            var action = _items[i].OnClick;
            Close();
            action?.Invoke();
        }
        base.OnMouseClick(e);
    }

    private int HitTest(int y)
    {
        for (int i = 0; i < _items.Count; i++)
            if (_items[i].IsClickable && _bounds[i].Top <= y && y < _bounds[i].Bottom)
                return i;
        return -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        for (int i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            var r = _bounds[i];

            if (it.Type == TrayMenuItem.Kind.Separator)
            {
                using var pen = new Pen(Palette.Rule);
                int cy = r.Top + r.Height / 2;
                g.DrawLine(pen, r.Left + S(12), cy, r.Right - S(12), cy);
                continue;
            }

            if (i == _hover && it.IsClickable)
            {
                var pill = new RectangleF(S(4), r.Top + S(2), r.Width - S(8), r.Height - S(4));
                using var path = Draw.Rounded(pill, S(7));
                using var b = new SolidBrush(Hover);
                g.FillPath(b, path);
            }

            if (it.Type == TrayMenuItem.Kind.Check && it.Checked)
            {
                float cx = S(16), cy = r.Top + r.Height / 2f;
                using var pen = new Pen(Palette.Accent, 2f * DeviceDpi / 96f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - S(5), cy),
                    new PointF(cx - S(1), cy + S(4)),
                    new PointF(cx + S(6), cy - S(5))
                });
            }

            bool status = it.Type == TrayMenuItem.Kind.Status;
            Font font = status ? StatusFont() : ItemFont(it.Bold);
            Color col = !it.Enabled
                ? Palette.TextFaint
                : (it.Bold ? Palette.TextTitle : Palette.TextBody);
            var textRect = new Rectangle(S(38), r.Top, r.Width - S(38) - S(16), r.Height);
            TextRenderer.DrawText(g, it.Label, font, textRect, col,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        using var bp = new Pen(Border);
        g.DrawRectangle(bp, 0, 0, Width - 1, Height - 1);
    }
}
