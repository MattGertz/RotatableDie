using System.Numerics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using YachtDiceMaui.Physics;
using YachtDiceMaui.Rendering;

namespace YachtDiceMaui.Views;

/// <summary>
/// A popup page for changing dice appearance with a live preview die.
/// Shows die color, pip color, and texture toggle all at once.
/// </summary>
public class DiceSkinsPage : ContentPage
{
    private readonly DiceAppearance _appearance;
    private readonly DieRenderer _renderer = new();
    private SKCanvasView _preview = null!;
    private Camera? _camera;

    // Track current selections for highlighting
    private int _selectedColorIndex;
    private int _selectedPipIndex;
    private readonly List<Border> _colorBorders = new();
    private readonly List<Border> _pipBorders = new();

    public DiceSkinsPage(DiceAppearance appearance)
    {
        _appearance = appearance;
        Title = "Dice Skins";
        BackgroundColor = Color.FromArgb("#16213E");

        // Find current selection indices
        _selectedColorIndex = Array.FindIndex(DiceAppearance.ColorOptions, c => c.Color == _appearance.DieColor);
        if (_selectedColorIndex < 0) _selectedColorIndex = 0;
        _selectedPipIndex = Array.FindIndex(DiceAppearance.PipColorOptions, c => c.Color == _appearance.PipColor);
        if (_selectedPipIndex < 0) _selectedPipIndex = 0;

        BuildUI();
    }

    private void BuildUI()
    {
        var rootGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(new GridLength(180)),   // Preview die
                new RowDefinition(GridLength.Auto),        // Die Color label
                new RowDefinition(GridLength.Auto),        // Die Color swatches
                new RowDefinition(GridLength.Auto),        // Pip Color label
                new RowDefinition(GridLength.Auto),        // Pip Color swatches
                new RowDefinition(GridLength.Auto),        // Numbers toggle
                new RowDefinition(GridLength.Auto),        // Translucent toggle
                new RowDefinition(GridLength.Star),        // spacer
                new RowDefinition(GridLength.Auto),        // Close button
            },
            Padding = new Thickness(16),
            RowSpacing = 8,
        };

        // Preview die
        _preview = new SKCanvasView { BackgroundColor = Colors.Transparent };
        _preview.PaintSurface += OnPreviewPaint;
        var previewBorder = new Border
        {
            Stroke = Color.FromArgb("#533E2D"),
            StrokeThickness = 2,
            Background = new SolidColorBrush(Color.FromArgb("#1B3A1B")),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = _preview,
        };
        rootGrid.Add(previewBorder, 0, 0);

        // Die Color section
        rootGrid.Add(MakeLabel("Die Color"), 0, 1);
        rootGrid.Add(BuildColorSwatches(), 0, 2);

        // Pip Color section
        rootGrid.Add(MakeLabel("Pip Color"), 0, 3);
        rootGrid.Add(BuildPipSwatches(), 0, 4);

        // Numbers toggle
        var numbersRow = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
        numbersRow.Add(new Label
        {
            Text = "Numbers Instead of Pips",
            TextColor = Colors.White,
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
        });
        var numbersSwitch = new Switch
        {
            IsToggled = _appearance.UseNumbers,
            OnColor = Color.FromArgb("#4CAF50"),
        };
        numbersSwitch.Toggled += (_, e) =>
        {
            _appearance.SetUseNumbers(e.Value);
            _preview.InvalidateSurface();
        };
        numbersRow.Add(numbersSwitch);
        rootGrid.Add(numbersRow, 0, 5);

        // Translucent toggle
        var translucentRow = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
        translucentRow.Add(new Label
        {
            Text = "Translucent Dice",
            TextColor = Colors.White,
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
        });
        var translucentSwitch = new Switch
        {
            IsToggled = _appearance.Translucent,
            OnColor = Color.FromArgb("#4CAF50"),
        };
        translucentSwitch.Toggled += (_, e) =>
        {
            _appearance.SetTranslucent(e.Value);
            _preview.InvalidateSurface();
        };
        translucentRow.Add(translucentSwitch);
        rootGrid.Add(translucentRow, 0, 6);

        // Close button
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
        rootGrid.Add(closeBtn, 0, 8);

        Content = rootGrid;
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

            // Add a border for visibility on dark swatches
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
        // Remove old highlight
        if (_selectedColorIndex >= 0 && _selectedColorIndex < _colorBorders.Count)
            _colorBorders[_selectedColorIndex].Stroke = Colors.Transparent;

        _selectedColorIndex = index;
        _colorBorders[index].Stroke = Colors.Gold;

        _appearance.SetColor(DiceAppearance.ColorOptions[index].Color);
        _preview.InvalidateSurface();
    }

    private void SelectPipColor(int index)
    {
        if (_selectedPipIndex >= 0 && _selectedPipIndex < _pipBorders.Count)
            _pipBorders[_selectedPipIndex].Stroke = Colors.Transparent;

        _selectedPipIndex = index;
        _pipBorders[index].Stroke = Colors.Gold;

        _appearance.SetPipColor(DiceAppearance.PipColorOptions[index].Color);
        _preview.InvalidateSurface();
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        TextColor = Colors.White,
        FontSize = 15,
        FontAttributes = FontAttributes.Bold,
        Margin = new Thickness(0, 4, 0, 0),
    };

    // ── Preview Rendering ────────────────────────────────────────

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

        // Draw a single die at origin, tilted for a nice 3-face view
        var rotation = Quaternion.CreateFromYawPitchRoll(-0.5f, 0.15f, 0.1f);
        var state = new DieState(new Vector3(0, 0.3f, 0), rotation, false);

        _renderer.DrawDie(canvas, state, 0, _appearance, _camera, false);
    }
}
