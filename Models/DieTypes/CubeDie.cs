using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes
{
    public class CubeDie : Die
    {
        public CubeDie(DieTextureService textureService) 
            : base(DieType.Cube, textureService)
        {
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            // Define the cube vertices (1x1x1 cube centered at origin)
            Point3D[] vertices = new Point3D[]
            {
                new Point3D(-0.5, -0.5, -0.5), // 0
                new Point3D( 0.5, -0.5, -0.5), // 1
                new Point3D( 0.5,  0.5, -0.5), // 2
                new Point3D(-0.5,  0.5, -0.5), // 3
                new Point3D(-0.5, -0.5,  0.5), // 4
                new Point3D( 0.5, -0.5,  0.5), // 5
                new Point3D( 0.5,  0.5,  0.5), // 6
                new Point3D(-0.5,  0.5,  0.5)  // 7
            };

            if (wireframeMode)
            {
                // Create wireframe edges
                AddWireframeEdge(modelGroup, vertices[0], vertices[1], color);
                AddWireframeEdge(modelGroup, vertices[1], vertices[2], color);
                AddWireframeEdge(modelGroup, vertices[2], vertices[3], color);
                AddWireframeEdge(modelGroup, vertices[3], vertices[0], color);
                
                AddWireframeEdge(modelGroup, vertices[4], vertices[5], color);
                AddWireframeEdge(modelGroup, vertices[5], vertices[6], color);
                AddWireframeEdge(modelGroup, vertices[6], vertices[7], color);
                AddWireframeEdge(modelGroup, vertices[7], vertices[4], color);
                
                AddWireframeEdge(modelGroup, vertices[0], vertices[4], color);
                AddWireframeEdge(modelGroup, vertices[1], vertices[5], color);
                AddWireframeEdge(modelGroup, vertices[2], vertices[6], color);
                AddWireframeEdge(modelGroup, vertices[3], vertices[7], color);
                
                return;
            }

            // Create textures for each face with the current die color
            BitmapImage[] textures = new BitmapImage[6];
            
            // For a standard d6, the opposite faces sum to 7
            textures[0] = TextureService.CreateDieTexture(1, color, Type); // 1 opposite 6
            textures[1] = TextureService.CreateDieTexture(2, color, Type); // 2 opposite 5
            textures[2] = TextureService.CreateDieTexture(3, color, Type); // 3 opposite 4
            textures[3] = TextureService.CreateDieTexture(4, color, Type); // 4 opposite 3
            textures[4] = TextureService.CreateDieTexture(5, color, Type); // 5 opposite 2
            textures[5] = TextureService.CreateDieTexture(6, color, Type); // 6 opposite 1
            
            // Standard die arrangement: opposite faces add up to 7
            GeometryHelper.CreateQuadFace(modelGroup, vertices, new int[] { 0, 3, 2, 1 }, textures[0]); // Front face - 1
            GeometryHelper.CreateQuadFace(modelGroup, vertices, new int[] { 1, 2, 6, 5 }, textures[1]); // Right face - 2
            GeometryHelper.CreateQuadFace(modelGroup, vertices, new int[] { 5, 6, 7, 4 }, textures[5]); // Back face - 6
            GeometryHelper.CreateQuadFace(modelGroup, vertices, new int[] { 4, 7, 3, 0 }, textures[2]); // Left face - 3
            GeometryHelper.CreateQuadFace(modelGroup, vertices, new int[] { 3, 7, 6, 2 }, textures[3]); // Top face - 4
            GeometryHelper.CreateQuadFace(modelGroup, vertices, new int[] { 4, 0, 1, 5 }, textures[4]); // Bottom face - 5
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
                double angle = i * 2 * System.Math.PI / segments;
                double x = thickness * System.Math.Cos(angle);
                double y = thickness * System.Math.Sin(angle);
                
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
            System.Windows.Media.Media3D.Quaternion rotation = new System.Windows.Media.Media3D.Quaternion();
            
            if (System.Math.Abs(Vector3D.DotProduct(direction, zaxis)) < 0.99999)
            {
                Vector3D rotationAxis = Vector3D.CrossProduct(zaxis, direction);
                rotationAxis.Normalize();
                double rotationAngle = System.Math.Acos(Vector3D.DotProduct(zaxis, direction));
                rotation = new System.Windows.Media.Media3D.Quaternion(rotationAxis, rotationAngle * 180 / System.Math.PI);
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
