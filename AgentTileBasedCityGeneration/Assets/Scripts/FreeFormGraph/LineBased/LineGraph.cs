using System;
using UnityEngine;
using System.Collections.Generic;
using FreeFormGraph;
using UnityEngine.Splines;

namespace FreeFormGraph.LineBased {
    public class LineGraph : MonoBehaviour, IStreetGraph {

        private readonly List<LineEdge> _edges = new();
        private readonly List<LineNode> _nodes = new();
        public IEnumerable<IStreetNode> Nodes => _nodes;
        public IEnumerable<IStreetEdge> Edges => _edges;


        public int NodeCount { get; }
        public int EdgeCount { get; }
        

        public float SnapToExistingNodeThreshold { get; set; } = 0;
        public float SnapToExistingEdgeThreshold { get; set; } = 0;

        public bool CreateEdge(IStreetNode from, Vector3 to, out IStreetEdge newEdge, out IStreetNode toNode,
            out bool isToNodeNew) {
                

                IStreetEdge? lastIntersectionEdge = null;
                //check for intersections
                //TODO: ignore source edge
                /*foreach(var e in _edges) {
                    var A = from.Position;
                    var a = to-A;
                    var B = e.NodeA.Position;
                    var b = e.NodeB.Position - B;
                    var intersects = Intersection(A, a, B, b, out var crossPoint, out var t, out var s);

                    if(intersects) {
                        to = crossPoint;
                        lastIntersectionEdge = e;
                    }
                }*/

                isToNodeNew = true;

                if(lastIntersectionEdge != null) {
                    InsertNodeOnEdge(lastIntersectionEdge, to, out toNode, out _, out _);
                } else {
                    var node = new LineNode() {
                        Position = to
                    };
                    _nodes.Add(node);
                    toNode = node;
                }



                return CreateEdge(from, toNode, out newEdge);
            }
        
        public bool CreateEdge(IStreetNode from, IStreetNode to, out IStreetEdge newEdge) {
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

        public bool RemoveNode(IStreetNode node) {
            throw new NotImplementedException();
        }

        public bool CreateUnconnectedNode(Vector3 position, out IStreetNode newNode) {
            var n = new LineNode() {
                Position = position
            };
            _nodes.Add(n);
            newNode = n;
            return true;
        }

        public static bool Intersection(Vector3 A, Vector3 a, Vector3 B, Vector3 b, out Vector3 intersectionPoint, out float t, out float s) 
        {
            float denominator = a.x * b.y - a.y * b.x;
            
            if (denominator == 0)  // If the lines are parallel
            {
                intersectionPoint = Vector3.zero;
                t = 0;
                s = 0;
                return false;
            }

            t = ((B.x - A.x) * b.y - (B.y - A.y) * b.x) / denominator;
            s = ((A.x - B.x) * a.y - (A.y - B.y) * a.x) / denominator;

            intersectionPoint = new Vector3(A.x + t * a.x, A.y + t * a.y, 0);
            return true;
        }

        public void InsertNodeOnEdge(IStreetEdge foundEdge, Vector3 positionOnEdge, out IStreetNode node, out IStreetEdge leftEdge, out IStreetEdge rightEdge) {
            var n = new LineNode() {
                Position = positionOnEdge
            };
            var e = (LineEdge) foundEdge;
            ((LineNode)e.NodeA).RemoveEdge(e);
            ((LineNode)e.NodeB).RemoveEdge(e);

            var lEdge = new LineEdge() {
                NodeA = foundEdge.NodeA,
                NodeB = n
            };

            var rEdge = new LineEdge() {
                NodeA = n,
                NodeB = foundEdge.NodeB
            };
            _nodes.Add(n);
            _edges.Remove(e);
            _edges.Add(lEdge);
            _edges.Add(rEdge);
            leftEdge = lEdge;
            rightEdge = rEdge;
            node = n;
        }

        public float GetDistanceEdgeToPosition(IStreetEdge edge, Vector3 position, out Vector3 positionOnEdge) {
            positionOnEdge = SplineMath.PointLineNearestPoint(position, edge.NodeA.Position, edge.NodeB.Position, out _);
            return Vector3.Distance(position, positionOnEdge);
        }

        
    }

    public class LineNode: IStreetNode {
        
        public Vector3 Position { get; set; }
        
        private List<LineEdge> _edges = new List<LineEdge>();
        public IEnumerable<IStreetEdge> Edges => _edges;
        
        public int ConnectedEdgesCount { get; }
        
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
        public Vector3 PosA => NodeA.Position;
        public Vector3 PosB => NodeB.Position;
        public IStreetNode NodeA { get; set;}
        public IStreetNode NodeB { get; set;}
        public float StreetWidth { get; set;} = 1;
        
        public Vector3[] SplitIntoEvenlySpacedPoints(float stepSize = 0.1f) {
            return new Vector3[] {};
        }
    }
}