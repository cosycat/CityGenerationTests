using System.Collections.Generic;
using UnityEngine;

namespace FreeFormGraph {

    public abstract class StreetGraphGameObject : MonoBehaviour, IStreetGraph {
        public abstract IEnumerable<IStreetNode> Nodes { get; }
        public abstract IEnumerable<IStreetEdge> Edges { get; }
        public abstract int NodeCount { get; }
        public abstract int EdgeCount { get; }
        public abstract float SnapToExistingNodeThreshold { get; set; }
        public abstract float SnapToExistingEdgeThreshold { get; set; }
        public abstract bool AddEdge(Vector3 from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode);
        public abstract bool AddEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode);
    }

    public interface IStreetGraph {
        
        /// <summary>
        /// All nodes of the street graph.
        /// </summary>
        public IEnumerable<IStreetNode> Nodes { get; }
        
        /// <summary>
        /// All edges of the street graph.
        /// </summary>
        public IEnumerable<IStreetEdge> Edges { get; }

        public int NodeCount { get; }
        public int EdgeCount { get; }
        
        /// <summary>
        /// The threshold for snapping the to position to an existing node when adding a new edge.
        ///
        /// When the to position is outside of <see cref="SnapToExistingNodeThreshold"/>, but within <see cref="SnapToExistingEdgeThreshold"/>
        /// of an existing edge, and the position on the edge is within <see cref="SnapToExistingNodeThreshold"/> of an existing node,
        /// the to position will still be snapped to the existing node.
        /// </summary>
        public float SnapToExistingNodeThreshold { get; set; }
        
        /// <summary>
        /// The threshold for snapping the to position to a new Node of an existing edge when adding a new edge.
        /// </summary>
        public float SnapToExistingEdgeThreshold { get; set; }

        /// <summary>
        /// Adds a new edge to the street graph.
        /// 
        /// If the to position is within <see cref="SnapToExistingNodeThreshold"/> of an existing node, the edge will be connected to that node.
        /// If the to position is within <see cref="SnapToExistingEdgeThreshold"/> of an existing edge, the edge will be connected to a new node on that edge,
        /// or to an existing node if the new position on the edge is within <see cref="SnapToExistingNodeThreshold"/> of an existing node.
        /// Otherwise, a new node will be created at the to position.
        /// </summary>
        /// <param name="from"> The node to connect the edge from. </param>
        /// <param name="to"> The position to connect the edge to. </param>
        /// <param name="newEdge"> The new edge that was created. </param>
        /// <param name="toNode"> The node that the edge was connected to. </param>
        /// <returns></returns>
        public bool AddEdge(Vector3 from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode);
        
        public bool AddEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode);

        /// <summary>
        /// Returns the closest node to the given position.
        ///
        /// Undefined behavior if there are no nodes in the graph.
        /// </summary>
        /// <param name="position"> The position to find the closest node to. </param>
        /// <returns> The closest node to the position. </returns>
        public IStreetNode FindClosestNode(Vector3 position) {
            var closestNode = default(IStreetNode);
            var closestDistance = float.MaxValue;
            foreach (var node in Nodes) {
                var distance = Vector3.Distance(node.Position, position);
                if (distance < closestDistance) {
                    closestNode = node;
                    closestDistance = distance;
                }
            }

            return closestNode;
        }
    }

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

    public interface IStreetEdge {
        
        /// <summary>
        /// The position of the first node of the street.
        /// </summary>
        public Vector3 PosA => NodeA.Position;
        /// <summary>
        /// The position of the second node of the street.
        /// </summary>
        public Vector3 PosB => NodeB.Position;

        /// <summary>
        /// The first node of the street.
        /// </summary>
        public IStreetNode NodeA { get; }

        /// <summary>
        /// The second node of the street.
        /// </summary>
        public IStreetNode NodeB { get; }

        /// <summary>
        /// The street width measured from the center of the street to the edge of the street.
        /// </summary>
        public float StreetWidth { get; }
        
        /// <summary>
        /// Returns a string representation of the edge for debugging purposes.
        /// </summary>
        /// <returns> A debug string representation of the edge. </returns>
        public string DebugString() {
            return $"Edge from {NodeA}\nto {NodeB}";
        }
    }

    
}