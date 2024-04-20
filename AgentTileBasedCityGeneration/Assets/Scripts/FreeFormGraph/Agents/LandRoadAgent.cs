using System.Collections.Generic;
using System.Linq;
using FreeFormGraph.World;
using FreeFormGraph;
using UnityEngine;
using Utils;

namespace FreeFormGraph.Agent {

    public class LandRoadAgent {
        public IStreetGraph StreetGraph;
        public IWorld World;

        private float SnapFactor { get; set; } = 0.5f;

        public void AStar(Vector3 start, Vector3 target) {

            var q = new PriorityQueue<Waypoint, float>();
            var q_set = new HashSet<Waypoint>();
            var came_from = new Dictionary<Waypoint, Waypoint>();
            var cost_so_far = new Dictionary<Waypoint, float>();

            cost_so_far.Add(new Waypoint(start), 0);
            var startNode = new Waypoint(start);
            came_from.Add(startNode, startNode); //TODO
            q.Enqueue(startNode, 0.0f);
            q_set.Add(startNode);
            
            var nodesChecked = 0;
            while(q.Count != 0) {
                var current = q.Dequeue();
                q_set.Remove(current);
                nodesChecked++;
                if(current.Pos == target) {
                    break;
                }
                
                foreach(var i in GetNeighbors(current)) {
                    var nextWaypoint = i;
                    if(nextWaypoint.Pos.x < 0 
                        || nextWaypoint.Pos.x >= World.Width
                        || nextWaypoint.Pos.y < 0
                        || nextWaypoint.Pos.y >= World.Height) {
                            continue;
                        }

                    if(StreetGraph.TryFindClosestNode(nextWaypoint.Pos, out var node, SnapFactor)) {
                        nextWaypoint.Pos = node.Position;
                        nextWaypoint.GraphNode = node;
                        //Debug.Log($"Found interscetion with node {nextWaypoint}");
                    } else if(StreetGraph.TryFindClosestEdge(nextWaypoint.Pos, out var edge, out var positionOnEdge, SnapFactor)) {
                        nextWaypoint.Pos = positionOnEdge;
                        nextWaypoint.GraphEdge = edge;
                        //Debug.Log($"Found interscetion with edge {nextWaypoint}");
                    }

                    var next = nextWaypoint;

                    if(q_set.Contains(next)) continue; //now that we move the position around it's possible that we would enqueue the same target again

                    var new_cost = cost_so_far[current] + Cost(current, next);
                    if(!cost_so_far.ContainsKey(next) || new_cost < cost_so_far[next]) {
                        cost_so_far[next] = new_cost;
                        var prio = new_cost + Heuristic(start, target, next.Pos);
                        q.Enqueue(next, prio);
                        came_from[next] = current;
                    }
                }
                
            }

            Debug.Log($"Nodes checked {nodesChecked}");
            BuildShortestPath(start, target, came_from);
        }

        private void BuildShortestPath(Vector3 start, Vector3 target, Dictionary<Waypoint, Waypoint> came_from) {
            var s = new HashSet<IStreetNode>();
            StreetGraph.CreateUnconnectedNode(target, out var lastNode);
            s.Add(lastNode);

            var dbg_visited_edges = new Dictionary<IStreetEdge, int>();

            var startNode = new Waypoint(start);
            var targetNode = new Waypoint(target);
            var current = targetNode;
            while(current != startNode) {

                Debug.Assert(dbg_visited_edges.All(i => i.Value == 1));
                if(current.GraphEdge != null) {
                    //dbg_visited_edges.Add(current.GraphEdge, 1 + dbg_visited_edges.GetValueOrDefault(current.GraphEdge, 0));
                }

                current = came_from[current];
                StreetGraph.CreateEdge(lastNode, current.Pos, out _, out lastNode, out _);

                Debug.Assert(!s.Contains(lastNode));
                s.Add(lastNode);
            }
        }

        private float Cost(Waypoint current, Waypoint next) {
            var cost = Vector3.Distance(current.Pos, next.Pos);
            /*if(cost <= 1.01f) {
                cost += 0.1f; //make short segments more costly to force fewer nodes
            }*/
            if(next.GraphEdge != null || next.GraphNode != null) {
                cost -= 0.5f;
            }
            if(next.CameFrom != null && next.CameFrom == current) {
                //we walked over an existing edge!
                //cost *= 0.1f;
                //cost = 0;
            }
            var slopeCost = SlopeCost(current, next);
            return cost + slopeCost;
        }

        private float SlopeCost(Waypoint a, Waypoint b) {
            var cost = Mathf.Abs(World.GetHeightAt(a.Pos.x, a.Pos.y) - World.GetHeightAt(b.Pos.x, b.Pos.y)) * 3;
            cost = cost * cost;
            if(cost >= 1.0f) return float.MaxValue;
            return cost;
        }

        private float Heuristic(Vector3 start, Vector3 target, Vector3 current) {
            return Vector3.Distance(target, current);
        }

        public List<Waypoint> GetNeighbors(Waypoint n, int k = 6) {
            var currentPosition = n.Pos;
            var list = new List<Waypoint>();
            if(n.GraphNode != null) {
                foreach(var edge in n.GraphNode.Edges) {
                    var otherNode = edge.NodeA;
                    if(otherNode == n.GraphNode) otherNode = edge.NodeB;

                    var newWaypoint = new Waypoint(currentPosition + otherNode.Position) {
                        GraphNode = n.GraphNode,
                        //GraphEdge = edge,
                        CameFrom = n
                    };
                    list.Add(newWaypoint);
                }
            }

            currentPosition = new Vector3(Mathf.Round(currentPosition.x), Mathf.Round(currentPosition.y), 0);
            //see Marechal et al. section 5.1
            for(int i = -k; i <= k; i++) {
                for(int j = -k; j <= k; j++) {
                    if(GCD(i, j) == 1) {
                        list.Add(new Waypoint(new Vector3(i, j, 0) + currentPosition));
                    }
                }
            }

            if(n.GraphEdge != null) {
                var newList = new List<Waypoint>();
                foreach(var wp in list) {
                    if(StreetGraph.TryFindClosestEdge(wp.Pos, out var foundEdge, out _, SnapFactor)) {
                        //if we would snap to the current edge again we don't allow this
                        //otherwise insertion code gets more complicated
                        //not really in issue in production (likely?) as edges will be short anyways
                        if(foundEdge != wp.GraphEdge) {
                            newList.Add(wp);
                        }
                    } else {
                        newList.Add(wp);
                    }
                }

                list.Add(new Waypoint(n.GraphEdge.NodeA.Position) {
                    GraphNode = n.GraphEdge.NodeA,
                    //GraphEdge = n.GraphEdge,
                    CameFrom = n
                });
                list.Add(new Waypoint(n.GraphEdge.NodeB.Position) {
                    GraphNode = n.GraphEdge.NodeB,
                    //GraphEdge = n.GraphEdge,
                    CameFrom = n
                });
            }

            return list;
        }

        public static int GCD(int p, int q)
        {
            if(p < 0) p = -p;
            if(q < 0) q = -q;
            if(p == 0) return q;
            if(q == 0) return p;
            if(p < q) {
                var tmp = p;
                p = q;
                q = tmp;
            }

            int r = p % q;
            return GCD(q, r);
        }


        public class Waypoint {
            public Vector3 Pos {get; set;}

            public IStreetNode? GraphNode {get; set;}
            public IStreetEdge? GraphEdge {get; set;}
            public Waypoint? CameFrom {get; set;}

            public Waypoint(Vector3 p) {
                Pos = p;
                GraphEdge = null;
                GraphNode = null;
                CameFrom = null;
            }

            public override string ToString() {
                return $"Node {Pos}";
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is Waypoint)) return false;
                var other = ((Waypoint) obj);
                return this.Pos == other.Pos 
                    && this.GraphNode == other.GraphNode 
                    && this.GraphEdge == other.GraphEdge;
            }

            public override int GetHashCode() => this.Pos.GetHashCode();

            public static bool operator ==(Waypoint c1, Waypoint c2) {
                if(c1 is null) return false;    
                return c1.Equals(c2); 
            }

            public static bool operator !=(Waypoint c1, Waypoint c2) { 
                if(c1 is null) return false;
                return !c1.Equals(c2); 
            }
        }

    }

}