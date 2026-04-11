namespace YachtDiceMaui.Services;

public static partial class PlatformHelpers
{
    /// <summary>Configure platform-specific lifecycle events (e.g. taskbar icon on Windows).</summary>
    public static partial void ConfigurePlatformLifecycle(MauiAppBuilder builder);

    /// <summary>Apply platform-specific window settings (e.g. size on desktop).</summary>
    public static partial void ConfigureWindow(Window window);

    /// <summary>Whether the platform should show an Exit button.</summary>
    public static partial bool SupportsExitButton();
}
