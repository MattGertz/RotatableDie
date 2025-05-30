using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes4D
{
    /// <summary>
    /// Represents a hexadecachoron (4D 16-cell) die with 16 tetrahedral cells
    /// </summary>
    public class HexadecachoronDie : Die4D
    {
        // Projected 3D vertices after 4D rotation
        private Point3D[] projectedVertices = Array.Empty<Point3D>();
        
        // Collection of tetrahedral cells that make up the hexadecachoron
        private List<TetrahedralCell> cells = new List<TetrahedralCell>();
        
        // Distance of the 4D viewer from the origin
        private double viewerDistance = 5.0;
        
        // Scale factor to keep the hexadecachoron size comparable to other dice
        private const double SCALE_FACTOR = 0.6;
        
        // Edge transparency handling
        private double edgeOpacity = 1.0;
        private bool useTransparency = true;
        
        public HexadecachoronDie(DieTextureService textureService) 
            : base(DieType.Hexadecachoron, textureService)
        {
            // Initialize the hexadecachoron geometry
            InitializeHexadecachoron();
        }
        
        /// <summary>
        /// Initialize the hexadecachoron vertices and cells
        /// </summary>
        private void InitializeHexadecachoron()
        {
            // The hexadecachoron has 8 vertices at the 4D unit coordinate axes
            OriginalVertices4D = new Point4D[8];
            
            // Create vertices at +/- unit positions on each axis
            // (+1,0,0,0), (-1,0,0,0), (0,+1,0,0), (0,-1,0,0), (0,0,+1,0), (0,0,-1,0), (0,0,0,+1), (0,0,0,-1)
            int index = 0;
            
            // X axis
            OriginalVertices4D[index++] = new Point4D(SCALE_FACTOR, 0, 0, 0);
            OriginalVertices4D[index++] = new Point4D(-SCALE_FACTOR, 0, 0, 0);
            
            // Y axis
            OriginalVertices4D[index++] = new Point4D(0, SCALE_FACTOR, 0, 0);
            OriginalVertices4D[index++] = new Point4D(0, -SCALE_FACTOR, 0, 0);
            
            // Z axis
            OriginalVertices4D[index++] = new Point4D(0, 0, SCALE_FACTOR, 0);
            OriginalVertices4D[index++] = new Point4D(0, 0, -SCALE_FACTOR, 0);
            
            // W axis
            OriginalVertices4D[index++] = new Point4D(0, 0, 0, SCALE_FACTOR);
            OriginalVertices4D[index++] = new Point4D(0, 0, 0, -SCALE_FACTOR);
            
            // Initialize projected vertices array
            projectedVertices = new Point3D[8];
            
            // Initialize the cells of the hexadecachoron (16 tetrahedra)
            InitializeCells();
        }
        
        /// <summary>
        /// Initialize the 16 tetrahedral cells that make up the hexadecachoron
        /// </summary>
        private void InitializeCells()
        {
            cells.Clear();
            
            // The hexadecachoron has 16 tetrahedral cells
            // Each cell is formed by selecting one vertex from each adjacent pair of opposite vertices
            
            // Note: Each tetrahedral cell has 4 faces (0-3), but face 3 is always pointed toward
            // the interior of the 16-cell and thus never visible from outside the polytope.
            // This is why users only ever see faces 0, 1, and 2 (numbered 1, 2, and 3).
            
            // Create all possible combinations of selecting one vertex from each opposite pair
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        for (int w = 0; w < 2; w++)
                        {
                            // Create a tetrahedral cell using these vertices
                            int[] vertexIndices = new int[4];
                            vertexIndices[0] = x; // Select from the first opposite pair (0,1)
                            vertexIndices[1] = y + 2; // Select from the second opposite pair (2,3)
                            vertexIndices[2] = z + 4; // Select from the third opposite pair (4,5)
                            vertexIndices[3] = w + 6; // Select from the fourth opposite pair (6,7)
                            
                            // Create the cell
                            cells.Add(new TetrahedralCell
                            {
                                VertexIndices = vertexIndices,
                                Number = cells.Count + 1
                            });
                        }
                    }
                }
            }
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
        
        /// <summary>
        /// Calculate visibility of each cell based on the W coordinate
        /// </summary>
        private void CalculateCellVisibility()
        {
            foreach (var cell in cells)
            {
                // Calculate the average W coordinate for this cell
                double avgW = 0;
                foreach (int vertexIndex in cell.VertexIndices)
                {
                    avgW += CurrentVertices4D[vertexIndex].W;
                }
                avgW /= cell.VertexIndices.Length;
                
                // Set the visibility based on the W coordinate
                // Cells with higher W values are more visible
                cell.Visibility = avgW;
            }
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
                // Calculate opacity based on the cell's position in W dimension
                // Scale between 0.3 (far) and 1.0 (near)
                double opacity = useTransparency ? 
                    0.3 + 0.7 * (1 + cell.Visibility) / 2 : 
                    1.0;
                    
                // Create a material with the appropriate transparency
                var cellMaterial = new DiffuseMaterial(
                    new SolidColorBrush(
                        Color.FromArgb(
                            (byte)(opacity * 255), 
                            originalColor.R, 
                            originalColor.G, 
                            originalColor.B)));
                
                // Render the tetrahedral cell
                RenderTetrahedralCell(modelGroup, cell, originalColor);
            }
        }
        
        /// <summary>
        /// Render the hexadecachoron in wireframe mode
        /// </summary>
        private void RenderWireframe(Model3DGroup modelGroup, Color color)
        {
            // Dictionary to track which edges we've already drawn
            HashSet<string> drawnEdges = new HashSet<string>();
            
            // For each cell
            foreach (var cell in cells)
            {
                // Always use full opacity for wireframe mode
                double opacity = 1.0;
                    
                // Define the edges of a tetrahedron as pairs of vertex indices
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
                    string edgeKey = Math.Min(vertex1Index, vertex2Index) + "-" + Math.Max(vertex1Index, vertex2Index);
                    
                    // Only draw this edge if we haven't already
                    if (!drawnEdges.Contains(edgeKey))
                    {
                        drawnEdges.Add(edgeKey);
                        
                        // Draw the edge as a wireframe line
                        AddWireframeEdge(
                            modelGroup, 
                            projectedVertices[vertex1Index], 
                            projectedVertices[vertex2Index], 
                            color,
                            opacity);
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
        /// Render a tetrahedral cell as a 3D mesh
        /// </summary>
        private void RenderTetrahedralCell(Model3DGroup modelGroup, TetrahedralCell cell, Color color)
        {
            // Get the indices of the tetrahedron vertices
            int[] indices = cell.VertexIndices;
            
            // Get the positions of the vertices in 3D
            Point3D[] vertices = new Point3D[4]
            {
                projectedVertices[indices[0]],
                projectedVertices[indices[1]],
                projectedVertices[indices[2]],
                projectedVertices[indices[3]]
            };
            
            // Calculate opacity based on the cell's position in W dimension
            double opacity = useTransparency ? 
                0.3 + 0.7 * (1 + cell.Visibility) / 2 : 
                1.0;
            
            // For each triangular face
            for (int faceIdx = 0; faceIdx < 4; faceIdx++)
            {
                // Define the triangular faces of the tetrahedron
                int[][] triangleFaces = new int[][]
                {
                    new int[] { 0, 1, 2 },
                    new int[] { 0, 1, 3 },
                    new int[] { 0, 2, 3 },
                    new int[] { 1, 2, 3 }
                };
                
                // Create a mesh for this face
                MeshGeometry3D triangleMesh = new MeshGeometry3D();
                
                // Add the vertices for this face
                triangleMesh.Positions.Add(vertices[triangleFaces[faceIdx][0]]);
                triangleMesh.Positions.Add(vertices[triangleFaces[faceIdx][1]]);
                triangleMesh.Positions.Add(vertices[triangleFaces[faceIdx][2]]);
                
                // Calculate face normal for correct orientation
                Vector3D v1 = vertices[triangleFaces[faceIdx][1]] - vertices[triangleFaces[faceIdx][0]];
                Vector3D v2 = vertices[triangleFaces[faceIdx][2]] - vertices[triangleFaces[faceIdx][0]];
                Vector3D normal = Vector3D.CrossProduct(v1, v2);
                normal.Normalize();
                
                // Calculate face center
                Point3D faceCenter = new Point3D(
                    (vertices[triangleFaces[faceIdx][0]].X + vertices[triangleFaces[faceIdx][1]].X + vertices[triangleFaces[faceIdx][2]].X) / 3,
                    (vertices[triangleFaces[faceIdx][0]].Y + vertices[triangleFaces[faceIdx][1]].Y + vertices[triangleFaces[faceIdx][2]].Y) / 3,
                    (vertices[triangleFaces[faceIdx][0]].Z + vertices[triangleFaces[faceIdx][1]].Z + vertices[triangleFaces[faceIdx][2]].Z) / 3
                );
                
                // Direction from origin to face center
                Vector3D toCenter = new Vector3D(faceCenter.X, faceCenter.Y, faceCenter.Z);
                toCenter.Normalize();
                
                // Check if normal is pointing outward (away from center)
                // For tetrahedra in a 16-cell, we want normals pointing outward
                bool normalPointingOutward = Vector3D.DotProduct(normal, toCenter) > 0;
                
                // Force all texture coordinates to be in the same orientation regardless of normal direction
                // This empirical fix ensures numbers always appear correctly oriented
                triangleMesh.TriangleIndices.Add(0);
                triangleMesh.TriangleIndices.Add(1);
                triangleMesh.TriangleIndices.Add(2);
                
                // Always use the same texture coordinate orientation to fix backwards numbers
                triangleMesh.TextureCoordinates.Add(new Point(0, 0));
                triangleMesh.TextureCoordinates.Add(new Point(1, 0));
                triangleMesh.TextureCoordinates.Add(new Point(0.5, 1));
                
                // Add normal for each vertex - still respecting the actual normal direction
                triangleMesh.Normals.Add(normalPointingOutward ? normal : -normal);
                triangleMesh.Normals.Add(normalPointingOutward ? normal : -normal);
                triangleMesh.Normals.Add(normalPointingOutward ? normal : -normal);
                
                // Create texture for this face - always using the original color
                ImageBrush faceBrush = new ImageBrush(
                    TextureService.Create4DCellTexture(
                        cell.Number - 1,       // Cell index 
                        faceIdx,               // Face index
                        color,                 // Original base color
                        false,                 // Not shared
                        -1,                    // No sharing cell
                        192));                 // Texture size
                
                // Fix the mirrored text by applying a ScaleTransform to the brush
                // This flips the texture horizontally to correct the mirroring issue
                faceBrush.RelativeTransform = new ScaleTransform(-1, 1, 0.5, 0.5);
                
                // Create material with the texture
                Material textureMaterial = new DiffuseMaterial(faceBrush);
                
                // Set opacity for the material
                if (opacity < 1.0)
                {
                    ((DiffuseMaterial)textureMaterial).Brush.Opacity = opacity;
                }
                
                // Create and add the textured model
                GeometryModel3D texturedModel = new GeometryModel3D(triangleMesh, textureMaterial);
                texturedModel.BackMaterial = textureMaterial; // Make both sides visible
                modelGroup.Children.Add(texturedModel);
                
                // Add the wireframe edges for this face for better visibility
                if (useTransparency)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int j = (i + 1) % 3;
                        AddWireframeEdge(
                            modelGroup,
                            vertices[triangleFaces[faceIdx][i]],
                            vertices[triangleFaces[faceIdx][j]],
                            color, // Use the original color for wireframe
                            opacity * 0.7);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add edges to a cell for better visualization
        /// </summary>
        private void AddCellEdges(Model3DGroup modelGroup, TetrahedralCell cell)
        {
            // Define the edges of a tetrahedron as pairs of vertex indices
            int[][] tetraEdges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 0, 2 }, new int[] { 0, 3 },
                new int[] { 1, 2 }, new int[] { 1, 3 }, new int[] { 2, 3 }
            };
            
            // Calculate opacity based on the cell's position in W dimension
            double opacity = useTransparency ? 
                edgeOpacity * (0.3 + 0.7 * (1 + cell.Visibility) / 2) : 
                edgeOpacity;
            
            // Add each edge
            foreach (int[] edge in tetraEdges)
            {
                // Get the two vertex positions
                int vertex1Index = cell.VertexIndices[edge[0]];
                int vertex2Index = cell.VertexIndices[edge[1]];
                
                // Draw the edge as a wireframe line
                AddWireframeEdge(
                    modelGroup, 
                    projectedVertices[vertex1Index], 
                    projectedVertices[vertex2Index], 
                    Colors.Black,
                    opacity);
            }
        }
    }
    
    /// <summary>
    /// Represents a tetrahedral cell of the hexadecachoron
    /// </summary>
    public class TetrahedralCell
    {
        // The vertex indices that make up this tetrahedral cell
        public int[] VertexIndices { get; set; } = Array.Empty<int>();
        
        // The cell number (1-16)
        public int Number { get; set; }
        
        // The visibility of this cell in the current 4D rotation (W coordinate)
        public double Visibility { get; set; }
    }
}