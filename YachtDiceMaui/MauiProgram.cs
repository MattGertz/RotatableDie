using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SkiaSharp.Views.Maui.Controls.Hosting;
using YachtDiceMaui.Services;
using YachtDiceMaui.Views;

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

		PlatformHelpers.ConfigurePlatformLifecycle(builder);

		// Services
		builder.Services.AddSingleton(AudioManager.Current);
		builder.Services.AddSingleton<SoundService>();
		builder.Services.AddTransient<GamePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
