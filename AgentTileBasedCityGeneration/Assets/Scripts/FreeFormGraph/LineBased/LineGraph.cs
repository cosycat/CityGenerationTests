using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using DataStructures;

namespace FreeFormGraph.LineBased {
    public class LineGraph : StreetGraphGameObject {

        private readonly List<LineEdge> edges = new();

        public BVH<LineEdge> bvh = new BVH<LineEdge>(new BVHLineAdapter(), new()); 

        private readonly List<LineNode> nodes = new();
        public override IEnumerable<IStreetNode> Nodes => nodes;
        public override IEnumerable<IStreetEdge> Edges => edges;


        public override int NodeCount => nodes.Count;
        public override int EdgeCount => edges.Count;

        private float eps = 0.0001f;
        

        public override float SnapToExistingNodeThreshold { get; set; } = 0.2f;
        public override float SnapToExistingEdgeThreshold { get; set; } = 0;

        public override bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew, out bool isEdgeNew, bool failIfIntersection = false) {
                Debug.Assert(from != null, $"CreateEdge: From node is null, to position: {to}");

                IStreetEdge lastIntersectionEdge = null;
                //check for intersections
                foreach(var e in edges) {
                    //skip node we are coming from to prevent finding intersection with edge we are connected to
                    if(e.NodeA == from || e.NodeB == from) continue;
                    var A = from.Position;
                    var a = to-A;
                    var B = e.NodeA.Position;
                    var b = e.NodeB.Position - B;
                    var intersects = Intersection(A, a, B, b, out var crossPoint, out var t, out var s);

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

                        Debug.Assert(!TryFindClosestNode(to, out var node, eps), $"Intersection with edge, but no node found at {to}");
                        InsertNodeOnEdge(lastIntersectionEdge, to, out toNode, out _, out _);
                    }

                } else {
                    IStreetNode node = null;
                    if (!((IStreetGraph)this).TryFindClosestNode(to, out node, SnapToExistingNodeThreshold)) {
                        toNode = node;
                        isToNodeNew = false;
                    }
                    else {
                        node = new LineNode() {
                            Position = to
                        };
                        nodes.Add((LineNode)node);
                        toNode = node;
                    }
                }
                
                return CreateEdge(from, toNode, out newEdge, out isEdgeNew);
            }
        
        public override bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge, out bool isEdgeNew) {
            if (from == null || to == null) {
                newEdge = null;
                isEdgeNew = false;
                return false;
            }

            // check if edge already exists
            foreach (var fromEdge in from.Edges) {
                if (fromEdge.NodeA == to || fromEdge.NodeB == to) {
                    newEdge = fromEdge;
                    isEdgeNew = false;
                    return true;
                }
            }

            // TODO check intersections here
            var fromNode = (LineNode)from; //why...
            var toNode = (LineNode)to;
            var edge = new LineEdge() {
                NodeA = fromNode,
                NodeB = toNode
            };
            addEdge(edge);
            fromNode.AddEdge(edge);
            toNode.AddEdge(edge);
            newEdge = edge;
            isEdgeNew = true;
            return true;
        }

        public override bool RemoveNode(IStreetNode node) {
            throw new NotImplementedException();
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            var n = new LineNode() {
                Position = position
            };
            nodes.Add(n);
            newNode = n;
            return true;
        }

        public static bool Intersection(Vector3 A, 
                Vector3 a, 
                Vector3 B, 
                Vector3 b, 
                out Vector3 intersectionPoint, 
                out float t, 
                out float s,
                float eps = 0.0001f) {
            Vector2 p = a;
            Vector2 q = b;
            Vector2 r = B - A;

            float denom = p.y * q.x - p.x * q.y;

            if (denom == 0) {
                //line parallel
                t = float.NaN;
                s = float.NaN;
                intersectionPoint = Vector3.zero;
                return false;
            }

            t = (r.y * q.x - r.x * q.y) / denom;
            s = (r.y * p.x - r.x * p.y) / denom;
            intersectionPoint = A + t * a;
            return (t > 0 - eps && t < 1 + eps && s > 0 - eps && s < 1 + eps);
        }

        public override void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node, out IStreetEdge leftEdge, out IStreetEdge rightEdge) {
            var n = new LineNode() {
                Position = positionOnEdge
            };
            var e = (LineEdge) foundEdge;
            ((LineNode)e.NodeA).RemoveEdge(e);
            ((LineNode)e.NodeB).RemoveEdge(e);
            removeEdge(e);

            var lEdge = new LineEdge() {
                NodeA = foundEdge.NodeA,
                NodeB = n
            };

            var rEdge = new LineEdge() {
                NodeA = n,
                NodeB = foundEdge.NodeB
            };
            nodes.Add(n);
            addEdge(lEdge);
            addEdge(rEdge);

            //if this fails, we would have an edge with length 0, which is weird and should not happen
            Debug.Assert(Vector3.Distance(lEdge.NodeA.Position, lEdge.NodeB.Position) > eps);
            Debug.Assert(Vector3.Distance(rEdge.NodeA.Position, rEdge.NodeB.Position) > eps, $"Distance between {rEdge.NodeA.Position} and {rEdge.NodeB.Position} is {Vector3.Distance(rEdge.NodeA.Position, rEdge.NodeB.Position)}");

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
                var n = new LineNode {
                    Position = node.Position
                };
                copy.nodes.Add(n);
            }

            for (var i = 0; i < edges.Count; i++) {
                var edge = edges[i];
                var e = new LineEdge {
                    NodeA = copy.nodes[nodes.IndexOf((LineNode)edge.NodeA)],
                    NodeB = copy.nodes[nodes.IndexOf((LineNode)edge.NodeB)]
                };
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
                if(g.GObjects != null) {
                    foreach(var edge in g.GObjects) {
                        var distance = edge.GetDistanceEdgeToPosition(position, out var posOnEdgeTmp);
                        if (distance < radius) {
                            closeEdges.Add(edge);
                        }
                    }
                }
            }
            return closeEdges.ToArray();
        }

        private void addEdge(LineEdge edge) {
            edges.Add(edge);
            bvh.Add(edge);
            bvh.Optimize(); //maybe use batch operations for adding?
        }

        private void removeEdge(LineEdge edge) {
            edges.Remove(edge);
            bvh.Remove(edge);
            bvh.Optimize();
        }
    }

    public class LineNode: IStreetNode {
        
        public Vector3 Position { get; set; }
        
        private List<LineEdge> _edges = new List<LineEdge>();
        public IEnumerable<IStreetEdge> Edges => _edges;
        
        public int ConnectedEdgesCount => _edges.Count;
        
        public int MaxConnectedEdges { get; }
        
        public bool IsMaxConnectedEdgesReached => ConnectedEdgesCount >= MaxConnectedEdges;

        public override string ToString() {
            return $"LineNode at {Position} with {ConnectedEdgesCount} edges.";
        }

        public void AddEdge(LineEdge e) {
            if(!_edges.Contains(e)) {
                _edges.Add(e);
            }
        }

        public void RemoveEdge(LineEdge e) {
            _edges.Remove(e);
        }

        public float? EntranceAngle { get; }
    }

    public class LineEdge: IStreetEdge {
        public Vector3 PositionNodeA => NodeA.Position;
        public Vector3 PositionNodeB => NodeB.Position;

        public Vector3 Position => PositionNodeA + (PositionNodeB-PositionNodeA)/2.0f;
        public float Radius => Vector3.Distance(PositionNodeB, PositionNodeA) / 2.0f;

        public IStreetNode NodeA { get; set;}
        public IStreetNode NodeB { get; set;}
        public float StreetWidth { get; set;} = 1;
        
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