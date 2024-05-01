#nullable enable
using System.Collections.Generic;
using System.Linq;
using System;
using FreeFormGraph.World;
using FreeFormGraph;
using UnityEngine;
using Utils;

namespace FreeFormGraph.Agent {

    public class Pathfinding {
        public IStreetGraph StreetGraph;
        public IWorld World;

        private float SnapFactorEdge { get; set; } = 1.0f;
        //ideally, Node factor should be higher than edge factor TODO explain
        private float SnapFactorNode { get; set; } = 1.5f;
        private int NeighborK = 4;

        public void AStar(Vector3 start, Vector3 target) {
            var path = AStar(start, target, wp => GetNeighbors(wp, SnapFactorNode, SnapFactorEdge, NeighborK));
            if(path != null) {
                BuildPath2(path);
            }
        }

        public List<Waypoint> AStar(Vector3 start, Vector3 target, Func<Waypoint, List<Waypoint>> GetNeighbors) {

            var q = new PriorityQueue<Waypoint, float>();
            var q_set = new HashSet<Waypoint>();
            var came_from = new Dictionary<Waypoint, Waypoint>();
            var cost_so_far = new Dictionary<Waypoint, float>();

            var startWaypoint = GetWaypoint(start); //Start position might be on edge or node already
            Waypoint targetwaypoint = default;
            cost_so_far.Add(startWaypoint, 0);
            came_from.Add(startWaypoint, startWaypoint);
            q.Enqueue(startWaypoint, 0.0f);
            q_set.Add(startWaypoint);
            
            var nodesChecked = 0;
            while(q.Count != 0) {
                var current = q.Dequeue();
                q_set.Remove(current);
                nodesChecked++;
                if(current.Pos == target) {
                    targetwaypoint = current;
                    break;
                }

                if(cost_so_far.ContainsKey(current) && cost_so_far[current] == float.PositiveInfinity) {
                    continue;
                }
                
                foreach(var i in GetNeighbors(current)) {
                    var nextWaypoint = i;
                    //TODO do this in GetWaypoint()
                    if(nextWaypoint.Pos.x < 0 
                        || nextWaypoint.Pos.x >= World.Width
                        || nextWaypoint.Pos.y < 0
                        || nextWaypoint.Pos.y >= World.Height) {
                            continue;
                        }

                    var next = nextWaypoint;

                    if(q_set.Contains(next)) continue; //now that we move the position around, it's possible that we would enqueue the same target again

                    var new_cost = cost_so_far[current] + Cost(current, next);
                    if(!cost_so_far.ContainsKey(next) || new_cost < cost_so_far[next]) {
                        cost_so_far[next] = new_cost;
                        var prio = new_cost + Heuristic(target, next.Pos);
                        q.Enqueue(next, prio);
                        came_from[next] = current;
                    }
                }
                
            }

            Debug.Log($"Nodes checked {nodesChecked}");
            if(targetwaypoint != null) {
                return GetShortestPath(startWaypoint, targetwaypoint, came_from);
            } else {
                return null;
            }
        }

        private Waypoint GetWaypoint(Vector3 pos) {
            if(StreetGraph.TryFindClosestNode(pos, out var node, SnapFactorNode)) {
                return new Waypoint(node.Position) {
                    GraphNode = node
                };
            }
            else if(StreetGraph.TryFindClosestEdge(pos, out var edge, out var posOnEdge, SnapFactorEdge)) {
                return new Waypoint(posOnEdge) {
                    GraphEdge = edge
                };
            }
            return new Waypoint(pos);

        }

        private List<Waypoint> GetShortestPath(Waypoint startNode, Waypoint targetNode, Dictionary<Waypoint, Waypoint> came_from) {
            var current = targetNode;
            List<Waypoint> path = new();
            while(current != startNode) {
                path.Add(current);
                current = came_from[current];
            }
            path.Add(startNode);
            return path;
        }

        private void BuildPath2(List<Waypoint> waypoints) {
            Debug.Assert(waypoints.Count >= 2);
            //TODO assert waypoints unique

            //#24
            var thresholdWpExistingConnection = 2.0f;

            waypoints.Reverse(); //easier to debug TODO

            var currentWaypointIndex = 0;
            IStreetNode lastNode = default;
            var wp = waypoints[currentWaypointIndex];
            if(wp.GraphNode != null) {
                //no need to build node
                lastNode = wp.GraphNode;
            } else if(wp.GraphEdge != null) {
                StreetGraph.InsertNodeOnEdge(wp.GraphEdge, wp.Pos, out lastNode, out _, out _);
            } else {
                StreetGraph.CreateUnconnectedNode(waypoints[currentWaypointIndex].Pos, out lastNode);
            }
            currentWaypointIndex++;
            var lastWp = waypoints[0];

            int breakCounter = 0;

            while(currentWaypointIndex < waypoints.Count) {
                Debug.Assert(lastNode != null);
                wp = waypoints[currentWaypointIndex];
                breakCounter++;
                if(breakCounter > 10000) {
                    Debug.Log("ESCAPE!");
                    Debug.Assert(false);
                    return;
                }
                if(wp.GraphEdge != null || wp.GraphNode != null) {
                    //ensure only one state at a time
                    Debug.Assert(wp.GraphEdge != null ^ wp.GraphNode != null);
                }

                //pathfinding does not handle intersections well. It's possible that the chosen path
                //generates new roads which connects two points which are already connected (the new path would be shorter tho).
                //#24
                var roadConnection = AStarStreetOnly(lastWp, wp);
                if(roadConnection != null && GetPathLength(roadConnection) / Vector3.Distance(lastWp.Pos, wp.Pos) < thresholdWpExistingConnection) {
                    currentWaypointIndex++;
                    lastWp = wp;
                    Debug.Assert(wp.GraphNode != null ^ wp.GraphEdge != null);

                    if(wp.GraphEdge != null) {
                        StreetGraph.InsertNodeOnEdge(wp.GraphEdge, wp.Pos, out lastNode, out _, out _);
                    } else {
                        lastNode = wp.GraphNode;
                    }
                } else {
                    StreetGraph.CreateEdge(lastNode, wp.Pos, out var newEdge, out lastNode, out var isNewNode);

                    if(Vector3.Distance(lastNode.Position, wp.Pos) > 0.001f) {
                        //found an intersection, keep next waypoint
                    } else {
                        currentWaypointIndex++;
                        lastWp = wp;
                    }
                }
            }
        }

        private float Cost(Waypoint current, Waypoint next) {
            var cost = Vector3.Distance(current.Pos, next.Pos)*2;
            /*if(cost <= 1.01f) {
                cost += 0.1f; //make short segments more costly to force fewer nodes
            }*/
            if(next.GraphEdge != null || next.GraphNode != null) {
                cost /= 2;
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
            if(cost >= 3.0f) return float.MaxValue;
            return cost;
        }

        private float Heuristic(Vector3 target, Vector3 current) {
            return Vector3.Distance(target, current);
        }

        public List<Waypoint> GetNeighbors(Waypoint n, float SnapFactorNode, float SnapFactorEdge, int k) {
            var list = new List<Waypoint>(); //list of new waypoint to be explored in A*
            var skipEdge = new List<IStreetEdge>();
            var skipNode = new List<IStreetNode>();
            
            //we are sitting on a node
            if(n.GraphNode != null) {
                foreach(var edge in n.GraphNode.Edges) {
                    var otherNode = edge.NodeA;
                    if(otherNode == n.GraphNode) otherNode = edge.NodeB;

                    var newWaypoint = new Waypoint(otherNode.Position) {
                        GraphNode = otherNode,
                        CameFrom = n
                    };
                    list.Add(newWaypoint);
                    skipEdge.Add(edge);
                }
                skipNode.Add(n.GraphNode);
            }
            //we are sitting on a edge
            if(n.GraphEdge != null) { //TODO assert that only one of them is active
                list.Add(new Waypoint(n.GraphEdge.NodeA.Position) {
                    GraphNode = n.GraphEdge.NodeA
                });
                list.Add(new Waypoint(n.GraphEdge.NodeB.Position) {
                    GraphNode = n.GraphEdge.NodeB
                });
                skipEdge.Add(n.GraphEdge);
                skipNode.Add(n.GraphEdge.NodeA);
                skipNode.Add(n.GraphEdge.NodeB);
            }

            var currentPosition = n.Pos;
            //snap position to grid in case we are on a edge/node which does not lie on grid
            currentPosition = new Vector3(Mathf.Round(currentPosition.x), Mathf.Round(currentPosition.y), 0);

            
            //see Marechal et al. section 5.1
            //this is the case were we are currently not on existing roads
            for(int i = -k; i <= k; i++) {
                for(int j = -k; j <= k; j++) {
                    if(GCD(i, j) == 1) {
                        var newPos = new Vector3(i, j, 0) + currentPosition;
                        var wp = new Waypoint(newPos);
                        if(StreetGraph.TryFindClosestNode(newPos, out var node, SnapFactorNode)) {
                            //move this point to the closest node
                            if(!skipNode.Contains(node)) {
                                wp.Pos = node.Position;
                                wp.GraphNode = node;
                                list.Add(wp);
                            }
                        } else if(StreetGraph.TryFindClosestEdge(newPos, out var edge, out var posOnEdge, SnapFactorEdge)) {
                            //move this point to the closest edge
                            if(!skipEdge.Contains(edge)) {
                                wp.Pos = posOnEdge;
                                wp.GraphEdge = edge;
                                list.Add(wp);
                            }
                        } else {
                            list.Add(wp);
                        }
                    }
                }
            }
            foreach(var wp in list) {
                Debug.Assert(!skipEdge.Contains(wp.GraphEdge));
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

        public List<Waypoint> AStarStreetOnly(Waypoint start, Waypoint target) {
            var startPos = start.Pos;
            var endPos = target.Pos;
            //a bit hacky, pathfinding on roads only makes only sense between nodes -> move position on edge to closest node :)
            if(start.GraphEdge != null) {
                if(Vector3.Distance(startPos, start.GraphEdge.NodeA.Position) < Vector3.Distance(startPos, start.GraphEdge.NodeB.Position)) {
                    startPos = start.GraphEdge.NodeA.Position;
                } else {
                    startPos = start.GraphEdge.NodeB.Position;
                }
            }
            if(target.GraphEdge != null) {
                if(Vector3.Distance(endPos, target.GraphEdge.NodeA.Position) < Vector3.Distance(endPos, target.GraphEdge.NodeB.Position)) {
                    endPos = target.GraphEdge.NodeA.Position;
                } else {
                    endPos = target.GraphEdge.NodeB.Position;
                }
            }

            //TODO refactor AStar to pass waypoint
            return AStar(startPos, endPos, wp => GetNeighbors(wp, 0, 0, 0));
        }

        public float GetPathLength(List<Waypoint> waypoints) {
            float length = 0;
            for(var i = 0; i < waypoints.Count-1; i++) {
                //calculating total length via geometry is not possible here
                //because there is no geometry yet (i.e. Length() from IStreetEdge)!
                length += Vector3.Distance(waypoints[i].Pos, waypoints[i+1].Pos);
            }
            return length;
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
                var hasNode = GraphNode != null ? " With node" : "";
                var hasEdge = GraphEdge != null ? " With edge" : "";
                return $"Waypoint {Pos}{hasNode}{hasEdge}";
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
                if(c1 is null && c2 is null) return true;
                if(c1 is null && c2 is not null) return false;
                return c1.Equals(c2); 
            }

            public static bool operator !=(Waypoint c1, Waypoint c2) { 
                if(c1 is null && c2 is null) return false;
                if(c1 is null && c2 is not null) return true;
                return !c1.Equals(c2); 
            }
        }

    }

}