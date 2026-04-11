using SkiaSharp;

namespace YachtDiceMaui.Rendering;

/// <summary>
/// Appearance settings for dice. Changeable at runtime.
/// </summary>
public class DiceAppearance
{
    public SKColor DieColor { get; set; } = new(0xCC, 0x22, 0x22); // Classic red
    public SKColor PipColor { get; set; } = SKColors.White;
    public bool UseNumbers { get; set; } = false;
    public bool Translucent { get; set; } = false;

    public event Action? Changed;

    public void SetColor(SKColor color)
    {
        DieColor = color;
        Changed?.Invoke();
    }

    public void SetPipColor(SKColor color)
    {
        PipColor = color;
        Changed?.Invoke();
    }

    public void SetUseNumbers(bool enabled)
    {
        UseNumbers = enabled;
        Changed?.Invoke();
    }

    public void SetTranslucent(bool enabled)
    {
        Translucent = enabled;
        Changed?.Invoke();
    }

    /// <summary>
    /// Predefined color options for the Dice Skins picker.
    /// </summary>
    public static readonly (string Name, SKColor Color)[] ColorOptions =
    {
        ("Classic Red", new SKColor(0xCC, 0x22, 0x22)),
        ("Ivory", new SKColor(0xF5, 0xF0, 0xE0)),
        ("Royal Blue", new SKColor(0x1A, 0x3C, 0x8A)),
        ("Forest Green", new SKColor(0x1B, 0x5E, 0x20)),
        ("Gold", new SKColor(0xD4, 0xA0, 0x17)),
        ("Purple", new SKColor(0x6A, 0x1B, 0x9A)),
        ("Black", new SKColor(0x20, 0x20, 0x20)),
        ("White", new SKColor(0xF0, 0xF0, 0xF0)),
        ("Turquoise", new SKColor(0x00, 0x96, 0x88)),
        ("Orange", new SKColor(0xE6, 0x5C, 0x00)),
    };

    public static readonly (string Name, SKColor Color)[] PipColorOptions =
    {
        ("White", SKColors.White),
        ("Black", new SKColor(0x20, 0x20, 0x20)),
        ("Gold", new SKColor(0xFF, 0xD7, 0x00)),
        ("Red", new SKColor(0xCC, 0x22, 0x22)),
    };
}
