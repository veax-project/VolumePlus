using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace VolumePlus;

/// <summary>
/// Fenetre principale de Volume+ : un gros pourcentage, un curseur 100%->500%,
/// des raccourcis, et l'option demarrage auto. Chrome sur-mesure indigo (charte MuteBind).
/// </summary>
public sealed class MainWindow : Form
{
    private readonly AppConfig _config;
    private readonly Action _onApplied;

    private Slider _slider = null!;
    private Label _hero = null!;
    private Label _status = null!;
    private ToggleSwitch _toggle = null!;
    private System.Windows.Forms.Timer? _debounce;
    private readonly List<(HoverButton btn, int val)> _presets = new();

    private int _W;
    private const int WinW = 440;
    private const int WinH = 400;

    public MainWindow(AppConfig config, Action onApplied)
    {
        _config = config;
        _onApplied = onApplied;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        ShowInTaskbar = true;
        BackColor = Palette.Window;
        KeyPreview = true;
        Text = "Volume+";
        Icon = AppRes.AppIcon();
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

        _W = S(WinW);
        ClientSize = new Size(_W, S(WinH));
        var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(wa.X + (wa.Width - Width) / 2, wa.Y + (wa.Height - Height) / 2);

        BuildUi();
        RefreshVisuals();
    }

    private void BuildUi()
    {
        int W = _W;
        int titleH = S(32);
        int footerH = S(52);
        int P = S(28);

        // ---------- Barre de titre ----------
        int logoSize = S(16), logoX = S(12);
        var titleBar = new Panel { Bounds = new Rectangle(0, 0, W, titleH), BackColor = Palette.TitleBar };
        titleBar.Paint += (_, ev) =>
        {
            if (Icon != null)
                ev.Graphics.DrawIcon(Icon, new Rectangle(logoX, (titleH - logoSize) / 2, logoSize, logoSize));
            using var pen = new Pen(Palette.TitleRule);
            ev.Graphics.DrawLine(pen, 0, titleH - 1, W, titleH - 1);
        };
        titleBar.MouseDown += (_, ev) => { if (ev.Button == MouseButtons.Left) Native.DragWindow(Handle); };
        Controls.Add(titleBar);

        var titleName = new Label
        {
            Text = "Volume+",
            Font = UiFont.Ui(Sf(12)),
            ForeColor = Palette.TextMuted,
            BackColor = Palette.TitleBar,
            AutoSize = true,
            UseMnemonic = false
        };
        titleBar.Controls.Add(titleName);
        titleName.Location = new Point(logoX + logoSize + S(8), (titleH - titleName.PreferredHeight) / 2);
        titleName.MouseDown += (_, ev) => { if (ev.Button == MouseButtons.Left) Native.DragWindow(Handle); };

        var btnClose = new CaptionButton
        {
            Type = CaptionButton.Kind.Close,
            HoverBg = Palette.CloseHover,
            HoverFg = Color.White,
            BackColor = Palette.TitleBar,
            Size = new Size(S(44), S(31)),
            Location = new Point(W - S(44), 0)
        };
        btnClose.Click += (_, _) => Close();
        titleBar.Controls.Add(btnClose);

        var btnMin = new CaptionButton
        {
            Type = CaptionButton.Kind.Minimize,
            BackColor = Palette.TitleBar,
            Size = new Size(S(44), S(31)),
            Location = new Point(W - S(44) * 2, 0)
        };
        btnMin.Click += (_, _) => WindowState = FormWindowState.Minimized;
        titleBar.Controls.Add(btnMin);

        var btnInfo = new CaptionButton
        {
            Type = CaptionButton.Kind.Info,
            BackColor = Palette.TitleBar,
            Size = new Size(S(44), S(31)),
            Location = new Point(W - S(44) * 3, 0)
        };
        btnInfo.Click += (_, _) => ShowInfo();
        titleBar.Controls.Add(btnInfo);

        // Equalizer APO manquant -> ecran de configuration guide au lieu du curseur.
        if (!EqApo.Available) { BuildSetup(W, titleH, footerH); return; }

        // ---------- Corps ----------
        int bodyH = S(WinH) - titleH - footerH;
        var body = new Panel { Bounds = new Rectangle(0, titleH, W, bodyH), BackColor = Palette.Window };
        Controls.Add(body);

        int y = S(18);

        var title = MakeLabel("Volume Booster", UiFont.Semi(Sf(19)), Palette.TextTitle);
        body.Controls.Add(title);
        title.Location = new Point((W - title.PreferredWidth) / 2, y);
        y += title.PreferredHeight + S(3);

        var subtitle = MakeLabel("Push your sound past the Windows 100% limit.",
            UiFont.Ui(Sf(12)), Palette.TextMuted);
        body.Controls.Add(subtitle);
        subtitle.Location = new Point((W - subtitle.PreferredWidth) / 2, y);
        y += subtitle.PreferredHeight + S(16);

        // Hero : gros pourcentage.
        _hero = MakeLabel("100%", UiFont.Semi(Sf(46)), Palette.TextTitle);
        body.Controls.Add(_hero);
        _hero.Location = new Point((W - _hero.PreferredWidth) / 2, y);
        y += _hero.PreferredHeight + S(6);

        // Curseur.
        _slider = new Slider
        {
            BackColor = Palette.Window,
            Minimum = 100,
            Maximum = 500,
            TickStep = 100,
            Value = _config.Volume,
            Bounds = new Rectangle(P, y, W - 2 * P, S(28))
        };
        _slider.ValueChanged += (_, _) => OnSliderChanged();
        body.Controls.Add(_slider);
        y += _slider.Height + S(2);

        // Bornes min / max.
        var minL = MakeLabel("100%", UiFont.Ui(Sf(10.5f)), Palette.TextFaint);
        body.Controls.Add(minL);
        minL.Location = new Point(P, y);
        var maxL = MakeLabel("500%", UiFont.Ui(Sf(10.5f)), Palette.TextFaint);
        body.Controls.Add(maxL);
        maxL.Location = new Point(W - P - maxL.PreferredWidth, y);
        y += minL.PreferredHeight + S(14);

        // Raccourcis (presets).
        int[] presets = { 100, 200, 300, 500 };
        int pillW = S(58), pillH = S(30), gap = S(8);
        int groupW = presets.Length * pillW + (presets.Length - 1) * gap;
        int px = (W - groupW) / 2;
        foreach (int p in presets)
        {
            var b = new HoverButton
            {
                Text = p + "%",
                Font = UiFont.Ui(Sf(12)),
                BackColor = Palette.Window,
                Size = new Size(pillW, pillH),
                Location = new Point(px, y)
            };
            int target = p;
            b.Click += (_, _) => _slider.Value = target;
            body.Controls.Add(b);
            _presets.Add((b, p));
            px += pillW + gap;
        }
        y += pillH + S(16);

        // Filet.
        body.Controls.Add(new Panel
        {
            Bounds = new Rectangle(P, y, W - 2 * P, Math.Max(1, S(1))),
            BackColor = Palette.Rule
        });
        y += S(1) + S(14);

        // Ligne demarrage auto.
        var toggleLabel = MakeLabel("Start automatically with Windows",
            UiFont.Ui(Sf(13.5f)), Palette.TextBody);
        body.Controls.Add(toggleLabel);

        _toggle = new ToggleSwitch { BackColor = Palette.Window, On = _config.StartWithWindows };
        _toggle.Size = new Size(S(40), S(20));
        _toggle.Toggled += (_, _) => OnToggleStartup();
        body.Controls.Add(_toggle);

        int rowH = Math.Max(toggleLabel.PreferredHeight, _toggle.Height);
        toggleLabel.Location = new Point(P, y + (rowH - toggleLabel.PreferredHeight) / 2);
        _toggle.Location = new Point(W - P - _toggle.Width, y + (rowH - _toggle.Height) / 2);

        // ---------- Pied de page ----------
        var footer = new Panel { Bounds = new Rectangle(0, S(WinH) - footerH, W, footerH), BackColor = Palette.Footer };
        footer.Paint += (_, ev) =>
        {
            using var pen = new Pen(Palette.Rule);
            ev.Graphics.DrawLine(pen, 0, 0, W, 0);
        };
        Controls.Add(footer);

        _status = MakeLabel("", UiFont.Ui(Sf(11.5f)), Palette.TextFaint);
        _status.BackColor = Palette.Footer;
        footer.Controls.Add(_status);
        _status.Location = new Point(P, (footerH - _status.PreferredHeight) / 2);

        var footerBtn = new HoverButton { Font = UiFont.Ui(Sf(13)), BackColor = Palette.Footer };
        if (EqApo.Available)
        {
            footerBtn.Text = "Reset";
            footerBtn.Size = new Size(S(84), S(32));
            footerBtn.Click += (_, _) => _slider.Value = 100;
        }
        else
        {
            footerBtn.Text = "Get Equalizer APO";
            footerBtn.Size = new Size(S(154), S(32));
            footerBtn.FillNormal = Palette.Accent;
            footerBtn.FillHover = Palette.AccentHover;
            footerBtn.FillPress = Palette.AccentPress;
            footerBtn.BorderColor = Palette.AccentHover;
            footerBtn.LabelColor = Color.White;
            footerBtn.Click += (_, _) => OpenUrl("https://sourceforge.net/projects/equalizerapo/");
        }
        footer.Controls.Add(footerBtn);
        footerBtn.Location = new Point(W - P - footerBtn.Width, (footerH - footerBtn.Height) / 2);
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    /// <summary>Popup d'info (bouton ⓘ) sur les risques du boost.</summary>
    private void ShowInfo()
    {
        new InfoPopup("About boosting past 100%",
            "Above 100%, Volume+ actually amplifies your sound. Push it too far and loud parts can distort — and very high volume can damage your hearing or your headphones. Turn Windows' volume down before you put them on, then raise it gradually.")
            .ShowCenteredOver(this);
    }

    /// <summary>Ecran affiche au 1er lancement quand Equalizer APO n'est pas installe.</summary>
    private void BuildSetup(int W, int titleH, int footerH)
    {
        int P = S(28);
        int bodyH = S(WinH) - titleH - footerH;
        var body = new Panel { Bounds = new Rectangle(0, titleH, W, bodyH), BackColor = Palette.Window };
        Controls.Add(body);

        int y = S(22);

        var title = MakeLabel("One-time setup", UiFont.Semi(Sf(19)), Palette.TextTitle);
        body.Controls.Add(title);
        title.Location = new Point((W - title.PreferredWidth) / 2, y);
        y += title.PreferredHeight + S(4);

        var sub = MakeLabel("Volume+ needs a small free add-on to go past 100%.",
            UiFont.Ui(Sf(12)), Palette.TextMuted);
        body.Controls.Add(sub);
        sub.Location = new Point((W - sub.PreferredWidth) / 2, y);
        y += sub.PreferredHeight + S(20);

        string[] steps =
        {
            "1.   Click \"Install Equalizer APO\" below (it's free).",
            "2.   In its setup, tick your speakers / headset.",
            "3.   Reboot your PC once.",
            "4.   Reopen Volume+ — done, your sound gets louder.",
        };
        foreach (var st in steps)
        {
            var l = MakeLabel(st, UiFont.Ui(Sf(13)), Palette.TextBody);
            body.Controls.Add(l);
            l.Location = new Point(P, y);
            y += l.PreferredHeight + S(11);
        }
        y += S(10);

        var btn = new HoverButton
        {
            Text = "Install Equalizer APO",
            Font = UiFont.Semi(Sf(13.5f)),
            BackColor = Palette.Window,
            FillNormal = Palette.Accent,
            FillHover = Palette.AccentHover,
            FillPress = Palette.AccentPress,
            BorderColor = Palette.AccentHover,
            LabelColor = Color.White,
            Size = new Size(S(224), S(38))
        };
        btn.Click += (_, _) => OpenUrl("https://sourceforge.net/projects/equalizerapo/");
        body.Controls.Add(btn);
        btn.Location = new Point((W - btn.Width) / 2, y);

        var footer = new Panel { Bounds = new Rectangle(0, S(WinH) - footerH, W, footerH), BackColor = Palette.Footer };
        footer.Paint += (_, ev) =>
        {
            using var pen = new Pen(Palette.Rule);
            ev.Graphics.DrawLine(pen, 0, 0, W, 0);
        };
        Controls.Add(footer);

        var note = MakeLabel("You only do this once. Windows can't go past 100% on its own.",
            UiFont.Ui(Sf(11.5f)), Palette.TextFaint);
        note.BackColor = Palette.Footer;
        footer.Controls.Add(note);
        note.Location = new Point(P, (footerH - note.PreferredHeight) / 2);
    }

    private void OnSliderChanged()
    {
        RefreshVisuals();
        ScheduleApply();
    }

    /// <summary>Met a jour le gros pourcentage + le statut (sans ecrire tout de suite).</summary>
    private void RefreshVisuals()
    {
        int pct = _slider?.Value ?? _config.Volume;

        if (_hero != null)
        {
            _hero.Text = pct + "%";
            _hero.ForeColor = pct > 100 ? Palette.Accent : Palette.TextTitle;
            _hero.Location = new Point((_W - _hero.PreferredWidth) / 2, _hero.Top);
        }

        if (_status != null)
        {
            if (!EqApo.Available)
            {
                _status.Text = "Equalizer APO not found";
                _status.ForeColor = Palette.Warn;
            }
            else if (pct <= 100)
            {
                _status.Text = "Normal volume";
                _status.ForeColor = Palette.TextFaint;
            }
            else
            {
                _status.Text = "Boosted  ×" + (pct / 100.0).ToString("0.0", CultureInfo.InvariantCulture);
                _status.ForeColor = Palette.Accent;
            }
        }

        foreach (var (btn, val) in _presets)
        {
            bool active = val == pct;
            btn.BorderColor = active ? Palette.Accent : Palette.BtnBorder;
            btn.LabelColor = active ? Palette.Accent : Palette.TextBody;
            btn.Invalidate();
        }
    }

    private void ScheduleApply()
    {
        _debounce ??= new System.Windows.Forms.Timer { Interval = 70 };
        _debounce.Stop();
        _debounce.Tick -= OnApplyTick;
        _debounce.Tick += OnApplyTick;
        _debounce.Start();
    }

    private void OnApplyTick(object? sender, EventArgs e)
    {
        _debounce!.Stop();
        _config.Volume = _slider.Value;
        EqApo.Apply(_config.Volume);
        _config.Save();
        _onApplied?.Invoke();
    }

    private void OnToggleStartup()
    {
        _config.StartWithWindows = _toggle.On;
        StartupManager.Set(_config.StartWithWindows);
        _config.Save();
    }

    /// <summary>Recale l'interrupteur si l'etat a change ailleurs (menu tray).</summary>
    public void SyncFromConfig()
    {
        if (InvokeRequired) { BeginInvoke(new Action(SyncFromConfig)); return; }
        if (_toggle != null) _toggle.On = _config.StartWithWindows;
        if (_slider != null && _slider.Value != _config.Volume) _slider.Value = _config.Volume;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        if (_slider != null)
        {
            if (keyData is Keys.Right or Keys.Up) { _slider.Value += 5; return true; }
            if (keyData is Keys.Left or Keys.Down) { _slider.Value -= 5; return true; }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_slider != null && e.Delta != 0)
            _slider.Value += Math.Sign(e.Delta) * 10;
    }

    private Label MakeLabel(string text, Font font, Color fg) => new()
    {
        Text = text,
        Font = font,
        ForeColor = fg,
        BackColor = Palette.Window,
        AutoSize = true,
        UseMnemonic = false
    };
}
