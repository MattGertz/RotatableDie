using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes
{
    public class DodecahedronDie : Die
    {
        public DodecahedronDie(DieTextureService textureService) 
            : base(DieType.Dodecahedron, textureService)
        {
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            double phi = (1 + Math.Sqrt(5)) / 2; // Golden ratio
            double scale = 0.4; // Scale to match other dice sizes
            
            // Create vertices using golden ratio-based coordinates for a regular dodecahedron
            List<Point3D> vertices = new List<Point3D>();
            
            // Add vertices based on mathematical definition
            // 8 vertices at the corners of a cube
            vertices.Add(new Point3D( 1,  1,  1)); // 0
            vertices.Add(new Point3D(-1,  1,  1)); // 1
            vertices.Add(new Point3D( 1, -1,  1)); // 2 
            vertices.Add(new Point3D(-1, -1,  1)); // 3
            vertices.Add(new Point3D( 1,  1, -1)); // 4
            vertices.Add(new Point3D(-1,  1, -1)); // 5
            vertices.Add(new Point3D( 1, -1, -1)); // 6
            vertices.Add(new Point3D(-1, -1, -1)); // 7
            
            // 12 vertices from the midpoints of the edges of an icosahedron
            vertices.Add(new Point3D(0, phi, 1/phi)); // 8
            vertices.Add(new Point3D(0, phi, -1/phi)); // 9
            vertices.Add(new Point3D(0, -phi, 1/phi)); // 10
            vertices.Add(new Point3D(0, -phi, -1/phi)); // 11
            
            vertices.Add(new Point3D(1/phi, 0, phi)); // 12
            vertices.Add(new Point3D(-1/phi, 0, phi)); // 13
            vertices.Add(new Point3D(1/phi, 0, -phi)); // 14
            vertices.Add(new Point3D(-1/phi, 0, -phi)); // 15
            
            vertices.Add(new Point3D(phi, 1/phi, 0)); // 16
            vertices.Add(new Point3D(-phi, 1/phi, 0)); // 17
            vertices.Add(new Point3D(phi, -1/phi, 0)); // 18
            vertices.Add(new Point3D(-phi, -1/phi, 0)); // 19
            
            // Scale all vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = new Point3D(
                    vertices[i].X * scale,
                    vertices[i].Y * scale,
                    vertices[i].Z * scale
                );
            }
            
            // Define the 12 pentagonal faces with consistent ordering
            // Each array defines vertices of a face in clockwise order
            int[][] faces = new int[][]
            {
                new int[] { 0, 8, 1, 13, 12 },     // Face 0
                new int[] { 0, 16, 4, 9, 8 },      // Face 1
                new int[] { 0, 12, 2, 18, 16 },    // Face 2
                new int[] { 1, 8, 9, 5, 17 },      // Face 3
                new int[] { 1, 17, 19, 3, 13 },    // Face 4
                new int[] { 2, 12, 13, 3, 10 },    // Face 5
                new int[] { 2, 10, 11, 6, 18 },    // Face 6
                new int[] { 3, 19, 7, 11, 10 },    // Face 7
                new int[] { 4, 16, 18, 6, 14 },    // Face 8
                new int[] { 4, 14, 15, 5, 9 },     // Face 9
                new int[] { 5, 15, 7, 19, 17 },    // Face 10
                new int[] { 6, 11, 7, 15, 14 }     // Face 11
            };

            if (wireframeMode)
            {
                // Create wireframe edges for each face
                foreach (int[] face in faces)
                {
                    for (int i = 0; i < face.Length; i++)
                    {
                        // Connect each vertex to the next one (loop back to first for the last one)
                        int nextIdx = (i + 1) % face.Length;
                        AddWireframeEdge(modelGroup, vertices[face[i]], vertices[face[nextIdx]], color);
                    }
                }
                
                return;
            }
            
            // First, examine which face corresponds to which number based on edges shared
            int[] faceNumbers = new int[12];

            // Face 1 is bordered by faces 2, 4, 6, 5, 3 in clockwise order
            faceNumbers[0] = 1;   // Face 0 shows number 1
            faceNumbers[1] = 2;   // Face 1 shows number 2
            faceNumbers[3] = 4;   // Face 3 shows number 4  
            faceNumbers[5] = 6;   // Face 5 shows number 6
            faceNumbers[4] = 5;   // Face 4 shows number 5
            faceNumbers[2] = 3;   // Face 2 shows number 3

            // Corrected second six face numbers with the three swaps
            faceNumbers[7] = 11;  // Face 7 shows number 11 (opposite to 2)
            faceNumbers[8] = 8;   // Face 8 shows number 8 (opposite to 5)
            faceNumbers[11] = 12; // Face 11 shows number 12 (opposite to 1)
            faceNumbers[6] = 9;   // Face 6 shows number 9 (opposite to 4)
            faceNumbers[9] = 7;   // Face 9 shows number 7 (opposite to 6)
            faceNumbers[10] = 10; // Face 10 shows number 10 (opposite to 3)

            // Create textures for each face with their assigned numbers
            BitmapImage[] numberTextures = new BitmapImage[12];
            for (int i = 0; i < 12; i++)
            {
                numberTextures[i] = TextureService.CreateDieTexture(faceNumbers[i], color, Type);
            }
            
            // Create each face with its proper texture
            for (int i = 0; i < 12; i++)
            {
                GeometryHelper.CreatePentagonFace(modelGroup, vertices.ToArray(), faces[i], numberTextures[i]);
            }
        }
        
        private void AddWireframeEdge(Model3DGroup modelGroup, Point3D point1, Point3D point2, Color color)
        {
            // Create a thin tube between the two points to represent an edge
            double thickness = 0.008; // Thinner lines for dodecahedron due to many edges
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
