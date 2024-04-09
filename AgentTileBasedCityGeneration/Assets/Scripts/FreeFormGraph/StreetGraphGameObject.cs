using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph {
    public abstract class StreetGraphGameObject : MonoBehaviour, IStreetGraph {
        
        public abstract IEnumerable<IStreetNode> Nodes { get; }
        public abstract IEnumerable<IStreetEdge> Edges { get; }
        public abstract int NodeCount { get; }
        public abstract int EdgeCount { get; }
        public abstract float SnapToExistingNodeThreshold { get; set; }
        public abstract float SnapToExistingEdgeThreshold { get; set; }

        public abstract bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge);
        public abstract bool RemoveNode(IStreetNode node);

        public bool TryFindClosestNode(Vector3 position, out IStreetNode foundNode, float threshold = float.MaxValue) {
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

        public bool TryFindClosestEdge(Vector3 position, out IStreetEdge foundEdge, out Vector3 positionOnEdge, float threshold = float.MaxValue) {
            foundEdge = null;
            var minDistance = threshold;
            positionOnEdge = default;
            foreach (var edge in Edges) {
                var distance = GetDistanceEdgeToPosition(edge, position, out positionOnEdge);
                if (distance < minDistance) {
                    minDistance = distance;
                    foundEdge = edge;
                }
            }
            return foundEdge != null;
        }

        public abstract float GetDistanceEdgeToPosition(IStreetEdge edge, Vector3 position, out Vector3 positionOnEdge);

        public abstract bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode);
        public abstract void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node);
    }
}