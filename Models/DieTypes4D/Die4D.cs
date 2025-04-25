using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using RotatableDie.Services;

namespace RotatableDie.Models.DieTypes4D
{
    /// <summary>
    /// Base class for all 4-dimensional dice
    /// </summary>
    public abstract class Die4D : Die
    {
        /// <summary>
        /// Current rotation angles for the 4D transformations (in radians)
        /// </summary>
        public double RotationXW { get; set; } = 0; // Rotation in the X-W plane
        public double RotationYW { get; set; } = 0; // Rotation in the Y-W plane
        public double RotationZW { get; set; } = 0; // Rotation in the Z-W plane
        
        // Track main W rotation direction
        protected double _wRotationSign = 1.0;
        
        /// <summary>
        /// Gets the combined W rotation angle in degrees (for display purposes)
        /// </summary>
        public double RotationWAngle
        {
            get
            {
                // Calculate average of the three 4D rotation components
                // This gives us a more consistent W rotation angle that respects the sign
                // and maintains the -180° to +180° range
                double sum = RotationXW + RotationYW + RotationZW;
                double average = sum / 3.0;
                
                // Convert from radians to degrees
                return average * (180.0 / Math.PI);
            }
        }
        
        /// <summary>
        /// Store the original 4D vertices before any rotations
        /// </summary>
        protected Point4D[] OriginalVertices4D;
        
        /// <summary>
        /// Currently rotated 4D vertices
        /// </summary>
        protected Point4D[] CurrentVertices4D;

        public Die4D(DieType dieType, DieTextureService textureService) 
            : base(dieType, textureService)
        {
            // Initialize arrays to avoid non-nullable warnings
            OriginalVertices4D = Array.Empty<Point4D>();
            CurrentVertices4D = Array.Empty<Point4D>();
        }
        
        /// <summary>
        /// Creates the 3D geometry based on the current 4D rotation state
        /// </summary>
        public override void CreateGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false)
        {
            // Update 4D rotation
            Apply4DRotation();
            
            // Project from 4D to 3D
            ProjectTo3D();
            
            // Render the projected 3D geometry
            RenderProjectedGeometry(modelGroup, color, wireframeMode);
        }
        
        /// <summary>
        /// Apply 4D rotations to vertices
        /// </summary>
        protected virtual void Apply4DRotation()
        {
            // Make a copy of the original vertices
            if (CurrentVertices4D == null || CurrentVertices4D.Length != OriginalVertices4D.Length)
            {
                CurrentVertices4D = new Point4D[OriginalVertices4D.Length];
            }
            
            // Copy original vertices
            Array.Copy(OriginalVertices4D, CurrentVertices4D, OriginalVertices4D.Length);
            
            // Apply 4D rotations in each of the 4D planes
            ApplyRotationXW(CurrentVertices4D, RotationXW);
            ApplyRotationYW(CurrentVertices4D, RotationYW);
            ApplyRotationZW(CurrentVertices4D, RotationZW);
        }
        
        /// <summary>
        /// Project the 4D vertices to 3D space
        /// </summary>
        protected abstract void ProjectTo3D();
        
        /// <summary>
        /// Render the 3D projected geometry to the model group
        /// </summary>
        protected abstract void RenderProjectedGeometry(Model3DGroup modelGroup, Color color, bool wireframeMode = false);
        
        /// <summary>
        /// Update the 4D rotation angles
        /// </summary>
        public virtual void Rotate4D(double deltaXW, double deltaYW, double deltaZW)
        {
            // Update rotation values
            RotationXW += deltaXW;
            RotationYW += deltaYW;
            RotationZW += deltaZW;
            
            // Normalize all angles to keep them within the -π to +π range (-180° to +180°)
            // This prevents the angles from growing too large over time
            const double PI = Math.PI;
            const double TwoPI = 2 * Math.PI;
            
            // First, get the angle into the range of 0 to 2π
            RotationXW = ((RotationXW % TwoPI) + TwoPI) % TwoPI;
            RotationYW = ((RotationYW % TwoPI) + TwoPI) % TwoPI;
            RotationZW = ((RotationZW % TwoPI) + TwoPI) % TwoPI;
            
            // Then convert from 0-2π to -π to +π
            if (RotationXW > PI) RotationXW -= TwoPI;
            if (RotationYW > PI) RotationYW -= TwoPI;
            if (RotationZW > PI) RotationZW -= TwoPI;
        }
        
        /// <summary>
        /// Apply a rotation in the X-W plane
        /// </summary>
        protected virtual void ApplyRotationXW(Point4D[] vertices, double angle)
        {
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);
            
            for (int i = 0; i < vertices.Length; i++)
            {
                double newX = vertices[i].X * cosAngle - vertices[i].W * sinAngle;
                double newW = vertices[i].X * sinAngle + vertices[i].W * cosAngle;
                
                vertices[i].X = newX;
                vertices[i].W = newW;
            }
        }
        
        /// <summary>
        /// Apply a rotation in the Y-W plane
        /// </summary>
        protected virtual void ApplyRotationYW(Point4D[] vertices, double angle)
        {
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);
            
            for (int i = 0; i < vertices.Length; i++)
            {
                double newY = vertices[i].Y * cosAngle - vertices[i].W * sinAngle;
                double newW = vertices[i].Y * sinAngle + vertices[i].W * cosAngle;
                
                vertices[i].Y = newY;
                vertices[i].W = newW;
            }
        }
        
        /// <summary>
        /// Apply a rotation in the Z-W plane
        /// </summary>
        protected virtual void ApplyRotationZW(Point4D[] vertices, double angle)
        {
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);
            
            for (int i = 0; i < vertices.Length; i++)
            {
                double newZ = vertices[i].Z * cosAngle - vertices[i].W * sinAngle;
                double newW = vertices[i].Z * sinAngle + vertices[i].W * cosAngle;
                
                vertices[i].Z = newZ;
                vertices[i].W = newW;
            }
        }
    }
    
    /// <summary>
    /// Represents a point in 4D space (x, y, z, w)
    /// </summary>
    public struct Point4D
    {
        public double X;
        public double Y;
        public double Z;
        public double W;
        
        public Point4D(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        
        /// <summary>
        /// Project this 4D point to 3D space using perspective projection
        /// </summary>
        /// <param name="viewerDistance">Distance of the 4D viewer from the origin</param>
        /// <returns>A 3D point representing the projection</returns>
        public Point3D ProjectTo3D(double viewerDistance)
        {
            // Add a small offset to avoid division by zero
            double w = W + viewerDistance;
            
            // Scale factor based on the W coordinate (farther in W = smaller)
            double scaleFactor = viewerDistance / Math.Max(w, 0.1);
            
            // Project to 3D by scaling X, Y, Z by the scale factor
            return new Point3D(
                X * scaleFactor,
                Y * scaleFactor,
                Z * scaleFactor);
        }
    }
}