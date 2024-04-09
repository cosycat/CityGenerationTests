using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace FreeFormGraph.Bezier {
    
    public class BezierStreetGraph : StreetGraphGameObject {
        private readonly List<BezierStreetEdge> _edges = new();
        private readonly List<BezierStreetNode> _nodes = new();
        public override IEnumerable<IStreetNode> Nodes => _nodes;
        public override IEnumerable<IStreetEdge> Edges => _edges;
        public override int NodeCount => _nodes.Count;
        public override int EdgeCount => _edges.Count;
        public override float SnapToExistingNodeThreshold { get; set; } = 0.1f;
        public override float SnapToExistingEdgeThreshold { get; set; } = 0.1f;

        private void Awake() {
            CreateUnconnectedNode(Vector3.zero, out _);
        }

        public override bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge) {
            Debug.Assert(from is BezierStreetNode && to is BezierStreetNode,
                $"Expected BezierStreetNodes, got fromNode {from.GetType()} and toNode {to.GetType()}");
            var fromNode = (BezierStreetNode)from;
            var toNode = (BezierStreetNode)to;
            if (fromNode.EntranceAngle.HasValue == false) {
                fromNode.EntranceAngle = Vector3.Angle(Vector3.right, toNode.Position - fromNode.Position);
            }

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
            if (!LineLineIntersection(out intersection, p0, fromNode.EntranceDirection.Value, p2, toNode.EntranceDirection.Value)) {
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
                return false;
            }

            _edges.Add(edge);
            newEdge = edge;
            return true;
        }

        public override float GetDistanceEdgeToPosition(IStreetEdge edge, Vector3 position, out Vector3 positionOnEdge) {
            var minDistance = float.MaxValue;
            positionOnEdge = default;
            foreach (var point in edge.SplitIntoPoints()) {
                var distance = Vector3.Distance(point, position);
                if (distance < minDistance) {
                    minDistance = distance;
                    positionOnEdge = point;
                }
            }
            return minDistance;
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            if (TryFindClosestNode(position, out var closestNodeInRange, SnapToExistingNodeThreshold)) {
                Debug.Log($"BezierStreetGraph::CreateUnconnectedNode - Node too close found: Closest node to {position} is {closestNodeInRange.Position} with distance {Vector3.Distance(position, closestNodeInRange.Position)}");
                newNode = null;
                return false;
            }
            var node = new BezierStreetNode(position);
            _nodes.Add(node);
            newNode = node;
            return true;
        }

        public override void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node) {
            throw new System.NotImplementedException();
        }

        public override bool RemoveNode(IStreetNode node) {
            Debug.Assert(node is BezierStreetNode);
            var bezierNode = (BezierStreetNode) node;
            if (!_nodes.Remove(bezierNode)) {
                return false;
            }
            foreach (var edge in bezierNode.BezierEdges) {
                Debug.Assert(edge.NodeA == bezierNode || edge.NodeB == bezierNode);
                if (!_edges.Remove(edge)) {
                    Debug.LogError($"Failed to remove edge {edge} from graph, but node was in graph and removed.");
                }
                if (edge.BezierNodeA == bezierNode) {
                    edge.BezierNodeB.RemoveEdge(edge);
                } else {
                    edge.BezierNodeA.RemoveEdge(edge);
                }
            }
            // TODO: check if this needs anything else.
            return true;
        }
        
        // Simple util method from https://stackoverflow.com/questions/59449628/check-when-two-vector3-lines-intersect-unity3d
        // TODO improve
        public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
            Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){

            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            //is coplanar, and not parallel
            if( Mathf.Abs(planarFactor) < 0.0001f 
                && crossVec1and2.sqrMagnitude > 0.0001f)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) 
                          / crossVec1and2.sqrMagnitude;
                intersection = linePoint1 + (lineVec1 * s);
                return true;
            }
            else
            {
                intersection = Vector3.zero;
                return false;
            }
        }
    }

    public class BezierStreetNode : IStreetNode {
        private readonly List<BezierStreetEdge> _edges = new();
        public Vector3 Position { get; }
        
        internal IEnumerable<BezierStreetEdge> BezierEdges => _edges;

        public IEnumerable<IStreetEdge> Edges => _edges;

        public int ConnectedEdgesCount => _edges.Count;

        public int MaxConnectedEdges => 4;
        
        public float? EntranceAngle { get; internal set; }
        public Vector3? EntranceDirection => EntranceAngle.HasValue ? Quaternion.Euler(0, 0, EntranceAngle.Value) * Vector3.right : null;

        public BezierStreetNode(Vector3 position) {
            Position = position;
        }

        /// <summary>
        /// Adds an edge to the node.
        /// </summary>
        /// <param name="edge"> The edge to add. </param>
        /// <returns> True if the edge was added, false if the maximum number of edges has been reached. </returns>
        internal bool AddEdge(BezierStreetEdge edge) {
            Debug.Assert(edge.NodeA == this || edge.NodeB == this);
            if (ConnectedEdgesCount >= MaxConnectedEdges) {
                return false;
            }
            _edges.Add(edge);
            if (EntranceAngle.HasValue == false) {
                var edgeDir = edge.BezierNodeA == this ? edge.TangentA : edge.TangentB;
                EntranceAngle = Vector3.Angle(Vector3.right, edgeDir);
            }
            return true;
        }

        internal void RemoveEdge(BezierStreetEdge edge) {
            _edges.Remove(edge);
        }
    }
    
    public class BezierStreetEdge : IStreetEdge {
        private readonly BezierStreetNode _nodeA;
        private readonly BezierStreetNode _nodeB;

        public BezierCurve Curve { get; }
        
        internal BezierStreetNode BezierNodeA => _nodeA;
        internal BezierStreetNode BezierNodeB => _nodeB;

        internal Vector3 TangentA => Curve.Tangent0;
        internal Vector3 TangentB => Curve.Tangent1;

        public IStreetNode NodeA => _nodeA;

        public IStreetNode NodeB => _nodeB;

        public float StreetWidth { get; }

        public BezierStreetEdge(BezierStreetNode nodeA, BezierStreetNode nodeB, BezierCurve curve, float streetWidth = 0.3f) {
            Debug.Assert(Vector3.Distance(curve.P0, nodeA.Position) < 0.01f, $"Distance from {curve.P0} to {nodeA.Position} is {Vector3.Distance(curve.P0, nodeA.Position)}");
            Debug.Assert(Vector3.Distance(curve.P3, nodeB.Position) < 0.01f, $"Distance from {curve.P3} to {nodeB.Position} is {Vector3.Distance(curve.P3, nodeB.Position)}");
            _nodeA = nodeA;
            _nodeB = nodeB;
            Curve = curve;
            StreetWidth = streetWidth;
        }

        public IEnumerable<Vector3> SplitIntoPoints(float stepSize) {
            var distance = CurveUtility.ApproximateLength(Curve);
            var steps = Mathf.CeilToInt(distance / stepSize);
            var step = 1f / steps;
            var points = new Vector3[steps];
            for (var i = 0; i < steps; i++) {
                points[i] = CurveUtility.EvaluatePosition(Curve, i * step);
            }
            return points;
        }
    }
}