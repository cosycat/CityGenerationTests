#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using FreeFormGraph.World;
using UnityEngine;
using Utils;
using UnityEngine.Profiling;
using DebugUtils;

namespace FreeFormGraph.Agents {

    public class Pathfinding {
        private readonly IStreetGraph streetGraph;
        private readonly IWorld world;

        private readonly PriorityQueue<Waypoint, float> q = new();
        
        private readonly Dictionary<Waypoint, Waypoint> cameFrom = new();
        private readonly Dictionary<Waypoint, float> costSoFar = new();

        /// <summary>
        /// This is the position where pathfinding was started from.
        /// Is null if pathfinding was not yet started.
        /// </summary>
        public Waypoint? StartPosition { get; private set; } = null;
        /// <summary>
        /// Current position of pathfinding process. If pathfinding 
        /// successfully finished, this will be the same as the target. If
        /// pathfinding fails to find a route, this will be the last position
        /// evaluated.
        /// </summary>
        public Waypoint? Current { get; private set; } = null;
        /// <summary>
        /// This is the goal to reach via pathfinding.
        /// Is null if pathfinding was not yet started.
        /// </summary>
        public Vector2? Target { get; private set; } = null;

        //only for statistics
        private int getNeighborsCalled = 0;
        private ISet<Waypoint> visited = new HashSet<Waypoint>();

        private readonly Parameters parameters;

        public Pathfinding(IStreetGraph streetGraph, IWorld world, Parameters parameters) {
            Debug.Assert(streetGraph != null);
            Debug.Assert(world != null);
            this.streetGraph = streetGraph!;
            this.world = world!;
            this.parameters = parameters;
        }

        public List<Waypoint>? AStar(Vector2 start, Vector2 target) {
            return AStar(start, target, wp => GetNeighbors(wp, parameters.SnapFactorNode, parameters.SnapFactorEdge, parameters.moveMaskK), () => false);
        }

        public List<Waypoint>? AStar(Vector2 start, Vector2 target, Func<bool> isCancelled, bool perfStats = true) {
            Debug.Log($"Start pathfinding from {start} to {target}");
            return AStar(start, target, wp => GetNeighbors(wp, parameters.SnapFactorNode, parameters.SnapFactorEdge, parameters.moveMaskK), isCancelled, perfStats);
        }

        public List<Waypoint>? AStar(Vector2 start, Vector2 target, Func<Waypoint, List<Waypoint>> getNeighbors, Func<bool> isCancelled, bool perfStats = false) {
            this.Target = target;
            var startWaypoint = GetWaypoint(start); //Start position might be on edge or node already
            StartPosition = startWaypoint;
            Waypoint? targetWaypoint = default;
            costSoFar.Add(startWaypoint, 0);
            cameFrom.Add(startWaypoint, startWaypoint);
            q.Enqueue(startWaypoint, 0.0f);
            
            var sw = new System.Diagnostics.Stopwatch();
            if(perfStats) sw.Start();


            var nodesChecked = 0;
            while(q.Count != 0) {
                if(isCancelled()) return null;

                Current = q.Dequeue();
                nodesChecked++;
                if(Current.Pos == target) {
                    targetWaypoint = Current;
                    break;
                }
                if(visited.Contains(Current)) continue;
                visited.Add(Current);
                if(costSoFar.ContainsKey(Current) && float.IsPositiveInfinity(costSoFar[Current])) continue;
                
                foreach(var i in getNeighbors(Current)) {
                    var nextWaypoint = i;
                    //TODO do this in GetWaypoint()
                    if(nextWaypoint.Pos.x < 0 
                        || nextWaypoint.Pos.x >= world.Width
                        || nextWaypoint.Pos.y < 0
                        || nextWaypoint.Pos.y >= world.Height) {
                        continue;
                    }
                    
                    Debug.Assert(nextWaypoint.Pos != Current.Pos);

                    var next = nextWaypoint;


                    var newCost = costSoFar[Current] + Cost(Current, next, parameters);
                    if(float.IsPositiveInfinity(newCost)) continue;
                    Debug.Assert(Cost(Current, next, parameters) >= Heuristic(Current.Pos, next.Pos, parameters));
                    if(!costSoFar.ContainsKey(next) || newCost < costSoFar[next]) {
                        costSoFar[next] = newCost;
                        var prio = newCost + Heuristic(target, next.Pos, parameters);
                        q.Enqueue(next, prio);
                        cameFrom[next] = Current;
                    }
                }
                
            }

            if(perfStats) {
                sw.Stop();
                var secs = sw.ElapsedMilliseconds / 1000.0f;
                var heuristicCost = Heuristic(start, target, parameters);
                // current is not null because it enters the loop at least once (start node)
                var actualCost = costSoFar[Current!];
                Debug.Log($"A* perf: Elapsed (s): {secs}; " +
                        $"Nodes checked: {nodesChecked}; " +
                        $"Throughput (nodes/sec): {nodesChecked / secs}; " +
                        $"World edges count: {streetGraph.Edges.ToList().Count}; " +
                        $"Queue size: {q.Count}; " +
                        $"Get neighbors calls: {getNeighborsCalled}; " +
                        $"Visited count: {visited.Count}; " +
                        $"Cost ratio: {actualCost / heuristicCost}");
            }

            if (targetWaypoint != null) {
                return GetShortestPath(startWaypoint, targetWaypoint);
            } else {
                return null;
            }
        }

        private Waypoint GetWaypoint(Vector2 pos) {
            if(streetGraph.TryFindClosestNode(pos, out var node, parameters.SnapFactorNode)) {
                return new Waypoint(node.Position, node);
            }
            else if(streetGraph.TryFindClosestEdge(pos, out var edge, out var posOnEdge, parameters.SnapFactorEdge)) {
                return new Waypoint(posOnEdge, edge: edge);
            }
            return new Waypoint(pos);

        }

        public List<Waypoint> GetShortestPath(Waypoint startNode, Waypoint targetNode) {
            var current = targetNode;
            List<Waypoint> path = new();
            while(current != startNode) {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(startNode);
            path.Reverse();
            return path;
        }

        public static bool BuildPath2(List<Waypoint> waypoints, IStreetGraph streetGraph, IWorld world, Parameters p) {
            Debug.Assert(waypoints.Count >= 2);
            //TODO assert waypoints unique

            //#24
            var thresholdWpExistingConnection = p.thresholdExistingConnection;

            var numNodesBefore = streetGraph.NodeCount;
            var numEdgesBefore = streetGraph.EdgeCount;
            var removeNodes = new List<IStreetNode>();
            var removeEdges = new List<IStreetEdge>();
            var removeRoad = false;

            var currentWaypointIndex = 0;
            IStreetNode? lastNode;
            var wp = waypoints[currentWaypointIndex];
            if(wp.GraphNode != null) {
                //no need to build node
                lastNode = wp.GraphNode;
            } else if(wp.GraphEdge != null) {
                streetGraph.InsertNodeOnEdge(wp.GraphEdge, wp.Pos, out lastNode, out _, out _);
                GraphDebugUtils.AssertStreetGraphConnectivity(world);
            } else {
                streetGraph.CreateUnconnectedNode(waypoints[currentWaypointIndex].Pos, out lastNode);
                removeNodes.Add(lastNode);
            }
            currentWaypointIndex++;
            var lastWp = waypoints[0];

            int breakCounter = 0;

            while(currentWaypointIndex < waypoints.Count) {
                Debug.Assert(lastNode != null);
                var pf = new Pathfinding(streetGraph, world, new());
                wp = waypoints[currentWaypointIndex];
                Debug.Assert(lastWp != wp);
                breakCounter++;
                if(breakCounter > 10000) {
                    Debug.Assert(false, $"Trying to build path with length: {waypoints.Count}, looping for too long...");
                    return false;
                }
                if(wp.GraphEdge != null || wp.GraphNode != null) {
                    //ensure only one state at a time
                    Debug.Assert(wp.GraphEdge != null ^ wp.GraphNode != null);
                }

                //pathfinding does not handle intersections well. It's possible that the chosen path
                //generates new roads which connects two points which are already connected (the new path would be shorter tho).
                //#24
                var roadConnection = AStarStreetOnly(world, lastWp, wp, () => false);
                if(roadConnection != null && GetPathLength(roadConnection) / Vector2.Distance(lastWp.Pos, wp.Pos) < thresholdWpExistingConnection) {
                    currentWaypointIndex++;
                    lastWp = wp;
                    Debug.Assert(wp.GraphNode != null ^ wp.GraphEdge != null);

                    if(wp.GraphEdge != null) {
                        streetGraph.InsertNodeOnEdge(wp.GraphEdge, wp.Pos, out lastNode, out _, out _);
                        removeNodes.Add(lastNode);
                    } else {
                        lastNode = wp.GraphNode;
                    }
                } else {
                    Debug.Assert(lastNode != null, "Last node is null");
                    var edgeCreated = streetGraph.CreateEdge(lastNode!, wp.Pos, out var newEdge, out lastNode, out var isToNodeNew, out var isEdgeNew);
                    //edge creation might fail because the road angle is to small or there are too many connections to a node already...
                    //the easiest way to handle these issues is to just remove the road altogether.
                    if(isToNodeNew) removeNodes.Add(lastNode);
                    if(isEdgeNew) removeEdges.Add(newEdge);
                    if(!edgeCreated) {
                        removeRoad = true;
                        break;
                    } 

                    if(Vector2.Distance(lastNode.Position, wp.Pos) > 0.001f) {
                        //found an intersection, keep next waypoint
                    } else {
                        currentWaypointIndex++;
                        lastWp = wp;
                    }
                }
            }

            if(removeRoad) {
                foreach(var e in removeEdges) {
                    streetGraph.RemoveEdge(e);
                }
                Debug.Assert(streetGraph.EdgeCount == numEdgesBefore);
                foreach(var n in removeNodes) {
                    streetGraph.RemoveNode(n);
                }
                Debug.Assert(streetGraph.NodeCount == numNodesBefore);
                return false;
            }
        
            GraphDebugUtils.AssertStreetGraphConnectivity(world);
            return true;
        }

        private float Cost(Waypoint current, Waypoint next, Parameters p) {
            Debug.Assert(current.Pos != next.Pos, "Current and next waypoint are the same");
            var cost = Vector2.Distance(current.Pos, next.Pos);
            /*if(cost <= 1.01f) {
                cost += 0.1f; //make short segments more costly to force fewer nodes
            }*/
            
            var costPenaltyForRoad = p.roadDistanceCostMultiplier1;
            float slopeCost = 0;
            if(next.DidUseRoad) {
                //we are walking over an existing road. make it cheap
                costPenaltyForRoad = p.roadDistanceCostMultiplier2;
            }
            else if(next.GraphEdge != null || next.GraphNode != null) {
                costPenaltyForRoad = p.roadDistanceCostMultiplier3;
            } 
            cost *= costPenaltyForRoad;

            var heightStart = world.GetHeightAt(current.Pos.x, current.Pos.y);
            var heightEnd = world.GetHeightAt(next.Pos.x, next.Pos.y);

            if(heightEnd > p.maxRoadElevation) {
                return float.PositiveInfinity;
            }

            if(!next.DidUseRoad) {
                //no slope penalty for existing roads
                slopeCost = SlopeCost(world, current, next, p, heightStart, heightEnd);
            }

            var heightPenalty = heightEnd * p.heightPenaltyMultiplier;
            var totalCost = cost + slopeCost + heightPenalty;
            Debug.Assert(Heuristic(current.Pos, next.Pos, p) <= totalCost);
            return totalCost;
        }

        public static float SlopeCost(IWorld w, Waypoint a, Waypoint b, Parameters p, float heightStart, float heightEnd) {
            var cost = Mathf.Abs(heightStart - heightEnd) / Vector2.Distance(a.Pos, b.Pos);
            //0.5f = 50 % slope
            if(cost > p.slopeCostMaxGrade) return float.PositiveInfinity;
            cost = cost * cost;
            cost *= p.slopeCostMultiplier;
            return cost;
        }

        private float Heuristic(Vector2 target, Vector2 current, Parameters p) {
            return Vector2.Distance(target, current) * p.heuristicMultiplier;
        }

        public List<Waypoint> GetNeighbors(Waypoint n, float snapFactorNode, float snapFactorEdge, int k) {
            getNeighborsCalled++;

            var list = new List<Waypoint>(); //list of new waypoint to be explored in A*
            var skipEdge = new List<IStreetEdge>();
            var skipNode = new List<IStreetNode>();
            
            //we are sitting on a node
            if(n.GraphNode != null) {
                foreach(var edge in n.GraphNode.Edges) {
                    var otherNode = edge.NodeA;
                    if(otherNode == n.GraphNode) otherNode = edge.NodeB;
                    Debug.Assert(edge.NodeA != edge.NodeB);

                    var newWaypoint = new Waypoint(otherNode.Position) {
                        GraphNode = otherNode,
                        DidUseRoad = true
                    };
                    list.Add(newWaypoint);
                    skipEdge.Add(edge);
                    skipNode.Add(otherNode);
                }
                skipNode.Add(n.GraphNode);
            }
            //we are sitting on a edge
            if(n.GraphEdge != null) { //TODO assert that only one of them is active
                list.Add(new Waypoint(n.GraphEdge.NodeA.Position) {
                    GraphNode = n.GraphEdge.NodeA,
                    DidUseRoad = true
                });
                list.Add(new Waypoint(n.GraphEdge.NodeB.Position) {
                    GraphNode = n.GraphEdge.NodeB,
                    DidUseRoad = true
                });
                skipEdge.Add(n.GraphEdge);
                skipNode.Add(n.GraphEdge.NodeA);
                skipNode.Add(n.GraphEdge.NodeB);
            }

            var currentPosition = n.Pos;
            //snap position to grid in case we are on a edge/node which does not lie on grid
            currentPosition = new Vector2(Mathf.Round(currentPosition.x), Mathf.Round(currentPosition.y));

            var possibleEdges = streetGraph.FindAllEdgesWithinRange(n.Pos, Mathf.Sqrt(k*k + k*k) + snapFactorEdge);
            var possibleNodes = new List<IStreetNode>();
            for(int i = 0; i < possibleEdges.Count(); i++) {
                //prefiltering nodes to be included in the radius is not necessary. TryFindClosestNode will
                //loop through them again anyway and discard the ones to far away
                possibleNodes.Add(possibleEdges[i].NodeA);
                possibleNodes.Add(possibleEdges[i].NodeB);
            } 

            //see Marechal et al. section 5.1
            //this is the case were we are currently not on existing roads
            for(int i = -k; i <= k; i++) {
                for(int j = -k; j <= k; j++) {
                    if (GCD(i, j) != 1) continue;
                    
                    var newPos = new Vector2(i, j) + currentPosition;
                    if(streetGraph.TryFindClosestNode(possibleNodes, newPos, out var node, snapFactorNode)) {
                        //move this point to the closest node
                        if(!skipNode.Contains(node)) {
                            var wp = new Waypoint(node.Position, node);
                            list.Add(wp);
                        }
                    } else if(streetGraph.TryFindClosestEdge(possibleEdges, newPos, out var edge, out var posOnEdge, snapFactorEdge)) {
                        //move this point to the closest edge
                        if(!skipEdge.Contains(edge)) {
                            var wp = new Waypoint(posOnEdge, edge: edge);
                            list.Add(wp);
                        }
                    } else {
                        var wp = new Waypoint(newPos);
                        list.Add(wp);
                    }
                }
            }
            foreach(var wp in list) {
                Debug.Assert(wp.GraphEdge == null || !skipEdge.Contains(wp.GraphEdge));
            }

            //TODO why does this fail so often?
            //Debug.Assert(list.Count == new HashSet<Waypoint>(list).Count);

            return list;
        }

        public static int GCD(int p, int q)
        {
            if(p < 0) p = -p;
            if(q < 0) q = -q;
            if(p == 0) return q;
            if(q == 0) return p;
            if(p < q) {
                (p, q) = (q, p);
            }

            int r = p % q;
            return GCD(q, r);
        }

        public static List<Waypoint>? AStarStreetOnly(IWorld world,
                Waypoint start, 
                Waypoint target, 
                Func<bool> isCancelled) {
            Debug.Assert(start.Pos != target.Pos);
            var pathfinding = new Pathfinding(world.StreetGraph, world, Parameters.GetRoadPathSearchParameters());
            var startPos = start.Pos;
            var endPos = target.Pos;
            //a bit hacky, pathfinding on roads only makes only sense between nodes -> move position on edge to closest node :)
            if(start.GraphEdge != null) {
                if(Vector2.Distance(startPos, start.GraphEdge.NodeA.Position) < Vector2.Distance(startPos, start.GraphEdge.NodeB.Position)) {
                    startPos = start.GraphEdge.NodeA.Position;
                } else {
                    startPos = start.GraphEdge.NodeB.Position;
                }
            }
            if(target.GraphEdge != null) {
                if(Vector2.Distance(endPos, target.GraphEdge.NodeA.Position) < Vector2.Distance(endPos, target.GraphEdge.NodeB.Position)) {
                    endPos = target.GraphEdge.NodeA.Position;
                } else {
                    endPos = target.GraphEdge.NodeB.Position;
                }
            }

            //TODO refactor AStar to pass waypoint
            return pathfinding.AStar(startPos, endPos, wp => pathfinding.GetNeighbors(wp, 0, 0, 0), isCancelled);
        }

        public static float GetPathLength(List<Waypoint> waypoints) {
            float length = 0;
            for(var i = 0; i < waypoints.Count-1; i++) {
                //calculating total length via geometry is not possible here
                //because there is no geometry yet (i.e. Length() from IStreetEdge)!
                length += Vector2.Distance(waypoints[i].Pos, waypoints[i+1].Pos);
            }
            return length;
        }

        [Serializable]
        public class Parameters {
            /// <summary>
            /// Describes how big the "moving mask" is when moving to neighboring
            /// positions in the world. Setting this parameter to 0 will prevent pathfinding
            /// from moving freely in the world; thus this can be used to do pathfinding
            /// through only the existing network.
            /// </summary>
            public int moveMaskK = 4;

            /// <summary>
            /// Describes the maximum elevation where a road can be placed. If a road is
            /// placed above this value, the cost will be set to infinity.
            /// </summary>
            public float maxRoadElevation = 50f;

            /// <summary>
            /// Higher roads can be penalised more by setting this parameter to > 0.
            /// </summary>
            public float heightPenaltyMultiplier = 0.5f;

            /// <summary>
            /// Heuristic is severly underestimating the actual cost. This parameter
            /// can be used to raise the heuristic artificially and thus speeding up the pathfiding.
            /// Note that if this is overestimating(i.e. it is not admissible) the cost, 
            /// a non-optimal path may be found.
            /// Setting this to 0 will cause the pathfinding to behave like Dijkstra.
            /// </summary>
            public float heuristicMultiplier = 1f;


            /// <summary>
            /// Cost of new road segment (distance) will be multiplied by this value.
            /// Will be used when placing a new road segment, which is not touching existing roads(either edge or node).
            /// </summary>
            public float roadDistanceCostMultiplier1 = 2.9f;
            /// <summary>
            /// Cost of road segment (distance) will be multiplied by this value.
            /// Will be used when moving over existing road. In this case, no new road segment is build, rather the
            /// pathfinding is simply moving over existing edges. Making this value lower than <see cref="roadDistanceCostMultiplier1"/>
            /// will motivate the pathfinding to reuse existing roads instead of building new ones.
            /// </summary>
            public float roadDistanceCostMultiplier2 = 1.0f;
            /// <summary>
            /// Cost of new road segment (distance) will be multiplied by this value.
            /// This value will be used when the new road segment does touch an existing road. Making this lower than 
            /// <see cref="roadDistanceCostMultiplier1"/> will motivate the pathfinding to build towards existing roads and
            /// make a connection to them.
            /// </summary>
            public float roadDistanceCostMultiplier3 = 1.1f;

            /// <summary>
            /// Distance (in world units) for a waypoint to be snapped to an edge.
            /// </summary>
            public float SnapFactorEdge = 1.0f;
            /// <summary>
            /// Distance (in world units) for a waypoint to be snapped to an node.
            /// </summary>
            public float SnapFactorNode = 1.5f;

            /// <summary>
            /// Maximum slope allowed for building a road. If the slope exceed this value, the cost of the road will
            /// be inifinity. A value of 0.12f means 12%. Thus a value of 1f means 100% (=45 degrees).
            /// </summary>
            public float slopeCostMaxGrade = 0.12f;
            
            /// <summary>
            /// Cost of slope will be multiplied by this value.
            /// </summary>
            public float slopeCostMultiplier = 30f;

            /// <summary>
            /// Related to issue #24. Sometimes during the path building step, the algorithm wouldn't
            /// realise that two waypoint are reachable via a road, yielding weird looking intersections. 
            /// One way to solve is by changing the cost function parameters, the other way is to check during 
            /// the building step if these waypoints are reachable with the help of A* (but only moving on the roads).
            /// If the existing road connection is <see cref="thresholdExistingConnection"/> times longer than the
            /// road which would be built, the existing road is ignored. Otherwise the existing road is used, meaning
            /// the new road will not be built between these two waypoints.
            /// </summary>
            
            public float thresholdExistingConnection = 2f;

            public static Parameters GetRoadPathSearchParameters() {
                var p = new Parameters();
                p.maxRoadElevation = float.MaxValue;
                p.slopeCostMaxGrade = float.MaxValue;
                return p;
            }
        }

        public class Waypoint {
            public Vector2 Pos { get; }

            public IStreetNode? GraphNode {get; set;}
            public IStreetEdge? GraphEdge {get; set;}
            public bool DidUseRoad = false;

            public Waypoint(IStreetNode node): this(node.Position, node, null) {}

            public Waypoint(Vector2 p, IStreetNode? node = null, IStreetEdge? edge = null) {
                Pos = p;
                GraphNode = node;
                GraphEdge = edge;
            }

            public override string ToString() {
                var hasNode = GraphNode != null ? " With node" : "";
                var hasEdge = GraphEdge != null ? " With edge" : "";
                return $"Waypoint {Pos}{hasNode}{hasEdge}";
            }

            public override bool Equals(object? obj) {
                if (obj is not Waypoint other) return false;
                return this.Pos == other.Pos 
                       && this.GraphNode == other.GraphNode 
                       && this.GraphEdge == other.GraphEdge;
            }

            public override int GetHashCode() => this.Pos.GetHashCode();

            public static bool operator ==(Waypoint? c1, Waypoint? c2) {
                if(c1 is null && c2 is null) return true;
                if(c1 is null && c2 is not null) return false;
                if(c1 is not null && c2 is null) return false;
                return c1!.Equals(c2!); 
            }

            public static bool operator !=(Waypoint? c1, Waypoint? c2) { 
                if(c1 is null && c2 is null) return false;
                if(c1 is null && c2 is not null) return true;
                if(c1 is not null && c2 is null) return true;
                return !c1!.Equals(c2!); 
            }
        }

    }

}