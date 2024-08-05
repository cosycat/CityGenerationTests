using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FreeFormGraph {
    public interface IStreetNode {
        /// <summary>
        /// The position of the node in the world.
        /// </summary>
        public Vector3 Position { get; }

        public Vector3 PositionMeters => Position * Constants.METERS_PER_UNIT;

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
        /// The minimum angle between edges connected to this node.
        /// New edges with an angle to an existing connected edge smaller than this will fail to connect.
        /// </summary>
        public float MinAngleBetweenEdges { get; }

        /// <summary>
        /// Whether the maximum number of edges that can be connected to this node has been reached.
        /// </summary>
        public bool IsMaxConnectedEdgesReached => ConnectedEdgesCount >= MaxConnectedEdges;

        public IList<IStreetNode> GetNeighbors() {
            var neighbors = new List<IStreetNode>();
            foreach (var edge in Edges) {
                var otherNode = edge.NodeA;
                if (otherNode == this) otherNode = edge.NodeB;
                neighbors.Add(otherNode);
            }

            Debug.Assert(neighbors.Distinct().Count() == neighbors.Count());
            return neighbors;
        }

        /// <summary>
        /// Returns a string representation of the node for debugging purposes.
        /// </summary>
        /// <returns> A debug string representation of the node. </returns>
        public string DebugString() {
            return
                $"Node at {Position} with {ConnectedEdgesCount} connected edges{(ConnectedEdgesCount > 0 ? $": {string.Join(", ", Edges)}" : "")}";
        }
    }
}