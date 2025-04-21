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
        
        public override void CreateGeometry(Model3DGroup modelGroup, Color color)
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
    }
}
