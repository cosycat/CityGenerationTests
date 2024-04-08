using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using ExtensionMethods;
using FreeFormGraph;

namespace SplineBased {

    public class StreetNode : IStreetNode {
        private readonly List<Spline> _correspondingSplines = new();

        public List<Spline> CorrespondingSplines => _correspondingSplines;

        public IEnumerable<(Spline spline, int index)> SplineIndices =>
            _correspondingSplines
                .SelectMany(spline => spline.Knots
                    .Select((k, index) => (k, index))
                    .Where(t => Math.Abs(t.k.Position.x - Position.x) < 0.0001f && Math.Abs(t.k.Position.y - Position.y) < 0.0001f)
                    .Select(t => (spline, t.index)))
                    .Distinct();
            

        public Vector3 Position { get; }
        public int MaxConnectedEdges { get; }
        public int ConnectedEdgesCount { get; }
        public List<IStreetEdge> Edges { get; } = new();
        public bool MaxSegmentCountReached => Edges.Count >= MaxConnectedEdges;

        public StreetNode(BezierKnot knot, Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(knot, out _));
            Position = knot.Position;
            _correspondingSplines.Add(spline);
        }

        public StreetNode(Vector3 position) {
            Position = position;
        }

        internal bool AddSegment(StreetSegment segment) {
            if (Edges.Count >= MaxConnectedEdges) {
                return false;
            }

            Edges.Add(segment);
            return true;
        }
        
        public void AddSpline(Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(Position, out _));
            _correspondingSplines.Add(spline);
        }

        public override string ToString() {
            return $"Node at {Position} with {Edges.Count} connected segments{(Edges.Count > 0 ? $": {string.Join(", ", Edges)}" : "")}";
        }
    }
}