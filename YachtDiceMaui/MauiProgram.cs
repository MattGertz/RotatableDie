using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif

namespace YachtDiceMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if WINDOWS
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
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
