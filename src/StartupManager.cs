using Microsoft.Win32;

namespace VolumePlus;

/// <summary>Demarrage automatique avec Windows via HKCU\...\Run (sans droits admin).</summary>
public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "VolumePlus";

    private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string;
        }
        catch { return false; }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;

            if (enabled)
                key.SetValue(ValueName, $"\"{ExePath}\" --tray");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch { /* silencieux */ }
    }
}
