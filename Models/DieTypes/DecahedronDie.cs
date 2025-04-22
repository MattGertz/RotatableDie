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
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color)
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
                Material material = new DiffuseMaterial(new ImageBrush(textures[topFaceNumbers[i]]));
                
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
                Material material = new DiffuseMaterial(textureBrush);
                
                // Create the model
                GeometryModel3D model = new GeometryModel3D(kiteMesh, material);
                model.BackMaterial = material;
                modelGroup.Children.Add(model);
            }
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