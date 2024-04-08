using System.Collections.Generic;
using UnityEngine;

namespace FreeFormGraph.Bezier {
    public class BezierStreetGraph : StreetGraphGameObject {
        private readonly List<BezierStreetEdge> _edges = new();
        private readonly List<BezierStreetNode> _nodes = new();
        public override IEnumerable<IStreetNode> Nodes => _nodes;
        public override IEnumerable<IStreetEdge> Edges => _edges;
        public override int NodeCount => _nodes.Count;
        public override int EdgeCount => _edges.Count;
        public override float SnapToExistingNodeThreshold { get; set; } = 0.1f;
        public override float SnapToExistingEdgeThreshold { get; set; } = 0.1f;
        public override bool AddEdge(Vector3 from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode) {
            throw new System.NotImplementedException();
        }

        public override bool AddEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode) {
            throw new System.NotImplementedException();
        }
    }

    public class BezierStreetNode : IStreetNode {
        private readonly List<BezierStreetEdge> _edges = new();
        public Vector3 Position { get; }

        public IEnumerable<IStreetEdge> Edges => _edges;

        public int ConnectedEdgesCount { get; }
        public int MaxConnectedEdges { get; }
    }
    
    public class BezierStreetEdge : IStreetEdge {
        private readonly BezierStreetNode _nodeA;
        private readonly BezierStreetNode _nodeB;

        public IStreetNode NodeA => _nodeA;

        public IStreetNode NodeB => _nodeB;

        public float StreetWidth { get; }
        

        public BezierStreetEdge(float streetWidth = 0.3f) {
            StreetWidth = streetWidth;
        }
    }
}