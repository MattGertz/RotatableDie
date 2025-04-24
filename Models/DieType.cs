using System;

namespace RotatableDie.Models
{
    /// <summary>
    /// Represents the different types of dice that can be rendered
    /// </summary>
    public enum DieType
    {
        Tetrahedron,
        Cube,
        Octahedron,
        Decahedron,
        Dodecahedron,
        Icosahedron,
        Pentachoron,  // 4D 5-cell (comes first as it has fewer cells)
        Tesseract     // 4D hypercube (8-cell)
    }
}
