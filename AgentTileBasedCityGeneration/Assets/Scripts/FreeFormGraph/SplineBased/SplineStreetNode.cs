using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;
using UnityEngine.Splines;

namespace FreeFormGraph.SplineBased {

    public class SplineStreetNode : IStreetNode {
        private readonly List<Spline> correspondingSplines = new();
        private readonly List<SplineStreetSegment> edges = new();
        
        /// <summary>
        /// A distinct list of all splines that contain this node.
        /// </summary>
        public List<Spline> CorrespondingSplines => correspondingSplines;

        public IEnumerable<(Spline spline, int index)> SplineIndices =>
            correspondingSplines
                .SelectMany(spline => spline.Knots
                    .Select((k, index) => (k, index))
                    .Where(t => Math.Abs(t.k.Position.x - Position.x) < 0.0001f && Math.Abs(t.k.Position.y - Position.y) < 0.0001f)
                    .Select(t => (spline, t.index)))
                    .Distinct();
            

        public Vector3 Position { get; }
        public int MaxConnectedEdges => 4;
        
        // TODO this is just a placeholder:
        public float? EntranceAngle => correspondingSplines.Count == 0 ? null : Vector3.Angle(Vector3.right, SplineIndices.First().spline[SplineIndices.First().index].TangentIn);
        
        public int ConnectedEdgesCount => edges.Count;

        public IEnumerable<IStreetEdge> Edges => edges;

        public bool MaxSegmentCountReached => edges.Count >= MaxConnectedEdges;

        public SplineStreetNode(BezierKnot knot, Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(knot, out _));
            Position = knot.Position;
            correspondingSplines.Add(spline);
        }

        public SplineStreetNode(Vector3 position) {
            Position = position;
        }

        internal bool AddSegment(SplineStreetSegment segment) {
            if (edges.Count >= MaxConnectedEdges) {
                return false;
            }

            edges.Add(segment);
            return true;
        }
        
        public void AddSpline(Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(Position, out _));
            if (!correspondingSplines.Contains(spline))
                correspondingSplines.Add(spline);
        }
        
        public void RemoveSpline(Spline spline) {
            correspondingSplines.Remove(spline);
        }

        public override string ToString() {
            return $"Node at {Position} with {edges.Count} connected segments{(edges.Count > 0 ? $": {string.Join(", ", Edges)}" : "")}";
        }

        public bool RemoveSegment(SplineStreetSegment segment) {
            return edges.Remove(segment);
        }
    }
}