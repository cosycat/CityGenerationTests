using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;

namespace FreeFormGraph.LineBased {
    public class LineGraph : StreetGraphGameObject {

        private readonly List<LineEdge> _edges = new();
        private readonly List<LineNode> _nodes = new();
        public override IEnumerable<IStreetNode> Nodes => _nodes;
        public override IEnumerable<IStreetEdge> Edges => _edges;


        public override int NodeCount => _nodes.Count;
        public override int EdgeCount => _edges.Count;

        private float eps = 0.0001f;
        

        public override float SnapToExistingNodeThreshold { get; set; } = 0.2f;
        public override float SnapToExistingEdgeThreshold { get; set; } = 0;

        public override bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew) {
                

                IStreetEdge? lastIntersectionEdge = null;
                //check for intersections
                foreach(var e in _edges) {
                    //skip node we are comming from to prevent finding intersection with edge we are connected to
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
                        InsertNodeOnEdge(lastIntersectionEdge, to, out toNode, out _, out _);
                    }

                } else {
                    var node = new LineNode() {
                        Position = to
                    };
                    _nodes.Add(node);
                    toNode = node;
                }



                return CreateEdge(from, toNode, out newEdge);
            }
        
        public override bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge) {
            var fromNode = (LineNode)from; //why...
            var toNode = (LineNode)to;
            var edge = new LineEdge() {
                NodeA = fromNode,
                NodeB = toNode
            };
            _edges.Add(edge);
            fromNode.AddEdge(edge);
            toNode.AddEdge(edge);
            newEdge = edge;
            return true;
        }

        public override bool RemoveNode(IStreetNode node) {
            throw new NotImplementedException();
        }

        public override bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            var n = new LineNode() {
                Position = position
            };
            _nodes.Add(n);
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
            _edges.Remove(e);

            var lEdge = new LineEdge() {
                NodeA = foundEdge.NodeA,
                NodeB = n
            };

            var rEdge = new LineEdge() {
                NodeA = n,
                NodeB = foundEdge.NodeB
            };
            _nodes.Add(n);
            _edges.Add(lEdge);
            _edges.Add(rEdge);

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

        
    }

    public class LineNode: IStreetNode {
        
        public Vector3 Position { get; set; }
        
        private List<LineEdge> _edges = new List<LineEdge>();
        public IEnumerable<IStreetEdge> Edges => _edges;
        
        public int ConnectedEdgesCount => _edges.Count;
        
        public int MaxConnectedEdges { get; }
        
        public bool IsMaxConnectedEdgesReached => ConnectedEdgesCount >= MaxConnectedEdges;

        public override string ToString() {
            return $"LineNode at Position {Position}";
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
    }
}