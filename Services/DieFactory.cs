using System;
using RotatableDie.Models.DieTypes;
using RotatableDie.Services;

namespace RotatableDie.Models
{
    public class DieFactory
    {
        private readonly DieTextureService _textureService;
        
        public DieFactory(DieTextureService textureService)
        {
            _textureService = textureService;
        }
        
        public Die CreateDie(DieType dieType)
        {
            return dieType switch
            {
                DieType.Tetrahedron => new TetrahedronDie(_textureService),
                DieType.Cube => new CubeDie(_textureService),
                DieType.Octahedron => new OctahedronDie(_textureService),
                DieType.Dodecahedron => new DodecahedronDie(_textureService),
                DieType.Icosahedron => new IcosahedronDie(_textureService),
                DieType.Decahedron => new DecahedronDie(_textureService),
                _ => throw new ArgumentException($"Unknown die type: {dieType}")
            };
        }
    }
}
