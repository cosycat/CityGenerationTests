using UnityEngine;

namespace FreeFormGraph.World.PoI {
    
    /// <summary>
    /// A point of interest is a specific location in the world.
    /// It can be of different types, see <see cref="PointOfInterestType"/>.
    /// </summary>
    public interface IPointOfInterest {
        
        public Vector2 Position { get; }
        public PointOfInterestType Type { get; }
        
        /// <summary>
        /// Checks if a point is within this point of interest.
        /// </summary>
        /// <param name="point"> The point to check. </param>
        /// <returns> True if the point is within this point of interest, false otherwise. </returns>
        public bool IsPointWithinRange(Vector2 point);
        
    }
    
    /// <summary>
    /// The type of <see cref="IPointOfInterest"/>.
    /// </summary>
    public enum PointOfInterestType {
        Village,
        Town,
        City,
    }
}