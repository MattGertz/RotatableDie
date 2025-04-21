using System;

namespace RotatableDie.Models
{
    /// <summary>
    /// Represents an item in the die type selection combo box
    /// </summary>
    public class DieTypeItem
    {
        public DieType Type { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
