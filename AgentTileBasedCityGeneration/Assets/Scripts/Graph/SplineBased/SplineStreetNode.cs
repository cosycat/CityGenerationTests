using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;
using UnityEngine.Splines;

namespace Graph.SplineBased {
    public class SplineStreetNode : IStreetNode {
        private readonly List<SplineStreetSegment> edges = new();

        public SplineStreetNode(BezierKnot knot, Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(knot, out _));
            Position = knot.Position;
            CorrespondingSplines.Add(spline);
        }

        public SplineStreetNode(Vector3 position) {
            Position = position;
        }

        /// <summary>
        ///     A distinct list of all splines that contain this node.
        /// </summary>
        public List<Spline> CorrespondingSplines { get; } = new();

        public IEnumerable<(Spline spline, int index)> SplineIndices =>
            CorrespondingSplines
                .SelectMany(spline => spline.Knots
                    .Select((k, index) => (k, index))
                    .Where(t => Math.Abs(t.k.Position.x - Position.x) < 0.0001f &&
                                Math.Abs(t.k.Position.y - Position.y) < 0.0001f)
                    .Select(t => (spline, t.index)))
                .Distinct();

        // TODO this is just a placeholder:
        public float? EntranceAngle => CorrespondingSplines.Count == 0
            ? null
            : Vector3.Angle(Vector3.right, SplineIndices.First().spline[SplineIndices.First().index].TangentIn);

        public bool MaxSegmentCountReached => edges.Count >= MaxConnectedEdges;


        public Vector3 Position { get; }
        public int MaxConnectedEdges => 4;

        public float MinAngleBetweenEdges { get; }

        public int ConnectedEdgesCount => edges.Count;

        public IEnumerable<IStreetEdge> Edges => edges;

        internal bool AddSegment(SplineStreetSegment segment) {
            if (edges.Count >= MaxConnectedEdges) return false;

            edges.Add(segment);
            return true;
        }

        public void AddSpline(Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(Position, out _));
            if (!CorrespondingSplines.Contains(spline))
                CorrespondingSplines.Add(spline);
        }

        public void RemoveSpline(Spline spline) {
            CorrespondingSplines.Remove(spline);
        }

        public override string ToString() {
            return
                $"Node at {Position} with {edges.Count} connected segments{(edges.Count > 0 ? $": {string.Join(", ", Edges)}" : "")}";
        }

        public bool RemoveSegment(SplineStreetSegment segment) {
            return edges.Remove(segment);
        }
    }
}