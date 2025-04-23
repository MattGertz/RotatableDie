using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using RotatableDie.Models;

namespace RotatableDie.Services
{
    /// <summary>
    /// Service for creating and managing die textures
    /// </summary>
    public class DieTextureService
    {
        private readonly Random random = new Random();
        private const int TEXTURE_SIZE = 512;
        private const int TETRAHEDRON_TEXTURE_SIZE = 768; // Larger texture for more detailed vertex labeling

        // Cache for tetrahedron textures to improve performance
        private readonly Dictionary<string, BitmapImage> _tetrahedronTextureCache = new Dictionary<string, BitmapImage>();
        
        /// <summary>
        /// Creates a texture with a number for a die face
        /// </summary>
        public BitmapImage CreateDieTexture(int number, Color baseColor, DieType dieType, int outputSize = 256)
        {
            // Tetrahedron textures are now created by the TetrahedronDie class
            if (dieType == DieType.Tetrahedron)
            {
                // This will never be called directly as TetrahedronDie overrides the texture creation
                // but keeping it as a fallback with a meaningful error
                throw new InvalidOperationException(
                    "Tetrahedron textures should be created by the TetrahedronDie class");
            }

            // Create a bitmap that we'll draw onto
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(outputSize, outputSize, 96, 96, PixelFormats.Pbgra32);
            
            // Create a visual to draw on
            DrawingVisual drawingVisual = new DrawingVisual();
            
            double fontSizeFactor = CalculateFontSizeFactor(number, dieType);
            
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Draw the background texture
                DrawBackgroundTexture(drawingContext, baseColor, outputSize);
                
                // Choose the best text color for maximum contrast
                Color textColor = ColorUtilities.GetOptimalTextColor(baseColor);
                SolidColorBrush textBrush = new SolidColorBrush(textColor);
                
                // Get shadow color from utility method
                Color shadowColor = ColorUtilities.GetShadowColor(baseColor, textColor);
                
                // Draw the number as text
                drawingContext.PushOpacity(0.9);
                
                // Create the formatted text for the die number
                FormattedText formattedText = CreateFormattedText(
                    number.ToString(), 
                    textBrush, 
                    outputSize * fontSizeFactor,
                    drawingVisual);

                // Position the text based on the die type
                double textX = (outputSize - formattedText.Width) / 2; // Horizontally centered
                double textY = CalculateTextVerticalPosition(dieType, formattedText.Height, outputSize);
                
                // Add a shadow effect with reduced offset for d20
                double shadowOffset = outputSize * (dieType == DieType.Icosahedron ? 0.005 : 0.01);

                // Create shadow text
                FormattedText shadowText = CreateFormattedText(
                    number.ToString(), 
                    new SolidColorBrush(shadowColor), 
                    outputSize * fontSizeFactor,
                    drawingVisual);

                // Draw shadow text
                drawingContext.DrawText(shadowText, new Point(textX + shadowOffset, textY + shadowOffset));
                
                // Draw the main text
                drawingContext.DrawText(formattedText, new Point(textX, textY));
                
                // For d12, d20, and d10, add underlines to 6 and 9 for orientation
                if ((dieType == DieType.Dodecahedron || dieType == DieType.Icosahedron || dieType == DieType.Decahedron) && 
                    (number == 6 || number == 9))
                {
                    DrawNumberUnderline(drawingContext, textBrush, textX, textY, formattedText, outputSize);
                }
                
                drawingContext.Pop();
            }
            
            // Render the drawing onto the bitmap
            renderBitmap.Render(drawingVisual);
            
            // Convert to BitmapImage for use with ImageBrush
            return ConvertToBitmapImage(renderBitmap);
        }
        
        /// <summary>
        /// Calculates the appropriate font size factor based on die type and number
        /// </summary>
        private double CalculateFontSizeFactor(int number, DieType dieType)
        {
            // Default size factor for d6
            double fontSizeFactor = 0.5;
            
            // Reduce font size for double digit numbers on polyhedral dice
            if (number > 9)
            {
                fontSizeFactor = 0.4;
            }
            
            // Further adjust based on die type
            switch (dieType)
            {
                case DieType.Icosahedron:
                    fontSizeFactor *= 0.8; // Smaller for d20
                    break;
                case DieType.Dodecahedron:
                    fontSizeFactor *= 0.85; // Smaller for d12
                    break;
                case DieType.Octahedron:
                    fontSizeFactor *= 0.9; // Slightly smaller for d8
                    break;
                case DieType.Decahedron:
                    fontSizeFactor *= 0.75; // Smaller for d10 kite faces
                    break;
                case DieType.Tesseract:
                    fontSizeFactor *= 0.7; // Smaller for tesseract cells
                    break;
            }
            
            return fontSizeFactor;
        }

        /// <summary>
        /// Draw the background, gradient, blotches and subtle texture
        /// </summary>
        private void DrawBackgroundTexture(DrawingContext drawingContext, Color baseColor, int size)
        {
            // Create base color with slightly darker edges
            Color edgeColor = ColorUtilities.DarkenColor(baseColor, 0.8);
            
            // Draw base color as gradient from edge to center
            RadialGradientBrush baseBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.7,
                RadiusY = 0.7,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(baseColor, 0.6),
                    new GradientStop(edgeColor, 1.0)
                }
            };
            
            drawingContext.DrawRectangle(baseBrush, null, new Rect(0, 0, size, size));
            
            // Add random blotches
            DrawRandomBlotches(drawingContext, baseColor, size);
            
            // Add subtle texture
            DrawSubtleTexture(drawingContext, baseColor, size);
        }
        
        /// <summary>
        /// Draw random blotches for texture effect
        /// </summary>
        private void DrawRandomBlotches(DrawingContext drawingContext, Color baseColor, int size)
        {
            int numBlotches = random.Next(10, 25);
            bool isDarkBackground = ColorUtilities.GetRelativeLuminance(baseColor) < 0.5;
            
            // Determine blotch color based on background
            Color blotchColor = isDarkBackground 
                ? Color.FromRgb(255, 215, 0)  // Gold for dark backgrounds
                : Color.FromRgb(64, 64, 64);  // Dark gray for light backgrounds
            
            for (int i = 0; i < numBlotches; i++)
            {
                // Determine blotch properties
                double x = random.NextDouble() * size;
                double y = random.NextDouble() * size;
                double blotchSize = random.Next(5, 20);
                
                // Draw the blotch
                RadialGradientBrush blotchBrush = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.5, 0.5),
                    Center = new Point(0.5, 0.5),
                    RadiusX = 1.0,
                    RadiusY = 1.0,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(blotchColor, 0.0),
                        new GradientStop(Color.FromArgb(0, blotchColor.R, blotchColor.G, blotchColor.B), 1.0)
                    }
                };
                
                drawingContext.DrawEllipse(blotchBrush, null, new Point(x, y), blotchSize, blotchSize);
            }
        }
        
        /// <summary>
        /// Draw subtle texture effect
        /// </summary>
        private void DrawSubtleTexture(DrawingContext drawingContext, Color baseColor, int size)
        {
            drawingContext.PushOpacity(0.1);
            bool isDarkBackground = ColorUtilities.GetRelativeLuminance(baseColor) < 0.5;
            
            // Determine dot color based on background
            Color dotColor = isDarkBackground 
                ? Color.FromRgb(255, 215, 0)  // Gold for dark backgrounds
                : Color.FromRgb(64, 64, 64);  // Dark gray for light backgrounds
            
            SolidColorBrush dotBrush = new SolidColorBrush(dotColor);
            
            for (int i = 0; i < 200; i++)
            {
                double x = random.NextDouble() * size;
                double y = random.NextDouble() * size;
                double dotSize = random.NextDouble() * 2 + 0.5;
                
                drawingContext.DrawEllipse(dotBrush, null, new Point(x, y), dotSize, dotSize);
            }
            
            drawingContext.Pop();
        }
        
        /// <summary>
        /// Creates formatted text with common settings
        /// </summary>
        private FormattedText CreateFormattedText(string text, Brush brush, double fontSize, Visual visual)
        {
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Times New Roman"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                fontSize,
                brush,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);
        }
        
        /// <summary>
        /// Calculate the vertical position for die number text
        /// </summary>
        private double CalculateTextVerticalPosition(DieType dieType, double textHeight, int outputSize)
        {
            if (dieType == DieType.Octahedron || dieType == DieType.Icosahedron)
            {
                // For triangular-faced dice, position text at the centroid:
                // The centroid is at 2/3 of the height from the top
                double triangleCentroidY = outputSize * (2.0/3.0);
                
                // Position the text so its center is at the centroid
                return triangleCentroidY - (textHeight / 2);
            }
            else if (dieType == DieType.Decahedron)
            {
                // For d10, position the text away from the equator by half a font size
                // Move it from 70% to 60% of the way down (moving toward the pole)
                return outputSize * 0.6 - (textHeight / 2);
            }
            else if (dieType == DieType.Tesseract)
            {
                // For tesseract, center the text vertically
                return (outputSize - textHeight) / 2;
            }
            else
            {
                // For other dice shapes, center the text vertically
                return (outputSize - textHeight) / 2;
            }
        }
        
        /// <summary>
        /// Draw an underline for 6 and 9 numbers to indicate orientation
        /// </summary>
        private void DrawNumberUnderline(DrawingContext drawingContext, Brush brush, double textX, 
            double textY, FormattedText text, int outputSize)
        {
            double underlineThickness = Math.Max(2.0, outputSize * 0.01); // Scale with die size, minimum 2px
            double underlineWidth = text.Width * 0.8; // 80% of number width
            double underlineY = textY + text.Height + underlineThickness; // Just below the number
            double underlineX = textX + (text.Width - underlineWidth) / 2; // Centered under number

            // Draw underline
            drawingContext.DrawLine(
                new Pen(brush, underlineThickness), 
                new Point(underlineX, underlineY), 
                new Point(underlineX + underlineWidth, underlineY));
        }
        
        /// <summary>
        /// Converts a RenderTargetBitmap to a BitmapImage for use in textures
        /// </summary>
        private BitmapImage ConvertToBitmapImage(RenderTargetBitmap renderBitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            
            using (var stream = new System.IO.MemoryStream())
            {
                // Create a PNG encoder and save to stream
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(stream);
                
                // Convert stream to BitmapImage
                stream.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Make it thread-safe
            }
            
            return bitmapImage;
        }
    }
}
