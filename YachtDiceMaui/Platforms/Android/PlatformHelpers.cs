namespace YachtDiceMaui.Services;

public static partial class PlatformHelpers
{
    public static partial void ConfigurePlatformLifecycle(MauiAppBuilder builder) { }

    public static partial void ConfigureWindow(Window window) { }

    public static partial bool SupportsExitButton() => false;

    public static partial int GetPressedNumber() => 0;
}
