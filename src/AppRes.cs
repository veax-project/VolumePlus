using System.Drawing;
using System.Reflection;

namespace VolumePlus;

/// <summary>Charge l'icone de l'app (logo VolumePlus.ico embarque), avec repli sur l'icone dessinee.</summary>
public static class AppRes
{
    private const string IconResource = "VolumePlus.Assets.VolumePlus.ico";

    /// <summary>Retourne une nouvelle Icon a chaque appel (le proprietaire s'en charge / la dispose).</summary>
    public static Icon AppIcon()
    {
        try
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResource);
            if (s != null) return new Icon(s);
        }
        catch { }
        return IconFactory.CreateAppIcon();
    }
}
