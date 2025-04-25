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
    /// Represents a 24-cell (octaplex) 4D die
    /// The 24-cell has 24 octahedral cells, 96 triangular faces, 96 edges, and 24 vertices
    /// </summary>
    public class OctaplexDie : Die4D
    {
        // Projected 3D vertices after 4D rotation
        private Point3D[] projectedVertices = Array.Empty<Point3D>();
        
        // Collection of octahedral cells that make up the octaplex
        private List<OctahedralCell> cells = new List<OctahedralCell>();
        
        // Distance of the 4D viewer from the origin
        private double viewerDistance = 5.0;
        
        // Scale factor to keep the octaplex size comparable to other dice
        private const double SCALE_FACTOR = 0.4; // Smaller scale due to complexity
        
        // Edge transparency handling
        private bool useTransparency = true;
        
        public OctaplexDie(DieTextureService textureService) 
            : base(DieType.Octaplex, textureService)
        {
            // Initialize the octaplex geometry
            InitializeOctaplex();
        }
        
        /// <summary>
        /// Initialize the octaplex vertices and cells
        /// </summary>
        private void InitializeOctaplex()
        {
            // The 24-cell has 24 vertices
            // In 4D, these vertices can be arranged as the vertices of the 24-cell (octaplex)
            // with coordinates: all permutations of (±1, ±1, 0, 0) with exactly two non-zero coordinates
            
            List<Point4D> vertices = new List<Point4D>();
            
            // Generate all permutations of (±1, ±1, 0, 0) with exactly two non-zero coordinates
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (i == j) continue; // Skip duplicate axes
                    
                    // Create permutations with ±1 on axes i and j
                    for (int si = -1; si <= 1; si += 2)
                    {
                        for (int sj = -1; sj <= 1; sj += 2)
                        {
                            double[] coords = new double[4];
                            coords[i] = si * SCALE_FACTOR;
                            coords[j] = sj * SCALE_FACTOR;
                            
                            vertices.Add(new Point4D(coords[0], coords[1], coords[2], coords[3]));
                        }
                    }
                }
            }
            
            // Store the original vertices
            OriginalVertices4D = vertices.ToArray();
            
            // Initialize projected vertices array
            projectedVertices = new Point3D[OriginalVertices4D.Length];
            
            // Initialize the cells of the octaplex
            InitializeCells();
        }
        
        /// <summary>
        /// Initialize the 24 octahedral cells that make up the octaplex
        /// </summary>
        private void InitializeCells()
        {
            cells.Clear();
            
            // The 24-cell has 24 octahedral cells
            // Each octahedral cell has 8 triangular faces
            
            // We'll construct the cells by identifying the vertices that form each octahedron
            // Each octahedron is formed by 6 vertices of the 24-cell
            
            // For each vertex of the 24-cell, there is a corresponding octahedral cell
            for (int centerIdx = 0; centerIdx < OriginalVertices4D.Length; centerIdx++)
            {
                // Find the 6 vertices that form an octahedron centered at each vertex
                List<int> octaVertices = new List<int>();
                
                for (int otherIdx = 0; otherIdx < OriginalVertices4D.Length; otherIdx++)
                {
                    if (centerIdx == otherIdx) continue; // Skip self
                    
                    // Use the new Distance method we added to Point4D
                    if (Distance(OriginalVertices4D[centerIdx], OriginalVertices4D[otherIdx]) - Math.Sqrt(2) * SCALE_FACTOR < 0.001)
                    {
                        octaVertices.Add(otherIdx);
                    }
                }
                
                // Create the octahedral cell
                cells.Add(new OctahedralCell
                {
                    CenterVertexIndex = centerIdx,
                    VertexIndices = octaVertices.ToArray(),
                    Number = cells.Count + 1
                });
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
                double avgW = CurrentVertices4D[cell.CenterVertexIndex].W;
                
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
                // Scale between 0.2 (far) and 0.9 (near) for better visibility in this complex shape
                double opacity = useTransparency ? 
                    0.2 + 0.7 * (1 + cell.Visibility) / 2 : 
                    1.0;
                    
                // Since the octaplex has many cells, reduce the opacity even more
                opacity *= 0.8;
                
                // Render the octahedral cell
                RenderOctahedralCell(modelGroup, cell, originalColor, opacity);
            }
        }
        
        /// <summary>
        /// Render the octaplex in wireframe mode
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
                
                // Only render edges for cells that are more visible (closer to the viewer in 4D)
                if (cell.Visibility < -0.2) continue;
                
                // Define the octahedron's edges between vertices
                for (int i = 0; i < cell.VertexIndices.Length; i++)
                {
                    for (int j = i + 1; j < cell.VertexIndices.Length; j++)
                    {
                        int v1 = cell.VertexIndices[i];
                        int v2 = cell.VertexIndices[j];
                        
                        // Use the new Distance method we added to Point4D
                        if (Distance(CurrentVertices4D[v1], CurrentVertices4D[v2]) - SCALE_FACTOR * Math.Sqrt(2) > 0.001) continue;
                        
                        // Create a unique key for this edge (smaller index first)
                        string edgeKey = Math.Min(v1, v2) + "-" + Math.Max(v1, v2);
                        
                        // Only draw this edge if we haven't already
                        if (!drawnEdges.Contains(edgeKey))
                        {
                            drawnEdges.Add(edgeKey);
                            
                            // Draw the edge as a wireframe line
                            AddWireframeEdge(
                                modelGroup, 
                                projectedVertices[v1], 
                                projectedVertices[v2], 
                                color,
                                opacity);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Render an octahedral cell as a 3D mesh
        /// </summary>
        private void RenderOctahedralCell(Model3DGroup modelGroup, OctahedralCell cell, Color color, double opacity)
        {
            // For an octahedron, we'll create 8 triangular faces
            // The octahedron can be constructed as a dual pyramid (two pyramids joined at their bases)
            
            // Get the center vertex and the surrounding vertices
            Point3D center = projectedVertices[cell.CenterVertexIndex];
            
            // We only need to render this cell if it's somewhat visible
            if (cell.Visibility < -0.5) return;
            
            // Check if we have enough vertices to form a valid octahedron
            if (cell.VertexIndices.Length < 6)
            {
                return; // Skip this cell if it doesn't have enough vertices
            }
            
            // For simplicity, we'll split the surrounding vertices into groups to form triangular faces
            // Each triangular face consists of the current vertex and two adjacent vertices
            for (int faceIdx = 0; faceIdx < 8; faceIdx++)
            {
                // Calculate the indices of the three vertices for this face
                int v1Index = faceIdx % cell.VertexIndices.Length;
                int v2Index = (faceIdx + 1) % cell.VertexIndices.Length;
                int v3Index = (faceIdx < 4) ? 
                    (faceIdx + 2) % cell.VertexIndices.Length : 
                    (faceIdx + 5) % cell.VertexIndices.Length;
                
                int v1 = cell.VertexIndices[v1Index];
                int v2 = cell.VertexIndices[v2Index];
                int v3 = cell.VertexIndices[v3Index];
                
                // Make sure all vertex indices are valid
                if (v1 < 0 || v1 >= projectedVertices.Length ||
                    v2 < 0 || v2 >= projectedVertices.Length ||
                    v3 < 0 || v3 >= projectedVertices.Length)
                {
                    continue; // Skip this face if any vertex is out of bounds
                }
                
                // Get the 3D positions
                Point3D p1 = projectedVertices[v1];
                Point3D p2 = projectedVertices[v2];
                Point3D p3 = projectedVertices[v3];
                
                // Create a mesh for this face
                MeshGeometry3D triangleMesh = new MeshGeometry3D();
                
                // Add the vertices for this face
                triangleMesh.Positions.Add(p1);
                triangleMesh.Positions.Add(p2);
                triangleMesh.Positions.Add(p3);
                
                // Add triangle indices
                triangleMesh.TriangleIndices.Add(0);
                triangleMesh.TriangleIndices.Add(1);
                triangleMesh.TriangleIndices.Add(2);
                
                // Calculate face normal for lighting
                Vector3D edge1 = p2 - p1;
                Vector3D edge2 = p3 - p1;
                Vector3D normal = Vector3D.CrossProduct(edge1, edge2);
                normal.Normalize();
                
                // Calculate face center
                Point3D faceCenter = new Point3D(
                    (p1.X + p2.X + p3.X) / 3,
                    (p1.Y + p2.Y + p3.Y) / 3,
                    (p1.Z + p2.Z + p3.Z) / 3
                );
                
                // Direction from origin to face center
                Vector3D toCenter = new Vector3D(faceCenter.X, faceCenter.Y, faceCenter.Z);
                toCenter.Normalize();
                
                // Check if normal is pointing outward (away from center)
                bool normalPointingOutward = Vector3D.DotProduct(normal, toCenter) > 0;
                
                // Always use consistent winding order regardless of normal direction
                // This helps with texture orientation
                
                // Add normals
                triangleMesh.Normals.Add(normalPointingOutward ? normal : -normal);
                triangleMesh.Normals.Add(normalPointingOutward ? normal : -normal);
                triangleMesh.Normals.Add(normalPointingOutward ? normal : -normal);
                
                // Add texture coordinates
                triangleMesh.TextureCoordinates.Add(new Point(0, 0));
                triangleMesh.TextureCoordinates.Add(new Point(1, 0));
                triangleMesh.TextureCoordinates.Add(new Point(0.5, 1));
                
                // Create texture for this face - always using the original color
                ImageBrush faceBrush = new ImageBrush(
                    TextureService.Create4DCellTexture(
                        cell.Number - 1,       // Cell index 
                        faceIdx,               // Face index
                        color,                 // Always use the original color
                        false,                 // Not shared
                        -1,                    // No sharing cell
                        192));                 // Texture size
                
                // Fix mirrored text by applying a ScaleTransform to the brush
                faceBrush.RelativeTransform = new ScaleTransform(-1, 1, 0.5, 0.5);
                
                // Create material with the texture and preserve the original color
                Material textureMaterial = new DiffuseMaterial(faceBrush);
                
                // Set opacity for the material
                ((DiffuseMaterial)textureMaterial).Brush.Opacity = opacity;
                
                // Create and add the textured model
                GeometryModel3D texturedModel = new GeometryModel3D(triangleMesh, textureMaterial);
                texturedModel.BackMaterial = textureMaterial; // Make both sides visible
                modelGroup.Children.Add(texturedModel);
            }
        }
        
        /// <summary>
        /// Add a wireframe edge between two points
        /// </summary>
        private void AddWireframeEdge(Model3DGroup modelGroup, Point3D point1, Point3D point2, Color color, double opacity = 1.0)
        {
            // Create a simple line segment between the two points
            double thickness = 0.01; // Updated to match 3D dice thickness
            
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
        /// Calculates the Euclidean distance between two 4D points
        /// </summary>
        /// <param name="p1">First 4D point</param>
        /// <param name="p2">Second 4D point</param>
        /// <returns>The Euclidean distance between the two points</returns>
        private static double Distance(Point4D p1, Point4D p2)
        {
            return Math.Sqrt(
                Math.Pow(p1.X - p2.X, 2) +
                Math.Pow(p1.Y - p2.Y, 2) +
                Math.Pow(p1.Z - p2.Z, 2) +
                Math.Pow(p1.W - p2.W, 2)
            );
        }
    }
    
    /// <summary>
    /// Represents an octahedral cell of the octaplex
    /// </summary>
    public class OctahedralCell
    {
        // The center vertex index
        public int CenterVertexIndex { get; set; }
        
        // The vertex indices that form the octahedron
        public int[] VertexIndices { get; set; } = Array.Empty<int>();
        
        // The cell number (1-24)
        public int Number { get; set; }
        
        // The visibility of this cell in the current 4D rotation (W coordinate)
        public double Visibility { get; set; }
    }
}