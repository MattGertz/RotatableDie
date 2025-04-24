using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes
{
    public class OctahedronDie : Die
    {
        public OctahedronDie(DieTextureService textureService) 
            : base(DieType.Octahedron, textureService)
        {
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            // Octahedron has 8 equilateral triangular faces
            double size = 0.7; // Scale to a reasonable size
            
            // Define the octahedron vertices
            Point3D[] vertices = new Point3D[6];
            vertices[0] = new Point3D(0, 0, size);     // Top
            vertices[1] = new Point3D(0, 0, -size);    // Bottom
            vertices[2] = new Point3D(size, 0, 0);     // Right
            vertices[3] = new Point3D(-size, 0, 0);    // Left
            vertices[4] = new Point3D(0, size, 0);     // Front
            vertices[5] = new Point3D(0, -size, 0);    // Back
            
            if (wireframeMode)
            {
                // Create wireframe edges connecting vertices
                // Top to cardinal points
                AddWireframeEdge(modelGroup, vertices[0], vertices[2], color); // Top to Right
                AddWireframeEdge(modelGroup, vertices[0], vertices[3], color); // Top to Left
                AddWireframeEdge(modelGroup, vertices[0], vertices[4], color); // Top to Front
                AddWireframeEdge(modelGroup, vertices[0], vertices[5], color); // Top to Back
                
                // Bottom to cardinal points
                AddWireframeEdge(modelGroup, vertices[1], vertices[2], color); // Bottom to Right
                AddWireframeEdge(modelGroup, vertices[1], vertices[3], color); // Bottom to Left
                AddWireframeEdge(modelGroup, vertices[1], vertices[4], color); // Bottom to Front
                AddWireframeEdge(modelGroup, vertices[1], vertices[5], color); // Bottom to Back
                
                // Middle square
                AddWireframeEdge(modelGroup, vertices[2], vertices[4], color); // Right to Front
                AddWireframeEdge(modelGroup, vertices[4], vertices[3], color); // Front to Left
                AddWireframeEdge(modelGroup, vertices[3], vertices[5], color); // Left to Back
                AddWireframeEdge(modelGroup, vertices[5], vertices[2], color); // Back to Right
                
                return;
            }
            
            // Create textures for each face
            BitmapImage[] textures = new BitmapImage[8];
            for (int i = 0; i < 8; i++)
            {
                textures[i] = TextureService.CreateDieTexture(i + 1, color, Type);
            }
            
            // Define faces with their proper opposite pairings
            // For a face defined by 3 vertices, the opposite face will have no vertices in common
            
            // Face 1: uses vertices 0 (top), 4 (front), 2 (right)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 0, 4, 2 }, textures[0]);
            
            // Face 8: OPPOSITE to face 1, uses vertices 1 (bottom), 3 (left), 5 (back)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 1, 3, 5 }, textures[7]);
            
            // Face 2: uses vertices 0 (top), 2 (right), 5 (back)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 0, 2, 5 }, textures[1]);
            
            // Face 7: OPPOSITE to face 2, uses vertices 1 (bottom), 4 (front), 3 (left)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 1, 4, 3 }, textures[6]);
            
            // Face 3: uses vertices 0 (top), 5 (back), 3 (left)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 0, 5, 3 }, textures[2]);
            
            // Face 6: OPPOSITE to face 3, uses vertices 1 (bottom), 2 (right), 4 (front)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 1, 2, 4 }, textures[5]);
            
            // Face 4: uses vertices 0 (top), 3 (left), 4 (front)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 0, 3, 4 }, textures[3]);
            
            // Face 5: OPPOSITE to face 4, uses vertices 1 (bottom), 5 (back), 2 (right)
            GeometryHelper.CreateTriangleFace(modelGroup, vertices, new int[] { 1, 5, 2 }, textures[4]);
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
    }
}
