using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RotatableDie.Services
{
    /// <summary>
    /// Helper methods for creating 3D geometry for dice faces
    /// </summary>
    public static class GeometryHelper
    {
        public static void CreateTriangleFace(Model3DGroup modelGroup, Point3D[] allVertices, int[] faceIndices, BitmapImage texture)
        {
            MeshGeometry3D faceMesh = new MeshGeometry3D();
            
            // Add the 3 vertices for this triangular face
            for (int i = 0; i < 3; i++)
            {
                faceMesh.Positions.Add(allVertices[faceIndices[i]]);
            }
            
            // Mathematical approach to position the number correctly:
            // For an equilateral triangle, the centroid is at 1/3 of the height from the bottom
            // The height of an equilateral triangle with side x is (?3/2)x
            // So the centroid is at (?3/6)x from the bottom, or 1/3 of the height
            
            // In texture coordinates:
            // Top vertex at (0.5, 0.0) 
            // Bottom-left vertex at (0.0, 1.0)
            // Bottom-right vertex at (1.0, 1.0)
            faceMesh.TextureCoordinates.Add(new Point(0.5, 0.0));   // Top vertex
            faceMesh.TextureCoordinates.Add(new Point(1.0, 1.0));   // Bottom-right
            faceMesh.TextureCoordinates.Add(new Point(0.0, 1.0));   // Bottom-left
            
            // This maps the texture so that (0.5, 1/3) is the centroid of the triangle
            // The number (which is centered in the texture) will appear at the right position
            
            // Add triangle - fixed winding order
            faceMesh.TriangleIndices.Add(0);
            faceMesh.TriangleIndices.Add(2);
            faceMesh.TriangleIndices.Add(1);
            
            // Create the material with the texture
            Material material = new DiffuseMaterial(new ImageBrush(texture));
            
            // Create the model and add it to the group
            GeometryModel3D model = new GeometryModel3D(faceMesh, material);
            model.BackMaterial = material; // Make both sides visible
            modelGroup.Children.Add(model);
        }
        
        public static void CreateQuadFace(Model3DGroup modelGroup, Point3D[] allVertices, int[] faceIndices, BitmapImage texture)
        {
            MeshGeometry3D faceMesh = new MeshGeometry3D();
            
            // Add the 4 vertices for this face
            for (int i = 0; i < 4; i++)
            {
                faceMesh.Positions.Add(allVertices[faceIndices[i]]);
            }
            
            // Add texture coordinates - full texture mapping
            faceMesh.TextureCoordinates.Add(new Point(0, 1)); // Bottom-left
            faceMesh.TextureCoordinates.Add(new Point(1, 1)); // Bottom-right
            faceMesh.TextureCoordinates.Add(new Point(1, 0)); // Top-right
            faceMesh.TextureCoordinates.Add(new Point(0, 0)); // Top-left
            
            // Add triangles - Two triangles make a quad/face
            faceMesh.TriangleIndices.Add(0);
            faceMesh.TriangleIndices.Add(1);
            faceMesh.TriangleIndices.Add(2);
            
            faceMesh.TriangleIndices.Add(0);
            faceMesh.TriangleIndices.Add(2);
            faceMesh.TriangleIndices.Add(3);
            
            // Create the material with the texture
            Material material = new DiffuseMaterial(new ImageBrush(texture));
            
            // Create the model and add it to the group
            GeometryModel3D model = new GeometryModel3D(faceMesh, material);
            modelGroup.Children.Add(model);
        }
        
        public static void CreatePentagonFace(Model3DGroup modelGroup, Point3D[] allVertices, int[] faceIndices, BitmapImage texture)
        {
            // Make sure we're working with a pentagon
            if (faceIndices.Length != 5) return;
            
            MeshGeometry3D faceMesh = new MeshGeometry3D();
            
            // 1. Calculate face normal for proper orientation
            Vector3D v1 = allVertices[faceIndices[1]] - allVertices[faceIndices[0]];
            Vector3D v2 = allVertices[faceIndices[2]] - allVertices[faceIndices[0]];
            Vector3D normal = Vector3D.CrossProduct(v1, v2);
            normal.Normalize();
            
            // 2. Calculate face center
            Point3D center = new Point3D(0, 0, 0);
            foreach (int index in faceIndices) {
                center.X += allVertices[index].X;
                center.Y += allVertices[index].Y;
                center.Z += allVertices[index].Z;
            }
            center.X /= 5;
            center.Y /= 5;
            center.Z /= 5;
            
            // 3. Add the pentagon vertices to the mesh
            for (int i = 0; i < 5; i++) {
                faceMesh.Positions.Add(allVertices[faceIndices[i]]);
            }
            
            // 4. Add center point to positions for triangulation
            faceMesh.Positions.Add(center);
            
            // 5. Create a custom UV mapping that better fits a pentagon
            // Use clockwise direction to ensure correct text orientation
            for (int i = 0; i < 5; i++) {
                // Calculate angle in radians, going clockwise
                double angle = Math.PI/2 - (i * 2 * Math.PI / 5); // Start from top, go clockwise
                
                // Map to a circle inside the texture square
                double radius = 0.4; // Keep points away from the edge
                double x = 0.5 + radius * Math.Cos(angle);
                double y = 0.5 + radius * Math.Sin(angle);
                
                faceMesh.TextureCoordinates.Add(new Point(x, y));
            }
            
            // Add center texture coordinate
            faceMesh.TextureCoordinates.Add(new Point(0.5, 0.5));
            
            // 6. Create triangles - fan from center to each edge
            for (int i = 0; i < 5; i++) {
                int next = (i + 1) % 5;
                
                // Triangle winding order for proper face orientation
                faceMesh.TriangleIndices.Add(5);  // Center vertex (at index 5)
                faceMesh.TriangleIndices.Add(next); // Next vertex
                faceMesh.TriangleIndices.Add(i);  // Current vertex
            }
            
            // 7. Create material and adjust texture parameters
            ImageBrush textureBrush = new ImageBrush(texture);
            textureBrush.TileMode = TileMode.None;
            textureBrush.Stretch = Stretch.Uniform;
            textureBrush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            textureBrush.AlignmentX = AlignmentX.Center;
            textureBrush.AlignmentY = AlignmentY.Center;
            
            // Set the brush as material
            Material material = new DiffuseMaterial(textureBrush);
            
            // Create model with both sides visible
            GeometryModel3D model = new GeometryModel3D(faceMesh, material);
            model.BackMaterial = material;
            modelGroup.Children.Add(model);
        }
        
        public static void CreateTriangleFaceWithMirroredTexture(Model3DGroup modelGroup, Point3D[] allVertices, int[] faceIndices, BitmapImage texture)
        {
            MeshGeometry3D faceMesh = new MeshGeometry3D();
            
            // Add the 3 vertices for this triangular face
            for (int i = 0; i < 3; i++)
            {
                faceMesh.Positions.Add(allVertices[faceIndices[i]]);
            }
            
            // Mirrored texture coordinates to avoid text appearing backwards
            // For bottom faces, we need to apply different texture coordinates
            // to ensure the numbers are oriented correctly and the opposite faces rule is maintained
            faceMesh.TextureCoordinates.Add(new Point(0.5, 0.0));  // Top vertex
            faceMesh.TextureCoordinates.Add(new Point(0.0, 1.0));  // Bottom-right (mirrored)
            faceMesh.TextureCoordinates.Add(new Point(1.0, 1.0));  // Bottom-left (mirrored)
            
            // Add triangle with the correct winding order for bottom faces
            faceMesh.TriangleIndices.Add(0);
            faceMesh.TriangleIndices.Add(1);
            faceMesh.TriangleIndices.Add(2);
            
            // Create the material with the texture
            Material material = new DiffuseMaterial(new ImageBrush(texture));
            
            // Create the model and add it to the group
            GeometryModel3D model = new GeometryModel3D(faceMesh, material);
            model.BackMaterial = material; // Make both sides visible
            modelGroup.Children.Add(model);
        }
    }
}
