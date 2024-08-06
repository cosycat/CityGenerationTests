#nullable enable
using UnityEngine;

namespace Graph.World.PoI {
    /// <summary>
    ///     A point of interest is a specific location in the world.
    ///     It is used for creating Settlements.
    /// </summary>
    public interface IPointOfInterest {
        public Vector2 Position { get; }

        void DebugVisualize(IWorld world);
    }
}