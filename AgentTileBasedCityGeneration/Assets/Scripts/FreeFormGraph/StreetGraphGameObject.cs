using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FreeFormGraph {
    public abstract class StreetGraphGameObject : MonoBehaviour, IStreetGraph {

        public abstract IEnumerable<IStreetNode> Nodes { get; }
        public abstract IEnumerable<IStreetEdge> Edges { get; }
        public virtual int NodeCount => Nodes.Count();
        public virtual int EdgeCount => Edges.Count();
        public abstract float SnapToExistingNodeThreshold { get; set; }
        public abstract float SnapToExistingEdgeThreshold { get; set; }
        
        public virtual bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew, bool failIfIntersection = false) {
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

        public abstract bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge);
        public abstract bool RemoveNode(IStreetNode node);
        public virtual bool TryFindClosestNode(Vector3 position, out IStreetNode foundNode, float threshold = Single.MaxValue) {
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

        public virtual bool TryFindClosestEdge(Vector3 position, out IStreetEdge foundEdge, out Vector3 positionOnEdge,
            float threshold = Single.MaxValue) {

            foundEdge = null;
            var minDistance = threshold;
            positionOnEdge = default;
            foreach (var edge in Edges) {
                var distance = edge.GetDistanceEdgeToPosition(position, out var posOnEdgeTmp);
                if (distance < minDistance) {
                    minDistance = distance;
                    foundEdge = edge;
                    positionOnEdge = posOnEdgeTmp;
                }
            }
            return foundEdge != null;
        }

        public abstract bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode);
        public virtual bool GetOrCreateNode(Vector3 position, float threshold, out IStreetNode node, out bool isNewlyCreatedNode) {
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

        public abstract void InsertNodeOnEdge(IStreetEdge edge, Vector3 positionOnEdge, out IStreetNode node, out IStreetEdge leftEdge, out IStreetEdge rightEdge);
    }
    
}