using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Globalization;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes
{
    public class TetrahedronDie : Die
    {
        private const int TETRAHEDRON_TEXTURE_SIZE = 768; // Same constant as in DieTextureService
        private readonly TetrahedronTextureGenerator _textureGenerator;
        
        public TetrahedronDie(DieTextureService textureService) 
            : base(DieType.Tetrahedron, textureService)
        {
            _textureGenerator = new TetrahedronTextureGenerator(textureService);
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            // Tetrahedron has 4 equilateral triangular faces
            double size = 0.8; // Scale to a reasonable size
            
            // Calculate coordinates for a regular tetrahedron using more accurate formulas
            Point3D[] vertices = new Point3D[4];
            
            // These coordinates create a perfect regular tetrahedron
            double a = size * Math.Sqrt(2) / 3.0;
            double b = size * -1.0 / 3.0;
            double c = size * 2.0 / 3.0;
            
            vertices[0] = new Point3D(0, 0, size);  // Top vertex (1)
            vertices[1] = new Point3D(size, 0, b);  // Base vertex (2) 
            vertices[2] = new Point3D(-size/2, size * Math.Sqrt(3)/2, b);  // Base vertex (3)
            vertices[3] = new Point3D(-size/2, -size * Math.Sqrt(3)/2, b); // Base vertex (4)
            
            if (wireframeMode)
            {
                // Create wireframe edges - connect all vertices to form a tetrahedron
                AddWireframeEdge(modelGroup, vertices[0], vertices[1], color);
                AddWireframeEdge(modelGroup, vertices[0], vertices[2], color);
                AddWireframeEdge(modelGroup, vertices[0], vertices[3], color);
                AddWireframeEdge(modelGroup, vertices[1], vertices[2], color);
                AddWireframeEdge(modelGroup, vertices[2], vertices[3], color);
                AddWireframeEdge(modelGroup, vertices[3], vertices[1], color);
                
                return;
            }
            
            // Create textures for each face with the vertex numbers using the specialized generator
            BitmapImage[] textures = new BitmapImage[4];
            for (int i = 0; i < 4; i++)
            {
                textures[i] = _textureGenerator.CreateTexture(i + 1, color);
            }
            
            // Create the four triangular faces
            // Each face is defined counterclockwise when viewed from outside
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 0, 2, 1 }, textures[0]);
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 0, 1, 3 }, textures[1]);
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 0, 3, 2 }, textures[2]);
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 1, 2, 3 }, textures[3]);
        }

        private void AddWireframeEdge(Model3DGroup modelGroup, Point3D point1, Point3D point2, Color color)
        {
            // Create a thin tube between the two points to represent an edge
            double thickness = 0.01; // Adjust thickness as needed
            Vector3D direction = point2 - point1;
            double length = direction.Length;
            direction.Normalize();
            
            // Create a transform to rotate and position the cylinder for this edge
            Transform3DGroup transformGroup = new Transform3DGroup();
            
            // Create a cylinder aligned with the Z-axis
            MeshGeometry3D edgeMesh = new MeshGeometry3D();
            
            // Create a simple cylinder with 8 segments around the circumference
            const int segments = 8;
            for (int i = 0; i <= segments; i++)
            {
                double angle = i * 2 * Math.PI / segments;
                double x = thickness * Math.Cos(angle);
                double y = thickness * Math.Sin(angle);
                
                // Add vertices at both ends of the cylinder
                edgeMesh.Positions.Add(new Point3D(x, y, 0));
                edgeMesh.Positions.Add(new Point3D(x, y, length));
            }
            
            // Create triangles for the cylinder
            for (int i = 0; i < segments; i++)
            {
                int baseIndex = i * 2;
                
                // Create two triangles for this segment
                edgeMesh.TriangleIndices.Add(baseIndex);
                edgeMesh.TriangleIndices.Add(baseIndex + 1);
                edgeMesh.TriangleIndices.Add(baseIndex + 2);
                
                edgeMesh.TriangleIndices.Add(baseIndex + 1);
                edgeMesh.TriangleIndices.Add(baseIndex + 3);
                edgeMesh.TriangleIndices.Add(baseIndex + 2);
            }
            
            // Create transform to align the edge with the specified points
            Vector3D zaxis = new Vector3D(0, 0, 1);
            Quaternion rotation = new Quaternion();
            
            if (Math.Abs(Vector3D.DotProduct(direction, zaxis)) < 0.99999)
            {
                Vector3D rotationAxis = Vector3D.CrossProduct(zaxis, direction);
                rotationAxis.Normalize();
                double rotationAngle = Math.Acos(Vector3D.DotProduct(zaxis, direction));
                rotation = new Quaternion(rotationAxis, rotationAngle * 180 / Math.PI);
            }
            
            RotateTransform3D rotateTransform = new RotateTransform3D(new QuaternionRotation3D(rotation));
            transformGroup.Children.Add(rotateTransform);
            
            // Add translation to position the edge at the start point
            transformGroup.Children.Add(new TranslateTransform3D(point1.X, point1.Y, point1.Z));
            
            // Create material for the edge (using black color for visibility)
            Material edgeMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Black));
            
            // Create the model and add it to the group
            GeometryModel3D model = new GeometryModel3D();
            model.Geometry = edgeMesh;
            model.Material = edgeMaterial;
            model.BackMaterial = edgeMaterial;
            model.Transform = transformGroup;
            
            modelGroup.Children.Add(model);
        }

        /// <summary>
        /// Specialized texture generator for tetrahedron dice
        /// </summary>
        private class TetrahedronTextureGenerator
        {
            private readonly Random random = new Random();
            private readonly DieTextureService _baseTextureService;
            private readonly Dictionary<string, BitmapImage> _textureCache = new Dictionary<string, BitmapImage>();
            
            // Static lookup table for vertex positions - maps [faceNumber-1, vertexNumber-1] to position index
            // Using 0-based arrays but preserving 1-based face and vertex numbering logic
            private static readonly int[,] VERTEX_POSITION_MAP = {
                // Vertex: 1  2  3  4
                /*Face 1*/{ 0, 2, 1,-1}, 
                /*Face 2*/{ 0,-1, 2, 1}, 
                /*Face 3*/{ 0, 1,-1, 2}, 
                /*Face 4*/{-1, 2, 0, 1}  
            };

            public TetrahedronTextureGenerator(DieTextureService baseTextureService)
            {
                _baseTextureService = baseTextureService;
            }
            
            /// <summary>
            /// Creates a tetrahedron face texture with vertex numbering
            /// </summary>
            public BitmapImage CreateTexture(int faceNumber, Color baseColor)
            {
                // Create a cache key for the texture
                string cacheKey = $"{faceNumber}_{baseColor.R}_{baseColor.G}_{baseColor.B}";
                
                // Check if the texture is already in cache
                if (_textureCache.TryGetValue(cacheKey, out var cachedTexture))
                {
                    return cachedTexture;
                }
                
                // Create the texture
                BitmapImage texture = CreateTetrahedronVertexTexture(faceNumber, baseColor);
                _textureCache[cacheKey] = texture;
                return texture;
            }

            /// <summary>
            /// Creates a texture for a d4 (tetrahedron) face with vertex-oriented numbers
            /// </summary>
            private BitmapImage CreateTetrahedronVertexTexture(int faceNumber, Color baseColor)
            {
                // Create the bitmap to draw on
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    TETRAHEDRON_TEXTURE_SIZE,
                    TETRAHEDRON_TEXTURE_SIZE,
                    96, 96,
                    PixelFormats.Pbgra32);
                    
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    // Draw the background texture
                    DrawBackgroundTexture(drawingContext, baseColor, TETRAHEDRON_TEXTURE_SIZE);
                    
                    // Choose the best text color for maximum contrast
                    Color textColor = ColorUtilities.GetOptimalTextColor(baseColor);
                    SolidColorBrush textBrush = new SolidColorBrush(textColor);
                    
                    // Get shadow color from utility method
                    Color shadowColor = ColorUtilities.GetShadowColor(baseColor, textColor);
                        
                    // Shadow offset
                    double shadowOffset = TETRAHEDRON_TEXTURE_SIZE * 0.005;
                    
                    // Font size for vertex numbers
                    double fontSize = TETRAHEDRON_TEXTURE_SIZE * 0.15;
                    
                    // Get the vertex numbers for this face
                    int[] faceVertices = GetTetrahedronVertexNumbers(faceNumber);
                    
                    // Define where the vertices are in texture space (normalized coordinates)
                    var vertexPositionsNormalized = new Point[]
                    {
                        new Point(0.5, 0.0),   // Top vertex
                        new Point(0.0, 1.0),   // Bottom-left vertex
                        new Point(1.0, 1.0)    // Bottom-right vertex
                    };
                    
                    // Draw each vertex number
                    foreach (int vertexNumber in faceVertices)
                    {
                        // Get the position index for this vertex on the current face using the static lookup table
                        // When using the lookup table, adjust indices for 0-based array:
                        int positionIndex = VERTEX_POSITION_MAP[faceNumber-1, vertexNumber-1];
                        
                        // Calculate vertex position and rotation
                        Point position = CalculateVertexPosition(
                            vertexPositionsNormalized[positionIndex], 
                            TETRAHEDRON_TEXTURE_SIZE);
                        
                        // Calculate rotation to make the number's TOP point TOWARD the vertex
                        double rotation = CalculateVertexRotation(positionIndex);
                        
                        // Draw the vertex number
                        DrawVertexNumber(drawingContext, position, vertexNumber, fontSize, textBrush, 
                            shadowColor, shadowOffset, rotation, drawingVisual);
                    }
                }
                
                renderBitmap.Render(drawingVisual);
                return ConvertToBitmapImage(renderBitmap);
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
            /// Calculate the position of a vertex number with proper inward offset
            /// </summary>
            private Point CalculateVertexPosition(Point normalizedPos, int size)
            {
                // Convert to pixel coordinates
                double x = normalizedPos.X * size;
                double y = normalizedPos.Y * size;
                
                // Adjust position to be slightly inward from the vertex
                double inwardFactor = 0.22; // Increased from 0.18 to create more space between vertex and number
                double centerX = size * 0.5;
                double centerY = size * (2.0/3.0); // Triangle centroid Y

                // Move position towards the centroid to avoid clipping
                x = x + (centerX - x) * inwardFactor;
                y = y + (centerY - y) * inwardFactor;
                
                return new Point(x, y);
            }
            
            /// <summary>
            /// Calculate rotation angle for a vertex number
            /// </summary>
            private double CalculateVertexRotation(int positionIndex)
            {
                switch (positionIndex)
                {
                    case 0: // Top vertex - number is upright (top points up toward the vertex)
                        return 0; // No rotation needed for top vertex
                    case 1: // Bottom-left vertex - rotate so top points to bottom-left
                        return -120; // 120 degrees clockwise
                    case 2: // Bottom-right vertex - rotate so top points to bottom-right
                        return 120; // 120 degrees counterclockwise
                    default:
                        return 0;
                }
            }
            
            /// <summary>
            /// Draw a vertex number with proper rotation and shadow
            /// </summary>
            private void DrawVertexNumber(DrawingContext drawingContext, Point position, int number,
                double fontSize, Brush textBrush, Color shadowColor, double shadowOffset, 
                double rotation, Visual visual)
            {
                // Create formatted text for the vertex number
                FormattedText formattedText = CreateFormattedText(
                    number.ToString(), 
                    textBrush, 
                    fontSize, 
                    visual);
                
                // Create shadow text
                FormattedText shadowText = CreateFormattedText(
                    number.ToString(), 
                    new SolidColorBrush(shadowColor), 
                    fontSize, 
                    visual);
                
                // Center text at position
                double offsetX = formattedText.Width / 2;
                double offsetY = formattedText.Height / 2;
                
                // Apply transforms
                drawingContext.PushTransform(new TranslateTransform(position.X, position.Y));
                drawingContext.PushTransform(new RotateTransform(rotation));
                
                // Draw shadow first
                drawingContext.DrawText(shadowText, new Point(-offsetX + shadowOffset, -offsetY + shadowOffset));
                // Draw main text
                drawingContext.DrawText(formattedText, new Point(-offsetX, -offsetY));
                
                drawingContext.Pop(); // Undo rotation
                drawingContext.Pop(); // Undo translation
            }
            
            /// <summary>
            /// Gets the three vertex numbers that should appear on a specific face of the tetrahedron
            /// </summary>
            private int[] GetTetrahedronVertexNumbers(int faceNumber)
            {
                // This maps each face to the three vertices it contains
                // For a tetrahedron with vertices labeled 1-4, each face contains three of these vertices
                switch (faceNumber)
                {
                    case 1: return new int[] { 1, 2, 3 }; // Face 1 contains vertices 1, 2, and 3
                    case 2: return new int[] { 1, 3, 4 }; // Face 2 contains vertices 1, 3, and 4
                    case 3: return new int[] { 1, 4, 2 }; // Face 3 contains vertices 1, 4, and 2
                    case 4: return new int[] { 2, 3, 4 }; // Face 4 contains vertices 2, 3, and 4
                    default: return new int[] { 1, 2, 3 }; // Default fallback
                }
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
}
