using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Graph.Bezier {
    public class BezierStreetEdge : IStreetEdge {
        public BezierStreetEdge(BezierStreetNode nodeA, BezierStreetNode nodeB, BezierCurve curve,
            float streetWidth = 0.3f) {
            Debug.Assert(Vector3.Distance(curve.P0, nodeA.Position) < 0.01f,
                $"Distance from {curve.P0} to {nodeA.Position} is {Vector3.Distance(curve.P0, nodeA.Position)}");
            Debug.Assert(Vector3.Distance(curve.P3, nodeB.Position) < 0.01f,
                $"Distance from {curve.P3} to {nodeB.Position} is {Vector3.Distance(curve.P3, nodeB.Position)}");
            this.BezierNodeA = nodeA;
            this.BezierNodeB = nodeB;
            Curve = curve;
            StreetWidth = streetWidth;
        }

        public BezierCurve Curve { get; }

        internal BezierStreetNode BezierNodeA { get; }

        internal BezierStreetNode BezierNodeB { get; }

        internal Vector3 TangentA => Curve.Tangent0;
        internal Vector3 TangentB => Curve.Tangent1;

        public IStreetNode NodeA => BezierNodeA;

        public IStreetNode NodeB => BezierNodeB;

        public float StreetWidth { get; }

        public virtual RoadType Type { get; set; } = RoadType.Primary;

        public Vector3[] SplitIntoEvenlySpacedPoints(out Vector3[] tangents, float stepSize = 0.1f) {
            var distance = CurveUtility.ApproximateLength(Curve);
            var steps = Mathf.CeilToInt(distance / stepSize);
            var step = 1f / steps;
            var points = new Vector3[steps];
            for (var i = 0; i < steps; i++) points[i] = CurveUtility.EvaluatePosition(Curve, i * step);
            tangents = Array.Empty<Vector3>();
            return points;
        }

        public float GetDistanceEdgeToPosition(Vector3 position, out Vector3 positionOnEdge) {
            var minDistance = float.MaxValue;
            positionOnEdge = default;
            foreach (var point in SplitIntoEvenlySpacedPoints(out _)) {
                var distance = Vector3.Distance(point, position);
                if (distance < minDistance) {
                    minDistance = distance;
                    positionOnEdge = point;
                }
            }

            return minDistance;
        }

        public float Length() {
            throw new NotImplementedException();
        }
    }
}