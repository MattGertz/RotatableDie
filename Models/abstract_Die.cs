using System.Windows.Media;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models
{
    /// <summary>
    /// Base abstract class for all die types
    /// </summary>
    public abstract class Die
    {
        protected DieTextureService TextureService { get; }
        
        public DieType Type { get; }
        
        protected Die(DieType type, DieTextureService textureService)
        {
            Type = type;
            TextureService = textureService;
        }
        
        /// <summary>
        /// Creates the geometry for this die type and adds it to the model group
        /// </summary>
        public abstract void CreateGeometry(Model3DGroup modelGroup, Color color);
    }
}
