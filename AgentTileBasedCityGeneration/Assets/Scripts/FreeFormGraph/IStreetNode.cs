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
        /// How many edges can be connected to this node.
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
        
        // // This should probably be specific methods using the appropriate implementation of IStreetEdge
        // /// <summary>
        // /// Adds an edge to the node.
        // ///
        // /// If the maximum number of edges that can be connected to this node has been reached, the edge will not be added.
        // /// </summary>
        // /// <param name="edge"> The edge to add. </param>
        // /// <returns> True if the edge was added, false otherwise. </returns>
        // public abstract bool AddEdge(TEdge edge);
        //
        // /// <summary>
        // /// Removes an edge from the node.
        // ///
        // /// If the edge is not connected to this node, the edge will not be removed.
        // /// </summary>
        // /// <param name="edge"> The edge to remove. </param>
        // /// <returns> True if the edge was removed, false otherwise. </returns>
        // public abstract bool RemoveEdge(TEdge edge);
        
        /// <summary>
        /// Returns a string representation of the node for debugging purposes.
        /// </summary>
        /// <returns> A debug string representation of the node. </returns>
        public string DebugString() {
            return $"Node at {Position} with {ConnectedEdgesCount} connected edges{(ConnectedEdgesCount > 0 ? $": {string.Join(", ", Edges)}" : "")}";
        }
        
    }
}