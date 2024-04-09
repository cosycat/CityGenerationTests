using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using FreeFormGraph.SplineBased;
using UnityEngine;
using UnityEngine.Splines;

namespace FreeFormGraph.SplineBased {

    public class SplineStreetNode : IStreetNode {
        private readonly List<Spline> _correspondingSplines = new();
        private readonly List<SplineStreetSegment> _edges = new();

        public List<Spline> CorrespondingSplines => _correspondingSplines;

        public IEnumerable<(Spline spline, int index)> SplineIndices =>
            _correspondingSplines
                .SelectMany(spline => spline.Knots
                    .Select((k, index) => (k, index))
                    .Where(t => Math.Abs(t.k.Position.x - Position.x) < 0.0001f && Math.Abs(t.k.Position.y - Position.y) < 0.0001f)
                    .Select(t => (spline, t.index)))
                    .Distinct();
            

        public Vector3 Position { get; }
        public int MaxConnectedEdges => 4;
        
        // TODO this is just a placeholder:
        public float? EntranceAngle => Vector3.Angle(Vector3.right, SplineIndices.First().spline[SplineIndices.First().index].TangentIn);
        
        public int ConnectedEdgesCount => _edges.Count;

        public IEnumerable<IStreetEdge> Edges => _edges;

        public bool MaxSegmentCountReached => _edges.Count >= MaxConnectedEdges;

        public SplineStreetNode(BezierKnot knot, Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(knot, out _));
            Position = knot.Position;
            _correspondingSplines.Add(spline);
        }

        public SplineStreetNode(Vector3 position) {
            Position = position;
        }

        internal bool AddSegment(SplineStreetSegment segment) {
            if (_edges.Count >= MaxConnectedEdges) {
                return false;
            }

            _edges.Add(segment);
            return true;
        }
        
        public void AddSpline(Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(Position, out _));
            _correspondingSplines.Add(spline);
        }

        public override string ToString() {
            return $"Node at {Position} with {_edges.Count} connected segments{(_edges.Count > 0 ? $": {string.Join(", ", Edges)}" : "")}";
        }

        public bool RemoveSegment(SplineStreetSegment segment) {
            return _edges.Remove(segment);
        }
    }
}