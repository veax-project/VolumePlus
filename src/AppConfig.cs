using System.Text.Json;

namespace VolumePlus;

/// <summary>Reglages persistants dans %APPDATA%\VolumePlus\config.json</summary>
public sealed class AppConfig
{
    public int Volume { get; set; } = 100;          // 100..500
    public bool StartWithWindows { get; set; }
    public bool FirstRunDone { get; set; }

    private static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumePlus");
    private static string FilePath => Path.Combine(Dir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var c = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath));
                if (c != null)
                {
                    c.Volume = Math.Clamp(c.Volume, 100, 500);
                    return c;
                }
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
