using System.Collections.Generic;
using UnityEngine;

namespace FreeFormGraph {
    public interface IStreetNode {
        
        /// <summary>
        /// The position of the node in the world.
        /// </summary>
        public Vector3 Position { get; }
        
        /// <summary>
        /// The edges connected to this node.
        /// </summary>
        public IEnumerable<IStreetEdge> Edges { get; }
        
        /// <summary>
        /// How many edges are connected to this node.
        /// </summary>
        public int ConnectedEdgesCount { get; }
        
        /// <summary>
        /// The maximum number of edges that can be connected to this node.
        /// </summary>
        public int MaxConnectedEdges { get; }
        
        /// <summary>
        /// Whether the maximum number of edges that can be connected to this node has been reached.
        /// </summary>
        public bool IsMaxConnectedEdgesReached => ConnectedEdgesCount >= MaxConnectedEdges;

        /// <summary>
        /// Returns a string representation of the node for debugging purposes.
        /// </summary>
        /// <returns> A debug string representation of the node. </returns>
        public string DebugString() {
            return $"Node at {Position} with {ConnectedEdgesCount} connected edges{(ConnectedEdgesCount > 0 ? $": {string.Join(", ", Edges)}" : "")}";
        }
        
    }
}