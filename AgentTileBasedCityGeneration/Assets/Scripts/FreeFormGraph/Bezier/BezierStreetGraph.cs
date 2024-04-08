using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

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
        

        public override bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew) {
            throw new System.NotImplementedException();
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            var closestNodeInRange = FindClosestNode(position, SnapToExistingNodeThreshold);
            if (closestNodeInRange != null) {
                newNode = null;
                return false;
            }
            throw new System.NotImplementedException();
        }
        
        public override bool RemoveNode(IStreetNode node) {
            Debug.Assert(node is BezierStreetNode);
            var bezierNode = (BezierStreetNode) node;
            if (!_nodes.Remove(bezierNode)) {
                return false;
            }
            foreach (var edge in bezierNode.BezierEdges) {
                Debug.Assert(edge.NodeA == bezierNode || edge.NodeB == bezierNode);
                if (!_edges.Remove(edge)) {
                    Debug.LogError($"Failed to remove edge {edge} from graph, but node was in graph and removed.");
                }
                if (edge.BezierNodeA == bezierNode) {
                    edge.BezierNodeB.RemoveEdge(edge);
                } else {
                    edge.BezierNodeA.RemoveEdge(edge);
                }
            }
            // TODO: check if this needs anything else.
            return true;
        }
    }

    public class BezierStreetNode : IStreetNode {
        private readonly List<BezierStreetEdge> _edges = new();
        public Vector3 Position { get; }
        
        internal IEnumerable<BezierStreetEdge> BezierEdges => _edges;

        public IEnumerable<IStreetEdge> Edges => _edges;

        public int ConnectedEdgesCount => _edges.Count;

        public int MaxConnectedEdges => 4;

        public BezierStreetNode(Vector3 position) {
            Position = position;
        }

        internal void RemoveEdge(BezierStreetEdge edge) {
            _edges.Remove(edge);
        }
    }
    
    public class BezierStreetEdge : IStreetEdge {
        private readonly BezierStreetNode _nodeA;
        private readonly BezierStreetNode _nodeB;

        public BezierCurve Curve { get; }
        
        internal BezierStreetNode BezierNodeA => _nodeA;
        internal BezierStreetNode BezierNodeB => _nodeB;

        public IStreetNode NodeA => _nodeA;

        public IStreetNode NodeB => _nodeB;

        public float StreetWidth { get; }
        

        public BezierStreetEdge(BezierStreetNode nodeA, BezierStreetNode nodeB, BezierCurve curve, float streetWidth = 0.3f) {
            _nodeA = nodeA;
            _nodeB = nodeB;
            Curve = curve;
            StreetWidth = streetWidth;
        }
    }
}