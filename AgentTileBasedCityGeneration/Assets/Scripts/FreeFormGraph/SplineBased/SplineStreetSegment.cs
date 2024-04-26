using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Splines;
using Utils;

namespace FreeFormGraph.SplineBased {

    public class SplineStreetSegment : IStreetEdge {
        
        private readonly SplineStreetNode nodeA;
        private readonly SplineStreetNode nodeB;
        
        public IStreetNode NodeA => nodeA;
        public IStreetNode NodeB => nodeB;
        
        public Spline Spline { get; internal set; }
        
        public BezierCurve Curve => Spline.GetCurve(GetIndices().lowerIndex);

        public float StreetWidth { get; } = 0.3f;
        
        public Vector3[] SplitIntoEvenlySpacedPoints(float stepSize = 0.1f) {
            return Curve.CalculateEvenlySpacedPoints(stepSize);
        }

        private SplineStreetSegment(SplineStreetNode nodeA, SplineStreetNode nodeB) {
            this.nodeA = nodeA;
            this.nodeB = nodeB;
        }
        
        
        /// <summary>
        /// Returns the indices of the spline knots that are connected by this segment.
        ///
        /// The startIndex is always the lower index of the two.
        /// </summary>
        /// <returns> The indices of the spline knots that are connected by this segment. </returns>
        public (int lowerIndex, int higherIndex) GetIndices() {
            var nodeASplineIndices = nodeA.SplineIndices.Where(t => t.spline == Spline);
            var nodeBSplineIndices = nodeB.SplineIndices.Where(t => t.spline == Spline);
            var indicesOneApart = nodeASplineIndices
                .SelectMany(splineIndexA => nodeBSplineIndices
                    .Select(splineIndexB => (splineIndexA, splineIndexB)))
                .Where(t => t.splineIndexA.index - t.splineIndexB.index is 1 or -1).ToArray();
            Debug.Assert(indicesOneApart.Length == 1, $"Expected exactly one pair of indices one apart, but found {indicesOneApart.Length} on {this}.");
            return indicesOneApart[0].splineIndexA.index < indicesOneApart[0].splineIndexB.index
                ? (indicesOneApart[0].splineIndexA.index, indicesOneApart[0].splineIndexB.index)
                : (indicesOneApart[0].splineIndexB.index, indicesOneApart[0].splineIndexA.index);
        }

        /// <summary>
        /// Generates a new StreetSegment between two nodes and adds it to the nodes.
        /// Sets the spline of the segment to the given spline if <paramref name="existingSpline"/> is not null.
        /// 
        /// Does not change splines, this has to be done manually before or after calling this method.
        /// (If done before, make sure to remove it again if this method returns false)
        /// </summary>
        /// <param name="from"> The first node </param>
        /// <param name="to"> The second node </param>
        /// <param name="existingSpline"> The spline that should be used for the segment, or null if a new spline will be generated and set afterwards </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <returns> True if the segment was added successfully, false otherwise </returns>
        internal static bool GenerateStreetSegment(SplineStreetNode from, SplineStreetNode to, [CanBeNull] Spline existingSpline, out SplineStreetSegment newSegment) {
            Debug.Log($"Generating segment from {from} to {to}");
            if (from.Edges.Any(edge => edge.NodeA == to || edge.NodeB == to)) {
                // if there is already a direct segment between the two nodes, return false
                newSegment = null;
                Debug.LogError("There is already a segment between the two nodes.");
                return false;
            }
            if (from == to) {
                // if the two nodes are the same, return false
                Debug.LogError("Cannot create a segment between the same node.");
                newSegment = null;
                return false;
            }
            
            newSegment = new SplineStreetSegment(from, to);
            if (!from.AddSegment(newSegment) || !to.AddSegment(newSegment)) {
                from.RemoveSegment(newSegment);
                to.RemoveSegment(newSegment); // probably not needed, but to be sure
                newSegment = null;
                Debug.LogWarning("Failed to add segment to nodes.");
                return false;
            }

            if (existingSpline != null) {
                newSegment.Spline = existingSpline;
            }

            return true;
        }
        
        public float GetDistanceEdgeToPosition(Vector3 position, out Vector3 positionOnEdge) {
            var evenlySpacedPoints = SplitIntoEvenlySpacedPoints();
            var closestPoint = Vector3.zero;
            var secondClosestPoint = Vector3.zero;
            var closestDistance = float.MaxValue;
            var secondClosestDistance = float.MaxValue;
            
            foreach (var point in evenlySpacedPoints) {
                var distance = Vector3.Distance(point, position);
                if (distance < closestDistance) {
                    secondClosestDistance = closestDistance;
                    closestDistance = distance;
                    secondClosestPoint = closestPoint;
                    closestPoint = point;
                }
                else if (distance < secondClosestDistance) {
                    secondClosestDistance = distance;
                    secondClosestPoint = point;
                }
            }

            var nearestPointOnLine = SplineMath.PointLineNearestPoint(position, closestPoint, secondClosestPoint, out var lineParameter);
            if (lineParameter < 0) {
                nearestPointOnLine = closestPoint;
            }
            else if (lineParameter > 1) {
                nearestPointOnLine = secondClosestPoint;
            }
            // Debug.Assert(lineParameter is >= 0 and <= 1, $"Line parameter is {lineParameter}");
            positionOnEdge = nearestPointOnLine;
            return Vector3.Distance(position, nearestPointOnLine);
        }

        public override string ToString() {
            return $"Segment from {NodeA.Position} to {NodeB.Position}";
        }
    }
}