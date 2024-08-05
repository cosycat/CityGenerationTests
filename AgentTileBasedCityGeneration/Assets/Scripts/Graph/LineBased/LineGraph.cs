#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Utils.DataStructures;
using Graph.World;
using UnityEngine;

namespace Graph.LineBased {
    public class LineGraph : StreetGraphGameObject {
        private const float EPS = 0.0001f;


        [field: SerializeField] public override float SnapToExistingNodeThreshold { get; set; } = 0.2f;

        [field: SerializeField]
        public override float SnapToExistingEdgeThreshold { get; set; } // TODO this is not used yet

        [field: SerializeField] public float MinAngleBetweenNewEdgesDegree { get; set; } = 15f;
        [SerializeField] private float edgeWidthMeters = 5f;
        private readonly List<LineEdge> edges = new();

        private readonly List<LineNode> nodes = new();

        private ISpatialPointDatastructure<IStreetNode> nodeDatastructure = null!;
        public override IEnumerable<IStreetNode> Nodes => nodes;
        public override IEnumerable<IStreetEdge> Edges => edges;


        public override int NodeCount => nodes.Count;
        public override int EdgeCount => edges.Count;
        public float MinAngleBetweenNewEdgesRad => Mathf.Deg2Rad * MinAngleBetweenNewEdgesDegree;

        public override void Init(IWorld world) {
            nodeDatastructure = new QuadTreePointAdapter(Vector2.zero, new Vector2(world.Width, world.Height));
        }

        public override bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew, out bool isEdgeNew, bool failIfIntersection = false) {
            // Debug.Assert(from != null, $"CreateEdge: From node is null, to position: {to}");
            var numEdgesBefore = edges.Count();
            var numNodesBefore = nodes.Count();

            IStreetEdge? lastIntersectionEdge = null;
            //check for intersections
            foreach (var e in edges) {
                //skip node we are coming from to prevent finding intersection with edge we are connected to
                if (e.NodeA == from || e.NodeB == from) continue;
                var aPosition = from.Position;
                var aDirection = to - aPosition;
                var bPosition = e.NodeA.Position;
                var bDirection = e.NodeB.Position - bPosition;
                var intersects = Intersection(aPosition, aDirection, bPosition, bDirection, out var crossPoint, out _,
                    out _);

                if (intersects) {
                    to = crossPoint;
                    lastIntersectionEdge = e;
                }
            }

            isToNodeNew = true;

            Action undo = () => { };

            if (lastIntersectionEdge != null) {
                if (Vector3.Distance(to, lastIntersectionEdge.NodeA.Position) < SnapToExistingNodeThreshold) {
                    isToNodeNew = false;
                    toNode = lastIntersectionEdge.NodeA;
                }
                else if (Vector3.Distance(to, lastIntersectionEdge.NodeB.Position) < SnapToExistingNodeThreshold) {
                    isToNodeNew = false;
                    toNode = lastIntersectionEdge.NodeB;
                }
                else {
                    if (failIfIntersection) {
                        newEdge = null!;
                        toNode = null!;
                        isToNodeNew = false;
                        isEdgeNew = false;
                        return false;
                    }

                    Debug.Assert(!TryFindClosestNode(to, out _, EPS),
                        $"Intersection with edge, but no node found at {to}");
                    InsertNodeOnEdge(lastIntersectionEdge, to, out toNode, out var leftEdge, out var rightEdge);
                    var nodeUsed = toNode;
                    undo = () => {
                        //reverting to previous state makes things easier for pathfinding removal
                        RemoveEdge(leftEdge);
                        RemoveEdge(rightEdge);
                        RemoveNode(nodeUsed);
                        CreateEdge(lastIntersectionEdge.NodeA, lastIntersectionEdge.NodeB, out var restoredEdge,
                            out var isNewEdge);
                        Debug.Assert(isNewEdge);
                    };
                }
            }
            else {
                if (TryFindClosestNode(to, out var node, SnapToExistingNodeThreshold)) {
                    toNode = node;
                    isToNodeNew = false;
                }
                else {
                    node = new LineNode(to, MinAngleBetweenNewEdgesRad);
                    AddNode((LineNode)node);
                    toNode = node;
                    undo = () => { RemoveNode(node); };
                }
            }

            var result = CreateEdge(from, toNode, out newEdge, out isEdgeNew);
            //edge building might still fail due to bad angles for example. Newly created nodes/edges have then to be removed again otherwise they are dangling
            if (!result) {
                undo();
                if (edges.Count() != numEdgesBefore) {
                    Debug.Assert(edges.Count() == numEdgesBefore);
                    Debug.Assert(nodes.Count() == numNodesBefore);
                }
            }

            return result;
        }

        public override bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge, out bool isEdgeNew) {
            if (from is not LineNode fromLineNode || to is not LineNode toLineNode) { // is checks for null as well
                newEdge = null!;
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
                newEdge = null!;
                isEdgeNew = false;
                return false;
            }

            // TODO check intersections here

            var edge = new LineEdge(fromLineNode, toLineNode, edgeWidthMeters);
            fromLineNode.AddEdge(edge);
            toLineNode.AddEdge(edge);
            AddEdge(edge);
            newEdge = edge;
            isEdgeNew = true;
            return true;
        }

        public static bool IsAngleOfNewEdgePossible(LineNode toLineNode, LineNode fromLineNode) {
            foreach (var fromEdge in fromLineNode.Edges) {
                // check if angle to another edge is too small on the from-node
                var newEdgeDirectionFrom = toLineNode.Position - fromLineNode.Position;
                var existingEdgeDirectionFrom = fromEdge.NodeA == fromLineNode
                    ? fromEdge.NodeB.Position - fromLineNode.Position
                    : fromEdge.NodeA.Position - fromLineNode.Position;
                if (Vector3.Angle(newEdgeDirectionFrom, existingEdgeDirectionFrom) * Mathf.Deg2Rad <
                    fromLineNode.MinAngleBetweenEdges) {
                    Debug.Log(
                        $"Angle between edges too small: {Vector3.Angle(newEdgeDirectionFrom, existingEdgeDirectionFrom)} < {fromLineNode.MinAngleBetweenEdges * Mathf.Rad2Deg}. On node {fromLineNode.Position}");
                    return false;
                }
            }

            foreach (var toEdge in toLineNode.Edges) {
                // check if angle to another edge is too small on the to-node
                var newEdgeDirectionTo = fromLineNode.Position - toLineNode.Position;
                var existingEdgeDirectionTo = toEdge.NodeA == toLineNode
                    ? toEdge.NodeB.Position - toLineNode.Position
                    : toEdge.NodeA.Position - toLineNode.Position;
                if (Vector3.Angle(newEdgeDirectionTo, existingEdgeDirectionTo) < toLineNode.MinAngleBetweenEdges) {
                    Debug.Log(
                        $"Angle between edges too small: {Vector3.Angle(newEdgeDirectionTo, existingEdgeDirectionTo)} < {toLineNode.MinAngleBetweenEdges * Mathf.Rad2Deg}. On node {toLineNode.Position}");
                    return false;
                }
            }

            return true;
        }

        public override bool RemoveNode(IStreetNode node) {
            if (node.ConnectedEdgesCount == 0) {
                nodes.Remove((LineNode)node);
                nodeDatastructure.Remove((LineNode)node);
                OnNodeRemoved((LineNode)node);
                return true;
            }

            throw new NotImplementedException("Node not allowed to remove because it still has edges");
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            var n = new LineNode(position, MinAngleBetweenNewEdgesRad);
            AddNode(n);
            newNode = n;
            return true;
        }

        /// <summary>
        ///     Checks if two lines intersect and returns the intersection point. TODO is this description correct?
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
            return t > 0 - eps && t < 1 + eps && s > 0 - eps && s < 1 + eps;
        }

        public override void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node,
            out IStreetEdge leftEdge, out IStreetEdge rightEdge) {
            var e = (LineEdge)foundEdge;
            var prevStreetWidth = e.StreetWidth;
            var nodeA = e.NodeA as LineNode ?? throw new ArgumentException();
            var nodeB = e.NodeB as LineNode ?? throw new ArgumentException();
            nodeA.RemoveEdge(e);
            nodeB.RemoveEdge(e);
            RemoveEdge(e);

            var n = new LineNode(positionOnEdge, MinAngleBetweenNewEdgesRad);
            AddNode(n);

            var lEdge = new LineEdge(foundEdge.NodeA, n, prevStreetWidth);
            AddEdge(lEdge);
            var rEdge = new LineEdge(n, foundEdge.NodeB, prevStreetWidth);
            AddEdge(rEdge);

            //if this fails, we would have an edge with length 0, which is weird and should not happen
            Debug.Assert(Vector3.Distance(lEdge.NodeA.Position, lEdge.NodeB.Position) > EPS);
            Debug.Assert(Vector3.Distance(rEdge.NodeA.Position, rEdge.NodeB.Position) > EPS,
                $"Distance between {rEdge.NodeA.Position} and {rEdge.NodeB.Position} is {Vector3.Distance(rEdge.NodeA.Position, rEdge.NodeB.Position)}");

            nodeA.AddEdge(lEdge);
            nodeB.AddEdge(rEdge);
            n.AddEdge(lEdge);
            n.AddEdge(rEdge);
            leftEdge = lEdge;
            rightEdge = rEdge;
            node = n;
        }

        public override bool TryFindClosestNode(Vector3 position, out IStreetNode foundNode,
            float threshold = float.MaxValue) {
            if (nodeDatastructure == null) {
                //logically it should not happen (init() should always be called before working with graph) but sometimes order of execution is not always quite right...
                foundNode = null!;
                return false;
            }

            var foundNodeOrNull = nodeDatastructure.FindNearest(position.x, position.y);
            if (foundNodeOrNull == null) {
                foundNode = null!;
                return false; //no nodes yet
            }

            foundNode = foundNodeOrNull;
            if (Vector2.Distance(foundNode.Position, position) > threshold) {
                foundNode = null!;
                return false;
            }
            return true;
        }

        public override IStreetEdge[] FindAllEdgesWithinRange(Vector2 position, float radius) {
            if (radius == 0) return new List<IStreetEdge>().ToArray();
            var closeEdges = new List<IStreetEdge>();
            Debug.Assert(nodeDatastructure != null, "nodeDatastructure == null");
            var bvhHits = nodeDatastructure!.FindRegion(
                new Vector2(position.x - radius, position.y - radius),
                new Vector2(position.x + radius, position.y + radius)
            );

            foreach (var g in bvhHits) {
                foreach (var edge in g.Edges) {
                    if (closeEdges.Contains(edge)) continue;
                    var distance = edge.GetDistanceEdgeToPosition(position, out _);
                    if (distance < radius) closeEdges.Add(edge);
                }
            }

            return closeEdges.ToArray();
        }

        public override bool RemoveEdge(IStreetEdge edge) {
            if (edge is LineEdge lineEdge) {
                ((LineNode)lineEdge.NodeA).RemoveEdge(lineEdge);
                ((LineNode)lineEdge.NodeB).RemoveEdge(lineEdge);
                OnEdgeRemoved(lineEdge);
                return edges.Remove(lineEdge);
            }

            return false;
        }

        private void AddEdge(LineEdge edge) {
            edges.Add(edge);
            OnEdgeAdded(edge);
        }

        private void AddNode(LineNode n) {
            nodes.Add(n);
            nodeDatastructure.Insert(n);
            OnNodeAdded(n);
        }
    }
}