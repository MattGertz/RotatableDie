using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes
{
    /// <summary>
    /// Represents a ten-sided die (d10) as a pentagonal trapezohedron with kite-shaped faces.
    /// </summary>
    public class DecahedronDie : Die
    {
        public DecahedronDie(DieTextureService textureService) 
            : base(DieType.Decahedron, textureService)
        {
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            // Scale to make size comparable to other dice
            double scale = 0.6;
            
            // A pentagonal trapezohedron has two poles (top and bottom)
            // and 10 vertices arranged to form kite-shaped faces
            
            // Define the poles
            Point3D topPole = new Point3D(0, 0, 1.0);           // Top pole
            Point3D bottomPole = new Point3D(0, 0, -1.0);       // Bottom pole
            
            // Define the offset for vertices above and below the equator
            double equatorialOffset = 0.15;  // Amount vertices are offset from the z=0 plane
            
            // Create the vertices for the top pentagon (slightly above the equator)
            Point3D[] topPentagonVertices = new Point3D[5];
            for (int i = 0; i < 5; i++)
            {
                double angle = i * (2 * Math.PI / 5); // 72-degree increments
                topPentagonVertices[i] = new Point3D(
                    Math.Cos(angle) * 0.85,
                    Math.Sin(angle) * 0.85,
                    equatorialOffset);
            }
            
            // Create the vertices for the bottom pentagon (slightly below the equator)
            // Offset by 36 degrees (π/5 radians) from the top pentagon
            Point3D[] bottomPentagonVertices = new Point3D[5];
            for (int i = 0; i < 5; i++)
            {
                double angle = i * (2 * Math.PI / 5) + Math.PI / 5; // 72-degree increments + 36-degree offset
                bottomPentagonVertices[i] = new Point3D(
                    Math.Cos(angle) * 0.85,
                    Math.Sin(angle) * 0.85,
                    -equatorialOffset);
            }
            
            // Scale all vertices
            topPole = ScalePoint3D(topPole, scale);
            bottomPole = ScalePoint3D(bottomPole, scale);
            
            for (int i = 0; i < 5; i++)
            {
                topPentagonVertices[i] = ScalePoint3D(topPentagonVertices[i], scale);
                bottomPentagonVertices[i] = ScalePoint3D(bottomPentagonVertices[i], scale);
            }
            
            // Create materials
            Material material;
            Material backMaterial;
            
            if (wireframeMode)
            {
                // For wireframe mode, use transparent material with black edges
                material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)));
                backMaterial = material;
            }
            else
            {
                // Create textures for each face with the numbering 0-9
                BitmapImage[] textures = new BitmapImage[10];
                for (int i = 0; i < 10; i++)
                {
                    textures[i] = TextureService.CreateDieTexture(i, color, Type);
                }
                
                // Define face numbering with corrected oppositions
                int[] topFaceNumbers = { 0, 2, 4, 6, 8 };     // Top faces (even numbers)
                int[] bottomFaceNumbers = { 5, 3, 1, 9, 7 };  // Bottom faces (odd numbers)
                
                // Create the 5 top kite faces with even numbers (0,2,4,6,8)
                for (int i = 0; i < 5; i++)
                {
                    // For top kites: connect top pole to two adjacent vertices from top pentagon
                    // and the corresponding vertex from bottom pentagon
                    MeshGeometry3D kiteMesh = new MeshGeometry3D();
                    
                    int nextIndex = (i + 1) % 5;
                    
                    // Add the 4 vertices that form the kite
                    kiteMesh.Positions.Add(topPole);                      // Top pole
                    kiteMesh.Positions.Add(topPentagonVertices[i]);       // Left vertex (above equator)
                    kiteMesh.Positions.Add(bottomPentagonVertices[i]);    // Bottom vertex (below equator)
                    kiteMesh.Positions.Add(topPentagonVertices[nextIndex]);// Right vertex (above equator)
                    
                    // Define triangles to form the kite face
                    kiteMesh.TriangleIndices.Add(0);  // Top pole
                    kiteMesh.TriangleIndices.Add(1);  // Left vertex
                    kiteMesh.TriangleIndices.Add(3);  // Right vertex
                    
                    kiteMesh.TriangleIndices.Add(1);  // Left vertex
                    kiteMesh.TriangleIndices.Add(2);  // Bottom vertex
                    kiteMesh.TriangleIndices.Add(3);  // Right vertex
                    
                    // Texture mapping for proper number orientation 
                    // Moving the base of numbers to same height as left/right vertices
                    kiteMesh.TextureCoordinates.Add(new Point(0.5, 0.05));  // Top pole (near top)
                    kiteMesh.TextureCoordinates.Add(new Point(0.15, 0.60)); // Left vertex
                    kiteMesh.TextureCoordinates.Add(new Point(0.5, 0.85));  // Bottom vertex
                    kiteMesh.TextureCoordinates.Add(new Point(0.85, 0.60)); // Right vertex
                    
                    // Create the material with the texture
                    material = new DiffuseMaterial(new ImageBrush(textures[topFaceNumbers[i]]));
                    
                    // Create the model
                    GeometryModel3D model = new GeometryModel3D(kiteMesh, material);
                    model.BackMaterial = material;
                    modelGroup.Children.Add(model);
                }
                
                // Create the 5 bottom kite faces with odd numbers (1,3,5,7,9) - COMPLETELY REVISED APPROACH
                for (int i = 0; i < 5; i++)
                {
                    MeshGeometry3D kiteMesh = new MeshGeometry3D();
                    
                    int nextIndex = (i + 1) % 5;
                    // FIX: Use (i+1)%5 for the matching top index as suggested
                    int matchingTopIndex = (i + 1) % 5;
                    
                    // Add the vertices for the kite in the correct order
                    kiteMesh.Positions.Add(bottomPole);                      // Bottom pole
                    kiteMesh.Positions.Add(bottomPentagonVertices[i]);       // Left vertex (below equator)
                    kiteMesh.Positions.Add(topPentagonVertices[matchingTopIndex]); // Top vertex (above equator) - FIXED INDEX
                    kiteMesh.Positions.Add(bottomPentagonVertices[nextIndex]);// Right vertex (below equator)
                    
                    // Define triangles with correct winding order
                    kiteMesh.TriangleIndices.Add(0);  // Bottom pole
                    kiteMesh.TriangleIndices.Add(3);  // Right vertex
                    kiteMesh.TriangleIndices.Add(1);  // Left vertex
                    
                    kiteMesh.TriangleIndices.Add(1);  // Left vertex
                    kiteMesh.TriangleIndices.Add(3);  // Right vertex
                    kiteMesh.TriangleIndices.Add(2);  // Top vertex
                    
                    // Texture mapping
                    kiteMesh.TextureCoordinates.Add(new Point(0.5, 0.95));  // Bottom pole
                    kiteMesh.TextureCoordinates.Add(new Point(0.15, 0.40)); // Left vertex
                    kiteMesh.TextureCoordinates.Add(new Point(0.5, 0.15));  // Top vertex
                    kiteMesh.TextureCoordinates.Add(new Point(0.85, 0.40)); // Right vertex
                    
                    // Apply rotation to the texture for bottom faces
                    ImageBrush textureBrush = new ImageBrush(textures[bottomFaceNumbers[i]])
                    {
                        RelativeTransform = new RotateTransform
                        {
                            Angle = 180,
                            CenterX = 0.5,
                            CenterY = 0.5
                        }
                    };
                    
                    // Create the material with the rotated texture
                    material = new DiffuseMaterial(textureBrush);
                    
                    // Create the model
                    GeometryModel3D model = new GeometryModel3D(kiteMesh, material);
                    model.BackMaterial = material;
                    modelGroup.Children.Add(model);
                }
                
                return;
            }
            
            // If wireframe mode is enabled, create wireframe edges instead of textured faces
            
            // Create edge lines for the top kites
            for (int i = 0; i < 5; i++)
            {
                int nextIndex = (i + 1) % 5;
                
                // Add the edges from pole to vertices
                AddWireframeEdge(modelGroup, topPole, topPentagonVertices[i], color);
                
                // We're NOT adding the edges along the top pentagon to avoid connecting L and R
                // REMOVING THIS LINE to eliminate the line between L and R in each kite
                // AddWireframeEdge(modelGroup, topPentagonVertices[i], topPentagonVertices[nextIndex], color);
                
                // Add edges connecting top pentagon to bottom pentagon
                AddWireframeEdge(modelGroup, topPentagonVertices[i], bottomPentagonVertices[i], color);
                
                // We're NOT adding the edges along the bottom pentagon to avoid connecting L and R in bottom pyramid
                // REMOVING THIS LINE to eliminate the line between L and R in each bottom kite
                // AddWireframeEdge(modelGroup, bottomPentagonVertices[i], bottomPentagonVertices[nextIndex], color);
                
                // Add edges from bottom pole to bottom pentagon vertices
                AddWireframeEdge(modelGroup, bottomPole, bottomPentagonVertices[i], color);
                
                // Add diagonal edge connecting top pentagon to bottom pentagon
                // This diagonal edge completes each kite shape without adding internal lines
                AddWireframeEdge(modelGroup, topPentagonVertices[nextIndex], bottomPentagonVertices[i], color);
            }
        }
        
        private void AddWireframeEdge(Model3DGroup modelGroup, Point3D point1, Point3D point2, Color color)
        {
            // Create a thin tube between the two points to represent an edge
            double thickness = 0.01; // Updated to match standard thickness used in 3D dice
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
            
            // Create material for the edge (use solid black for visibility)
            Material edgeMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Black));
            
            // Create the model and add it to the group
            GeometryModel3D model = new GeometryModel3D();
            model.Geometry = edgeMesh;
            model.Material = edgeMaterial;
            model.BackMaterial = edgeMaterial;
            model.Transform = transformGroup;
            
            modelGroup.Children.Add(model);
        }
        
        private Point3D ScalePoint3D(Point3D point, double scale)
        {
            return new Point3D(
                point.X * scale,
                point.Y * scale,
                point.Z * scale
            );
        }
    }
}