using System.Globalization;
using System.Text;

namespace VolumePlus;

/// <summary>
/// Ecrit la config d'Equalizer APO (le moteur audio natif de Windows qui applique
/// le gain). Volume+ ne fait que piloter son preamp — c'est le seul point legitime
/// pour depasser 100% (la chaine d'APO du moteur audio).
/// </summary>
internal static class EqApo
{
    public static readonly string? ConfigPath = FindConfig();

    private static string? FindConfig()
    {
        string[] roots =
        {
            @"C:\Program Files\EqualizerAPO\config\config.txt",
            @"C:\Program Files (x86)\EqualizerAPO\config\config.txt",
        };
        foreach (var p in roots)
            if (File.Exists(p)) return p;
        return null;
    }

    /// <summary>Force l'etat "absent" (pour l'apercu de l'ecran de configuration).</summary>
    public static bool ForceMissing;

    public static bool Available => !ForceMissing && ConfigPath != null;

    /// <summary>Convertit un pourcentage (100 = normal) en decibels de preamp.</summary>
    public static double DbFor(int volumePercent)
    {
        double pct = Math.Max(1, volumePercent);
        return 20.0 * Math.Log10(pct / 100.0); // 100% -> 0 dB, 200% -> +6 dB, 500% -> +14 dB
    }

    /// <summary>Applique le volume (en %). 100 = neutre.</summary>
    public static void Apply(int volumePercent)
    {
        if (ConfigPath is null) return;

        double db = DbFor(volumePercent);
        var sb = new StringBuilder();
        sb.AppendLine("# Genere par Volume+ — ne pas editer a la main");
        sb.AppendLine($"Preamp: {db.ToString("0.0", CultureInfo.InvariantCulture)} dB");

        try { File.WriteAllText(ConfigPath, sb.ToString(), new UTF8Encoding(false)); }
        catch { /* silencieux */ }
    }
}
