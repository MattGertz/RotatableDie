using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RotatableDie.Converters
{
    /// <summary>
    /// Converts a SolidColorBrush to a readable color name
    /// </summary>
    public class BrushToColorNameConverter : IValueConverter
    {
        private static readonly Dictionary<Color, string> KnownColorNames = new Dictionary<Color, string>
        {
            { Colors.White, "White" },
            { Colors.Black, "Black" },
            { Colors.Red, "Red" },
            { Colors.Green, "Green" },
            { Colors.Blue, "Blue" },
            { Colors.Yellow, "Yellow" },
            { Colors.Orange, "Orange" },
            { Colors.Purple, "Purple" },
            { Colors.Pink, "Pink" },
            { Colors.Cyan, "Cyan" },
            { Colors.Brown, "Brown" },
            { Colors.Gray, "Gray" },
            { Colors.Silver, "Silver" },
            { Color.FromRgb(255, 215, 0), "Gold" },
            { Colors.Lime, "Lime" },
            { Colors.Teal, "Teal" },
            { Colors.Indigo, "Indigo" },
            { Colors.Violet, "Violet" },
            { Colors.Magenta, "Magenta" },
            { Colors.Navy, "Navy" },
            { Colors.Olive, "Olive" },
            { Colors.Maroon, "Maroon" },
            { Color.FromRgb(64, 224, 208), "Turquoise" },
            { Colors.Coral, "Coral" },
            { Colors.Crimson, "Crimson" },
            { Colors.SlateBlue, "SlateBlue" },
            { Colors.ForestGreen, "ForestGreen" },
            { Colors.DeepPink, "DeepPink" },
            { Colors.Khaki, "Khaki" },
            { Colors.SteelBlue, "SteelBlue" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                if (KnownColorNames.TryGetValue(brush.Color, out string? name) && name != null)
                {
                    return name;
                }
                return $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
