using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using FreeFormGraph.LineBased;

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
        public abstract bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode);

        public abstract void InsertNodeOnEdge(IStreetEdge edge, Vector3 positionOnEdge, out IStreetNode node, out IStreetEdge leftEdge, out IStreetEdge rightEdge);
    }
    
}