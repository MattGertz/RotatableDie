namespace YachtDiceMaui.Services;

/// <summary>
/// Persists user options via MAUI Preferences.
/// </summary>
public static class AppSettings
{
    public static string PlayerName
    {
        get => Preferences.Default.Get("PlayerName", "Player");
        set => Preferences.Default.Set("PlayerName", value);
    }

    public static bool SoundEnabled
    {
        get => Preferences.Default.Get("SoundEnabled", true);
        set => Preferences.Default.Set("SoundEnabled", value);
    }

    public static int DieColorIndex
    {
        get => Preferences.Default.Get("DieColorIndex", 0);
        set => Preferences.Default.Set("DieColorIndex", value);
    }

    public static int PipColorIndex
    {
        get => Preferences.Default.Get("PipColorIndex", 0);
        set => Preferences.Default.Set("PipColorIndex", value);
    }

    public static bool UseNumbers
    {
        get => Preferences.Default.Get("UseNumbers", false);
        set => Preferences.Default.Set("UseNumbers", value);
    }

    public static bool Translucent
    {
        get => Preferences.Default.Get("Translucent", false);
        set => Preferences.Default.Set("Translucent", value);
    }
}
