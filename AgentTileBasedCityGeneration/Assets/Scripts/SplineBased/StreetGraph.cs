using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
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
            GenerateNewUnconnectedNode(Vector3.zero, out var newNode, true);
        }

        /// <summary>
        /// Adds a Street Segment between two existing nodes.
        /// </summary>
        /// <param name="from"> The first node </param>
        /// <param name="to"> The second node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <returns> True if the segment was added successfully, false otherwise </returns>
        public bool AddNewSegment(StreetNode from, StreetNode to, out StreetSegment newSegment) {
            Debug.Log($"Adding new segment between {from} to {to}");
            if (!StreetSegment.GenerateStreetSegment(from, to, out newSegment)) {
                return false;
            }
            if (!AddSplineForSegment(newSegment, out var newSpline)) {
                // TODO: remove the segment from the nodes
                throw new NotImplementedException("Remove the segment from the nodes");
                return false;
            }

            from.AddSpline(newSpline);
            to.AddSpline(newSpline);
            Edges.Add(newSegment);
            _splineExtrude.Rebuild();
            return true;
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
            Debug.Log($"Adding new segment from {from} to new Node at {to}");
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
            Debug.Log($"GenerateAndConnectNewNode from {from} to new position {to}");
            if (from.MaxSegmentCountReached) {
                newNode = null;
                newSegment = null;
                return false;
            }

            foreach (var (spline, index) in from.SplineIndices) {
                Debug.Assert(spline.Count > index
                             && Math.Abs(spline[index].Position.x - from.Position.x) < 0.0001f && Math.Abs(spline[index].Position.y - from.Position.y) < 0.0001f
                             && spline.ContainsKnotPos(from.Position, out _)); // Two times the same, but to be sure
                if (index > 0 && index < spline.Count - 1) {
                    continue;
                }

                // there is a knot at the beginning or end of the spline. we can just add a new knot to this spline.
                var newKnot = new BezierKnot(to) {
                    Rotation = Quaternion.LookRotation(from.Position - to, Vector3.forward)
                };
                if (index == 0) {
                    spline.Insert(0, newKnot, TangentMode.AutoSmooth); // TODO check if AutoSmooth would be better.
                }
                else if (index == spline.Count - 1) { // TODO check if AutoSmooth would be better.
                    spline.Add(newKnot, TangentMode.AutoSmooth);
                }
                else {
                    Debug.LogError(
                        $"This should not happen. The knot is not at the beginning, end or middle of the spline.");
                    throw new Exception("This should not happen. The knot is not at the beginning, end or middle of the spline.");
                    continue;
                }
                
                from.AddSpline(spline);
                Nodes.Add(newNode = new StreetNode(newKnot, spline));
                if (!StreetSegment.GenerateStreetSegment(from, newNode, out newSegment)) {
                    // TODO: remove the new knot from the spline
                    throw new NotImplementedException("Remove the new knot from the spline");
                    return false;
                }

                return true;
            }

            // all the knots are in the middle of a spline. we need to create a new spline.
            if (!GenerateNewUnconnectedNode(to, out newNode, false)) { // TODO: FIX THIS, this creates a new spline with a single knot and afterwards creates another new spline.
                newSegment = null;
                return false;
            }

            if (!StreetSegment.GenerateStreetSegment(from, newNode, out newSegment)) {
                // TODO: remove the new node from the nodes
                throw new NotImplementedException("Remove the new node from the nodes");
                return false;
            }
            if (!AddSplineForSegment(newSegment, out var newSpline)) {
                // TODO: remove the new node and segments
                throw new NotImplementedException("Remove the new node and segments");
                return true;
            }
            
            return true;
        }

        private bool GenerateNewUnconnectedNode(Vector3 position, out StreetNode newNode, bool generateSplineAndKnot) {
            Debug.Log($"Generating new unconnected node at {position}");
            if (!IsPositionValidForNewKnot(position)) {
                newNode = null;
                return false;
            }

            if (generateSplineAndKnot) {
                var spline = _splineContainer.AddSpline();
                var knot = new BezierKnot(position) {
                    Rotation = Quaternion.Euler(270, 0, 0) // TODO set correct rotation
                };
                spline.Add(knot);
                newNode = new StreetNode(knot, spline);
            }
            else {
                newNode = new StreetNode(position);
            }
            Nodes.Add(newNode);

            return true;
        }

        private bool IsPositionValidForNewKnot(Vector3 position) {
            // TODO: check if the position is not too close to an existing knot
            return true;
        }

        private bool AddSplineForSegment(StreetSegment newSegment, out Spline newSpline) {
            Debug.Log("Creating new Spline for segment");
            newSpline = _splineContainer.AddSpline();
            var fromKnot = new BezierKnot(newSegment.From.Position) {
                Rotation = Quaternion.LookRotation(newSegment.To.Position - newSegment.From.Position, Vector3.forward)
            };
            var toKnot = new BezierKnot(newSegment.To.Position) {
                Rotation = Quaternion.LookRotation(newSegment.From.Position - newSegment.To.Position, Vector3.forward)
            };
            newSpline.Add(fromKnot); // TODO set correct tangent rotations for the knots
            newSpline.Add(toKnot);
            
            // TODO check if the spline is valid
            
            newSegment.From.AddSpline(newSpline);
            newSegment.To.AddSpline(newSpline);
            return true;
        }

        public StreetNode FindClosestNode(Vector2 pos) {
            if (Nodes.Count == 0) {
                Debug.LogWarning("No nodes in the graph. Should not happen.");
                return null;
            }

            var closestNode = Nodes[0];
            var closestDistance = Vector2.Distance(pos, closestNode.Position);
            foreach (var node in Nodes) {
                var distance = Vector2.Distance(pos, node.Position);
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

        /// <summary>
        /// Generates a new StreetSegment between two nodes and adds it to the nodes.
        ///
        /// Does not change splines, this has to be done manually before or after calling this method.
        /// (If done before, make sure to remove it again if this method returns false)
        /// </summary>
        /// <param name="from"> The first node </param>
        /// <param name="to"> The second node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <returns> True if the segment was added successfully, false otherwise </returns>
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
        private readonly List<Spline> _correspondingSplines = new();

        public IEnumerable<(Spline spline, int index)> SplineIndices =>
            _correspondingSplines
                .SelectMany(spline => spline.Knots
                    .Select((k, index) => (k, index))
                    .Where(t => Math.Abs(t.k.Position.x - Position.x) < 0.0001f && Math.Abs(t.k.Position.y - Position.y) < 0.0001f)
                    .Select(t => (spline, t.index)));
            

        public Vector3 Position { get; }
        public List<StreetSegment> ConnectedSegments { get; } = new();
        public bool MaxSegmentCountReached => ConnectedSegments.Count >= MaxConnectedSegments;

        public StreetNode(BezierKnot knot, Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(knot, out _));
            Position = knot.Position;
            _correspondingSplines.Add(spline);
        }

        public StreetNode(Vector3 position) {
            Position = position;
        }

        internal bool AddSegment(StreetSegment segment) {
            if (ConnectedSegments.Count >= MaxConnectedSegments) {
                return false;
            }

            ConnectedSegments.Add(segment);
            return true;
        }
        
        public void AddSpline(Spline spline) {
            Debug.Assert(spline.ContainsKnotPos(Position, out _));
            _correspondingSplines.Add(spline);
        }

        public override string ToString() {
            return $"Node at {Position} with {ConnectedSegments.Count} connected segments{(ConnectedSegments.Count > 0 ? $": {string.Join(", ", ConnectedSegments)}" : "")}";
        }
    }
    
}



namespace ExtensionMethods {
    
    public static class SplineExtensions {
        
        public static bool ContainsKnotPos(this Spline spline, Vector3 pos, out int index, float tolerance = 0.0001f) {
            for (int i = 0; i < spline.Count; i++) {
                if (Math.Abs(spline[i].Position.x - pos.x) < tolerance && Math.Abs(spline[i].Position.y - pos.y) < tolerance) {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public static bool ContainsKnotPos(this Spline spline, BezierKnot knot, out int index, float tolerance = 0.0001f) {
            return ContainsKnotPos(spline, knot.Position, out index, tolerance);
        }
        
    }
    
}
    
    