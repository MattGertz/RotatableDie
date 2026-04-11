using System.Numerics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using YachtDiceMaui.Physics;
using YachtDiceMaui.Rendering;
using YachtDiceMaui.Services;

namespace YachtDiceMaui.Views;

/// <summary>
/// Options page: player name, sound effects toggle, dice appearance settings with live preview.
/// </summary>
public class OptionsPage : ContentPage
{
    private readonly DiceAppearance _appearance;
    private readonly SoundService _sound;
    private readonly Action<string> _onNameChanged;
    private readonly DieRenderer _renderer = new();
    private SKCanvasView _preview = null!;
    private Camera? _camera;

    private int _selectedColorIndex;
    private int _selectedPipIndex;
    private readonly List<Border> _colorBorders = new();
    private readonly List<Border> _pipBorders = new();

    public OptionsPage(DiceAppearance appearance, SoundService sound, string currentName, Action<string> onNameChanged)
    {
        _appearance = appearance;
        _sound = sound;
        _onNameChanged = onNameChanged;
        Title = "Options";
        BackgroundColor = Color.FromArgb("#16213E");

        _selectedColorIndex = Array.FindIndex(DiceAppearance.ColorOptions, c => c.Color == _appearance.DieColor);
        if (_selectedColorIndex < 0) _selectedColorIndex = 0;
        _selectedPipIndex = Array.FindIndex(DiceAppearance.PipColorOptions, c => c.Color == _appearance.PipColor);
        if (_selectedPipIndex < 0) _selectedPipIndex = 0;

        BuildUI(currentName);
    }

    private void BuildUI(string currentName)
    {
        var scroll = new ScrollView();
        var rootStack = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 8,
        };

        // ── Player Name ──
        rootStack.Add(MakeLabel("Player Name"));
        var nameEntry = new Entry
        {
            Text = currentName,
            FontSize = 16,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#0D1117"),
            Placeholder = "Enter your name",
            PlaceholderColor = Color.FromArgb("#666666"),
            HorizontalTextAlignment = TextAlignment.Center,
            HeightRequest = 44,
        };
        nameEntry.TextChanged += (_, e) =>
        {
            _onNameChanged(e.NewTextValue ?? "");
            AppSettings.PlayerName = e.NewTextValue ?? "";
        };
        rootStack.Add(nameEntry);

        // ── Sound Effects ──
        rootStack.Add(MakeToggleRow("Sound Effects", _sound.Enabled, enabled =>
        {
            _sound.Enabled = enabled;
            AppSettings.SoundEnabled = enabled;
        }));

        // ── Dice Preview ──
        rootStack.Add(MakeSeparator());
        rootStack.Add(MakeLabel("Dice Appearance"));
        _preview = new SKCanvasView { BackgroundColor = Colors.Transparent, HeightRequest = 160 };
        _preview.PaintSurface += OnPreviewPaint;
        var previewBorder = new Border
        {
            Stroke = Color.FromArgb("#533E2D"),
            StrokeThickness = 2,
            Background = new SolidColorBrush(Color.FromArgb("#1B3A1B")),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = _preview,
        };
        rootStack.Add(previewBorder);

        // ── Die Color ──
        rootStack.Add(MakeLabel("Die Color"));
        rootStack.Add(BuildColorSwatches());

        // ── Pip Color ──
        rootStack.Add(MakeLabel("Pip Color"));
        rootStack.Add(BuildPipSwatches());

        // ── Toggles ──
        rootStack.Add(MakeToggleRow("Numbers Instead of Pips", _appearance.UseNumbers, enabled =>
        {
            _appearance.SetUseNumbers(enabled);
            AppSettings.UseNumbers = enabled;
            _preview.InvalidateSurface();
        }));

        rootStack.Add(MakeToggleRow("Translucent Dice", _appearance.Translucent, enabled =>
        {
            _appearance.SetTranslucent(enabled);
            AppSettings.Translucent = enabled;
            _preview.InvalidateSurface();
        }));

        // ── Done button ──
        rootStack.Add(new BoxView { HeightRequest = 12 });
        var closeBtn = new Button
        {
            Text = "Done",
            FontSize = 18,
            BackgroundColor = Color.FromArgb("#0F3460"),
            TextColor = Colors.White,
            CornerRadius = 6,
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.Center,
            Padding = new Thickness(40, 0),
        };
        closeBtn.Clicked += async (_, _) => await Navigation.PopModalAsync();
        rootStack.Add(closeBtn);

        scroll.Content = rootStack;
        Content = scroll;
    }

    private FlexLayout BuildColorSwatches()
    {
        var flex = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
            AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center,
        };

        for (int i = 0; i < DiceAppearance.ColorOptions.Length; i++)
        {
            var (name, color) = DiceAppearance.ColorOptions[i];
            int idx = i;

            var swatch = new BoxView
            {
                Color = Color.FromRgba(color.Red, color.Green, color.Blue, color.Alpha),
                WidthRequest = 36,
                HeightRequest = 36,
                CornerRadius = 4,
            };

            var border = new Border
            {
                Stroke = idx == _selectedColorIndex ? Colors.Gold : Colors.Transparent,
                StrokeThickness = 3,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(2),
                Margin = new Thickness(3),
                Content = swatch,
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => SelectDieColor(idx);
            border.GestureRecognizers.Add(tap);

            _colorBorders.Add(border);
            flex.Add(border);
        }

        return flex;
    }

    private FlexLayout BuildPipSwatches()
    {
        var flex = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
            AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center,
        };

        for (int i = 0; i < DiceAppearance.PipColorOptions.Length; i++)
        {
            var (name, color) = DiceAppearance.PipColorOptions[i];
            int idx = i;

            var swatch = new BoxView
            {
                Color = Color.FromRgba(color.Red, color.Green, color.Blue, color.Alpha),
                WidthRequest = 36,
                HeightRequest = 36,
                CornerRadius = 4,
            };

            var border = new Border
            {
                Stroke = idx == _selectedPipIndex ? Colors.Gold : Colors.Transparent,
                StrokeThickness = 3,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(2),
                Margin = new Thickness(3),
                Content = swatch,
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => SelectPipColor(idx);
            border.GestureRecognizers.Add(tap);

            _pipBorders.Add(border);
            flex.Add(border);
        }

        return flex;
    }

    private void SelectDieColor(int index)
    {
        if (_selectedColorIndex >= 0 && _selectedColorIndex < _colorBorders.Count)
            _colorBorders[_selectedColorIndex].Stroke = Colors.Transparent;

        _selectedColorIndex = index;
        _colorBorders[index].Stroke = Colors.Gold;

        _appearance.SetColor(DiceAppearance.ColorOptions[index].Color);
        AppSettings.DieColorIndex = index;
        _preview.InvalidateSurface();
    }

    private void SelectPipColor(int index)
    {
        if (_selectedPipIndex >= 0 && _selectedPipIndex < _pipBorders.Count)
            _pipBorders[_selectedPipIndex].Stroke = Colors.Transparent;

        _selectedPipIndex = index;
        _pipBorders[index].Stroke = Colors.Gold;

        _appearance.SetPipColor(DiceAppearance.PipColorOptions[index].Color);
        AppSettings.PipColorIndex = index;
        _preview.InvalidateSurface();
    }

    private static HorizontalStackLayout MakeToggleRow(string text, bool current, Action<bool> onChanged)
    {
        var row = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
        row.Add(new Label
        {
            Text = text,
            TextColor = Colors.White,
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
        });
        var sw = new Switch
        {
            IsToggled = current,
            OnColor = Color.FromArgb("#4CAF50"),
        };
        sw.Toggled += (_, e) => onChanged(e.Value);
        row.Add(sw);
        return row;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        TextColor = Colors.White,
        FontSize = 15,
        FontAttributes = FontAttributes.Bold,
        Margin = new Thickness(0, 4, 0, 0),
    };

    private static BoxView MakeSeparator() => new()
    {
        HeightRequest = 1,
        Color = Color.FromArgb("#333333"),
        Margin = new Thickness(0, 8),
    };

    private void OnPreviewPaint(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(new SKColor(0x1B, 0x3A, 0x1B));

        if (_camera == null)
        {
            _camera = new Camera
            {
                Position = new Vector3(1.5f, 1.8f, 3.0f),
                Target = new Vector3(0, 0.3f, 0),
                Up = Vector3.UnitY,
                FieldOfView = 22f,
            };
        }
        _camera.SetViewport(info.Width, info.Height);

        var rotation = Quaternion.CreateFromYawPitchRoll(-0.5f, 0.15f, 0.1f);
        var state = new DieState(new Vector3(0, 0.3f, 0), rotation, false);
        _renderer.DrawDie(canvas, state, 0, _appearance, _camera, false);
    }
}
