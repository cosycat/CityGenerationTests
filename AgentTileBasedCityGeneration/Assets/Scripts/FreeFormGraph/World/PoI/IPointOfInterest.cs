#nullable enable
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    
    /// <summary>
    /// A point of interest is a specific location in the world.
    /// </summary>
    public interface IPointOfInterest {
        
        public Vector2 Position { get; }

        public float Radius { get; set; }
        
        /// <summary>
        /// Checks if a point is within this point of interest.
        /// </summary>
        /// <param name="point"> The point to check. </param>
        /// <returns> True if the point is within this point of interest, false otherwise. </returns>
        public bool IsPointWithinRange(Vector2 point);

        
        IStreetNode[] FindAllNodes(IWorld world);

        public float Budget {get;set;}
    }
}