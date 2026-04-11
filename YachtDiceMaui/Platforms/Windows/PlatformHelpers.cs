using System.Runtime.InteropServices;
using Microsoft.Maui.LifecycleEvents;

namespace YachtDiceMaui.Services;

public static partial class PlatformHelpers
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

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

    public static partial int GetPressedNumber()
    {
        // VK_1 (0x31) through VK_6 (0x36)
        for (int i = 1; i <= 6; i++)
        {
            if ((GetAsyncKeyState(0x30 + i) & 0x8000) != 0)
                return i;
        }
        return 0;
    }
}
