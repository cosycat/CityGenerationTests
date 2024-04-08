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
        public abstract bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew);
        public abstract bool RemoveNode(IStreetNode node);

        public IStreetNode FindClosestNode(Vector3 position, float threshold = float.MaxValue) {
            var closestNode = default(IStreetNode);
            var closestDistance = threshold;
            foreach (var node in Nodes) {
                var distance = Vector3.Distance(node.Position, position);
                if (distance < closestDistance) {
                    closestNode = node;
                    closestDistance = distance;
                }
            }

            return closestNode;
        }

        public abstract bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode);
    }
}