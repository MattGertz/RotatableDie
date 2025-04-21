using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes
{
    public class OctahedronDie : Die
    {
        public OctahedronDie(DieTextureService textureService) 
            : base(DieType.Octahedron, textureService)
        {
        }
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color)
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
    }
}
