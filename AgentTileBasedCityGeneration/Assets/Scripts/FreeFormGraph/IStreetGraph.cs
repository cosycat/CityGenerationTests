#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using FreeFormGraph.World;

namespace FreeFormGraph {
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
        
        // public float MinEdgeLength { get; } // TODO implement this
        
        /// <summary>
        /// The threshold for snapping the to position to an existing node when adding a new edge.
        ///
        /// When the to position is outside <see cref="SnapToExistingNodeThreshold"/>, but within <see cref="SnapToExistingEdgeThreshold"/>
        /// of an existing edge, and the position on the edge is within <see cref="SnapToExistingNodeThreshold"/> of an existing node,
        /// the to position will still be snapped to the existing node.
        /// </summary>
        public float SnapToExistingNodeThreshold { get; set; }
        
        /// <summary>
        /// The threshold for snapping the to position to a new Node of an existing edge when adding a new edge.
        /// </summary>
        public float SnapToExistingEdgeThreshold { get; set; }

        public void Init(IWorld world) {}

        /// <summary>
        /// Adds a new edge to the street graph.
        /// 
        /// If the to position is within <see cref="SnapToExistingNodeThreshold"/> of an existing node, the edge will be connected to that node.
        /// If the to position is within <see cref="SnapToExistingEdgeThreshold"/> of an existing edge, the edge will be connected to a new node on that edge,
        /// or to an existing node if the new position on the edge is within <see cref="SnapToExistingNodeThreshold"/> of an existing node.
        /// Otherwise, a new node will be created at the to position.
        /// </summary>
        /// <param name="from"> The position to connect the edge from. </param>
        /// <param name="to"> The position to connect the edge to. </param>
        /// <param name="newEdge"> The new edge that was created. </param>
        /// <param name="fromNode"> The node that the edge was connected from. May be a newly generated node. </param>
        /// <param name="toNode"> The node that the edge was connected to. </param>
        /// <param name="isFromNodeNew"> Whether the from node was newly created or not. </param>
        /// <param name="isToNodeNew"> Whether the to node was newly created or not. </param>
        /// <returns> True if the edge was created, false otherwise. </returns>
        public bool CreateEdge(Vector3 from, Vector3 to, out IStreetEdge newEdge, out IStreetNode fromNode,
            out IStreetNode toNode, out bool isFromNodeNew, out bool isToNodeNew) {
            if (!GetOrCreateNode(from, SnapToExistingNodeThreshold, out fromNode, out isFromNodeNew)) {
                newEdge = null!;
                toNode = null!;
                isToNodeNew = false;
                return false;
            }
            // try to create the edge
            var success = CreateEdge(fromNode, to, out newEdge, out toNode, out isToNodeNew, out _);
            if (success) {
                return true;
            }
            // if the edge creation failed, remove the node if it was newly created
            if (isFromNodeNew) {
                RemoveNode(fromNode);
            }
            return false;
        }

        public bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew, out bool isEdgeNew, bool failIfIntersection = false);
        
        public bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge, out bool isEdgeNew);

        bool RemoveNode(IStreetNode node);

        /// <summary>
        /// Finds the closest node to the given position, if it is within the given threshold.
        /// </summary>
        /// <param name="position"> The position to find the closest node to. </param>
        /// <param name="foundNode"> The closest node to the position, if it is within the threshold, null otherwise. </param>
        /// <param name="threshold"> The maximum distance to consider a node as the closest. </param>
        /// <returns> True if a node was found, false otherwise. </returns>
        public bool TryFindClosestNode(Vector3 position, out IStreetNode foundNode, float threshold = float.MaxValue) =>
            TryFindClosestNode(Nodes, position, out foundNode, threshold);

        public bool TryFindClosestNode(IEnumerable<IStreetNode> nodes, Vector3 position, out IStreetNode foundNode, float threshold = float.MaxValue);

        /// <summary>
        /// Finds the closest edge to the given position, if it is within the given threshold.
        /// </summary>
        /// <param name="position"> The position to find the closest edge to. </param>
        /// <param name="foundEdge"> The closest edge to the position, if it is within the threshold, null otherwise. </param>
        /// <param name="positionOnEdge"> The position on the edge that is closest to the given position. </param>
        /// <param name="threshold"> The maximum distance to consider an edge as the closest. </param>
        /// <returns> True if an edge was found, false otherwise. </returns>
        public bool TryFindClosestEdge(Vector3 position, out IStreetEdge foundEdge, out Vector3 positionOnEdge, float threshold = float.MaxValue) =>
            TryFindClosestEdge(Edges, position, out foundEdge, out positionOnEdge, threshold);

        public bool TryFindClosestEdge(IEnumerable<IStreetEdge> edges, Vector3 position, out IStreetEdge foundEdge, out Vector3 positionOnEdge, float threshold = float.MaxValue);

        /// <summary>
        /// Creates a new node at the given position without connecting it to any edges.
        ///
        /// Will fail, if too close to an existing node or edge.
        /// 
        /// Useful for creating new nodes that will be connected to edges in a later step,
        /// or for starting a completely new graph.
        /// </summary>
        /// <param name="position"> The position of the new node. </param>
        /// <param name="newNode"> The new node that was created. </param>
        /// <returns> True if the node was created, false otherwise. </returns>
        public bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode);

        /// <summary>
        /// Returns the closest node to the given position, or creates a new node at the position if no node is close enough.
        /// 
        /// If a node is found or created, it will be returned in the out parameter and the method will return true.
        /// If no node is found and no node can be created, the out parameter will be null and the method will return false.
        /// </summary>
        /// <param name="position"> The position to find or create a node for. </param>
        /// <param name="threshold"> The maximum distance to consider a node as the closest. </param>
        /// <param name="node"> The closest node to the position, or the newly created node. </param>
        /// <param name="isNewlyCreatedNode"> Whether the node was newly created or not. </param>
        /// <returns> True if a node was found or created, false otherwise. </returns>
        public bool GetOrCreateNode(Vector3 position, float threshold, out IStreetNode node, out bool isNewlyCreatedNode);

        void InsertNodeOnEdge(IStreetEdge edge, Vector3 positionOnEdge, out IStreetNode node, out IStreetEdge leftEdge, out IStreetEdge rightEdge);
        IStreetNode[] FindAllNodesWithinRange(Vector2 position, float radius);
        IStreetEdge[] FindAllEdgesWithinRange(Vector2 position, float radius);

        #region Events
        
        public event EventHandler<NodeEventArgs> NodeAdded;
        
        public event EventHandler<NodeEventArgs> NodeRemoved;
        
        public event EventHandler<EdgeEventArgs> EdgeAdded;
        
        public event EventHandler<EdgeEventArgs> EdgeRemoved;
        
        
        
        #endregion
        
    }
    
    public class NodeEventArgs : EventArgs {
        public IStreetNode Node { get; }

        public NodeEventArgs(IStreetNode node) {
            Node = node;
        }
    }
    
    public class EdgeEventArgs : EventArgs {
        public IStreetEdge Edge { get; }

        public EdgeEventArgs(IStreetEdge edge) {
            Edge = edge;
        }
    }
    
    
}