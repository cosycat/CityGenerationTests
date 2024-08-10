using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExtensionMethods;
using Graph;
using Graph.SplineBased;
using UnityEngine;
using UnityEngine.Splines;
using Debug = UnityEngine.Debug;

namespace FreeFormGraph.SplineBased {
    public class SplineStreetGraph : StreetGraphGameObject {
        [SerializeField] private bool snapToGrid = true;

        public new GameObject gameObject;
        private readonly List<SplineStreetSegment> edges = new();
        private readonly List<SplineStreetNode> nodes = new();

        private SplineContainer splineContainer;
        private SplineExtrude splineExtrude;

        public SplineStreetGraph(GameObject gameObject) {
            this.gameObject = gameObject;
            splineContainer = gameObject.GetComponentInChildren<SplineContainer>();
            splineExtrude = gameObject.GetComponentInChildren<SplineExtrude>();
            Debug.Assert(splineContainer != null);
            Debug.Assert(splineExtrude != null);
        }

        public override IEnumerable<IStreetNode> Nodes => nodes;

        public override IEnumerable<IStreetEdge> Edges => edges;

        public override int NodeCount => nodes.Count;
        public override int EdgeCount => edges.Count;
        public override float SnapToExistingNodeThreshold { get; set; }
        public override float SnapToExistingEdgeThreshold { get; set; }


        private void Awake() {
            splineContainer = gameObject.GetComponentInChildren<SplineContainer>();
            splineExtrude = gameObject.GetComponentInChildren<SplineExtrude>();
            GenerateNewUnconnectedNode(Vector3.zero, out _, true, out var _, out var _);
        }

        public override bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge, out bool isEdgeNew,
            RoadType roadType) {
            var res = AddNewSegment((SplineStreetNode)from, (SplineStreetNode)to, out var newSegment);
            newEdge = newSegment;
            isEdgeNew = res;
            return res;
        }

        public override bool TryRemoveNode(IStreetNode node) {
            Debug.Assert(node is SplineStreetNode);
            var splineNode = (SplineStreetNode)node;
            if (node.ConnectedEdgesCount == 0) {
                nodes.Remove(splineNode);
                Debug.Assert(splineNode.CorrespondingSplines.Count <= 1,
                    $"Too many splines for unconnected node {splineNode}");
                foreach (var spline in splineNode.CorrespondingSplines) // for loop just to be sure.
                    splineContainer.RemoveSpline(spline);
                return true;
            }

            throw new NotImplementedException($"Remove connected node {node}");
        }

        /// <summary>
        ///     Adds a Street Segment between two existing nodes.
        /// </summary>
        /// <param name="from"> The first node </param>
        /// <param name="to"> The second node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <param name="failOnIntersection"> Whether to fail if an intersection is detected </param>
        /// <returns> True if the segment was added successfully, false otherwise </returns>
        public bool AddNewSegment(SplineStreetNode from, SplineStreetNode to, out SplineStreetSegment newSegment,
            bool failOnIntersection = false) {
            Debug.Log($"Adding new segment between {from} to {to}");

            var fromConnectionCount = from.ConnectedEdgesCount;
            var toConnectionCount = to.ConnectedEdgesCount;

            if (!SplineStreetSegment.GenerateStreetSegment(from, to, null, out newSegment)) return false;

            if ((fromConnectionCount <= 1 && toConnectionCount == 1) ||
                (fromConnectionCount == 1 && toConnectionCount <= 1)) {
                Debug.Log("Connecting to existing spline");
                var (nodeToConnect, nodeSplineToUse) = fromConnectionCount == 1 ? (to, from) : (from, to);
                Debug.Assert(nodeSplineToUse.CorrespondingSplines.Count == 1,
                    $"Too many splines for node {nodeSplineToUse}");
                var (existingSpline, nodeSplineToUseIndex) = nodeSplineToUse.SplineIndices.First();
                Debug.Assert(nodeSplineToUseIndex == 0 || nodeSplineToUseIndex == existingSpline.Count - 1,
                    $"Index is {nodeSplineToUseIndex}, but should be 0 or {existingSpline.Count - 1}");
                // Add the new knot to the existing spline at the end or start (where the nodeSplineToUse is)
                existingSpline.Insert(nodeSplineToUseIndex == 0 ? 0 : existingSpline.Count,
                    new BezierKnot(nodeToConnect.Position), TangentMode.AutoSmooth);
                nodeToConnect.AddSpline(existingSpline);
                newSegment.Spline = existingSpline;
            }
            else {
                Debug.Log("Creating new spline for segment");
                if (!AddSplineForSegment(newSegment, out var newSpline))
                    // TODO: remove the segment from the nodes
                    throw new NotImplementedException("Remove the segment from the nodes");
                //return false;
                newSegment.Spline = newSpline;
                from.AddSpline(newSpline);
                to.AddSpline(newSpline);
            }

            edges.Add(newSegment);

            CleanupEmptySplines(from);
            CleanupEmptySplines(to);

            // test for intersections and split up the spline if necessary
            if (SplineIntersections.HasIntersection(newSegment, edges, out var intersection)) {
                if (failOnIntersection) {
                    edges.Remove(newSegment);
                    from.RemoveSegment(newSegment);
                    CleanupEmptySplines(from);
                    to.RemoveSegment(newSegment);
                    CleanupEmptySplines(to);
                    return false;
                }

                // 1. add new knot to the spline
                var existingSegment = intersection.ExistingSegment;
                var existingSpline = existingSegment.Spline;
                var newKnot = new BezierKnot(intersection.IntersectionPoint) {
                    Rotation = Quaternion.LookRotation(intersection.ExistingTangentAtIntersection, Vector3.forward)
                };
                var index = existingSegment.GetIndices().lowerIndex;
                existingSpline.Insert(index + 1, newKnot, TangentMode.AutoSmooth);
                // if (spline.Count == intersection.ExistingBezierIndex + 1) {
                //     spline.Add(newKnot, TangentMode.AutoSmooth);
                // }
                // else {
                //     spline.Insert(intersection.ExistingBezierIndex + 1, newKnot, TangentMode.AutoSmooth);
                // }

                var addedNode = new SplineStreetNode(newKnot, existingSpline);
                nodes.Add(addedNode);
                // 2. split the existing segment and replace it with two new segments
                edges.Remove(existingSegment);
                ((SplineStreetNode)existingSegment.NodeA).RemoveSegment(existingSegment);
                ((SplineStreetNode)existingSegment.NodeB).RemoveSegment(existingSegment);
                // only generate a segment, not a spline, because we already have the spline
                var addedReplacingSplitSegment1 = SplineStreetSegment.GenerateStreetSegment(
                    (SplineStreetNode)existingSegment.NodeA, addedNode, existingSpline, out var splitSegment1);
                var addedReplacingSplitSegment2 = SplineStreetSegment.GenerateStreetSegment(addedNode,
                    (SplineStreetNode)existingSegment.NodeB, existingSpline, out var splitSegment2);
                Debug.Assert(addedReplacingSplitSegment1 && addedReplacingSplitSegment2,
                    "Failed to generate split segments");
                edges.Add(splitSegment1);
                edges.Add(splitSegment2);

                // 3. split the new segment and replace it with two new segments
                edges.Remove(newSegment);
                from.RemoveSegment(newSegment);
                to.RemoveSegment(newSegment);
                // TODO if we created a new spline for the new segment, it has to be removed again here.
                newSegment = null;
                var addedNewSegment1 = AddNewSegment(from, addedNode, out _, failOnIntersection);
                var addedNewSegment2 = AddNewSegment(addedNode, to, out _, failOnIntersection);
                if (!addedNewSegment1 && !addedNewSegment2) return false;
                if (!addedNewSegment1 || !addedNewSegment2) {
                    // TODO what exactly to do, if only one of the two new segments could be added?
                    // for now just accept it.
                }
            }

            splineExtrude.Rebuild();
            return true;

            void CleanupEmptySplines(SplineStreetNode node) {
                if (node.CorrespondingSplines.Any(s => s.Count <= 1)) {
                    var splineToRemove = node.CorrespondingSplines.First(s => s.Count <= 1);
                    node.CorrespondingSplines.Remove(splineToRemove);
                    splineContainer.RemoveSpline(splineToRemove);
                }
            }
        }

        /// <summary>
        ///     Adds a new Node and a Street Segment between the new node and an existing node.
        /// </summary>
        /// <param name="from"> The existing node </param>
        /// <param name="to"> The position of the new node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <param name="newNode"> The newly generated node </param>
        /// <returns> True if the Node and the Segment was added successfully, false otherwise </returns>
        public bool AddNewSegment(SplineStreetNode from, Vector3 to, out SplineStreetSegment newSegment,
            out SplineStreetNode newNode) {
            Debug.Log($"Adding new segment from {from} to new Node at {to}");

            //grid snapping
            if (snapToGrid) {
                to.x = Mathf.Round(to.x);
                to.y = Mathf.Round(to.y);
            }

            if (!GenerateAndConnectNewNode(from, to, out newNode, out newSegment, out _, out _, out _)) {
                Debug.Log("Failed to generate and connect new node");
                return false;
            }

            if (SplineIntersections.HasIntersection(newSegment, edges, out var intersection)) {
                // TODO: maybe refactor this into a separate method?
                Debug.Log("Intersection detected: " + intersection);
                Debug.Assert(newNode.SplineIndices.Count() == 1 &&
                             newNode.SplineIndices.First().spline == newSegment.Spline);

                // TODO check if near the end or start node, and if so, connect to that if possible (and return false if not possible).

                // 1. split up the existing spline with a new knot
                // 1.1 add the new knot to the spline
                var tangentAtIntersection = intersection.ExistingTangentAtIntersection;
                var newBezierKnot = new BezierKnot(intersection.IntersectionPoint) {
                    Rotation = Quaternion.LookRotation(tangentAtIntersection, Vector3.forward)
                };
                var existingSegmentToSplit = intersection.ExistingSegment;
                existingSegmentToSplit.Spline.Insert(intersection.ExistingBezierIndex + 1, newBezierKnot,
                    TangentMode.AutoSmooth);
                var addedNode = new SplineStreetNode(newBezierKnot, intersection.ExistingSegment.Spline);
                nodes.Add(addedNode);

                // 1.2 split the existing segment and replace it with two new segments
                edges.Remove(existingSegmentToSplit);
                ((SplineStreetNode)existingSegmentToSplit.NodeA).RemoveSegment(existingSegmentToSplit);
                ((SplineStreetNode)existingSegmentToSplit.NodeB).RemoveSegment(existingSegmentToSplit);
                var success1 = SplineStreetSegment.GenerateStreetSegment((SplineStreetNode)existingSegmentToSplit.NodeA,
                    addedNode, existingSegmentToSplit.Spline, out var splitSegment1);
                var success2 = SplineStreetSegment.GenerateStreetSegment(addedNode,
                    (SplineStreetNode)existingSegmentToSplit.NodeB, existingSegmentToSplit.Spline,
                    out var splitSegment2);
                Debug.Assert(success1 && success2);
                edges.Add(splitSegment1);
                edges.Add(splitSegment2);


                // 2. remove the new node and segment. This has to be done after splitting the existing segment, otherwise the index of the split would be wrong.
                newSegment.Spline.RemoveAt(newNode.SplineIndices.First()
                    .index); // Remove the new knot from the existing spline
                nodes.Remove(newNode);
                edges.Remove(newSegment);
                from.RemoveSegment(newSegment);
                newNode = null;
                newSegment = null;

                // 3. add a new segment between the existing node (from) and the newly added intersection node
                if (!AddNewSegment(from, addedNode, out newSegment)) // Add a segment between two existing nodes
                    // TODO remove the new node and segment
                    throw new NotImplementedException("Remove the new node and segment");

                newNode = addedNode;
                return true;
            }

            // There was no intersection, all is well

            splineExtrude.Rebuild();
            SanityChecks();
            return true;
        }

        /// <summary>
        ///     Creates a new Node at the given position and connects it to the given existing node.
        ///     If the existing node is at the beginning or end of a spline, a new knot is added to that spline.
        ///     Otherwise, a new spline is created.
        ///     The spline gets added to the new node and the existing node and the new segment.
        /// </summary>
        /// <param name="from"> The existing node </param>
        /// <param name="to"> The position of the new node </param>
        /// <param name="newNode"> The newly generated node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <param name="lastModifiedSpline"> The spline that was modified or added </param>
        /// <param name="lastModifiedCurve"> The curve that was added </param>
        /// <param name="bezierIndex"> The lower index of one of the two knots of the new segment </param>
        /// <returns> True if the Node was added successfully, false otherwise </returns>
        private bool GenerateAndConnectNewNode(SplineStreetNode from,
            Vector3 to,
            out SplineStreetNode newNode,
            out SplineStreetSegment newSegment,
            out Spline lastModifiedSpline,
            out BezierCurve lastModifiedCurve,
            out int bezierIndex) {
            Debug.Log($"GenerateAndConnectNewNode from {from} to new position {to}");
            if (from.MaxSegmentCountReached) {
                newNode = null;
                newSegment = null;
                lastModifiedSpline = null;
                lastModifiedCurve =
                    default; // we just use default here, because we know from returning false, that it is not a valid value.
                bezierIndex = -1;
                return false;
            }

            foreach (var (spline, index) in from.SplineIndices) {
                Debug.Assert(spline.Count > index
                             && Math.Abs(spline[index].Position.x - from.Position.x) < 0.0001f &&
                             Math.Abs(spline[index].Position.y - from.Position.y) < 0.0001f
                             && spline.ContainsKnotPos(from.Position, out _)); // Two times the same, but to be sure
                if (index > 0 && index < spline.Count - 1) continue;

                // there is a knot at the beginning or end of the spline. we can just add a new knot to this spline.
                var newKnot = new BezierKnot(to) {
                    Rotation = Quaternion.LookRotation(from.Position - to, Vector3.forward)
                };
                if (index == 0) {
                    spline.Insert(0, newKnot, TangentMode.AutoSmooth); // TODO check if AutoSmooth would be better.
                    bezierIndex = 0;
                }
                else if (index == spline.Count - 1) { // TODO check if AutoSmooth would be better.
                    spline.Add(newKnot, TangentMode.AutoSmooth);
                    bezierIndex = spline.Count - 2;
                }
                else {
                    Debug.LogError(
                        "This should not happen. The knot is not at the beginning, end or middle of the spline.");
                    throw new Exception(
                        "This should not happen. The knot is not at the beginning, end or middle of the spline.");
                    //continue;
                }

                from.AddSpline(spline);
                nodes.Add(newNode = new SplineStreetNode(newKnot, spline));
                if (!SplineStreetSegment.GenerateStreetSegment(from, newNode, spline, out newSegment))
                    // TODO: remove the new knot from the spline
                    throw new NotImplementedException("Remove the new knot from the spline");
                //return false;
                edges.Add(newSegment);

                lastModifiedSpline = spline;
                lastModifiedCurve = spline.GetCurve(index);

                return true;
            }

            // all the knots are in the middle of a spline. we need to create a new spline.
            if (!GenerateNewUnconnectedNode(to, out newNode, false, out _, out _)) {
                // TODO: FIX THIS, this creates a new spline with a single knot and afterwards creates another new spline.
                newSegment = null;
                lastModifiedSpline = null;
                lastModifiedCurve = default;
                bezierIndex = -1;
                return false;
            }

            if (!SplineStreetSegment.GenerateStreetSegment(from, newNode, null, out newSegment))
                // TODO: remove the new node from the nodes
                throw new NotImplementedException("Remove the new node from the nodes");
            //return false;
            if (!AddSplineForSegment(newSegment, out var newSpline))
                // TODO: remove the new node and segments
                throw new NotImplementedException("Remove the new node and segments");
            //return true;
            edges.Add(newSegment);

            // Since we added a new spline, it has only one curve
            lastModifiedSpline = newSpline;
            lastModifiedCurve = newSpline.GetCurve(0);
            bezierIndex = 0;

            return true;
        }

        /// <summary>
        ///     Generates a new node at the given position and adds it to the graph.
        ///     Mainly used, if generating a new Node, to then immediately connect it to an existing Node afterwards.
        ///     If <paramref name="generateSplineAndKnot" /> is true, a new spline is created and the knot is added to it.
        ///     This is used, if the new Node should start a completely new spline, and not immediately connect to an existing one.
        /// </summary>
        /// <param name="position"> The position of the new node </param>
        /// <param name="newNode"> The newly generated node </param>
        /// <param name="generateSplineAndKnot"> If true, a new spline is created and the knot is added to it </param>
        /// <param name="newSpline"> The newly created spline </param>
        /// <param name="newCurve"> The newly created curve </param>
        /// <returns></returns>
        private bool GenerateNewUnconnectedNode(
            Vector3 position,
            out SplineStreetNode newNode,
            bool generateSplineAndKnot,
            out Spline newSpline,
            out BezierCurve newCurve) {
            Debug.Log($"Generating new unconnected node at {position}");
            if (!IsPositionValidForNewKnot(position)) {
                newNode = null;
                newSpline = null;
                newCurve = default;
                return false;
            }

            if (generateSplineAndKnot) {
                var spline = splineContainer.AddSpline();
                var knot = new BezierKnot(position) {
                    Rotation = Quaternion.Euler(270, 0, 0) // TODO set correct rotation
                };
                spline.Add(knot);
                newNode = new SplineStreetNode(knot, spline);
                newSpline = spline;
                newCurve = spline.GetCurve(0);
            }
            else {
                newNode = new SplineStreetNode(position);
                newSpline = null;
                newCurve = default;
            }

            nodes.Add(newNode);

            return true;
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            var result = GenerateNewUnconnectedNode(position, out var newNodeProxy, true, out _, out _);
            newNode = newNodeProxy;
            return result;
        }

        public void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node) {
            throw new NotImplementedException();
        }

        private bool IsPositionValidForNewKnot(Vector3 position) {
            // TODO: check if the position is not too close to an existing knot
            return true;
        }

        /// <summary>
        ///     Adds a new Spline for the given segment.
        ///     Adds the spline to the nodes of the segment and sets the spline for the segment.
        /// </summary>
        /// <param name="newSegment"> The segment for which a new spline should be created </param>
        /// <param name="newSpline"> The newly created spline </param>
        /// <returns> True if the spline was added successfully, false otherwise </returns>
        private bool AddSplineForSegment(SplineStreetSegment newSegment, out Spline newSpline) {
            Debug.Log("Creating new Spline for segment");
            newSpline = splineContainer.AddSpline();
            var fromKnot = new BezierKnot(newSegment.NodeA.Position) {
                Rotation = Quaternion.LookRotation(newSegment.NodeB.Position - newSegment.NodeA.Position,
                    Vector3.forward)
            };
            var toKnot = new BezierKnot(newSegment.NodeB.Position) {
                Rotation = Quaternion.LookRotation(newSegment.NodeA.Position - newSegment.NodeB.Position,
                    Vector3.forward)
            };
            newSpline.Add(fromKnot); // TODO set correct tangent rotations for the knots
            newSpline.Add(toKnot);

            // TODO check if the spline is valid

            ((SplineStreetNode)newSegment.NodeA).AddSpline(newSpline);
            ((SplineStreetNode)newSegment.NodeB).AddSpline(newSpline);
            newSegment.Spline = newSpline;
            return true;
        }

        /// <summary>
        ///     Finds the closest node to the given position.
        /// </summary>
        /// <param name="pos"> The position to find the closest node to </param>
        /// <returns> The closest node </returns>
        public SplineStreetNode FindClosestNode(Vector2 pos) {
            if (NodeCount == 0) {
                Debug.LogWarning("No nodes in the graph. Should not happen.");
                return null;
            }

            var closestNode = nodes[0];
            var closestDistance = Vector2.Distance(pos, closestNode.Position);
            foreach (var node in Nodes) {
                var distance = Vector2.Distance(pos, node.Position);
                if (distance < closestDistance) {
                    closestNode = node as SplineStreetNode;
                    closestDistance = distance;
                }
            }

            return closestNode;
        }

        public override bool RemoveEdge(IStreetEdge edge) {
            throw new NotImplementedException();
        }

        public override void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node,
            out IStreetEdge leftEdge, out IStreetEdge rightEdge) {
            throw new NotImplementedException();
        }

        [Conditional("DEBUG")]
        private void SanityChecks() {
            foreach (var n in Nodes) {
                if (n.ConnectedEdgesCount >= 1)
                    Debug.Assert(((SplineStreetNode)n).CorrespondingSplines.Count != 0, n);
            }
        }
    }
}


namespace ExtensionMethods {
    public static class SplineExtensions {
        public static bool ContainsKnotPos(this Spline spline, Vector3 pos, out int index, float tolerance = 0.0001f) {
            for (var i = 0; i < spline.Count; i++) {
                if (Math.Abs(spline[i].Position.x - pos.x) < tolerance &&
                    Math.Abs(spline[i].Position.y - pos.y) < tolerance) {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public static bool ContainsKnotPos(this Spline spline, BezierKnot knot, out int index,
            float tolerance = 0.0001f) {
            return ContainsKnotPos(spline, knot.Position, out index, tolerance);
        }
    }
}