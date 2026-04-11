using Microsoft.Maui.LifecycleEvents;

namespace YachtDiceMaui.Services;

public static partial class PlatformHelpers
{
    public static partial void ConfigurePlatformLifecycle(MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windows => windows.OnWindowCreated(window =>
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "dieicon.ico");
                if (System.IO.File.Exists(iconPath))
                    appWindow.SetIcon(iconPath);
            }));
        });
    }

    public static partial void ConfigureWindow(Window window)
    {
        window.Width = 1100;
        window.Height = 650;
    }

    public static partial bool SupportsExitButton() => true;
}
