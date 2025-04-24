using System;
using System.Windows.Media.Media3D;

namespace RotatableDie.Models
{
    /// <summary>
    /// Represents a point in 4D space with X, Y, Z and W coordinates
    /// </summary>
    public class Point4D
    {
        // Coordinates
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }
        
        // Constructors
        public Point4D()
        {
            X = Y = Z = W = 0.0;
        }
        
        public Point4D(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        
        // Copy constructor
        public Point4D(Point4D other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
            W = other.W;
        }
        
        // Project a 4D point to 3D using perspective projection
        public Point3D ProjectTo3D(double viewerDistance)
        {
            // Perspective projection from 4D to 3D
            double factor = viewerDistance / (viewerDistance - W);
            
            return new Point3D(
                X * factor,
                Y * factor,
                Z * factor
            );
        }
        
        /// <summary>
        /// Calculates the Euclidean distance between two 4D points
        /// </summary>
        /// <param name="p1">First 4D point</param>
        /// <param name="p2">Second 4D point</param>
        /// <returns>The Euclidean distance between the two points</returns>
        public static double Distance(Point4D p1, Point4D p2)
        {
            if (p1 == null || p2 == null)
                throw new ArgumentNullException("Points cannot be null");
                
            return Math.Sqrt(
                Math.Pow(p1.X - p2.X, 2) +
                Math.Pow(p1.Y - p2.Y, 2) +
                Math.Pow(p1.Z - p2.Z, 2) +
                Math.Pow(p1.W - p2.W, 2)
            );
        }
    }
}