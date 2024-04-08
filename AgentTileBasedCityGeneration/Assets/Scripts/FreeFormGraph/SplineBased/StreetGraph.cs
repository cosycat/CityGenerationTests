using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExtensionMethods;
using FreeFormGraph;
using FreeFormGraph.SplineBased;
using UnityEngine;
using UnityEngine.Splines;
using Debug = UnityEngine.Debug;

namespace FreeFormGraph.SplineBased {
    public class StreetGraph : StreetGraphGameObject {
        
        [SerializeField] private bool snapToGrid = true;

        private SplineContainer _splineContainer;
        private SplineExtrude _splineExtrude;
        private readonly List<StreetSegment> _edges = new();
        private readonly List<StreetNode> _nodes = new();
        
        public override IEnumerable<IStreetNode> Nodes => _nodes;

        public override IEnumerable<IStreetEdge> Edges => _edges;

        public override int NodeCount => _nodes.Count;
        public override int EdgeCount => _edges.Count;
        public override float SnapToExistingNodeThreshold { get; set; }
        public override float SnapToExistingEdgeThreshold { get; set; }
        public override bool AddEdge(Vector3 from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode) {
            return AddEdge(FindClosestNode(from), to, out newEdge, out toNode);
        }

        public override bool AddEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode) {
            var res = AddNewSegment((StreetNode)from, to, out var newSegment, out var toNodeA);
            newEdge = newSegment;
            toNode = toNodeA;
            return res;
        }


        private void Awake() {
            _splineContainer = GetComponentInChildren<SplineContainer>();
            _splineExtrude = GetComponentInChildren<SplineExtrude>();
            GenerateNewUnconnectedNode(Vector3.zero, out var newNode, true, out var newSpline, out var curve);
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
            // TODO if one of the Nodes has <= 1 connections, just add the segment to the existing spline, instead of creating a new one.
            
            if (!StreetSegment.GenerateStreetSegment(from, to, null, out newSegment)) {
                return false;
            }
            if (!AddSplineForSegment(newSegment, out var newSpline)) {
                // TODO: remove the segment from the nodes
                throw new NotImplementedException("Remove the segment from the nodes");
                return false;
            }
            
            // TODO check intersections as well
            // If intersection, add intersection point, split up the spline, then recursively add two new segments, from - intersection and intersection - to.

            from.AddSpline(newSpline);
            to.AddSpline(newSpline);
            _edges.Add(newSegment);
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

            //grid snapping
            if (snapToGrid) {
                to.x = Mathf.Round(to.x);
                to.y = Mathf.Round(to.y);
            }

            if (!GenerateAndConnectNewNode(from, to, out newNode, out newSegment, out var lastModifiedSpline, out var lastModifiedCurve, out var bezierIndex)) {
                Debug.Log("Failed to generate and connect new node");
                return false;
            }

            if (Intersections.HasIntersection(newSegment, lastModifiedSpline, lastModifiedCurve, _edges, bezierIndex, out var intersection)) {
                // TODO: maybe refactor this into a separate method?
                Debug.Log("Intersection detected: " + intersection);
                Debug.Assert(newNode.SplineIndices.Count() == 1 && newNode.SplineIndices.First().spline == newSegment.Spline);
                
                // TODO check if near the end or start node, and if so, connect to that if possible (and return false if not possible).
                
                // 1. split up the existing spline with a new knot
                // 1.1 add the new knot to the spline
                var tangentAtIntersection = intersection.ExistingTangentAtIntersection;
                var newBezierKnot = new BezierKnot(intersection.IntersectionPoint) {
                    Rotation = Quaternion.LookRotation(tangentAtIntersection, Vector3.forward)
                };
                var existingSegmentToSplit = intersection.ExistingSegment;
                existingSegmentToSplit.Spline.Insert(intersection.ExistingBezierIndex + 1, newBezierKnot, TangentMode.AutoSmooth);
                var addedNode = new StreetNode(newBezierKnot, intersection.ExistingSegment.Spline);
                _nodes.Add(addedNode);

                // 1.2 split the existing segment and replace it with two new segments
                _edges.Remove(existingSegmentToSplit);
                ((StreetNode)existingSegmentToSplit.NodeA).RemoveSegment(existingSegmentToSplit);
                ((StreetNode)existingSegmentToSplit.NodeB).RemoveSegment(existingSegmentToSplit);
                var success1 = StreetSegment.GenerateStreetSegment((StreetNode)existingSegmentToSplit.NodeA, addedNode, existingSegmentToSplit.Spline, out var splitSegment1);
                var success2 = StreetSegment.GenerateStreetSegment(addedNode, (StreetNode)existingSegmentToSplit.NodeB, existingSegmentToSplit.Spline, out var splitSegment2);
                Debug.Assert(success1 && success2);
                _edges.Add(splitSegment1);
                _edges.Add(splitSegment2);
                
                
                // 2. remove the new node and segment. This has to be done after splitting the existing segment, otherwise the index of the split would be wrong.
                newSegment.Spline.RemoveAt(newNode.SplineIndices.First().index); // Remove the new knot from the existing spline
                _nodes.Remove(newNode);
                _edges.Remove(newSegment);
                from.RemoveSegment(newSegment);
                newNode = null;
                newSegment = null;
                
                // 3. add a new segment between the existing node (from) and the newly added intersection node
                if (!AddNewSegment(from, addedNode, out newSegment)) { // Add a segment between two existing nodes
                    // TODO remove the new node and segment
                    throw new NotImplementedException("Remove the new node and segment");
                }
                
                newNode = addedNode;
                return true;
            }
            
            // There was no intersection, all is well
            
            _splineExtrude.Rebuild();
            SanityChecks();
            return true;
        }

        /// <summary>
        /// Creates a new Node at the given position and connects it to the given existing node.
        /// If the existing node is at the beginning or end of a spline, a new knot is added to that spline.
        /// Otherwise, a new spline is created.
        ///
        /// The spline gets added to the new node and the existing node and the new segment.
        /// 
        /// </summary>
        /// <param name="from"> The existing node </param>
        /// <param name="to"> The position of the new node </param>
        /// <param name="newNode"> The newly generated node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <param name="lastModifiedSpline"> The spline that was modified or added </param>
        /// <param name="lastModifiedCurve"> The curve that was added </param>
        /// <param name="bezierIndex"> The lower index of one of the two knots of the new segment </param>
        /// <returns> True if the Node was added successfully, false otherwise </returns>
        private bool GenerateAndConnectNewNode(StreetNode from, 
                Vector3 to, 
                out StreetNode newNode,
                out StreetSegment newSegment,
                out Spline lastModifiedSpline,
                out BezierCurve lastModifiedCurve,
                out int bezierIndex) {
            Debug.Log($"GenerateAndConnectNewNode from {from} to new position {to}");
            if (from.MaxSegmentCountReached) {
                newNode = null;
                newSegment = null;
                lastModifiedSpline = null;
                lastModifiedCurve = default; // we just use default here, because we know from returning false, that it is not a valid value.
                bezierIndex = -1;
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
                    bezierIndex = 0;
                }
                else if (index == spline.Count - 1) { // TODO check if AutoSmooth would be better.
                    spline.Add(newKnot, TangentMode.AutoSmooth);
                    bezierIndex = spline.Count - 2;
                }
                else {
                    Debug.LogError(
                        $"This should not happen. The knot is not at the beginning, end or middle of the spline.");
                    throw new Exception("This should not happen. The knot is not at the beginning, end or middle of the spline.");
                    continue;
                }
                
                from.AddSpline(spline);
                _nodes.Add(newNode = new StreetNode(newKnot, spline));
                if (!StreetSegment.GenerateStreetSegment(from, newNode, spline, out newSegment)) {
                    // TODO: remove the new knot from the spline
                    throw new NotImplementedException("Remove the new knot from the spline");
                    return false;
                }
                _edges.Add(newSegment);
                
                lastModifiedSpline = spline;
                lastModifiedCurve = spline.GetCurve(index);

                return true;
            }

            // all the knots are in the middle of a spline. we need to create a new spline.
            if (!GenerateNewUnconnectedNode(to, out newNode, false, out _, out _)) { // TODO: FIX THIS, this creates a new spline with a single knot and afterwards creates another new spline.
                newSegment = null;
                lastModifiedSpline = null;
                lastModifiedCurve = default;
                bezierIndex = -1;
                return false;
            }

            if (!StreetSegment.GenerateStreetSegment(from, newNode, null, out newSegment)) {
                // TODO: remove the new node from the nodes
                throw new NotImplementedException("Remove the new node from the nodes");
                return false;
            }
            if (!AddSplineForSegment(newSegment, out var newSpline)) {
                // TODO: remove the new node and segments
                throw new NotImplementedException("Remove the new node and segments");
                return true;
            }
            _edges.Add(newSegment);
            
            // Since we added a new spline, it has only one curve
            lastModifiedSpline = newSpline;
            lastModifiedCurve = newSpline.GetCurve(0);
            bezierIndex = 0;
            
            return true;
        }

        /// <summary>
        /// Generates a new node at the given position and adds it to the graph.
        ///
        /// Mainly used, if generating a new Node, to then immediately connect it to an existing Node afterwards.
        /// 
        /// If <paramref name="generateSplineAndKnot"/> is true, a new spline is created and the knot is added to it.
        /// This is used, if the new Node should start a completely new spline, and not immediately connect to an existing one.
        /// </summary>
        /// <param name="position"> The position of the new node </param>
        /// <param name="newNode"> The newly generated node </param>
        /// <param name="generateSplineAndKnot"> If true, a new spline is created and the knot is added to it </param>
        /// <param name="newSpline"> The newly created spline </param>
        /// <param name="newCurve"> The newly created curve </param>
        /// <returns></returns>
        private bool GenerateNewUnconnectedNode(
                Vector3 position, 
                out StreetNode newNode, 
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
                var spline = _splineContainer.AddSpline();
                var knot = new BezierKnot(position) {
                    Rotation = Quaternion.Euler(270, 0, 0) // TODO set correct rotation
                };
                spline.Add(knot);
                newNode = new StreetNode(knot, spline);
                newSpline = spline;
                newCurve = spline.GetCurve(0);
            }
            else {
                newNode = new StreetNode(position);
                newSpline = null;
                newCurve = default;
            }
            _nodes.Add(newNode);

            return true;
        }

        private bool IsPositionValidForNewKnot(Vector3 position) {
            // TODO: check if the position is not too close to an existing knot
            return true;
        }

        /// <summary>
        /// Adds a new Spline for the given segment.
        /// Adds the spline to the nodes of the segment and sets the spline for the segment.
        /// </summary>
        /// <param name="newSegment"> The segment for which a new spline should be created </param>
        /// <param name="newSpline"> The newly created spline </param>
        /// <returns> True if the spline was added successfully, false otherwise </returns>
        private bool AddSplineForSegment(StreetSegment newSegment, out Spline newSpline) {
            Debug.Log("Creating new Spline for segment");
            newSpline = _splineContainer.AddSpline();
            var fromKnot = new BezierKnot(newSegment.NodeA.Position) {
                Rotation = Quaternion.LookRotation(newSegment.NodeB.Position - newSegment.NodeA.Position, Vector3.forward)
            };
            var toKnot = new BezierKnot(newSegment.NodeB.Position) {
                Rotation = Quaternion.LookRotation(newSegment.NodeA.Position - newSegment.NodeB.Position, Vector3.forward)
            };
            newSpline.Add(fromKnot); // TODO set correct tangent rotations for the knots
            newSpline.Add(toKnot);
            
            // TODO check if the spline is valid
            
            ((StreetNode)newSegment.NodeA).AddSpline(newSpline);
            ((StreetNode)newSegment.NodeB).AddSpline(newSpline);
            newSegment.Spline = newSpline;
            return true;
        }

        /// <summary>
        /// Finds the closest node to the given position.
        /// </summary>
        /// <param name="pos"> The position to find the closest node to </param>
        /// <returns> The closest node </returns>
        public StreetNode FindClosestNode(Vector2 pos) {
            if (NodeCount == 0) {
                Debug.LogWarning("No nodes in the graph. Should not happen.");
                return null;
            }

            var closestNode = _nodes[0];
            var closestDistance = Vector2.Distance(pos, closestNode.Position);
            foreach (var node in Nodes) {
                var distance = Vector2.Distance(pos, node.Position);
                if (distance < closestDistance) {
                    closestNode = node as StreetNode;
                    closestDistance = distance;
                }
            }

            return closestNode;
        }

        [Conditional("DEBUG")]
        private void SanityChecks() {
            foreach(var n in Nodes) {
                if(n.ConnectedEdgesCount >= 1) {
                    Debug.Assert(((StreetNode)n).CorrespondingSplines.Count != 0, n);
                }
            }
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
    
    