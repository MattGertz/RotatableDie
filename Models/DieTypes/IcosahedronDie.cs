using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes
{
    public class IcosahedronDie : Die
    {
        public IcosahedronDie(DieTextureService textureService) 
            : base(DieType.Icosahedron, textureService)
        {
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            // Icosahedron has 20 equilateral triangular faces
            double phi = (1 + Math.Sqrt(5)) / 2; // Golden ratio
            double scale = 0.5; // Scale to a reasonable size
            
            // Define the icosahedron vertices
            Point3D[] vertices = new Point3D[12];
            
            vertices[0] = new Point3D(0, scale, phi * scale);
            vertices[1] = new Point3D(0, -scale, phi * scale);
            vertices[2] = new Point3D(0, scale, -phi * scale);
            vertices[3] = new Point3D(0, -scale, -phi * scale);
            
            vertices[4] = new Point3D(scale, phi * scale, 0);
            vertices[5] = new Point3D(-scale, phi * scale, 0);
            vertices[6] = new Point3D(scale, -phi * scale, 0);
            vertices[7] = new Point3D(-scale, -phi * scale, 0);
            
            vertices[8] = new Point3D(phi * scale, 0, scale);
            vertices[9] = new Point3D(phi * scale, 0, -scale);
            vertices[10] = new Point3D(-phi * scale, 0, scale);
            vertices[11] = new Point3D(-phi * scale, 0, -scale);
            
            // Face indices for the 20 triangular faces
            int[][] faceIndices = new int[][]
            {
                new int[] { 0, 1, 8 },    // Face 0
                new int[] { 0, 8, 4 },    // Face 1
                new int[] { 0, 4, 5 },    // Face 2
                new int[] { 0, 5, 10 },   // Face 3
                new int[] { 0, 10, 1 },   // Face 4
                
                new int[] { 1, 6, 8 },    // Face 5
                new int[] { 8, 6, 9 },    // Face 6
                new int[] { 8, 9, 4 },    // Face 7
                new int[] { 4, 9, 2 },    // Face 8
                new int[] { 4, 2, 5 },    // Face 9
                
                new int[] { 5, 2, 11 },   // Face 10
                new int[] { 5, 11, 10 },  // Face 11
                new int[] { 10, 11, 7 },  // Face 12
                new int[] { 10, 7, 1 },   // Face 13
                new int[] { 1, 7, 6 },    // Face 14
                
                new int[] { 6, 7, 3 },    // Face 15
                new int[] { 6, 3, 9 },    // Face 16
                new int[] { 9, 3, 2 },    // Face 17
                new int[] { 2, 3, 11 },   // Face 18
                new int[] { 11, 3, 7 }    // Face 19
            };
            
            if (wireframeMode)
            {
                // For wireframe mode, we'll create edges for each triangle face
                // We'll use a HashSet to avoid duplicating edges
                HashSet<string> edges = new HashSet<string>();
                
                foreach (int[] face in faceIndices)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int v1 = face[i];
                        int v2 = face[(i + 1) % 3];
                        
                        // Create a unique key for this edge (smaller index first)
                        string edgeKey = v1 < v2 ? $"{v1}-{v2}" : $"{v2}-{v1}";
                        
                        // Only add the edge if we haven't already
                        if (!edges.Contains(edgeKey))
                        {
                            edges.Add(edgeKey);
                            AddWireframeEdge(modelGroup, vertices[v1], vertices[v2], color);
                        }
                    }
                }
                
                return;
            }
            
            // Create textures for each face
            BitmapImage[] textures = new BitmapImage[20];
            
            // Define the proper face numbering with opposite pairs summing to 21
            // Note: Array indices don't have geographical meaning, they just correspond
            // to the face indices defined above
            int[] faceNumbers = new int[20];
            
            // First 10 faces show numbers 1-10
            faceNumbers[0] = 1;   // Face 0 shows number 1
            faceNumbers[1] = 2;   // Face 1 shows number 2
            faceNumbers[2] = 3;   // Face 2 shows number 3
            faceNumbers[3] = 4;   // Face 3 shows number 4
            faceNumbers[4] = 5;   // Face 4 shows number 5
            faceNumbers[5] = 6;   // Face 5 shows number 6
            faceNumbers[6] = 7;   // Face 6 shows number 7
            faceNumbers[7] = 8;   // Face 7 shows number 8
            faceNumbers[8] = 9;   // Face 8 shows number 9
            faceNumbers[9] = 10;  // Face 9 shows number 10
            
            // Second 10 faces show numbers 11-20
            // Arranged so that opposite faces sum to 21
            faceNumbers[10] = 15; // Face 10 shows number 15
            faceNumbers[11] = 14; // Face 11 shows number 14
            faceNumbers[12] = 13; // Face 12 shows number 13
            faceNumbers[13] = 12; // Face 13 shows number 12
            faceNumbers[14] = 11; // Face 14 shows number 11
            faceNumbers[15] = 18; // Face 15 shows number 18
            faceNumbers[16] = 17; // Face 16 shows number 17
            faceNumbers[17] = 16; // Face 17 shows number 16
            faceNumbers[18] = 20; // Face 18 shows number 20
            faceNumbers[19] = 19; // Face 19 shows number 19
            
            // Create textures for each face with their assigned numbers
            for (int i = 0; i < 20; i++)
            {
                textures[i] = TextureService.CreateDieTexture(faceNumbers[i], color, Type);
            }
            
            // Create the 20 triangular faces
            for (int i = 0; i < 20; i++)
            {
                // Need to reverse the face indices order to fix texture orientation for the d20
                int[] reversedIndices = new int[] { faceIndices[i][0], faceIndices[i][2], faceIndices[i][1] };
                GeometryHelper.CreateTriangleFace(modelGroup, vertices, reversedIndices, textures[i]);
            }
        }
        
        private void AddWireframeEdge(Model3DGroup modelGroup, Point3D point1, Point3D point2, Color color)
        {
            // Create a thin tube between the two points to represent an edge
            double thickness = 0.008; // Thinner lines for icosahedron due to many edges
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
