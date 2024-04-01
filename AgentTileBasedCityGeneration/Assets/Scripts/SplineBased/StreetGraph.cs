using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Splines;

namespace SplineBased {
    public class StreetGraph : MonoBehaviour {

        public List<StreetNode> Nodes { get; } = new();
        public List<StreetSegment> Edges { get; } = new();

        private SplineContainer _splineContainer;
        private SplineExtrude _splineExtrude;

        private void Awake() {
            _splineContainer = GetComponentInChildren<SplineContainer>();
            _splineExtrude = GetComponentInChildren<SplineExtrude>();
            GenerateNewUnconnectedNode(Vector3.zero, out var newNode);
        }

        /// <summary>
        /// Adds a Street Segment between two existing nodes.
        /// </summary>
        /// <param name="from"> The first node </param>
        /// <param name="to"> The second node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <returns> True if the segment was added successfully, false otherwise </returns>
        public bool AddNewSegment(StreetNode from, StreetNode to, out StreetSegment newSegment) {
            if (!StreetSegment.GenerateStreetSegment(from, to, out newSegment)) {
                return false;
            }
            Edges.Add(newSegment);
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Adds a new Node and a Street Segment between the new node and an existing node.
        /// </summary>
        /// <param name="from"> The existing node </param>
        /// <param name="to"> The position of the new node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <param name="newNode"> The newly generated node </param>
        /// <returns> True if the Node and the Segment was added successfully, false otherwise </returns>
        public bool AddNewSegment(StreetNode from, Vector3 to, out StreetSegment newSegment, out StreetNode newNode) {
            if (!GenerateAndConnectNewNode(from, to, out newNode, out newSegment)) {
                return false;
            }
            Edges.Add(newSegment);
            _splineExtrude.Rebuild();
            return true;
        }

        /// <summary>
        /// Creates a new Node at the given position and connects it to the given existing node.
        /// If the existing node is at the beginning or end of a spline, a new knot is added to that spline.
        /// Otherwise, a new spline is created.
        /// </summary>
        /// <param name="from"> The existing node </param>
        /// <param name="to"> The position of the new node </param>
        /// <param name="newNode"> The newly generated node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <returns> True if the Node was added successfully, false otherwise </returns>
        private bool GenerateAndConnectNewNode(StreetNode from, Vector3 to, out StreetNode newNode, out StreetSegment newSegment) {
            if (from.MaxSegmentCountReached) {
                newNode = null;
                newSegment = null;
                return false;
            }

            foreach (var (knot, spline) in from._correspondingKnots) {
                if (spline.IndexOf(knot) > 0 && spline.IndexOf(knot) < spline.Count - 1) {
                    continue;
                }

                // there is a knot at the beginning or end of the spline. we can just add a new knot to this spline.
                var newKnot = new BezierKnot(to);
                if (spline.IndexOf(knot) == 0) {
                    spline.Insert(0, newKnot, TangentMode.Mirrored, 1); // TODO check if AutoSmooth would be better.
                }
                else if (spline.IndexOf(knot) == spline.Count - 1) { // TODO check if AutoSmooth would be better.
                    spline.Add(newKnot, TangentMode.Mirrored, 1);
                }
                else {
                    Debug.LogError(
                        "This should not happen. The knot is not at the beginning, end or middle of the spline.");
                    continue;
                }

                Nodes.Add(newNode = new StreetNode(newKnot, spline));
                if (!StreetSegment.GenerateStreetSegment(from, newNode, out newSegment)) {
                    // TODO: remove the new knot from the spline
                    throw new NotImplementedException("Remove the new knot from the spline");
                    return false;
                }

                return true;
            }

            // all the knots are in the middle of a spline. we need to create a new spline.
            if (!GenerateNewUnconnectedNode(to, out newNode)) {
                newSegment = null;
                return false;
            }

            if (!StreetSegment.GenerateStreetSegment(from, newNode, out newSegment)) {
                return false;
            }

            AddSplineForSegment(newSegment);
            return true;
        }

        private bool GenerateNewUnconnectedNode(Vector3 position, out StreetNode newNode) {
            if (!IsPositionValidForNewKnot(position)) {
                newNode = null;
                return false;
            }

            var spline = _splineContainer.AddSpline();
            var knot = new BezierKnot(position);
            spline.Add(knot);
            Nodes.Add(newNode = new StreetNode(knot, spline));
            return true;
        }

        private bool IsPositionValidForNewKnot(Vector3 position) {
            // TODO: check if the position is not too close to an existing knot
            return true;
        }

        private void AddSplineForSegment(StreetSegment newSegment) {
            var spline = _splineContainer.AddSpline();
            spline.Add(new BezierKnot(newSegment.From.Position)); // TODO set correct tangent rotations for the knots
            spline.Add(new BezierKnot(newSegment.To.Position));
        }

        public StreetNode FindClosestNode(Vector3 pos) {
            if (Nodes.Count == 0) {
                Debug.LogWarning("No nodes in the graph. Should not happen.");
                return null;
            }

            var closestNode = Nodes[0];
            var closestDistance = Vector3.Distance(pos, closestNode.Position);
            foreach (var node in Nodes) {
                var distance = Vector3.Distance(pos, node.Position);
                if (distance < closestDistance) {
                    closestNode = node;
                    closestDistance = distance;
                }
            }

            return closestNode;
        }
    }

    public class StreetSegment {
        public StreetNode From { get; }
        public StreetNode To { get; }

        private StreetSegment(StreetNode from, StreetNode to) {
            From = from;
            To = to;
        }

        internal static bool GenerateStreetSegment(StreetNode from, StreetNode to, out StreetSegment newSegment) {
            newSegment = new StreetSegment(from, to);
            if (!from.AddSegment(newSegment) || !to.AddSegment(newSegment)) {
                from.ConnectedSegments.Remove(newSegment);
                to.ConnectedSegments.Remove(newSegment); // probably not needed, but to be sure
                return false;
            }

            return true;
        }
        
        public override string ToString() {
            return $"Segment from {From.Position} to {To.Position}";
        }
    }

    public class StreetNode {
        private const int MaxConnectedSegments = 4;
        public readonly List<(BezierKnot knot, Spline spline)> _correspondingKnots = new();

        public Vector3 Position { get; }
        public List<StreetSegment> ConnectedSegments { get; } = new();
        public bool MaxSegmentCountReached => ConnectedSegments.Count >= MaxConnectedSegments;

        public StreetNode(BezierKnot knot, Spline spline) {
            Position = knot.Position;
            _correspondingKnots.Add((knot, spline));
        }

        internal bool AddSegment(StreetSegment segment) {
            if (ConnectedSegments.Count >= MaxConnectedSegments) {
                return false;
            }

            ConnectedSegments.Add(segment);
            return true;
        }

        public override string ToString() {
            return $"Node at {Position} with {ConnectedSegments.Count} connected segments: {string.Join(", ", ConnectedSegments)}";
        }
    }
}
    
    