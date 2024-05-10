using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

namespace FreeFormGraph.Agents {
    
    public class SettlementDeveloperAgent : IAgent {
        
        public int WorkFrequency { get; set; } = 1;
        
        private readonly SdaParameters parameters;

        private readonly IPointOfInterest pointOfInterest;
        
        private readonly Random random = new Random();

        private int lastWorkCycleWithChange = 0;

        public SettlementDeveloperAgent([NotNull] IPointOfInterest pointOfInterest, [NotNull] IWorld world, [CanBeNull] SdaParameters parameters = null) {
            this.pointOfInterest = pointOfInterest;
            this.parameters = parameters ?? new SdaParameters();
            var startNode = world.StreetGraph.TryFindClosestNode(pointOfInterest.Position, out var closestNode, this.parameters.MinStreetLength) 
                ? closestNode : world.StreetGraph.CreateUnconnectedNode(pointOfInterest.Position, out var newNode) ? newNode : throw new Exception("Could not find or create a node for the point of interest.");
        }

        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {
            if (lastWorkCycleWithChange > 100 && lastWorkCycleWithChange % 1000 != 0) {
                lastWorkCycleWithChange++;
                return;
            }
            var nodes = pointOfInterest.FindAllNodes(world);
            // Debug.Log("PoIDeveloperAgent: Growing road network.");
            var roadNetworkGrown = GrowRoadNetwork(world, nodes);
            cancellationToken.ThrowIfCancellationRequested();
            // Debug.Log("PoIDeveloperAgent: Connecting road network.");
            var newConnections = ConnectRoadNetwork(world, nodes, cancellationToken);
            // Debug.Log($"PoIDeveloperAgent: Connected {newConnections} cul-de-sacs.");
            var didChange = roadNetworkGrown || newConnections > 0;
            if (didChange) lastWorkCycleWithChange = 0;
            else lastWorkCycleWithChange++;
            cancellationToken.ThrowIfCancellationRequested();
        }

        private bool GrowRoadNetwork(IWorld world, IStreetNode[] nodes) {
            var node = GetRandomNode(nodes);
            Debug.Assert(node != null, $"PoIDeveloperAgent: Node is null.");
            // var averageInPosition = GetAverageInPosition(node); // TODO take a random incoming edge as direction
            var averageInPosition = GetRandomConnectedNodePosition(node);
            var direction = node.Position - averageInPosition;
            var angle = Mathf.Atan2(direction.y, direction.x);
            var angleRandom = (float)random.NextDouble() * 2f * parameters.AngleRandomMax - parameters.AngleRandomMax; //UnityEngine.Random.Range(-angleRandomMax, angleRandomMax);
            var length = (float)random.NextDouble() * (parameters.MaxStreetLength - parameters.MinStreetLength) + parameters.MinStreetLength; //UnityEngine.Random.Range(minStreetLength, maxStreetLength);
            var newPointPosition = new Vector2(node.Position.x + Mathf.Cos(angle + parameters.AngleOffset + angleRandom) * length, node.Position.y + Mathf.Sin(angle + parameters.AngleOffset + angleRandom) * length);
            var newPoint = parameters.SnapToGrid ? new Vector2(Mathf.Round(newPointPosition.x), Mathf.Round(newPointPosition.y)) : newPointPosition;
            
            // TODO check for steepness with parameter
            // TODO check if the new angle is in a legal range for every edge
            if(world.StreetGraph.TryFindClosestNode(newPoint, out var closestNode, length)) {
                // Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too close to an existing node: {closestNode.Position}");
                return false;
            }
            if (world.StreetGraph.TryFindClosestEdge(newPoint, out var foundEdge, out var positionOnEdge, parameters.MinNodeEdgeDistance)) {
                // Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too close to an existing edge: {foundEdge.NodeA.Position} - {foundEdge.NodeB.Position}");
                return false;
            }
            if (!pointOfInterest.IsPointWithinRange(newPoint)) {
                // Debug.Log($"PoIDeveloperAgent: New point {newPoint} is outside the point of interest.");
                return false;
            }

            if (!world.StreetGraph.CreateEdge(node, new Vector3(newPoint.x, newPoint.y), out var newEdge, out var toNode, out var isToNodeNew, out var isEdgeNew, failIfIntersection: true)) {
                Debug.LogWarning("Could not create a new node for the PoIDeveloperAgent.");
                return false;
            }

            return true;
        }

        private int ConnectRoadNetwork(IWorld world, IStreetNode[] nodes, CancellationToken cancellationToken) {
            
            if (nodes.Length < 2) return 0;
            switch (parameters.ConnectCulDeSacs) {
                case SdaParameters.ConnectionHandling.ConnectNone:
                    return 0;
                
                case SdaParameters.ConnectionHandling.ConnectSlowly: {
                    var nodeA = GetRandomNode(nodes);
                    var nodeB = GetRandomNode(nodes);
                    while (nodeA == nodeB) {
                        nodeB = GetRandomNode(nodes);
                        if (nodes.Length < 2) { // sanity check and in case some parallel code modifies the nodes list (which it shouldn't)
                            Debug.LogError("PoIDeveloperAgent: Not enough nodes to connect. Nodes modified during connection.");
                            return 0;
                        }
                    }
                    if (!ConnectCulDeSacs(nodeA, nodeB, world)) return 0;
                    return 1;
                }
                
                case SdaParameters.ConnectionHandling.ConnectAll: {
                    var connections = 0;
                    foreach (var nodeA in nodes) {
                        foreach (var nodeB in nodes) {
                            if (cancellationToken.IsCancellationRequested) {
                                return connections;
                            }
                            if (ConnectCulDeSacs(nodeA, nodeB, world)) {
                                connections++;
                            }
                        }
                    }
                    return connections;
                }
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool ConnectCulDeSacs(IStreetNode nodeA, IStreetNode nodeB, IWorld world) {
            if (Vector3.Distance(nodeA.Position, nodeB.Position) > parameters.MaxConnectionDistance) return false;
            if ((parameters.ConnectCulDeSacWithNonCulDeSac && nodeA.Edges.Count() > 1 && nodeB.Edges.Count() > 1) 
                || !parameters.ConnectCulDeSacWithNonCulDeSac && nodeA.Edges.Count() > 1 || nodeB.Edges.Count() > 1) return false; // Too many nodes are not cul-de-sacs
            // TODO check if nodeA and nodeB are connected already.
            
            if (!world.StreetGraph.CreateEdge(nodeA, nodeB.Position, out var newEdge, out var toNode, out var isToNodeNew, out var isEdgeNew, failIfIntersection: true)) {
                // Debug.Log("Could not connect the cul-de-sacs.");
                return false;
            }
            Debug.Assert(!isToNodeNew, $"PoIDeveloperAgent: Cul-de-sac connection created a new node at {toNode}.");
            // Debug.Log($"PoIDeveloperAgent: Connected cul-de-sacs {nodeA.Position} and {nodeB.Position}.");
            return isEdgeNew;
        }

        private Vector3 GetRandomConnectedNodePosition(IStreetNode node) {
            if (node.ConnectedEdgesCount == 0) {
                Debug.Log("PoIDeveloperAgent: Node has no connected edges.");
                return node.Position + new Vector3(1, 0, 0);
            } // TODO Random direction
            var randomEdge = node.Edges.ToArray()[random.Next(0, node.Edges.Count())];
            var otherNode = randomEdge.NodeA == node ? randomEdge.NodeB : randomEdge.NodeA;
            return otherNode.Position;
        }

        private Vector3 GetAverageInPosition(IStreetNode node) {
            if (node.ConnectedEdgesCount == 0) return new Vector3(1, 0, 0); // TODO Random direction
            var average = new Vector3(0, 0, 0);
            foreach (var edge in node.Edges) {
                var otherNode = edge.NodeA == node ? edge.NodeB : edge.NodeA;
                average += otherNode.Position;
            }
            return average / node.ConnectedEdgesCount;
        }

        private IStreetNode GetRandomNode(IStreetNode[] nodes) {
            return nodes[random.Next(0, nodes.Length)];
        }

        [Serializable]
        public class SdaParameters {
            [field: SerializeField] public float MinStreetLength { get; set; } = 2f;
            [field: SerializeField] public float MaxStreetLength { get; set; } = 5f;
            [field: SerializeField] public float MinNodeEdgeDistance { get; set; } = 0.7f;
            [field: SerializeField] public float AngleOffset { get; set; } = Mathf.Deg2Rad * 90f;
            [field: SerializeField] public float AngleRandomMax { get; set; } = Mathf.Deg2Rad * 0f;
            [field: SerializeField] public float MaxConnectionDistance { get; set; } = 3.5f;
            [field: SerializeField] public bool SnapToGrid { get; set; } = false;
            [field: SerializeField] public ConnectionHandling ConnectCulDeSacs { get; set; } = ConnectionHandling.ConnectAll;
            [field: SerializeField] public bool ConnectCulDeSacWithNonCulDeSac { get; set; } = true;

            public SdaParameters() { }

            public SdaParameters(SdaParameters other) {
                if (other == null) {
                    return;
                }
                MinStreetLength = other.MinStreetLength;
                MaxStreetLength = other.MaxStreetLength;
                MinNodeEdgeDistance = other.MinNodeEdgeDistance;
                AngleOffset = other.AngleOffset;
                AngleRandomMax = other.AngleRandomMax;
                MaxConnectionDistance = other.MaxConnectionDistance;
                SnapToGrid = other.SnapToGrid;
                ConnectCulDeSacs = other.ConnectCulDeSacs;
                ConnectCulDeSacWithNonCulDeSac = other.ConnectCulDeSacWithNonCulDeSac;
            }

            public SdaParameters(float minStreetLength, float maxStreetLength, float minNodeEdgeDistance, float angleOffset, float angleRandomMax, float maxConnectionDistance, bool snapToGrid, ConnectionHandling connectCulDeSacs, bool connectCulDeSacWithNonCulDeSac) {
                MinStreetLength = minStreetLength;
                MaxStreetLength = maxStreetLength;
                MinNodeEdgeDistance = minNodeEdgeDistance;
                AngleOffset = angleOffset;
                AngleRandomMax = angleRandomMax;
                MaxConnectionDistance = maxConnectionDistance;
                SnapToGrid = snapToGrid;
                ConnectCulDeSacs = connectCulDeSacs;
                ConnectCulDeSacWithNonCulDeSac = connectCulDeSacWithNonCulDeSac;
            }

            public enum ConnectionHandling {
                ConnectNone,
                ConnectSlowly,
                ConnectAll
            }
        }

        
    }
    
}