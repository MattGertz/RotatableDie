namespace YachtDiceMaui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
#if WINDOWS
		window.Width = 1100;
		window.Height = 650;
#endif
		return window;
	}
}