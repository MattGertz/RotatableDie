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
            // A pentagonal trapezohedron (d10) has 10 kite-shaped faces
            // Scale to make size comparable to other dice
            double scale = 0.65;
            
            // Set up parameters for the pentagonal trapezohedron
            // Using Golden Ratio for proportions (long bisector is 75% longer than short bisector)
            double phi = (1 + Math.Sqrt(5)) / 2; // Golden ratio for nice proportions
            double equatorialRadius = 0.85;      // Radius of the pentagonal equator
            double polarHeight = 1.2;            // Height to the poles from equator
            
            // Create vertices
            // 2 poles + 5 vertices forming a regular pentagon (not 10 as in previous version)
            Point3D[] vertices = new Point3D[7];
            
            // Poles
            vertices[0] = new Point3D(0, 0, polarHeight);         // Top pole
            vertices[1] = new Point3D(0, 0, -polarHeight);        // Bottom pole
            
            // Create a regular pentagon at the equator
            for (int i = 0; i < 5; i++)
            {
                double angle = i * (2 * Math.PI / 5); // 72-degree increments
                double x = equatorialRadius * Math.Cos(angle);
                double y = equatorialRadius * Math.Sin(angle);
                vertices[i + 2] = new Point3D(x, y, 0); // Vertices 2-6 form the pentagon
            }
            
            // Scale all vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Point3D(
                    vertices[i].X * scale,
                    vertices[i].Y * scale,
                    vertices[i].Z * scale
                );
            }
            
            // Create textures for each face with the numbering 0-9
            BitmapImage[] textures = new BitmapImage[10];
            for (int i = 0; i < 10; i++)
            {
                textures[i] = TextureService.CreateDieTexture(i, color, Type);
            }
            
            // Define the top faces (which still use even numbers 0,2,4,6,8)
            int[] topFaceNumbers = { 0, 2, 4, 6, 8 };
            
            // Define the bottom faces with corrected ordering based on empirical testing
            // Original: { 1, 3, 5, 7, 9 }
            // Corrected mappings: 
            // - Face with 5 should show 1
            // - Face with 7 should show 9
            // - Face with 9 should show 7
            // - Face with 1 should show 5
            // - Face with 3 is correct and stays as 3
            int[] bottomFaceNumbers = { 5, 3, 1, 9, 7 };  // Corrected order
            
            // Create the 10 kite-shaped faces
            // Top 5 faces - even numbers (0,2,4,6,8)
            for (int i = 0; i < 5; i++)
            {
                int nextI = (i + 1) % 5;
                
                // Top face: connects top pole with two adjacent pentagon vertices
                int[] faceIndices = { 0, i + 2, nextI + 2 };
                
                // Create top face with proper texture (even numbers)
                CreateKiteFace(modelGroup, vertices, faceIndices, textures[topFaceNumbers[i]], false);
            }
            
            // Bottom 5 faces - odd numbers (1,3,5,7,9) with corrected positions
            for (int i = 0; i < 5; i++)
            {
                int nextI = (i + 1) % 5;
                
                // Bottom face: connects bottom pole with two adjacent pentagon vertices
                // Note the order is reversed to ensure proper winding
                int[] faceIndices = { 1, nextI + 2, i + 2 };
                
                // Create bottom face with proper texture using the corrected mapping
                CreateKiteFace(modelGroup, vertices, faceIndices, textures[bottomFaceNumbers[i]], true);
            }
        }
        
        // Helper method to create a kite face with proper texture mapping
        private void CreateKiteFace(Model3DGroup modelGroup, Point3D[] allVertices, int[] faceIndices, BitmapImage texture, bool isBottomFace)
        {
            if (faceIndices.Length != 3) return;
            
            MeshGeometry3D faceMesh = new MeshGeometry3D();
            
            // Add the 3 vertices for this kite face
            for (int i = 0; i < 3; i++)
            {
                faceMesh.Positions.Add(allVertices[faceIndices[i]]);
            }
            
            // Create a triangle for the kite face
            faceMesh.TriangleIndices.Add(0); // Pole
            faceMesh.TriangleIndices.Add(1); // First equator point
            faceMesh.TriangleIndices.Add(2); // Second equator point
            
            // Moving numbers much closer to the equator (where the pyramids join)
            if (!isBottomFace)
            {
                // Top faces - moved numbers significantly lower toward the equator
                // Now positioned at 70% of the way down from the pole (vs 33% before)
                faceMesh.TextureCoordinates.Add(new Point(0.5, 0.7));   // Pole point (top) - moved much further down
                faceMesh.TextureCoordinates.Add(new Point(0.3, 0.95));  // Left equator point
                faceMesh.TextureCoordinates.Add(new Point(0.7, 0.95));  // Right equator point
                
                // Create the material with the texture
                Material material = new DiffuseMaterial(new ImageBrush(texture));
                
                // Create the model and add it to the group
                GeometryModel3D model = new GeometryModel3D(faceMesh, material);
                model.BackMaterial = material; // Make both sides visible
                modelGroup.Children.Add(model);
            }
            else
            {
                // For bottom faces, moved numbers significantly higher toward the equator
                // Now positioned at 70% of the way up from the pole (vs 33% before)
                faceMesh.TextureCoordinates.Add(new Point(0.5, 0.3));   // Pole point at bottom - moved much further up
                faceMesh.TextureCoordinates.Add(new Point(0.7, 0.05));  // Right equator point
                faceMesh.TextureCoordinates.Add(new Point(0.3, 0.05));  // Left equator point
                
                // Apply rotation to the texture for bottom faces
                ImageBrush textureBrush = new ImageBrush(texture)
                {
                    RelativeTransform = new RotateTransform
                    {
                        Angle = 180,
                        CenterX = 0.5,
                        CenterY = 0.5
                    }
                };
                
                // Create the material with the rotated texture
                Material bottomMaterial = new DiffuseMaterial(textureBrush);
                
                // Create the model and add it to the group
                GeometryModel3D bottomModel = new GeometryModel3D(faceMesh, bottomMaterial);
                bottomModel.BackMaterial = bottomMaterial; // Make both sides visible
                modelGroup.Children.Add(bottomModel);
            }
        }
    }
}