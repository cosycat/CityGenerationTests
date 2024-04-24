using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

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
                newEdge = null;
                toNode = null;
                isToNodeNew = false;
                return false;
            }
            // try to create the edge
            var success = CreateEdge(fromNode, to, out newEdge, out toNode, out isToNodeNew);
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
            out bool isToNodeNew) {
            if (!GetOrCreateNode(to, SnapToExistingNodeThreshold, out toNode, out isToNodeNew)) {
                Debug.Log("IStreetGraph::CreateEdge - Failed to create node at to position");
                newEdge = null;
                return false;
            }
            // try to create the edge
            var success = CreateEdge(from, toNode, out newEdge);
            if (success) {
                return true;
            }
            // if the edge creation failed, remove the node if it was newly created
            if (isToNodeNew) {
                RemoveNode(toNode);
            }
            return false;
        }
        
        public bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge);
        
        bool RemoveNode(IStreetNode node);

        /// <summary>
        /// Finds the closest node to the given position, if it is within the given threshold.
        /// </summary>
        /// <param name="position"> The position to find the closest node to. </param>
        /// <param name="foundNode"> The closest node to the position, if it is within the threshold, null otherwise. </param>
        /// <param name="threshold"> The maximum distance to consider a node as the closest. </param>
        /// <returns> True if a node was found, false otherwise. </returns>
        public bool TryFindClosestNode(Vector3 position, out IStreetNode foundNode,
            float threshold = float.MaxValue) {
            foundNode = null;
            var minDistance = threshold;
            foreach (var node in Nodes) {
                var distance = Vector3.Distance(node.Position, position);
                if (distance < minDistance) {
                    minDistance = distance;
                    foundNode = node;
                }
            }
            return foundNode != null;
        }

        /// <summary>
        /// Finds the closest edge to the given position, if it is within the given threshold.
        /// </summary>
        /// <param name="position"> The position to find the closest edge to. </param>
        /// <param name="foundEdge"> The closest edge to the position, if it is within the threshold, null otherwise. </param>
        /// <param name="positionOnEdge"> The position on the edge that is closest to the given position. </param>
        /// <param name="threshold"> The maximum distance to consider an edge as the closest. </param>
        /// <returns> True if an edge was found, false otherwise. </returns>
        public bool TryFindClosestEdge(Vector3 position, out IStreetEdge foundEdge, out Vector3 positionOnEdge,
            float threshold = float.MaxValue) {

            foundEdge = null;
            var minDistance = threshold;
            positionOnEdge = default;
            foreach (var edge in Edges) {
                var distance = GetDistanceEdgeToPosition(edge, position, out var posOnEdgeTmp);
                if (distance < minDistance) {
                    minDistance = distance;
                    foundEdge = edge;
                    positionOnEdge = posOnEdgeTmp;
                }
            }
            return foundEdge != null;
        }

        /// <summary>
        /// Returns the distance from the given position to the given edge.
        /// </summary>
        /// <param name="edge"> The edge to measure the distance to. </param>
        /// <param name="position"> The position to measure the distance from. </param>
        /// <param name="positionOnEdge"> The position on the edge that is closest to the given position. </param>
        /// <returns> The distance from the position to the edge. </returns>
        public float GetDistanceEdgeToPosition(IStreetEdge edge, Vector3 position, out Vector3 positionOnEdge);

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
        public bool GetOrCreateNode(Vector3 position, float threshold, out IStreetNode node, out bool isNewlyCreatedNode) {
            // Snap to node if possible
            if (TryFindClosestNode(position, out node, threshold)) {
                isNewlyCreatedNode = false;
                return true;
            }
            // snap to edge if possible
            if (TryFindClosestEdge(position, out var edge, out var positionOnEdge, SnapToExistingEdgeThreshold)) {
                Debug.Log($"IStreetGraph::GetOrCreateNode - Found edge to snap to. Position on edge: {positionOnEdge}, edge: {edge}");
                // snap to existing node on edge if possible
                if (TryFindClosestNode(positionOnEdge, out node, SnapToExistingNodeThreshold)) {
                    isNewlyCreatedNode = false;
                    return true;
                }
                // otherwise create new node on edge
                InsertNodeOnEdge(edge, positionOnEdge, out node, out _, out _);
                isNewlyCreatedNode = true;
                return true;
            }
            // otherwise create new node
            var success = CreateUnconnectedNode(position, out node); // TODO for optimization: CreateUnconnectedNode will most likely call FindClosestNode again
            isNewlyCreatedNode = success;
            return success;
        }

        void InsertNodeOnEdge(IStreetEdge edge, Vector3 positionOnEdge, out IStreetNode node, out IStreetEdge leftEdge, out IStreetEdge rightEdge);
    }
}