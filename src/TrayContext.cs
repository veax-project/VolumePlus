using System.Drawing;
using System.Windows.Forms;

namespace VolumePlus;

/// <summary>Contexte principal : icone barre des taches, menu, fenetre de reglage.</summary>
public sealed class TrayContext : ApplicationContext
{
    private readonly AppConfig _config;
    private readonly NotifyIcon _tray;
    private readonly Icon _icon;
    private MainWindow? _win;

    public TrayContext(bool startedFromStartup)
    {
        _config = AppConfig.Load();

        // Applique le dernier volume au demarrage + garde l'entree de demarrage a jour.
        EqApo.Apply(_config.Volume);
        StartupManager.Set(_config.StartWithWindows);

        _icon = AppRes.AppIcon();
        _tray = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            Text = TrayText()
        };
        _tray.MouseUp += OnTrayMouseUp;
        _tray.DoubleClick += (_, _) => OpenWindow();

        if (!EqApo.Available)
            _tray.ShowBalloonTip(6000, "Volume+",
                "Equalizer APO not found — reinstall it so the boost can work.",
                ToolTipIcon.Warning);

        if (!_config.FirstRunDone)
        {
            _config.FirstRunDone = true;
            _config.Save();
            OpenWindow();
        }
        else if (startedFromStartup)
        {
            _tray.ShowBalloonTip(2500, "Volume+",
                $"Running in the background — current volume {_config.Volume}%.",
                ToolTipIcon.Info);
        }
    }

    private string TrayText()
    {
        string t = $"Volume+  —  {_config.Volume}%";
        return t.Length > 63 ? t[..63] : t;
    }

    private void OnTrayMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right) ShowTrayMenu();
    }

    private void ShowTrayMenu()
    {
        var items = new List<TrayMenuItem>
        {
            TrayMenuItem.Act("Open Volume+…", OpenWindow, bold: true),
            TrayMenuItem.Act("Reset to 100%", ResetVolume),
            TrayMenuItem.Sep(),
            TrayMenuItem.Chk("Start with Windows", _config.StartWithWindows, ToggleStartup),
            TrayMenuItem.Sep(),
            TrayMenuItem.Act("Quit", ExitApp)
        };
        new TrayMenu(items).ShowAtCursor();
    }

    private void OpenWindow()
    {
        if (_win == null || _win.IsDisposed)
        {
            _win = new MainWindow(_config, OnApplied);
            _win.FormClosed += (_, _) => _win = null;
            _win.Show();
        }
        _win.WindowState = FormWindowState.Normal;
        _win.Activate();
        _win.BringToFront();
    }

    private void OnApplied() => _tray.Text = TrayText();

    private void ResetVolume()
    {
        _config.Volume = 100;
        EqApo.Apply(100);
        _config.Save();
        _tray.Text = TrayText();
        _win?.SyncFromConfig();
    }

    private void ToggleStartup()
    {
        _config.StartWithWindows = !_config.StartWithWindows;
        StartupManager.Set(_config.StartWithWindows);
        _config.Save();
        _win?.SyncFromConfig();
    }

    private void ExitApp()
    {
        _tray.Visible = false;
        Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _icon.Dispose();
            _win?.Dispose();
        }
        base.Dispose(disposing);
    }
}
