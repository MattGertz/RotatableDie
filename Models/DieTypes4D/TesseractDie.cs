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
    /// Represents a tesseract (4D hypercube) die with 8 cubic cells
    /// </summary>
    public class TesseractDie : Die4D
    {
        // Projected 3D vertices after 4D rotation
        private Point3D[] projectedVertices = Array.Empty<Point3D>();
        
        // Collection of cubic cells that make up the tesseract
        private List<CubicCell> cells = new List<CubicCell>();
        
        // Distance of the 4D viewer from the origin
        private double viewerDistance = 5.0;
        
        // Scale factor to keep the tesseract size comparable to other dice
        private const double SCALE_FACTOR = 0.4;
        
        // Edge transparency handling
        private double edgeOpacity = 1.0;
        private bool useTransparency = true;
        
        public TesseractDie(DieTextureService textureService) 
            : base(DieType.Tesseract, textureService)
        {
            // Initialize the tesseract geometry
            InitializeTesseract();
        }
        
        /// <summary>
        /// Initialize the tesseract vertices and cells
        /// </summary>
        private void InitializeTesseract()
        {
            // A tesseract has 16 vertices, with coordinates (±1, ±1, ±1, ±1)
            OriginalVertices4D = new Point4D[16];
            int index = 0;
            
            // Generate all combinations of ±1 for each coordinate
            for (int w = -1; w <= 1; w += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int x = -1; x <= 1; x += 2)
                        {
                            OriginalVertices4D[index++] = new Point4D(
                                x * SCALE_FACTOR, 
                                y * SCALE_FACTOR, 
                                z * SCALE_FACTOR, 
                                w * SCALE_FACTOR);
                        }
                    }
                }
            }
            
            // Initialize projected vertices array
            projectedVertices = new Point3D[16];
            
            // Initialize the cells of the tesseract (8 cubes)
            InitializeCells();
        }
        
        /// <summary>
        /// Initialize the 8 cubic cells that make up the tesseract
        /// </summary>
        private void InitializeCells()
        {
            cells = new List<CubicCell>();
            
            // Define the 8 cubic cells of the tesseract
            // Each cell is defined by the indices of its 8 vertices
            
            // Cell 0 (w = -1)
            cells.Add(new CubicCell(
                new int[] { 0, 1, 2, 3, 4, 5, 6, 7 },
                0)); // Cell number 0
                
            // Cell 1 (w = 1)
            cells.Add(new CubicCell(
                new int[] { 8, 9, 10, 11, 12, 13, 14, 15 },
                1)); // Cell number 1
                
            // Cell 2 (x = -1)
            cells.Add(new CubicCell(
                new int[] { 0, 2, 4, 6, 8, 10, 12, 14 },
                2)); // Cell number 2
                
            // Cell 3 (x = 1)
            cells.Add(new CubicCell(
                new int[] { 1, 3, 5, 7, 9, 11, 13, 15 },
                3)); // Cell number 3
                
            // Cell 4 (y = -1)
            cells.Add(new CubicCell(
                new int[] { 0, 1, 4, 5, 8, 9, 12, 13 },
                4)); // Cell number 4
                
            // Cell 5 (y = 1)
            cells.Add(new CubicCell(
                new int[] { 2, 3, 6, 7, 10, 11, 14, 15 },
                5)); // Cell number 5
                
            // Cell 6 (z = -1)
            cells.Add(new CubicCell(
                new int[] { 0, 1, 2, 3, 8, 9, 10, 11 },
                6)); // Cell number 6
                
            // Cell 7 (z = 1)
            cells.Add(new CubicCell(
                new int[] { 4, 5, 6, 7, 12, 13, 14, 15 },
                7)); // Cell number 7
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
                    RenderCubicCell(modelGroup, cell, originalColor);
                }
            }
        }
        
        /// <summary>
        /// Render the tesseract in wireframe mode
        /// </summary>
        private void RenderWireframe(Model3DGroup modelGroup, Color color)
        {
            // Define all edges in the tesseract
            HashSet<string> renderedEdges = new HashSet<string>();
            
            // 1. First add the edges of each cubic cell
            foreach (var cell in cells)
            {
                // Only render cells with some visibility
                if (cell.Visibility > 0.05)
                {
                    // Define the edges of a cube (pairs of vertex indices)
                    int[][] cubeEdges = new int[][]
                    {
                        new int[] { 0, 1 }, new int[] { 1, 3 }, new int[] { 3, 2 }, new int[] { 2, 0 }, // Front face
                        new int[] { 4, 5 }, new int[] { 5, 7 }, new int[] { 7, 6 }, new int[] { 6, 4 }, // Back face
                        new int[] { 0, 4 }, new int[] { 1, 5 }, new int[] { 2, 6 }, new int[] { 3, 7 }  // Connecting edges
                    };
                    
                    // Add each edge
                    foreach (int[] edge in cubeEdges)
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
                            
                            // Always use full opacity for wireframe mode to match 3D dice visibility
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
                
                avgW /= 8; // Divide by number of vertices per cell (8 for cube)
                
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
        /// Render a cubic cell of the tesseract
        /// </summary>
        private void RenderCubicCell(Model3DGroup modelGroup, CubicCell cell, Color baseColor)
        {
            // Apply transparency based on cell visibility
            double opacity = cell.Opacity;
            
            // Define the faces of a cube (groups of 4 vertices)
            int[][] cubeFaces = new int[][]
            {
                new int[] { 0, 1, 3, 2 }, // Front face
                new int[] { 4, 6, 7, 5 }, // Back face
                new int[] { 0, 4, 5, 1 }, // Bottom face
                new int[] { 2, 3, 7, 6 }, // Top face
                new int[] { 0, 2, 6, 4 }, // Left face
                new int[] { 1, 5, 7, 3 }  // Right face
            };
            
            // Create each face of this cell
            for (int faceIndex = 0; faceIndex < cubeFaces.Length; faceIndex++)
            {
                MeshGeometry3D faceMesh = new MeshGeometry3D();
                
                // Add the 4 vertices for this face
                foreach (int vertexOffset in cubeFaces[faceIndex])
                {
                    int vertexIndex = cell.VertexIndices[vertexOffset];
                    faceMesh.Positions.Add(projectedVertices[vertexIndex]);
                }
                
                // Add triangulation indices (divide quad into two triangles)
                faceMesh.TriangleIndices.Add(0);
                faceMesh.TriangleIndices.Add(1);
                faceMesh.TriangleIndices.Add(2);
                
                faceMesh.TriangleIndices.Add(0);
                faceMesh.TriangleIndices.Add(2);
                faceMesh.TriangleIndices.Add(3);
                
                // Add texture coordinates
                faceMesh.TextureCoordinates.Add(new Point(0, 0));
                faceMesh.TextureCoordinates.Add(new Point(1, 0));
                faceMesh.TextureCoordinates.Add(new Point(1, 1));
                faceMesh.TextureCoordinates.Add(new Point(0, 1));
                
                // Check if this face is shared with another cell
                bool isShared = false;
                int sharingCellIndex = -1;
                
                // Determine if this is a shared face by checking other cells
                foreach (var otherCell in cells)
                {
                    // Skip the current cell
                    if (otherCell.Number == cell.Number)
                        continue;
                    
                    // Check if the face vertices are all shared with the other cell
                    bool allVerticesShared = true;
                    foreach (int vertexOffset in cubeFaces[faceIndex])
                    {
                        int vertexIndex = cell.VertexIndices[vertexOffset];
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
                
                // Create texture for this face using the specialized method
                // Always use the passed in baseColor to maintain color during 4D rotation
                BitmapImage texture = TextureService.Create4DCellTexture(
                    cell.Number,         // Cell index for numbering system
                    faceIndex,           // Face number
                    baseColor,           // Original base color selected by user - use as is
                    isShared,            // Whether this face is shared
                    sharingCellIndex);   // Index of the sharing cell, if any
                
                // Create material with the texture - apply transparency here
                ImageBrush textureBrush = new ImageBrush(texture);
                
                // Fix the mirrored text by applying a ScaleTransform
                
                // Detect face orientation - we need to calculate if a face is "inward" or "outward" facing
                // by examining the face's normal vector relative to the center
                
                // Calculate face center
                Point3D faceCenter = new Point3D(
                    (faceMesh.Positions[0].X + faceMesh.Positions[1].X + faceMesh.Positions[2].X + faceMesh.Positions[3].X) / 4,
                    (faceMesh.Positions[0].Y + faceMesh.Positions[1].Y + faceMesh.Positions[2].Y + faceMesh.Positions[3].Y) / 4,
                    (faceMesh.Positions[0].Z + faceMesh.Positions[1].Z + faceMesh.Positions[2].Z + faceMesh.Positions[3].Z) / 4
                );
                
                // Calculate face normal vector
                Vector3D edge1 = faceMesh.Positions[1] - faceMesh.Positions[0];
                Vector3D edge2 = faceMesh.Positions[3] - faceMesh.Positions[0];
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
                
                // Apply opacity through the brush instead of the material
                textureBrush.Opacity = opacity;
                Material faceMat = new DiffuseMaterial(textureBrush);
                
                // Create and add the 3D model for this face
                GeometryModel3D faceModel = new GeometryModel3D(faceMesh, faceMat);
                faceModel.BackMaterial = faceMat; // Make both sides visible
                
                modelGroup.Children.Add(faceModel);
            }
            
            // Add edges for better visualization - always use original color for edges
            AddCellEdges(modelGroup, cell, baseColor);
        }
        
        /// <summary>
        /// Add edges to the cubic cell for better visualization
        /// </summary>
        private void AddCellEdges(Model3DGroup modelGroup, CubicCell cell, Color baseColor)
        {
            // Define the edges of a cube (pairs of vertex indices)
            int[][] cubeEdges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 1, 3 }, new int[] { 3, 2 }, new int[] { 2, 0 }, // Front face
                new int[] { 4, 5 }, new int[] { 5, 7 }, new int[] { 7, 6 }, new int[] { 6, 4 }, // Back face
                new int[] { 0, 4 }, new int[] { 1, 5 }, new int[] { 2, 6 }, new int[] { 3, 7 }  // Connecting edges
            };
            
            // Adjust edge color based on the base cell color but darker
            Color edgeColor = Color.FromArgb(
                (byte)(255 * edgeOpacity * cell.Opacity), 
                (byte)(baseColor.R * 0.7), 
                (byte)(baseColor.G * 0.7), 
                (byte)(baseColor.B * 0.7));
            
            Material edgeMaterial = new DiffuseMaterial(new SolidColorBrush(edgeColor));
            
            // Thickness of the edges (adjust as needed)
            double edgeThickness = 0.02;
            
            // Create each edge
            foreach (int[] edge in cubeEdges)
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
        /// Helper class representing a cubic cell of a tesseract
        /// </summary>
        private class CubicCell
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
            public CubicCell(int[] vertexIndices, int number)
            {
                VertexIndices = vertexIndices;
                Number = number;
                Visibility = 0.5;
                Opacity = 1.0;
            }
        }
    }
}