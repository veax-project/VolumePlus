using System.Windows.Forms;

namespace VolumePlus;

internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    private static void Main(string[] args)
    {
        _mutex = new Mutex(true, "VolumePlus_SingleInstance_{a3f1c7e2}", out bool createdNew);
        if (!createdNew) return;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // Apercu du popup info (pour capture) : VolumePlus.exe --infodemo
        if (args.Any(a => string.Equals(a, "--infodemo", StringComparison.OrdinalIgnoreCase)))
        {
            var info = new InfoPopup("About boosting past 100%",
                "Above 100%, Volume+ actually amplifies your sound. Push it too far and loud parts can distort — and very high volume can damage your hearing or your headphones. Turn Windows' volume down before you put them on, then raise it gradually.")
            { AutoClose = false };
            info.FormClosed += (_, _) => Application.ExitThread();
            info.Show();
            var wa = Screen.PrimaryScreen!.WorkingArea;
            info.Location = new Point(wa.X + (wa.Width - info.Width) / 2, wa.Y + (wa.Height - info.Height) / 2);
            Application.Run();
            GC.KeepAlive(_mutex);
            return;
        }

        // Apercu de l'ecran de configuration (pour capture) : VolumePlus.exe --setupdemo
        if (args.Any(a => string.Equals(a, "--setupdemo", StringComparison.OrdinalIgnoreCase)))
        {
            EqApo.ForceMissing = true;
            var win0 = new MainWindow(new AppConfig(), () => { });
            win0.FormClosed += (_, _) => Application.ExitThread();
            win0.Show();
            Application.Run();
            GC.KeepAlive(_mutex);
            return;
        }

        // Apercu de la fenetre (pour capture) : VolumePlus.exe --windemo
        if (args.Any(a => string.Equals(a, "--windemo", StringComparison.OrdinalIgnoreCase)))
        {
            var cfg = new AppConfig { Volume = 200 };
            var win = new MainWindow(cfg, () => { });
            win.FormClosed += (_, _) => Application.ExitThread();
            win.Show();
            Application.Run();
            GC.KeepAlive(_mutex);
            return;
        }

        // Apercu du menu tray (pour capture) : VolumePlus.exe --menudemo
        if (args.Any(a => string.Equals(a, "--menudemo", StringComparison.OrdinalIgnoreCase)))
        {
            var demo = new TrayMenu(new List<TrayMenuItem>
            {
                TrayMenuItem.Act("Open Volume+…", () => { }, bold: true),
                TrayMenuItem.Act("Reset to 100%", () => { }),
                TrayMenuItem.Sep(),
                TrayMenuItem.Chk("Start with Windows", true, () => { }),
                TrayMenuItem.Sep(),
                TrayMenuItem.Act("Quit", () => { })
            })
            { AutoClose = false };
            demo.FormClosed += (_, _) => Application.ExitThread();
            demo.ShowDemoCentered();
            Application.Run();
            GC.KeepAlive(_mutex);
            return;
        }

        bool fromStartup = args.Any(a => string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase));

        using var ctx = new TrayContext(fromStartup);
        Application.Run(ctx);
        GC.KeepAlive(_mutex);
    }
}
