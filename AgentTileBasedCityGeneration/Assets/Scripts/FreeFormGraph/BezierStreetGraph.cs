using System.Collections.Generic;
using UnityEngine;

namespace FreeFormGraph {
    public class BezierStreetGraph : StreetGraphGameObject {
        public override IEnumerable<IStreetNode> Nodes { get; }
        public override IEnumerable<IStreetEdge> Edges { get; }
        public override int NodeCount { get; }
        public override int EdgeCount { get; }
        public override float SnapToExistingNodeThreshold { get; set; }
        public override float SnapToExistingEdgeThreshold { get; set; }
        public override bool AddEdge(Vector3 from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode) {
            throw new System.NotImplementedException();
        }

        public override bool AddEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode) {
            throw new System.NotImplementedException();
        }
    }
}