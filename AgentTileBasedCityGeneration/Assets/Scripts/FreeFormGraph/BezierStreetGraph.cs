using System.Collections.Generic;
using UnityEngine;

namespace FreeFormGraph {
    
    /// <summary>
    /// A street graph that uses Bezier curves to represent the edges.
    /// </summary>
    public class BezierStreetGraph : StreetGraphGameObject<BezierStreetNode, BezierStreetEdge> {
        [SerializeField] private float snapToExistingNodeThreshold;
        [SerializeField] private float snapToExistingEdgeThreshold;

        public override List<BezierStreetNode> Nodes { get; }
        public override List<BezierStreetEdge> Edges { get; }

        public override float SnapToExistingNodeThreshold {
            get => snapToExistingNodeThreshold;
            set => snapToExistingNodeThreshold = value;
        }

        public override float SnapToExistingEdgeThreshold {
            get => snapToExistingEdgeThreshold;
            set => snapToExistingEdgeThreshold = value;
        }

        public override bool AddEdge(BezierStreetNode from, Vector3 to, out BezierStreetEdge newEdge, out BezierStreetNode toNode) {
            throw new System.NotImplementedException();
        }
        
    }

    public class BezierStreetNode : BaseStreetNode<BezierStreetNode, BezierStreetEdge> {
        
        public Vector3 Position { get; }
        public List<IStreetEdge> Edges { get; }
        public int MaxConnectedEdges => 4;
        
        public override bool AddEdge(BezierStreetEdge edge) {
            throw new System.NotImplementedException();
        }

        public override bool RemoveEdge(BezierStreetEdge edge) {
            throw new System.NotImplementedException();
        }
    }
    
    public class BezierStreetEdge : BaseStreetEdge<BezierStreetNode, BezierStreetEdge> {
        
        public IStreetNode NodeA { get; }
        public IStreetNode NodeB { get; }
        public float StreetWidth { get; }
        
        public override bool IsPointWithinThreshold(Vector3 point, float threshold) {
            throw new System.NotImplementedException();
        }
    }
}