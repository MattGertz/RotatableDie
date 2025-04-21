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
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color)
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
    }
}
