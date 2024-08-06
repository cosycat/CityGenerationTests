using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Graph.Bezier {
    public class BezierStreetGraph : StreetGraphGameObject {
        private readonly List<BezierStreetEdge> edges = new();
        private readonly List<BezierStreetNode> nodes = new();
        public override IEnumerable<IStreetNode> Nodes => nodes;
        public override IEnumerable<IStreetEdge> Edges => edges;
        public override int NodeCount => nodes.Count;
        public override int EdgeCount => edges.Count;
        public override float SnapToExistingNodeThreshold { get; set; } = 0.1f;
        public override float SnapToExistingEdgeThreshold { get; set; } = 0.1f;

        private void Awake() {
            CreateUnconnectedNode(Vector3.zero, out _);
        }

        public override bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge, out bool isEdgeNew) {
            Debug.Assert(from is BezierStreetNode && to is BezierStreetNode,
                $"Expected BezierStreetNodes, got fromNode {from.GetType()} and toNode {to.GetType()}");
            var fromNode = (BezierStreetNode)from;
            var toNode = (BezierStreetNode)to;
            if (fromNode.EntranceAngle.HasValue == false)
                fromNode.EntranceAngle = Vector3.Angle(Vector3.right, toNode.Position - fromNode.Position);

            if (toNode.EntranceAngle.HasValue == false) {
                Debug.Assert(fromNode.EntranceDirection.HasValue); // this should have been set above
                var fromRay = new Ray(fromNode.Position, fromNode.EntranceDirection.Value);
                var fromToTo = toNode.Position - fromNode.Position;
                var angle = Vector3.Angle(fromRay.direction, fromToTo);
                toNode.EntranceAngle = Vector3.Angle(Vector3.right, fromToTo) - angle;
            }

            var p0 = fromNode.Position;
            var p2 = toNode.Position;
            // p1 is the intersection of the Rays of the two EntranceDirections
            Debug.Assert(fromNode.EntranceDirection.HasValue && toNode.EntranceDirection.HasValue);
            var rayFrom = new Ray2D(fromNode.Position, fromNode.EntranceDirection.Value);
            var rayTo = new Ray2D(toNode.Position, toNode.EntranceDirection.Value);
            var intersection = Vector3.zero;
            var p1 = Vector3.zero;
            if (!LineLineIntersection(out intersection, p0, fromNode.EntranceDirection.Value, p2,
                    toNode.EntranceDirection.Value)) {
                // TODO: handle this case better (parallel rays)
                Debug.Log($"No Ray intersection found: {rayFrom} and {rayTo}");
                p1 = (p0 + p2) / 2;
            }

            p1 = intersection;

            var curve = new BezierCurve(p0, p1, p2);

            // TODO check for intersections with existing edges

            var edge = new BezierStreetEdge(fromNode, toNode, curve);
            if (!fromNode.AddEdge(edge) || !toNode.AddEdge(edge)) {
                fromNode.RemoveEdge(edge);
                newEdge = null;
                isEdgeNew = false;
                return false;
            }

            edges.Add(edge);
            newEdge = edge;
            isEdgeNew = true;
            return true;
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            //TODO why do i need to cast here???
            if (((IStreetGraph)this).TryFindClosestNode(position, out var closestNodeInRange,
                    SnapToExistingNodeThreshold)) {
                Debug.Log(
                    $"BezierStreetGraph::CreateUnconnectedNode - Node too close found: Closest node to {position} is {closestNodeInRange.Position} with distance {Vector3.Distance(position, closestNodeInRange.Position)}");
                newNode = null;
                return false;
            }

            var node = new BezierStreetNode(position);
            nodes.Add(node);
            newNode = node;
            return true;
        }

        public override void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node,
            out IStreetEdge leftEdge, out IStreetEdge rightEdge) {
            throw new NotImplementedException();
        }

        public override bool TryRemoveNode(IStreetNode node) {
            Debug.Assert(node is BezierStreetNode);
            var bezierNode = (BezierStreetNode)node;
            if (!nodes.Remove(bezierNode)) return false;
            foreach (var edge in bezierNode.BezierEdges) {
                Debug.Assert(edge.NodeA == bezierNode || edge.NodeB == bezierNode);
                if (!edges.Remove(edge))
                    Debug.LogError($"Failed to remove edge {edge} from graph, but node was in graph and removed.");
                if (edge.BezierNodeA == bezierNode)
                    edge.BezierNodeB.RemoveEdge(edge);
                else
                    edge.BezierNodeA.RemoveEdge(edge);
            }

            // TODO: check if this needs anything else.
            return true;
        }

        public override bool RemoveEdge(IStreetEdge edge) {
            throw new NotImplementedException();
        }

        // Simple util method from https://stackoverflow.com/questions/59449628/check-when-two-vector3-lines-intersect-unity3d
        // TODO improve
        public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
            Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2) {
            var lineVec3 = linePoint2 - linePoint1;
            var crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            var crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

            var planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            //is coplanar, and not parallel
            if (Mathf.Abs(planarFactor) < 0.0001f
                && crossVec1and2.sqrMagnitude > 0.0001f) {
                var s = Vector3.Dot(crossVec3and2, crossVec1and2)
                        / crossVec1and2.sqrMagnitude;
                intersection = linePoint1 + lineVec1 * s;
                return true;
            }

            intersection = Vector3.zero;
            return false;
        }
    }
}