#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Graph.LineBased {
    public class LineEdge : IStreetEdge {
        public LineEdge(IStreetNode nodeA, IStreetNode nodeB, float streetWidth, RoadType roadType) {
            NodeA = nodeA;
            NodeB = nodeB;
            StreetWidth = streetWidth;
            Type = roadType;
        }

        public Vector3 Position => PositionNodeA + (PositionNodeB - PositionNodeA) / 2.0f;
        public float Radius => Vector3.Distance(PositionNodeB, PositionNodeA) / 2.0f;
        public Vector3 PositionNodeA => NodeA.Position;
        public Vector3 PositionNodeB => NodeB.Position;

        public IStreetNode NodeA { get; }
        public IStreetNode NodeB { get; }
        public float StreetWidth { get; set; }

        public RoadType Type { get; set; } = RoadType.Primary;

        public Vector3[] SplitIntoEvenlySpacedPoints(out Vector3[] tangents, float stepSize = 0.1f) {
            var points = new List<Vector3>();
            var direction = NodeB.Position - NodeA.Position;
            var distance = direction.magnitude;
            var steps = Mathf.CeilToInt(distance / stepSize);
            var step = direction / steps;
            for (var i = 0; i < steps; i++) points.Add(NodeA.Position + i * step);
            points.Add(NodeB.Position);
            tangents = new Vector3[points.Count];
            var directionNormalized = direction.normalized;
            for (var i = 0; i < points.Count; i++) tangents[i] = directionNormalized;
            return points.ToArray();
        }

        public float GetDistanceEdgeToPosition(Vector3 position, out Vector3 positionOnEdge) {
            positionOnEdge = SplineMath.PointLineNearestPoint(position, NodeA.Position, NodeB.Position, out _);
            return Vector3.Distance(position, positionOnEdge);
        }

        public float Length() {
            return Vector3.Distance(PositionNodeA, PositionNodeB);
        }

        public override string ToString() {
            return $"LineEdge from {PositionNodeA} to {PositionNodeB}";
        }
    }
}