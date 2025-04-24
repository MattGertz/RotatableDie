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
        /// Creates formatted text with common settings, applying special styling for binary and hex
        /// </summary>
        private FormattedText CreateFormattedText(string text, Brush brush, double fontSize, Visual visual)
        {
            // Check for special formatting prefixes
            FontStyle fontStyle = FontStyles.Normal;
            FontWeight fontWeight = FontWeights.Bold; // Default is bold for all dice
            
            string displayText = text;
            
            // Check for binary prefix (use extra bold)
            if (text.StartsWith("B"))
            {
                fontWeight = FontWeights.ExtraBold;
                displayText = text.Substring(1); // Remove prefix for display
            }
            // Check for hexadecimal prefix (use italic)
            else if (text.StartsWith("H"))
            {
                fontStyle = FontStyles.Italic;
                displayText = text.Substring(1); // Remove prefix for display
            }
            // Check for Roman numeral prefix (use smaller font size)
            else if (text.StartsWith("R"))
            {
                fontSize *= 0.8; // Reduce font size for Roman numerals
                displayText = text.Substring(1); // Remove prefix for display
            }
            
            return new FormattedText(
                displayText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Times New Roman"), fontStyle, fontWeight, FontStretches.Normal),
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

        /// <summary>
        /// Creates a texture for a tesseract cell face with different numbering systems
        /// </summary>
        /// <param name="cellIndex">The index of the cubic cell (0-7)</param>
        /// <param name="faceIndex">The face index within the cell (0-5)</param>
        /// <param name="baseColor">The base color for the texture</param>
        /// <param name="isShared">Whether this face is shared with another cell</param>
        /// <param name="sharingCellIndex">The index of the cell sharing this face, if shared</param>
        /// <param name="outputSize">The size of the output texture in pixels</param>
        /// <returns>A BitmapImage texture for the cell face</returns>
        public BitmapImage Create4DCellTexture(int cellIndex, int faceIndex, Color baseColor, bool isShared, int sharingCellIndex, int outputSize = 256)
        {
            // Create a bitmap that we'll draw onto
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(outputSize, outputSize, 96, 96, PixelFormats.Pbgra32);
            
            // Create a visual to draw on
            DrawingVisual drawingVisual = new DrawingVisual();
            
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Draw the background texture
                DrawBackgroundTexture(drawingContext, baseColor, outputSize);
                
                // Choose the best text color for maximum contrast
                Color textColor = ColorUtilities.GetOptimalTextColor(baseColor);
                SolidColorBrush textBrush = new SolidColorBrush(textColor);
                
                // Get shadow color from utility method
                Color shadowColor = ColorUtilities.GetShadowColor(baseColor, textColor);
                
                // Draw the text for this face based on cell's numbering system
                drawingContext.PushOpacity(0.9);
                
                if (isShared && sharingCellIndex >= 0)
                {
                    // Handle shared face: show both numbering systems
                    // Get text for both cells
                    string faceText1 = GetCellFaceText(cellIndex, faceIndex);
                    string faceText2 = GetCellFaceText(sharingCellIndex, faceIndex);
                    
                    // Use smaller font for shared faces to fit both symbols
                    double fontSizeFactor = 0.3;
                    
                    // Create formatted text for the first cell
                    FormattedText formattedText1 = CreateFormattedText(
                        faceText1, 
                        textBrush, 
                        outputSize * fontSizeFactor,
                        drawingVisual);

                    // Create formatted text for the second cell
                    FormattedText formattedText2 = CreateFormattedText(
                        faceText2, 
                        textBrush, 
                        outputSize * fontSizeFactor,
                        drawingVisual);

                    // Position text1 in the upper part of the face
                    double text1X = (outputSize - formattedText1.Width) / 2;
                    double text1Y = outputSize * 0.25 - formattedText1.Height / 2;
                    
                    // Position text2 in the lower part of the face
                    double text2X = (outputSize - formattedText2.Width) / 2;
                    double text2Y = outputSize * 0.75 - formattedText2.Height / 2;
                    
                    // Add a shadow effect
                    double shadowOffset = outputSize * 0.008;

                    // Create shadow text
                    FormattedText shadowText1 = CreateFormattedText(
                        faceText1, 
                        new SolidColorBrush(shadowColor), 
                        outputSize * fontSizeFactor,
                        drawingVisual);
                        
                    FormattedText shadowText2 = CreateFormattedText(
                        faceText2, 
                        new SolidColorBrush(shadowColor), 
                        outputSize * fontSizeFactor,
                        drawingVisual);

                    // Draw shadow text
                    drawingContext.DrawText(shadowText1, new Point(text1X + shadowOffset, text1Y + shadowOffset));
                    drawingContext.DrawText(shadowText2, new Point(text2X + shadowOffset, text2Y + shadowOffset));
                    
                    // Draw the main text for both cells
                    drawingContext.DrawText(formattedText1, new Point(text1X, text1Y));
                    drawingContext.DrawText(formattedText2, new Point(text2X, text2Y));
                }
                else
                {
                    // Single cell face - just show one numbering system
                    string faceText = GetCellFaceText(cellIndex, faceIndex);
                    double fontSizeFactor = 0.4; // Larger font for single number
                    
                    // Create the formatted text
                    FormattedText formattedText = CreateFormattedText(
                        faceText, 
                        textBrush, 
                        outputSize * fontSizeFactor,
                        drawingVisual);

                    // Position the text in the center of the face
                    double textX = (outputSize - formattedText.Width) / 2;
                    double textY = (outputSize - formattedText.Height) / 2;
                    
                    // Add a shadow effect
                    double shadowOffset = outputSize * 0.01;

                    // Create shadow text
                    FormattedText shadowText = CreateFormattedText(
                        faceText, 
                        new SolidColorBrush(shadowColor), 
                        outputSize * fontSizeFactor,
                        drawingVisual);

                    // Draw shadow text
                    drawingContext.DrawText(shadowText, new Point(textX + shadowOffset, textY + shadowOffset));
                    
                    // Draw the main text
                    drawingContext.DrawText(formattedText, new Point(textX, textY));
                }
                
                drawingContext.Pop();
            }
            
            // Render the drawing onto the bitmap
            renderBitmap.Render(drawingVisual);
            
            // Convert to BitmapImage for use with ImageBrush
            return ConvertToBitmapImage(renderBitmap);
        }

        /// <summary>
        /// Determines which cell (if any) shares this face with the current cell
        /// </summary>
        private int GetSharingCellIndex(int cellIndex, int faceIndex)
        {
            // In a 4D die, certain faces are shared between cells
            // This maps the sharing relationship for each cell and face
            // -1 means the face is not shared
            
            // Check if this is a Tesseract (4D hypercube) with 8 cells
            if (cellIndex < 8)
            {
                // These mappings are based on a tesseract where:
                // - Cell 0 is at origin (0,0,0,0)
                // - Cell 1 is at (1,0,0,0)
                // - Cell 2 is at (0,1,0,0)
                // - Cell 3 is at (1,1,0,0)
                // - Cell 4 is at (0,0,1,0)
                // - Cell 5 is at (1,0,1,0)
                // - Cell 6 is at (0,1,1,0)
                // - Cell 7 is at (1,1,1,0)
                
                switch (cellIndex)
                {
                    case 0: // Origin cell (0,0,0,0)
                        if (faceIndex == 0) return 1; // +X face connects to cell 1
                        if (faceIndex == 1) return 2; // +Y face connects to cell 2
                        if (faceIndex == 2) return 4; // +Z face connects to cell 4
                        break;
                    
                    case 1: // (1,0,0,0)
                        if (faceIndex == 3) return 0; // -X face connects to cell 0
                        if (faceIndex == 1) return 3; // +Y face connects to cell 3
                        if (faceIndex == 2) return 5; // +Z face connects to cell 5
                        break;
                    
                    case 2: // (0,1,0,0)
                        if (faceIndex == 0) return 3; // +X face connects to cell 3
                        if (faceIndex == 4) return 0; // -Y face connects to cell 0
                        if (faceIndex == 2) return 6; // +Z face connects to cell 6
                        break;
                    
                    case 3: // (1,1,0,0)
                        if (faceIndex == 3) return 2; // -X face connects to cell 2
                        if (faceIndex == 4) return 1; // -Y face connects to cell 1
                        if (faceIndex == 2) return 7; // +Z face connects to cell 7
                        break;
                    
                    case 4: // (0,0,1,0)
                        if (faceIndex == 0) return 5; // +X face connects to cell 5
                        if (faceIndex == 1) return 6; // +Y face connects to cell 6
                        if (faceIndex == 5) return 0; // -Z face connects to cell 0
                        break;
                    
                    case 5: // (1,0,1,0)
                        if (faceIndex == 3) return 4; // -X face connects to cell 4
                        if (faceIndex == 1) return 7; // +Y face connects to cell 7
                        if (faceIndex == 5) return 1; // -Z face connects to cell 1
                        break;
                    
                    case 6: // (0,1,1,0)
                        if (faceIndex == 0) return 7; // +X face connects to cell 7
                        if (faceIndex == 4) return 4; // -Y face connects to cell 4
                        if (faceIndex == 5) return 2; // -Z face connects to cell 2
                        break;
                    
                    case 7: // (1,1,1,0)
                        if (faceIndex == 3) return 6; // -X face connects to cell 6
                        if (faceIndex == 4) return 5; // -Y face connects to cell 5
                        if (faceIndex == 5) return 3; // -Z face connects to cell 3
                        break;
                }
            }
            else if (cellIndex < 16)
            {
                // For the 16-cell Hexadecachoron (16-cell 4D simplex)
                // Each tetrahedral cell shares its triangular faces with 4 neighboring cells
                // This is a simplified adjacency model for the 16-cell
                
                int cellOffset = cellIndex - 8; // Convert to 0-7 index for the additional 8 cells
                
                // For each face in a tetrahedral cell (4 triangular faces)
                // Map to the correct adjacent cell based on topology of 16-cell
                switch (cellOffset)
                {
                    case 0:
                        if (faceIndex == 0) return 9;  // Face 0 connects to cell 9
                        if (faceIndex == 1) return 10; // Face 1 connects to cell 10
                        if (faceIndex == 2) return 12; // Face 2 connects to cell 12
                        if (faceIndex == 3) return 14; // Face 3 connects to cell 14
                        break;
                        
                    case 1:
                        if (faceIndex == 0) return 8;  // Face 0 connects to cell 8
                        if (faceIndex == 1) return 11; // Face 1 connects to cell 11
                        if (faceIndex == 2) return 13; // Face 2 connects to cell 13
                        if (faceIndex == 3) return 15; // Face 3 connects to cell 15
                        break;
                        
                    case 2:
                        if (faceIndex == 0) return 8;  // Face 0 connects to cell 8
                        if (faceIndex == 1) return 11; // Face 1 connects to cell 11
                        if (faceIndex == 2) return 13; // Face 2 connects to cell 13
                        if (faceIndex == 3) return 15; // Face 3 connects to cell 15
                        break;
                        
                    case 3:
                        if (faceIndex == 0) return 9;  // Face 0 connects to cell 9
                        if (faceIndex == 1) return 10; // Face 1 connects to cell 10 
                        if (faceIndex == 2) return 12; // Face 2 connects to cell 12
                        if (faceIndex == 3) return 14; // Face 3 connects to cell 14
                        break;
                        
                    case 4:
                        if (faceIndex == 0) return 9;  // Face 0 connects to cell 9
                        if (faceIndex == 1) return 11; // Face 1 connects to cell 11
                        if (faceIndex == 2) return 12; // Face 2 connects to cell 12
                        if (faceIndex == 3) return 15; // Face 3 connects to cell 15
                        break;
                        
                    case 5:
                        if (faceIndex == 0) return 8;  // Face 0 connects to cell 8
                        if (faceIndex == 1) return 10; // Face 1 connects to cell 10
                        if (faceIndex == 2) return 13; // Face 2 connects to cell 13
                        if (faceIndex == 3) return 14; // Face 3 connects to cell 14
                        break;
                        
                    case 6:
                        if (faceIndex == 0) return 8;  // Face 0 connects to cell 8
                        if (faceIndex == 1) return 10; // Face 1 connects to cell 10
                        if (faceIndex == 2) return 12; // Face 2 connects to cell 12
                        if (faceIndex == 3) return 15; // Face 3 connects to cell 15
                        break;
                        
                    case 7:
                        if (faceIndex == 0) return 9;  // Face 0 connects to cell 9
                        if (faceIndex == 1) return 11; // Face 1 connects to cell 11
                        if (faceIndex == 2) return 13; // Face 2 connects to cell 13
                        if (faceIndex == 3) return 14; // Face 3 connects to cell 14
                        break;
                }
            }
            
            return -1; // No sharing
        }

        /// <summary>
        /// Determines the text to display on each tesseract cell face based on the cell index
        /// Each cell uses a different numbering system to distinguish them
        /// </summary>
        /// <param name="cellIndex">The 0-7 index of the cubic cell</param>
        /// <param name="faceIndex">The 0-5 face index within the cell</param>
        /// <returns>Text to display on the face</returns>
        private string GetCellFaceText(int cellIndex, int faceIndex)
        {
            int value = faceIndex + 1;
    
            // For the 8-cell Tesseract (hypercube)
            if (cellIndex < 8)
            {
                switch (cellIndex)
                {
                    case 0: // Regular numbers
                        return value.ToString();
                        
                    case 1: // Letters
                        return ((char)('A' + faceIndex)).ToString();
                        
                    case 2: // Roman numerals with "R" prefix
                        return "R" + ConvertToRomanNumeral(value);
                        
                    case 3: // Greek letters
                        string[] greekLetters = { "α", "β", "γ", "δ", "ε", "ζ" };
                        return greekLetters[faceIndex];
                        
                    case 4: // Binary with "B" prefix (bold)
                        return "B" + Convert.ToString(value, 2).PadLeft(3, '0');
                        
                    case 5: // Hex with "H" prefix (italic)
                        return "H" + value.ToString("X");
                        
                    case 6: // Dots
                        return new string('●', value);
                        
                    case 7: // Circled numbers
                        string[] circledNumbers = { "①", "②", "③", "④", "⑤", "⑥" };
                        return circledNumbers[faceIndex];
                }
            }
            // For the 16-cell Hexadecachoron
            else if (cellIndex < 24)
            {
                // Each tetrahedral cell has 4 triangular faces (value ranges from 1-4)
                int adjustedValue = faceIndex + 1; // Face values are 1-4 for tetrahedra
                
                switch (cellIndex - 8) // Adjust to 0-15 range for the cells
                {
                    case 0: // Prefix with "x" instead of double digits
                        return "x" + adjustedValue.ToString();
                        
                    case 1: // Prefix with "y" instead of double letters
                        return "y" + adjustedValue.ToString();
                        
                    case 2: // Changed: Square brackets prefix instead of wrapping
                        return "[" + adjustedValue.ToString();
                        
                    case 3: // Changed: Curly braces prefix instead of wrapping
                        return "{" + adjustedValue.ToString();
                        
                    case 4: // Asterisk prefix
                        return "*" + adjustedValue.ToString();
                        
                    case 5: // Hash prefix
                        return "#" + adjustedValue.ToString();
                        
                    case 6: // Diamond prefix
                        return "◆" + adjustedValue.ToString();
                        
                    case 7: // Changed: Underscore prefix instead of wrapping
                        return "_" + adjustedValue.ToString();
                        
                    case 8: // Plus prefix
                        return "+" + adjustedValue.ToString();
                        
                    case 9: // Tilde prefix
                        return "~" + adjustedValue.ToString();
                        
                    case 10: // Changed: Parentheses prefix instead of wrapping
                        return "(" + adjustedValue.ToString();
                        
                    case 11: // Caret prefix
                        return "^" + adjustedValue.ToString();
                        
                    case 12: // Equal prefix
                        return "=" + adjustedValue.ToString();
                        
                    case 13: // At symbol prefix
                        return "@" + adjustedValue.ToString();
                        
                    case 14: // Dollar sign prefix
                        return "$" + adjustedValue.ToString();
                        
                    case 15: // Percent suffix - keep as is
                        return adjustedValue.ToString() + "%";
                }
            }
            
            // Default fallback
            return value.ToString();
        }

        private string ConvertToRomanNumeral(int number)
        {
            // Simple Roman numeral conversion for 1-6
            string[] romanNumerals = { "I", "II", "III", "IV", "V", "VI" };
            if (number >= 1 && number <= 6)
                return romanNumerals[number - 1];
            return number.ToString();
        }

        /// <summary>
        /// Draws an indicator showing that this face is shared with another cell in the tesseract
        /// </summary>
        private void DrawSharedFaceIndicator(DrawingContext drawingContext, Brush brush, int outputSize, int sharingCellIndex)
        {
            // Calculate indicator size based on output size
            double indicatorSize = outputSize * 0.1;
            double cornerOffset = outputSize * 0.05;
            
            // Use direct FormattedText creation with fixed DPI value
            FormattedText indicatorText = new FormattedText(
                $"→{sharingCellIndex}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Times New Roman"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                indicatorSize,
                brush,
                96.0);  // Use standard 96 DPI
                
            // Position in the bottom right corner
            Point position = new Point(
                outputSize - indicatorText.Width - cornerOffset,
                outputSize - indicatorText.Height - cornerOffset);
                
            // Draw a semi-transparent background for the indicator
            Rect indicatorRect = new Rect(
                position.X - 2, 
                position.Y - 2,
                indicatorText.Width + 4, 
                indicatorText.Height + 4);
            
            // Create a semi-transparent background color (works with any brush type)
            Color bgColor = Colors.Gray;
            byte alpha = 40; // Low alpha for transparency
            
            if (brush is SolidColorBrush solidBrush)
            {
                // If we have a solid color brush, use its color
                bgColor = solidBrush.Color;
            }
            
            SolidColorBrush bgBrush = new SolidColorBrush(Color.FromArgb(alpha, bgColor.R, bgColor.G, bgColor.B));
            
            // Draw background and border
            drawingContext.DrawRectangle(
                bgBrush,
                new Pen(brush, 1),
                indicatorRect);
                
            // Draw the indicator text
            drawingContext.DrawText(indicatorText, position);
        }
    }
}
