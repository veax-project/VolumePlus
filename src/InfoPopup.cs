using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VolumePlus;

/// <summary>Petite carte d'info (charte indigo) affichee au clic sur le bouton ⓘ.</summary>
public sealed class InfoPopup : Form
{
    private readonly string _title;
    private readonly string _body;

    public bool AutoClose = true;

    public InfoPopup(string title, string body)
    {
        _title = title;
        _body = body;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Palette.Window;
        TopMost = true;
        KeyPreview = true;
        DoubleBuffered = true;
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassStyle |= 0x00020000; return cp; } // ombre
    }

    private int S(float px) => (int)Math.Round(px * DeviceDpi / 96f);
    private float Sf(float px) => px * DeviceDpi / 96f;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Native.EnableRoundedCorners(Handle);

        int pad = S(18);
        int W = S(326);
        int textW = W - 2 * pad;

        var titleFont = UiFont.Semi(Sf(14.5f));
        var bodyFont = UiFont.Ui(Sf(12.5f));

        var meas = new Size(textW, int.MaxValue);
        var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
        int titleH = TextRenderer.MeasureText(_title, titleFont, meas, flags).Height;
        int bodyH = TextRenderer.MeasureText(_body, bodyFont, meas, flags).Height + S(2);

        int y = pad;

        Controls.Add(new Label
        {
            Text = _title, Font = titleFont, ForeColor = Palette.TextTitle, BackColor = Palette.Window,
            AutoSize = false, UseMnemonic = false, Bounds = new Rectangle(pad, y, textW, titleH)
        });
        y += titleH + S(9);

        Controls.Add(new Label
        {
            Text = _body, Font = bodyFont, ForeColor = Palette.TextMuted, BackColor = Palette.Window,
            AutoSize = false, UseMnemonic = false, Bounds = new Rectangle(pad, y, textW, bodyH)
        });
        y += bodyH + S(16);

        var ok = new HoverButton
        {
            Text = "Got it",
            Font = UiFont.Semi(Sf(12.5f)),
            BackColor = Palette.Window,
            FillNormal = Palette.Accent,
            FillHover = Palette.AccentHover,
            FillPress = Palette.AccentPress,
            BorderColor = Palette.AccentHover,
            LabelColor = Color.White,
            Size = new Size(S(90), S(30))
        };
        ok.Click += (_, _) => Close();
        Controls.Add(ok);
        ok.Location = new Point(W - pad - ok.Width, y);
        y += ok.Height + pad;

        ClientSize = new Size(W, y);
    }

    public void ShowCenteredOver(Form owner)
    {
        Show();
        var r = owner.Bounds;
        Location = new Point(r.X + (r.Width - Width) / 2, r.Y + (r.Height - Height) / 2);
        Native.SetForegroundWindow(Handle);
        Activate();
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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Palette.BtnBorder);
        using var path = Draw.Rounded(new RectangleF(0, 0, Width - 1, Height - 1), S(12));
        e.Graphics.DrawPath(pen, path);
    }
}
