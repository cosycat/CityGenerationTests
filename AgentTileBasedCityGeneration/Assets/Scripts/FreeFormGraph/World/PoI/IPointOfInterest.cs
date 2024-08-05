#nullable enable
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    /// <summary>
    /// A point of interest is a specific location in the world.
    /// </summary>
    public interface IPointOfInterest {
        public Vector2 Position { get; }

        void DebugVisualize(IWorld world);
    }
}