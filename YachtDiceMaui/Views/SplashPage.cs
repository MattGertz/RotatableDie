namespace YachtDiceMaui.Views;

public class SplashPage : ContentPage
{
    public SplashPage()
    {
        BackgroundColor = Color.FromArgb("#0A1A2F");
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 0,
            Children =
            {
                new Label
                {
                    Text = "Matt's",
                    FontSize = 52,
                    FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
                    TextColor = Colors.Gold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Shadow = new Shadow
                    {
                        Brush = new SolidColorBrush(Colors.DarkGoldenrod),
                        Offset = new Point(2, 2),
                        Radius = 6,
                    },
                },
                new Label
                {
                    Text = "Yacht",
                    FontSize = 72,
                    FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
                    TextColor = Colors.Gold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Shadow = new Shadow
                    {
                        Brush = new SolidColorBrush(Colors.DarkGoldenrod),
                        Offset = new Point(2, 2),
                        Radius = 8,
                    },
                },
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(2000);
        Application.Current!.Windows[0].Page = new AppShell();
    }
}
