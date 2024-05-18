using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using DataStructures;

namespace FreeFormGraph.LineBased {
    public class LineGraph : StreetGraphGameObject {

        private readonly List<LineEdge> edges = new();

        private readonly BVH<LineEdge> bvh = new(new BVHLineAdapter(), new List<LineEdge>()); 

        private readonly List<LineNode> nodes = new();
        public override IEnumerable<IStreetNode> Nodes => nodes;
        public override IEnumerable<IStreetEdge> Edges => edges;


        public override int NodeCount => nodes.Count;
        public override int EdgeCount => edges.Count;

        private const float EPS = 0.0001f;


        [field: SerializeField] public override float SnapToExistingNodeThreshold { get; set; } = 0.2f;
        [field: SerializeField] public override float SnapToExistingEdgeThreshold { get; set; } // TODO this is not used yet
        [field: SerializeField] public float MinAngleBetweenNewEdgesDegree { get; set; } = 15f;

        public override bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew, out bool isEdgeNew, bool failIfIntersection = false) {
                Debug.Assert(from != null, $"CreateEdge: From node is null, to position: {to}");

                IStreetEdge lastIntersectionEdge = null;
                //check for intersections
                foreach(var e in edges) {
                    //skip node we are coming from to prevent finding intersection with edge we are connected to
                    if(e.NodeA == from || e.NodeB == from) continue;
                    var aPosition = from.Position;
                    var aDirection = to-aPosition;
                    var bPosition = e.NodeA.Position;
                    var bDirection = e.NodeB.Position - bPosition;
                    var intersects = Intersection(aPosition, aDirection, bPosition, bDirection, out var crossPoint, out _, out _);

                    if(intersects) {
                        to = crossPoint;
                        lastIntersectionEdge = e;
                    }
                }

                isToNodeNew = true;

                if(lastIntersectionEdge != null) {
                    if(Vector3.Distance(to, lastIntersectionEdge.NodeA.Position) < SnapToExistingNodeThreshold) {
                        isToNodeNew = false;
                        toNode = lastIntersectionEdge.NodeA;
                    }
                    else if(Vector3.Distance(to, lastIntersectionEdge.NodeB.Position) < SnapToExistingNodeThreshold) {
                        isToNodeNew = false;
                        toNode = lastIntersectionEdge.NodeB;
                    }
                    else {
                        if(failIfIntersection) {
                            newEdge = null;
                            toNode = null;
                            isToNodeNew = false;
                            isEdgeNew = false;
                            return false;
                        }

                        Debug.Assert(!TryFindClosestNode(to, out _, EPS), $"Intersection with edge, but no node found at {to}");
                        InsertNodeOnEdge(lastIntersectionEdge, to, out toNode, out _, out _);
                    }

                } else {
                    if (TryFindClosestNode(to, out var node, SnapToExistingNodeThreshold)) {
                        toNode = node;
                        isToNodeNew = false;
                    }
                    else {
                        node = new LineNode(to, Mathf.Deg2Rad * MinAngleBetweenNewEdgesDegree);
                        nodes.Add((LineNode)node);
                        toNode = node;
                    }
                }
                
                return CreateEdge(from, toNode, out newEdge, out isEdgeNew);
            }
        
        public override bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge, out bool isEdgeNew) {
            if (from is not LineNode fromLineNode || to is not LineNode toLineNode) { // is checks for null as well
                newEdge = null;
                isEdgeNew = false;
                return false;
            }
            
            foreach (var fromEdge in from.Edges) {
                // check if edge already exists
                if (fromEdge.NodeA == to || fromEdge.NodeB == to) {
                    newEdge = fromEdge;
                    isEdgeNew = false;
                    return true;
                }
                
            }

            if (!IsAngleOfNewEdgePossible(toLineNode, fromLineNode)) {
                newEdge = null;
                isEdgeNew = false;
                return false;
            }

            // TODO check intersections here
            
            var edge = new LineEdge(fromLineNode, toLineNode);
            AddEdge(edge);
            fromLineNode.AddEdge(edge);
            toLineNode.AddEdge(edge);
            newEdge = edge;
            isEdgeNew = true;
            return true;
        }

        private static bool IsAngleOfNewEdgePossible(LineNode toLineNode, LineNode fromLineNode) {
            foreach (var fromEdge in fromLineNode.Edges) {
                // check if angle to another edge is too small on the from-node
                var newEdgeDirectionFrom = toLineNode.Position - fromLineNode.Position;
                var existingEdgeDirectionFrom = fromEdge.NodeA == fromLineNode ? fromEdge.NodeB.Position - fromLineNode.Position : fromEdge.NodeA.Position - fromLineNode.Position;
                if (Vector3.Angle(newEdgeDirectionFrom, existingEdgeDirectionFrom) * Mathf.Deg2Rad < fromLineNode.MinAngleBetweenEdges) {
                    Debug.Log($"Angle between edges too small: {Vector3.Angle(newEdgeDirectionFrom, existingEdgeDirectionFrom)} < {fromLineNode.MinAngleBetweenEdges * Mathf.Rad2Deg}. On node {fromLineNode.Position}");
                    return false;
                }
            }

            foreach (var toEdge in toLineNode.Edges) {
                // check if angle to another edge is too small on the to-node
                var newEdgeDirectionTo = fromLineNode.Position - toLineNode.Position;
                var existingEdgeDirectionTo = toEdge.NodeA == toLineNode ? toEdge.NodeB.Position - toLineNode.Position : toEdge.NodeA.Position - toLineNode.Position;
                if (Vector3.Angle(newEdgeDirectionTo, existingEdgeDirectionTo) < toLineNode.MinAngleBetweenEdges) {
                    Debug.Log($"Angle between edges too small: {Vector3.Angle(newEdgeDirectionTo, existingEdgeDirectionTo)} < {toLineNode.MinAngleBetweenEdges * Mathf.Rad2Deg}. On node {toLineNode.Position}");
                    return false;
                }
            }

            return true;
        }

        public override bool RemoveNode(IStreetNode node) {
            throw new NotImplementedException();
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            var n = new LineNode(position, Mathf.Deg2Rad * MinAngleBetweenNewEdgesDegree);
            nodes.Add(n);
            newNode = n;
            return true;
        }
        
        /// <summary>
        /// Checks if two lines intersect and returns the intersection point. TODO is this description correct?
        /// </summary>
        /// <param name="aPosition"> The position of the first line. </param>
        /// <param name="aLineVector"> The direction and length of the first line. </param>
        /// <param name="bPosition"> The position of the second line. </param>
        /// <param name="bLineVector"> The direction and length of the second line. </param>
        /// <param name="intersectionPoint"> The intersection point of the two lines. </param>
        /// <param name="t">TODO</param>
        /// <param name="s">TODO</param>
        /// <param name="eps"> The epsilon value for the intersection check. </param>
        /// <returns> True if the lines intersect, false otherwise. </returns>
        private static bool Intersection(Vector3 aPosition, Vector3 aLineVector, Vector3 bPosition, Vector3 bLineVector,
            out Vector3 intersectionPoint, out float t, out float s, float eps = 0.0001f) {
            Vector2 p = aLineVector;
            Vector2 q = bLineVector;
            Vector2 r = bPosition - aPosition;

            var denominator = p.y * q.x - p.x * q.y;

            if (denominator == 0) {
                //line parallel
                t = float.NaN;
                s = float.NaN;
                intersectionPoint = Vector3.zero;
                return false;
            }

            t = (r.y * q.x - r.x * q.y) / denominator;
            s = (r.y * p.x - r.x * p.y) / denominator;
            intersectionPoint = aPosition + t * aLineVector;
            return (t > 0 - eps && t < 1 + eps && s > 0 - eps && s < 1 + eps);
        }

        public override void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node, out IStreetEdge leftEdge, out IStreetEdge rightEdge) {
            var n = new LineNode(positionOnEdge, Mathf.Deg2Rad * MinAngleBetweenNewEdgesDegree);
            var e = (LineEdge) foundEdge;
            ((LineNode)e.NodeA).RemoveEdge(e);
            ((LineNode)e.NodeB).RemoveEdge(e);
            RemoveEdge(e);
            
            var lEdge = new LineEdge(foundEdge.NodeA, n);
            var rEdge = new LineEdge(n, foundEdge.NodeB);
            
            nodes.Add(n);
            AddEdge(lEdge);
            AddEdge(rEdge);

            //if this fails, we would have an edge with length 0, which is weird and should not happen
            Debug.Assert(Vector3.Distance(lEdge.NodeA.Position, lEdge.NodeB.Position) > EPS);
            Debug.Assert(Vector3.Distance(rEdge.NodeA.Position, rEdge.NodeB.Position) > EPS, $"Distance between {rEdge.NodeA.Position} and {rEdge.NodeB.Position} is {Vector3.Distance(rEdge.NodeA.Position, rEdge.NodeB.Position)}");

            ((LineNode)e.NodeA).AddEdge(lEdge);
            ((LineNode)e.NodeB).AddEdge(rEdge);
            n.AddEdge(lEdge);
            n.AddEdge(rEdge);
            leftEdge = lEdge;
            rightEdge = rEdge;
            node = n;
        }

        public override IStreetGraph Copy() {
            var copy = new GameObject().AddComponent<LineGraph>();
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                var n = new LineNode(node.Position, node.MinAngleBetweenEdges, node.MaxConnectedEdges);
                copy.nodes.Add(n);
            }

            for (var i = 0; i < edges.Count; i++) {
                var edge = edges[i];
                var e = new LineEdge(copy.nodes[nodes.IndexOf((LineNode)edge.NodeA)],copy.nodes[nodes.IndexOf((LineNode)edge.NodeB)]);
                copy.edges.Add(e);
                ((LineNode)e.NodeA).AddEdge(e);
                ((LineNode)e.NodeB).AddEdge(e);
            }

            return copy;
        }
        
        public override IStreetEdge[] FindAllEdgesWithinRange(Vector2 position, float radius) {
            var closeEdges = new List<IStreetEdge>();
            var bvhHits = bvh.Traverse(BVHHelper.RadialNodeTraversalTest(position, radius));

            foreach(var g in bvhHits) {
                if (g.GObjects == null) continue;
                foreach(var edge in g.GObjects) {
                    var distance = edge.GetDistanceEdgeToPosition(position, out _);
                    if (distance < radius) {
                        closeEdges.Add(edge);
                    }
                }
            }
            return closeEdges.ToArray();
        }

        private void AddEdge(LineEdge edge) {
            edges.Add(edge);
            bvh.Add(edge);
            bvh.Optimize(); //maybe use batch operations for adding?
        }

        private void RemoveEdge(LineEdge edge) {
            edges.Remove(edge);
            bvh.Remove(edge);
            bvh.Optimize();
        }
    }

    public class LineNode: IStreetNode {
        
        public Vector3 Position { get; set; }
        
        private readonly List<LineEdge> edges = new();

        public LineNode(Vector3 position, float minAngleBetweenEdges = Mathf.Deg2Rad * 30f, int maxConnectedEdges = 6) {
            Position = position;
            MinAngleBetweenEdges = minAngleBetweenEdges;
            MaxConnectedEdges = maxConnectedEdges;
        }

        public IEnumerable<IStreetEdge> Edges => edges;
        
        public int ConnectedEdgesCount => edges.Count;
        
        public int MaxConnectedEdges { get; set; }
        
        public float MinAngleBetweenEdges { get; }

        public bool IsMaxConnectedEdgesReached => ConnectedEdgesCount >= MaxConnectedEdges;

        public override string ToString() {
            return $"LineNode at {Position} with {ConnectedEdgesCount} edges.";
        }

        public void AddEdge(LineEdge e) {
            if(!edges.Contains(e)) {
                edges.Add(e);
            }
        }

        public void RemoveEdge(LineEdge e) {
            edges.Remove(e);
        }

    }

    public class LineEdge: IStreetEdge {
        public Vector3 PositionNodeA => NodeA.Position;
        public Vector3 PositionNodeB => NodeB.Position;

        public Vector3 Position => PositionNodeA + (PositionNodeB-PositionNodeA)/2.0f;
        public float Radius => Vector3.Distance(PositionNodeB, PositionNodeA) / 2.0f;
        
        public IStreetNode NodeA { get; }
        public IStreetNode NodeB { get; }
        public float StreetWidth { get; set;}

        public LineEdge(IStreetNode nodeA, IStreetNode nodeB, float streetWidth = 1f) {
            NodeA = nodeA;
            NodeB = nodeB;
            StreetWidth = streetWidth;
        }

        public Vector3[] SplitIntoEvenlySpacedPoints(float stepSize = 0.1f) {
            return new Vector3[] {};
        }

        public float GetDistanceEdgeToPosition(Vector3 position, out Vector3 positionOnEdge) {
            positionOnEdge = SplineMath.PointLineNearestPoint(position, NodeA.Position, NodeB.Position, out _);
            return Vector3.Distance(position, positionOnEdge);
        }

        public override string ToString() {
            return $"LineEdge from {PositionNodeA} to {PositionNodeB}";
        }

        public float Length() {
            return Vector3.Distance(PositionNodeA, PositionNodeB);
        }
    }
}