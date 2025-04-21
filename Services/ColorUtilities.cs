using System;
using System.Windows.Media;

namespace RotatableDie.Services
{
    /// <summary>
    /// Provides utility methods for working with colors
    /// </summary>
    public static class ColorUtilities
    {
        /// <summary>
        /// Gets the optimal text color (black or white) based on background color
        /// </summary>
        public static Color GetOptimalTextColor(Color backgroundColor)
        {
            // Calculate contrast with both black and white
            double contrastWithBlack = GetContrastRatio(backgroundColor, Colors.Black);
            double contrastWithWhite = GetContrastRatio(backgroundColor, Colors.White);
            
            // Return the color with better contrast (WCAG recommends at least 4.5:1 for normal text)
            return contrastWithBlack > contrastWithWhite ? Colors.Black : Colors.White;
        }

        /// <summary>
        /// Gets a shadow color that has good contrast with both the background and text
        /// </summary>
        public static Color GetShadowColor(Color backgroundColor, Color textColor)
        {
            // If text is black, use a dark gray shadow
            if (textColor.R == 0 && textColor.G == 0 && textColor.B == 0)
            {
                return Color.FromArgb(120, 60, 60, 60); // Semi-transparent dark gray
            }
            // If text is white, use a light shadow
            else
            {
                return Color.FromArgb(120, 200, 200, 200); // Semi-transparent light gray
            }
        }

        /// <summary>
        /// Calculates relative luminance according to WCAG 2.0 formula
        /// </summary>
        public static double GetRelativeLuminance(Color color)
        {
            // https://www.w3.org/TR/WCAG20/#relativeluminancedef
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            
            // Convert sRGB to linear RGB
            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);
            
            // Calculate luminance
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
        
        /// <summary>
        /// Calculates contrast ratio according to WCAG 2.0 formula
        /// </summary>
        public static double GetContrastRatio(Color c1, Color c2)
        {
            // https://www.w3.org/TR/WCAG20/#contrast-ratiodef
            double l1 = GetRelativeLuminance(c1);
            double l2 = GetRelativeLuminance(c2);
            
            // Ensure the lighter color is used as l1
            if (l1 < l2)
            {
                (l1, l2) = (l2, l1);
            }
            
            return (l1 + 0.05) / (l2 + 0.05);
        }

        /// <summary>
        /// Darkens a color by a specified factor
        /// </summary>
        public static Color DarkenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Max(color.R * factor, 0),
                (byte)Math.Max(color.G * factor, 0),
                (byte)Math.Max(color.B * factor, 0));
        }

        /// <summary>
        /// Lightens a color by a specified factor
        /// </summary>
        public static Color LightenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Min(color.R * factor, 255),
                (byte)Math.Min(color.G * factor, 255),
                (byte)Math.Min(color.B * factor, 255));
        }

        /// <summary>
        /// Adjusts a color by the specified factor (darkens if factor < 1, lightens if factor > 1)
        /// </summary>
        public static Color AdjustColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Min(Math.Max(color.R * factor, 0), 255),
                (byte)Math.Min(Math.Max(color.G * factor, 0), 255),
                (byte)Math.Min(Math.Max(color.B * factor, 0), 255));
        }
    }
}
