using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes4D
{
    /// <summary>
    /// Represents a pentachoron (4D 5-cell) die with 5 tetrahedral cells
    /// </summary>
    public class PentachoronDie : Die4D
    {
        // Projected 3D vertices after 4D rotation
        private Point3D[] projectedVertices = Array.Empty<Point3D>();
        
        // Collection of tetrahedral cells that make up the pentachoron
        private List<TetrahedralCell> cells = new List<TetrahedralCell>();
        
        // Distance of the 4D viewer from the origin
        private double viewerDistance = 5.0;
        
        // Scale factor to keep the pentachoron size comparable to other dice
        private const double SCALE_FACTOR = 0.7;
        
        // Edge transparency handling
        private double edgeOpacity = 1.0;
        private bool useTransparency = true;
        
        public PentachoronDie(DieTextureService textureService) 
            : base(DieType.Pentachoron, textureService)
        {
            // Initialize the pentachoron geometry
            InitializePentachoron();
        }
        
        /// <summary>
        /// Initialize the pentachoron vertices and cells
        /// </summary>
        private void InitializePentachoron()
        {
            // A pentachoron has 5 vertices in 4D
            OriginalVertices4D = new Point4D[5];
            
            // Define the 5 vertices of the regular pentachoron
            // These coordinates represent a symmetric pentachoron in 4D space
            
            // First vertex at origin - this helps simplify the structure
            OriginalVertices4D[0] = new Point4D(0, 0, 0, -SCALE_FACTOR);
            
            // Four vertices in the form of a tetrahedron in 4D space
            double a = SCALE_FACTOR * Math.Sqrt(5) / 2;
            double b = SCALE_FACTOR * Math.Sqrt(5) / (2 * Math.Sqrt(5) - 2);
            
            OriginalVertices4D[1] = new Point4D(0, 0, a, b);
            OriginalVertices4D[2] = new Point4D(0, a * Math.Sqrt(8) / 3, -a/3, b);
            OriginalVertices4D[3] = new Point4D(-a * Math.Sqrt(6) / 3, -a * Math.Sqrt(2) / 3, -a/3, b);
            OriginalVertices4D[4] = new Point4D(a * Math.Sqrt(6) / 3, -a * Math.Sqrt(2) / 3, -a/3, b);
            
            // Initialize projected vertices array
            projectedVertices = new Point3D[5];
            
            // Initialize the cells of the pentachoron (5 tetrahedra)
            InitializeCells();
        }
        
        /// <summary>
        /// Initialize the 5 tetrahedral cells that make up the pentachoron
        /// </summary>
        private void InitializeCells()
        {
            cells = new List<TetrahedralCell>();
            
            // Define the 5 tetrahedral cells of the pentachoron
            // Each cell is defined by the indices of its 4 vertices
            
            // Cell 0 (formed by vertices 1, 2, 3, 4)
            cells.Add(new TetrahedralCell(
                new int[] { 1, 2, 3, 4 },
                0)); // Cell number 0
                
            // Cell 1 (formed by vertices 0, 2, 3, 4)
            cells.Add(new TetrahedralCell(
                new int[] { 0, 2, 3, 4 },
                1)); // Cell number 1
                
            // Cell 2 (formed by vertices 0, 1, 3, 4)
            cells.Add(new TetrahedralCell(
                new int[] { 0, 1, 3, 4 },
                2)); // Cell number 2
                
            // Cell 3 (formed by vertices 0, 1, 2, 4)
            cells.Add(new TetrahedralCell(
                new int[] { 0, 1, 2, 4 },
                3)); // Cell number 3
                
            // Cell 4 (formed by vertices 0, 1, 2, 3)
            cells.Add(new TetrahedralCell(
                new int[] { 0, 1, 2, 3 },
                4)); // Cell number 4
        }
        
        /// <summary>
        /// Project the 4D vertices to 3D space
        /// </summary>
        protected override void ProjectTo3D()
        {
            // Project each 4D vertex to 3D
            for (int i = 0; i < CurrentVertices4D.Length; i++)
            {
                projectedVertices[i] = CurrentVertices4D[i].ProjectTo3D(viewerDistance);
            }
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            // Apply 4D rotation
            Apply4DRotation();
            
            // Project the 4D vertices to 3D space
            ProjectTo3D();
            
            // Render the projected 3D geometry with wireframe mode
            RenderProjectedGeometry(modelGroup, color, wireframeMode);
        }
        
        /// <summary>
        /// Render the projected 3D geometry
        /// </summary>
        protected override void RenderProjectedGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode)
        {
            // Calculate cell visibility based on the current 4D rotation
            CalculateCellVisibility();
            
            // Store the original color to ensure it doesn't change during rotation
            Color originalColor = color;
            
            // Sort cells by visibility for proper rendering order (most visible first)
            cells.Sort((a, b) => b.Visibility.CompareTo(a.Visibility));
            
            if (wireframeMode)
            {
                RenderWireframe(modelGroup, originalColor);
                return;
            }
            
            // Render each cell
            foreach (var cell in cells)
            {
                // Only render cells with some visibility
                if (cell.Visibility > 0.05)
                {
                    // Always use the original color for each cell
                    RenderTetrahedralCell(modelGroup, cell, originalColor);
                }
            }
        }
        
        /// <summary>
        /// Render the pentachoron in wireframe mode
        /// </summary>
        private void RenderWireframe(Model3DGroup modelGroup, Color color)
        {
            // Define all edges in the pentachoron
            HashSet<string> renderedEdges = new HashSet<string>();
            
            // 1. Add edges of each tetrahedral cell
            foreach (var cell in cells)
            {
                // Only render cells with some visibility
                if (cell.Visibility > 0.05)
                {
                    // Define the edges of a tetrahedron (pairs of vertex indices)
                    int[][] tetraEdges = new int[][]
                    {
                        new int[] { 0, 1 }, new int[] { 0, 2 }, new int[] { 0, 3 }, 
                        new int[] { 1, 2 }, new int[] { 1, 3 }, new int[] { 2, 3 }
                    };
                    
                    // Add each edge
                    foreach (int[] edge in tetraEdges)
                    {
                        // Get the two vertex positions
                        int vertex1Index = cell.VertexIndices[edge[0]];
                        int vertex2Index = cell.VertexIndices[edge[1]];
                        
                        // Create a unique key for this edge (smaller index first)
                        string edgeKey = vertex1Index < vertex2Index ? 
                            $"{vertex1Index}-{vertex2Index}" : $"{vertex2Index}-{vertex1Index}";
                        
                        // Only add the edge if we haven't already
                        if (!renderedEdges.Contains(edgeKey))
                        {
                            renderedEdges.Add(edgeKey);
                            
                            // Always use full opacity for wireframe mode
                            double opacity = 1.0;
                            
                            // Add the wireframe edge
                            AddWireframeEdge(modelGroup, 
                                projectedVertices[vertex1Index], 
                                projectedVertices[vertex2Index], 
                                color,
                                opacity);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a wireframe edge between two points
        /// </summary>
        private void AddWireframeEdge(Model3DGroup modelGroup, Point3D point1, Point3D point2, Color color, double opacity = 1.0)
        {
            // Create a simple line segment between the two points
            double thickness = 0.005; // Thinner wireframe for 4D objects (half of the 3D dice thickness)
            
            // Create a line segment mesh (two triangles forming a thin rectangle)
            MeshGeometry3D lineMesh = new MeshGeometry3D();
            
            // Calculate a perpendicular vector for giving the line some thickness
            // We need two perpendicular vectors to create a 3D line with thickness
            Vector3D lineDir = point2 - point1;
            lineDir.Normalize();
            
            // Find two perpendicular vectors to the line direction
            Vector3D perpVec1, perpVec2;
            
            // Choose a reference vector that's unlikely to be parallel to lineDir
            Vector3D refVector = Math.Abs(lineDir.Z) > 0.9 ? new Vector3D(1, 0, 0) : new Vector3D(0, 0, 1);
            
            // Create two perpendicular vectors to form the "thickness" of the line
            perpVec1 = Vector3D.CrossProduct(lineDir, refVector);
            perpVec1.Normalize();
            perpVec1 *= thickness;
            
            perpVec2 = Vector3D.CrossProduct(lineDir, perpVec1);
            perpVec2.Normalize();
            perpVec2 *= thickness;
            
            // Create the vertices of the line segment (as a thin 3D tube with 4 sides)
            Point3D[] vertices = new Point3D[8];
            
            // Starting point vertices (form a small square around the starting point)
            vertices[0] = point1 + perpVec1 + perpVec2;
            vertices[1] = point1 + perpVec1 - perpVec2;
            vertices[2] = point1 - perpVec1 - perpVec2;
            vertices[3] = point1 - perpVec1 + perpVec2;
            
            // Ending point vertices (form a small square around the ending point)
            vertices[4] = point2 + perpVec1 + perpVec2;
            vertices[5] = point2 + perpVec1 - perpVec2;
            vertices[6] = point2 - perpVec1 - perpVec2;
            vertices[7] = point2 - perpVec1 + perpVec2;
            
            // Add all vertices to the mesh
            foreach (Point3D vertex in vertices)
            {
                lineMesh.Positions.Add(vertex);
            }
            
            // Create the faces of the 3D line (triangles)
            // Side 1
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(1);
            lineMesh.TriangleIndices.Add(5);
            
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(5);
            lineMesh.TriangleIndices.Add(4);
            
            // Side 2
            lineMesh.TriangleIndices.Add(1);
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(6);
            
            lineMesh.TriangleIndices.Add(1);
            lineMesh.TriangleIndices.Add(6);
            lineMesh.TriangleIndices.Add(5);
            
            // Side 3
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(7);
            
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(7);
            lineMesh.TriangleIndices.Add(6);
            
            // Side 4
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(4);
            
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(7);
            
            // Optional: Add end caps if desired
            // End cap 1
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(2);
            
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(1);
            
            // End cap 2
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(5);
            lineMesh.TriangleIndices.Add(6);
            
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(6);
            lineMesh.TriangleIndices.Add(7);
            
            // Override with solid black color for wireframe edges in 4D dice
            Color edgeColor = Colors.Black;
            Material edgeMaterial = new DiffuseMaterial(new SolidColorBrush(edgeColor));
            
            // Create the 3D model and add it to the group
            GeometryModel3D lineModel = new GeometryModel3D();
            lineModel.Geometry = lineMesh;
            lineModel.Material = edgeMaterial;
            lineModel.BackMaterial = edgeMaterial;
            
            modelGroup.Children.Add(lineModel);
        }
        
        /// <summary>
        /// Calculate the visibility of each cell based on W coordinates
        /// </summary>
        private void CalculateCellVisibility()
        {
            foreach (var cell in cells)
            {
                double avgW = 0;
                
                // Calculate average W coordinate for the cell's vertices
                foreach (int vertexIndex in cell.VertexIndices)
                {
                    avgW += CurrentVertices4D[vertexIndex].W;
                }
                
                avgW /= 4; // Divide by number of vertices per cell (4 for tetrahedron)
                
                // Convert to visibility value (0-1 range, higher for cells with higher W)
                // Normalize based on the maximum possible W value
                cell.Visibility = (avgW + SCALE_FACTOR) / (2 * SCALE_FACTOR);
                
                // Calculate opacity based on W coordinate (cells with higher W are more opaque)
                if (useTransparency)
                {
                    cell.Opacity = Math.Min(1.0, Math.Max(0.1, cell.Visibility));
                }
                else
                {
                    cell.Opacity = 1.0; // No transparency
                }
            }
        }
        
        /// <summary>
        /// Render a tetrahedral cell of the pentachoron
        /// </summary>
        private void RenderTetrahedralCell(Model3DGroup modelGroup, TetrahedralCell cell, Color baseColor)
        {
            // Define the faces of a tetrahedron (groups of 3 vertices)
            int[][] tetraFaces = new int[][]
            {
                new int[] { 0, 1, 2 }, // Face 0
                new int[] { 0, 2, 3 }, // Face 1
                new int[] { 0, 3, 1 }, // Face 2
                new int[] { 1, 3, 2 }  // Face 3
            };
            
            // Create each face of this cell
            for (int faceIndex = 0; faceIndex < tetraFaces.Length; faceIndex++)
            {
                MeshGeometry3D faceMesh = new MeshGeometry3D();
                
                // Add the 3 vertices for this face
                foreach (int vertexOffset in tetraFaces[faceIndex])
                {
                    int vertexIndex = cell.VertexIndices[vertexOffset];
                    faceMesh.Positions.Add(projectedVertices[vertexIndex]);
                }
                
                // Add triangulation indices (already triangles)
                faceMesh.TriangleIndices.Add(0);
                faceMesh.TriangleIndices.Add(1);
                faceMesh.TriangleIndices.Add(2);
                
                // Add texture coordinates for triangle face
                faceMesh.TextureCoordinates.Add(new Point(0, 0));
                faceMesh.TextureCoordinates.Add(new Point(1, 0));
                faceMesh.TextureCoordinates.Add(new Point(0.5, 0.866)); // ~ sin(60°) for equilateral triangle

                // Check if this face is shared with another cell
                bool isShared = false;
                int sharingCellIndex = -1;
                
                // Determine if this is a shared face by checking other cells
                foreach (var otherCell in cells)
                {
                    // Skip the current cell
                    if (otherCell.Number == cell.Number)
                        continue;
                    
                    // Get the 3 vertex indices for this face
                    int[] faceVertices = new int[3];
                    for (int i = 0; i < 3; i++)
                    {
                        faceVertices[i] = cell.VertexIndices[tetraFaces[faceIndex][i]];
                    }
                    
                    // Check if these 3 vertices are all contained in the other cell
                    bool allVerticesShared = true;
                    foreach (int vertexIndex in faceVertices)
                    {
                        if (!Array.Exists(otherCell.VertexIndices, v => v == vertexIndex))
                        {
                            allVerticesShared = false;
                            break;
                        }
                    }
                    
                    if (allVerticesShared)
                    {
                        isShared = true;
                        sharingCellIndex = otherCell.Number;
                        break;
                    }
                }
                
                // Create texture for this face - always use the original base color
                BitmapImage texture = CreatePentachoronFaceTexture(
                    cell.Number,         // Cell index for numbering system
                    faceIndex,           // Face number
                    baseColor,           // Original base color - important to use this consistently
                    isShared,            // Whether this face is shared
                    sharingCellIndex);   // Index of the sharing cell, if any
                
                // Create material with the texture - apply transparency here
                ImageBrush textureBrush = new ImageBrush(texture);
                
                // Fix the mirrored text by applying a ScaleTransform based on face orientation
                
                // Calculate face center
                Point3D faceCenter = new Point3D(
                    (faceMesh.Positions[0].X + faceMesh.Positions[1].X + faceMesh.Positions[2].X) / 3,
                    (faceMesh.Positions[0].Y + faceMesh.Positions[1].Y + faceMesh.Positions[2].Y) / 3,
                    (faceMesh.Positions[0].Z + faceMesh.Positions[1].Z + faceMesh.Positions[2].Z) / 3
                );
                
                // Calculate face normal vector
                Vector3D edge1 = faceMesh.Positions[1] - faceMesh.Positions[0];
                Vector3D edge2 = faceMesh.Positions[2] - faceMesh.Positions[0];
                Vector3D normal = Vector3D.CrossProduct(edge1, edge2);
                normal.Normalize();
                
                // Vector from origin to face center
                Vector3D toCenter = new Vector3D(faceCenter.X, faceCenter.Y, faceCenter.Z);
                toCenter.Normalize();
                
                // If the normal is pointing inward (dot product with direction to center is positive)
                // We need to flip the texture horizontally for correct text orientation
                if (Vector3D.DotProduct(normal, toCenter) > 0)
                {
                    textureBrush.RelativeTransform = new ScaleTransform(-1, 1, 0.5, 0.5);
                }
                
                // Apply opacity through the brush
                textureBrush.Opacity = cell.Opacity;
                Material faceMat = new DiffuseMaterial(textureBrush);
                
                // Create and add the 3D model for this face
                GeometryModel3D faceModel = new GeometryModel3D(faceMesh, faceMat);
                faceModel.BackMaterial = faceMat; // Make both sides visible
                
                modelGroup.Children.Add(faceModel);
            }
            
            // Add edges for better visualization - using the same base color
            AddCellEdges(modelGroup, cell, baseColor);
        }
        
        /// <summary>
        /// Creates a texture for a pentachoron cell face with different numbering systems
        /// </summary>
        private BitmapImage CreatePentachoronFaceTexture(int cellIndex, int faceIndex, Color baseColor, 
            bool isShared, int sharingCellIndex, int outputSize = 256)
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
                Color textColor = GetOptimalTextColor(baseColor);
                SolidColorBrush textBrush = new SolidColorBrush(textColor);
                
                // Get shadow color
                Color shadowColor = Color.FromArgb(128, textColor.R, textColor.G, textColor.B);
                
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
        /// Draw background texture with gradient, blotches and subtlety
        /// </summary>
        private void DrawBackgroundTexture(DrawingContext drawingContext, Color baseColor, int size)
        {
            Random random = new Random();
            
            // Create base color with slightly darker edges
            Color edgeColor = Color.FromArgb(
                baseColor.A,
                (byte)(baseColor.R * 0.8),
                (byte)(baseColor.G * 0.8),
                (byte)(baseColor.B * 0.8));
            
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
            drawingContext.PushOpacity(0.1);
            bool isDarkBackground = GetRelativeLuminance(baseColor) < 0.5;
            
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
        /// Calculate the relative luminance of a color for determining text contrast
        /// </summary>
        private double GetRelativeLuminance(Color color)
        {
            // Normalized RGB values
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            
            // Calculate luminance using the formula from WCAG 2.0
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
        
        /// <summary>
        /// Choose the optimal text color (black or white) based on background color
        /// </summary>
        private Color GetOptimalTextColor(Color backgroundColor)
        {
            // Calculate the luminance - brighter colors have higher values
            double luminance = GetRelativeLuminance(backgroundColor);
            
            // Use white text on dark backgrounds, black text on light backgrounds
            return luminance < 0.5 ? Colors.White : Colors.Black;
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
            
            return new FormattedText(
                displayText,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Times New Roman"), fontStyle, fontWeight, FontStretches.Normal),
                fontSize,
                brush,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);
        }
        
        /// <summary>
        /// Determines the text to display on each pentachoron cell face based on the cell index
        /// Each cell uses a different numbering system to distinguish them
        /// </summary>
        private string GetCellFaceText(int cellIndex, int faceIndex)
        {
            // For each of the 5 tetrahedral cells in the pentachoron, use a different numbering system
            // Each tetrahedral cell has 4 triangular faces
            switch (cellIndex)
            {
                case 0: // Use Arabic numerals 1-4
                    return (faceIndex + 1).ToString();
                
                case 1: // Use letters A-D
                    return ((char)('A' + faceIndex)).ToString();
                
                case 2: // Use Roman numerals I-IV
                    string[] romanNumerals = { "I", "II", "III", "IV" };
                    return romanNumerals[faceIndex];
                
                case 3: // Use binary numbers 01-100 (BOLD)
                    // Prefix with 'B' to indicate binary 
                    return "B" + Convert.ToString(faceIndex + 1, 2).PadLeft(2, '0');
                
                case 4: // Use dot patterns like on dice
                    return new string('●', faceIndex + 1);
                
                default:
                    return (faceIndex + 1).ToString();
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
        
        /// <summary>
        /// Add edges to the tetrahedral cell for better visualization
        /// </summary>
        private void AddCellEdges(Model3DGroup modelGroup, TetrahedralCell cell, Color baseColor)
        {
            // Define the edges of a tetrahedron (pairs of vertex indices)
            int[][] tetraEdges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 0, 2 }, new int[] { 0, 3 }, 
                new int[] { 1, 2 }, new int[] { 1, 3 }, new int[] { 2, 3 }
            };
            
            // Use darker version of the cell's color for edges
            Color edgeColor;
            if (modelGroup.Children.Count > 0 && 
                modelGroup.Children[0] is GeometryModel3D geometryModel && 
                geometryModel.Material is DiffuseMaterial diffuseMaterial &&
                diffuseMaterial.Brush is SolidColorBrush colorBrush)
            {
                // Get the current material color
                edgeColor = colorBrush.Color;
                
                // Darken it
                edgeColor = Color.FromArgb(
                    (byte)(255 * edgeOpacity * cell.Opacity), 
                    (byte)(edgeColor.R * 0.7), 
                    (byte)(edgeColor.G * 0.7), 
                    (byte)(edgeColor.B * 0.7));
            }
            else
            {
                // Default color if we can't get the material
                edgeColor = Color.FromArgb(
                    (byte)(255 * edgeOpacity * cell.Opacity),
                    100, 100, 100);
            }
            
            Material edgeMaterial = new DiffuseMaterial(new SolidColorBrush(edgeColor));
            
            // Thickness of the edges (adjust as needed)
            double edgeThickness = 0.01;
            
            // Create each edge
            foreach (int[] edge in tetraEdges)
            {
                // Get the two vertex positions
                int vertex1Index = cell.VertexIndices[edge[0]];
                int vertex2Index = cell.VertexIndices[edge[1]];
                
                Point3D p1 = projectedVertices[vertex1Index];
                Point3D p2 = projectedVertices[vertex2Index];
                
                // Create a thin cuboid representing this edge
                Vector3D edgeVector = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                double length = edgeVector.Length;
                
                // Skip very short edges
                if (length < 0.01) continue;
                
                // Create a local coordinate system for the edge
                Vector3D dir = edgeVector / length;
                Vector3D up = new Vector3D(0, 1, 0);
                
                // Handle case where dir is parallel to up
                if (Math.Abs(Vector3D.DotProduct(dir, up)) > 0.9)
                {
                    up = new Vector3D(1, 0, 0);
                }
                
                Vector3D right = Vector3D.CrossProduct(dir, up);
                right.Normalize();
                up = Vector3D.CrossProduct(right, dir);
                up.Normalize();
                
                // Create the edge as a thin cuboid
                MeshGeometry3D edgeMesh = new MeshGeometry3D();
                
                // Define the 8 vertices of the thin cuboid
                Point3D[] edgeVertices = new Point3D[8];
                
                edgeVertices[0] = p1 + (right + up) * edgeThickness;
                edgeVertices[1] = p1 + (right - up) * edgeThickness;
                edgeVertices[2] = p1 + (-right - up) * edgeThickness;
                edgeVertices[3] = p1 + (-right + up) * edgeThickness;
                
                edgeVertices[4] = p2 + (right + up) * edgeThickness;
                edgeVertices[5] = p2 + (right - up) * edgeThickness;
                edgeVertices[6] = p2 + (-right - up) * edgeThickness;
                edgeVertices[7] = p2 + (-right + up) * edgeThickness;
                
                // Add vertices to mesh
                foreach (Point3D v in edgeVertices)
                {
                    edgeMesh.Positions.Add(v);
                }
                
                // Define the 12 triangles of the cuboid (2 per face)
                int[][] edgeFaces = new int[][]
                {
                    new int[] { 0, 1, 5, 4 }, // Right face
                    new int[] { 1, 2, 6, 5 }, // Bottom face
                    new int[] { 2, 3, 7, 6 }, // Left face
                    new int[] { 3, 0, 4, 7 }, // Top face
                    new int[] { 0, 3, 2, 1 }, // Start face
                    new int[] { 4, 5, 6, 7 }  // End face
                };
                
                // Add triangles for each face
                foreach (int[] face in edgeFaces)
                {
                    edgeMesh.TriangleIndices.Add(face[0]);
                    edgeMesh.TriangleIndices.Add(face[1]);
                    edgeMesh.TriangleIndices.Add(face[2]);
                    
                    edgeMesh.TriangleIndices.Add(face[0]);
                    edgeMesh.TriangleIndices.Add(face[2]);
                    edgeMesh.TriangleIndices.Add(face[3]);
                }
                
                // Create and add the edge model
                GeometryModel3D edgeModel = new GeometryModel3D(edgeMesh, edgeMaterial);
                edgeModel.BackMaterial = edgeMaterial;
                
                modelGroup.Children.Add(edgeModel);
            }
        }
        
        /// <summary>
        /// Helper class representing a tetrahedral cell of a pentachoron
        /// </summary>
        private class TetrahedralCell
        {
            /// <summary>
            /// Indices of the vertices that form this cell
            /// </summary>
            public int[] VertexIndices { get; }
            
            /// <summary>
            /// The number/label of this cell
            /// </summary>
            public int Number { get; }
            
            /// <summary>
            /// Visibility factor of this cell (0-1, higher is more visible)
            /// </summary>
            public double Visibility { get; set; }
            
            /// <summary>
            /// Opacity of this cell for rendering (0-1, higher is more opaque)
            /// </summary>
            public double Opacity { get; set; }
            
            /// <summary>
            /// Constructor
            /// </summary>
            public TetrahedralCell(int[] vertexIndices, int number)
            {
                VertexIndices = vertexIndices;
                Number = number;
                Visibility = 0.5;
                Opacity = 1.0;
            }
        }
    }
}