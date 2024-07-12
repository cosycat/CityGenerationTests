#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FreeFormGraph.World;

namespace FreeFormGraph {
    public abstract class StreetGraphGameObject : MonoBehaviour, IStreetGraph {

        public abstract IEnumerable<IStreetNode> Nodes { get; }
        public abstract IEnumerable<IStreetEdge> Edges { get; }
        public virtual int NodeCount => Nodes.Count();
        public virtual int EdgeCount => Edges.Count();
        public abstract float SnapToExistingNodeThreshold { get; set; }
        public abstract float SnapToExistingEdgeThreshold { get; set; }
        
        void Start() {
            Init(FindObjectOfType<WorldGameObject>());
        }
        
        public virtual void Init(IWorld world) {}
        
        public virtual bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew, out bool isEdgeNew, bool failIfIntersection = false) {
            if (!GetOrCreateNode(to, SnapToExistingNodeThreshold, out toNode, out isToNodeNew)) {
                Debug.Log("IStreetGraph::CreateEdge - Failed to create node at to position");
                newEdge = null!;
                isEdgeNew = false;
                return false;
            }
            // try to create the edge
            var success = CreateEdge(from, toNode, out newEdge, out isEdgeNew);
            if (success) {
                return true;
            }
            // if the edge creation failed, remove the node if it was newly created
            if (isToNodeNew) {
                RemoveNode(toNode);
            }
            return false;
        }

        public abstract bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge, out bool isEdgeNew);
        public abstract bool RemoveNode(IStreetNode node);
        public abstract bool RemoveEdge(IStreetEdge edge);

        public virtual bool TryFindClosestNode(IEnumerable<IStreetNode> nodes, Vector3 position, out IStreetNode foundNode, float threshold = Single.MaxValue) {
            IStreetNode? optionalFoundNode = null;
            var minDistance = threshold;
            foreach (var node in nodes) {
                var distance = Vector3.Distance(node.Position, position);
                if (distance <= minDistance) { //"<=" is important for when threshold = 0!
                    minDistance = distance;
                    optionalFoundNode = node;
                }
            }
            foundNode = optionalFoundNode ?? null!;
            return optionalFoundNode != null;
        }

        public virtual bool TryFindClosestNode(Vector3 position, out IStreetNode foundNode, float threshold = float.MaxValue) =>
            TryFindClosestNode(Nodes, position, out foundNode, threshold);

        public virtual bool TryFindClosestEdge(IEnumerable<IStreetEdge> edges, Vector3 position, out IStreetEdge foundEdge, out Vector3 positionOnEdge,
            float threshold = float.MaxValue) {
            IStreetEdge? optionalFoundEdge = null;
            var minDistance = threshold;
            positionOnEdge = default;
            foreach (var edge in edges) {
                var distance = edge.GetDistanceEdgeToPosition(position, out var posOnEdgeTmp);
                if (distance < minDistance) {
                    minDistance = distance;
                    optionalFoundEdge = edge;
                    positionOnEdge = posOnEdgeTmp;
                }
            }
            foundEdge = optionalFoundEdge ?? null!;
            return optionalFoundEdge != null;
        }

        public bool TryFindClosestEdge(Vector3 position, out IStreetEdge foundEdge, out Vector3 positionOnEdge, float threshold = float.MaxValue) =>
            TryFindClosestEdge(Edges, position, out foundEdge, out positionOnEdge, threshold);

        public abstract bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode);
        public virtual bool GetOrCreateNode(Vector3 position, float threshold, out IStreetNode node, out bool isNewlyCreatedNode) {
            // Snap to node if possible
            if (((IStreetGraph)this).TryFindClosestNode(position, out node, threshold)) {
                isNewlyCreatedNode = false;
                return true;
            }
            // snap to edge if possible
            if (((IStreetGraph)this).TryFindClosestEdge(position, out var edge, out var positionOnEdge, SnapToExistingEdgeThreshold)) {
                Debug.Log($"IStreetGraph::GetOrCreateNode - Found edge to snap to. Position on edge: {positionOnEdge}, edge: {edge}");
                // snap to existing node on edge if possible
                if (((IStreetGraph)this).TryFindClosestNode(positionOnEdge, out node, SnapToExistingNodeThreshold)) {
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
        
        public IStreetNode[] FindAllNodesWithinRange(Vector2 position, float radius) {
            var nodes = new List<IStreetNode>();
            foreach (var streetNode in Nodes) {
                if (Vector2.Distance(streetNode.Position, position) <= radius) {
                    nodes.Add(streetNode);
                }
            }
            return nodes.ToArray();
        }

        public virtual IStreetEdge[] FindAllEdgesWithinRange(Vector2 position, float radius) {
            var edges = new List<IStreetEdge>();
            foreach (var streetEdge in Edges) {
                if (streetEdge.GetDistanceEdgeToPosition(position, out _) <= radius) {
                    edges.Add(streetEdge);
                }
            }
            return edges.ToArray();
        }

        #region Events
        
        public event EventHandler<NodeEventArgs>? NodeAdded;
        public event EventHandler<NodeEventArgs>? NodeRemoved;
        public event EventHandler<EdgeEventArgs>? EdgeAdded;
        public event EventHandler<EdgeEventArgs>? EdgeRemoved;
        
        #endregion

        protected virtual void OnNodeAdded(IStreetNode node) {
            NodeAdded?.Invoke(this, new NodeEventArgs(node));
        }
        
        protected virtual void OnNodeRemoved(IStreetNode node) {
            NodeRemoved?.Invoke(this, new NodeEventArgs(node));
        }
        
        protected virtual void OnEdgeAdded(IStreetEdge edge) {
            EdgeAdded?.Invoke(this, new EdgeEventArgs(edge));
        }
        
        protected virtual void OnEdgeRemoved(IStreetEdge edge) {
            EdgeRemoved?.Invoke(this, new EdgeEventArgs(edge));
        }
        
    }
    
}