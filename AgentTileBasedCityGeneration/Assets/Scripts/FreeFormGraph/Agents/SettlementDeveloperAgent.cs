#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;
using UnityEngine;
using Random = System.Random;
using DebugUtils;

namespace FreeFormGraph.Agents {
    
    public class SettlementDeveloperAgent : IAgent {
        
        public AgentVariableInt WorkFrequency { get; } = new("Work Frequency", 1, 1, 100);

        public List<IAgentVariable> AgentVariables => GetCurrentParamaterSet().AllVariables;
        
        private readonly List<SdaParameters> allParameters;
        private int currentParameterSetIndex = 0;

        private readonly BudgetPointOfInterest pointOfInterest;
        
        private int age = 0;

        public SettlementDeveloperAgent(BudgetPointOfInterest pointOfInterest, IWorld world, List<SdaParameters> parameters) {
            Debug.Assert(pointOfInterest != null);
            this.pointOfInterest = pointOfInterest!;
            this.allParameters = (parameters != null && parameters.Count > 0)
                ? parameters
                : new List<SdaParameters> { new() };
            CreateStartNode(world, GetCurrentParamaterSet());
        }

        private IStreetNode CreateStartNode(IWorld world, SdaParameters parameters) {
            Debug.Assert(world != null);
            Debug.Assert(world!.StreetGraph != null);
            if (world!.StreetGraph!.TryFindClosestNode(pointOfInterest.Position, out var closestNode,
                    parameters.MinStreetLength))
                return closestNode;
            if (world.StreetGraph.CreateUnconnectedNode(pointOfInterest.Position, out var createdNode)) {
                world.PointsOfInterest.AddNodeRelationToPointOfInterest(createdNode, pointOfInterest);
                return createdNode; 
            }
            throw new Exception("Could not find or create a node for the point of interest.");
        }

        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {
            var nodes = world.PointsOfInterest.GetNodesFromPointOfIntereset(pointOfInterest);
            if (nodes.Count == 0) {
                Debug.LogWarning("PoIDeveloperAgent: No nodes found for the point of interest. Creating a new one at the center. PS: This should not happen because this agent is always placed after building a road.");
                var newNode = CreateStartNode(world, GetCurrentParamaterSet());
                nodes = new[] {newNode};
            }
            
            var currentParameterSet = GetCurrentParamaterSet();
            var roadNetworkGrown = GrowRoadNetwork(world, nodes, context.Random, currentParameterSet);
            cancellationToken.ThrowIfCancellationRequested();
            
            var newConnections = ConnectRoadNetwork(world, nodes, cancellationToken, context.Random, currentParameterSet);
            var didChange = roadNetworkGrown || newConnections > 0;
            SelectParameterSet(didChange);
        }

        private void SelectParameterSet(bool didChange) {
            if(!didChange
            || currentParameterSetIndex == allParameters.Count - 1 //we are in last timeline
            || allParameters[currentParameterSetIndex].time == -1) return;
            age++;
            if(age > allParameters[currentParameterSetIndex].time) {
                currentParameterSetIndex++;
                age = 0;
                Debug.Log("Settlement developer: swithed to a new timeline!");
            }
        }

        private SdaParameters GetCurrentParamaterSet() {
            return allParameters[currentParameterSetIndex];
        }

        private bool GrowRoadNetwork(IWorld world, IReadOnlyList<IStreetNode> nodes, Random random, SdaParameters parameters) {
            var node = GetRandomNode(nodes, random);
            // Debug.Assert(node != null, $"PoIDeveloperAgent: Node is null.");
            // var averageInPosition = GetAverageInPosition(node); // TODO take a random incoming edge as direction
            var averageInPosition = GetRandomConnectedNodePosition(node, random);
            Debug.Assert(node.Position != averageInPosition);
            var direction = node.Position - averageInPosition;
            var inStreetAngle = Mathf.Atan2(direction.y, direction.x);
            var angleRandom = (float)random.NextDouble() * 2f * parameters.AngleRandomMax - parameters.AngleRandomMax; //UnityEngine.Random.Range(-angleRandomMax, angleRandomMax);
            var angleOffset = parameters.AngleInBothDirections ? (random.Next(2) == 1 ? parameters.AngleOffset : -parameters.AngleOffset) : parameters.AngleOffset;
            var combinedAngle = inStreetAngle + angleOffset + angleRandom;
            var length = (float)random.NextDouble() * (parameters.MaxStreetLength - parameters.MinStreetLength) + parameters.MinStreetLength; //UnityEngine.Random.Range(minStreetLength, maxStreetLength);
            var newPointPosition = new Vector2(node.Position.x + Mathf.Cos(combinedAngle) * length, node.Position.y + Mathf.Sin(combinedAngle) * length);
            var newPoint = parameters.SnapToGrid ? new Vector2(Mathf.Round(newPointPosition.x), Mathf.Round(newPointPosition.y)) : newPointPosition;
            
            // TODO check for steepness with parameter
            // TODO check if the new angle is in a legal range for every edge
            if (node.IsMaxConnectedEdgesReached) {
                //Debug.Log($"PoIDeveloperAgent: Node {node.Position} has too many connected edges.");
                return false;
            }
            if(!IsRoadWithinBudget(world, node.Position, newPoint, parameters)) {
                //Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too expensive.");
                return false;
            }
            if(world.StreetGraph.TryFindClosestNode(newPoint, out _, length)) {
                //Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too close to an existing node: ");
                return false;
            }
            if (world.StreetGraph.TryFindClosestEdge(newPoint, out _, out _, parameters.MinNodeEdgeDistance)) {
                //Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too close to an existing edge: ");
                return false;
            }
            if (!world.StreetGraph.CreateEdge(node, newPoint, out var newEdge, out var newNode, out _, out _, failIfIntersection: true)) {
                //Debug.LogWarning($"Could not create a new node for the PoIDeveloperAgent at {newPoint} from {node.Position} {newNode.Position} edge: ({newEdge == null}).");
                return false;
            }
            GraphDebugUtils.AssertStreetGraphConnectivity(world);
            DecreasePOIBudget(world, node.Position, newPoint, parameters);
            world.PointsOfInterest.AddNodeRelationToPointOfInterest(newNode, pointOfInterest);

            return true;
        }

        private void DecreasePOIBudget(IWorld world, 
                Vector2 start, 
                Vector2 end, 
                SdaParameters parameters) {
            pointOfInterest.Budget -= CostForRoad(world, start, end, parameters);
            Debug.Assert(pointOfInterest.Budget >= 0);
        }

        private bool IsRoadWithinBudget(
                IWorld world, 
                Vector2 startPosition, 
                Vector2 endPosition,
                SdaParameters parameters) {
            return pointOfInterest.Budget >= CostForRoad(world, startPosition, endPosition, parameters);
        }

        private float CostForRoad(IWorld world, 
                Vector2 startPos, 
                Vector2 endPos, 
                SdaParameters parameters) {
            Debug.Assert(startPos != endPos);
            var poiDistanceCost = Vector2.Distance(pointOfInterest.Position, endPos);
            var roadLength = Vector2.Distance(startPos, endPos);
            var elevationStart = world.GetHeightAt(startPos.x, startPos.y);
            var elevationEnd = world.GetHeightAt(endPos.x, endPos.y);
            var slope = Mathf.Abs(elevationStart - elevationEnd) / roadLength;
            if(slope > parameters.MaxSlope) {
                return float.PositiveInfinity;
            }
            return poiDistanceCost + roadLength;
        }

        private int ConnectRoadNetwork(IWorld world, 
                IReadOnlyList<IStreetNode> nodes, 
                CancellationToken cancellationToken, 
                Random random,
                SdaParameters parameters) {
            
            if (nodes.Count < 2) return 0;
            switch (parameters.ConnectCulDeSacs.Value) {
                case SdaParameters.ConnectionHandling.ConnectNone:
                    return 0;
                
                case SdaParameters.ConnectionHandling.ConnectSlowly: {
                    var nodeA = GetRandomNode(nodes, random);
                    var nodeB = GetRandomNode(nodes, random);
                    while (nodeA == nodeB) {
                        nodeB = GetRandomNode(nodes, random);
                        if (nodes.Count < 2) { // sanity check and in case some parallel code modifies the nodes list (which it shouldn't)
                            Debug.LogError("PoIDeveloperAgent: Not enough nodes to connect. Nodes modified during connection.");
                            return 0;
                        }
                    }
                    if (!ConnectCulDeSacs(nodeA, nodeB, world, parameters)) return 0;
                    return 1;
                }
                
                case SdaParameters.ConnectionHandling.ConnectAll: {
                    var connections = 0;
                    foreach (var nodeA in nodes) {
                        foreach (var nodeB in nodes) {
                            if (cancellationToken.IsCancellationRequested) {
                                return connections;
                            }
                            if (nodeA != nodeB && ConnectCulDeSacs(nodeA, nodeB, world, parameters)) {
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

        private bool ConnectCulDeSacs(IStreetNode nodeA, IStreetNode nodeB, IWorld world, SdaParameters parameters) {
            Debug.Assert(nodeA != nodeB);
            if (Vector3.Distance(nodeA.Position, nodeB.Position) > parameters.MaxConnectionDistance) return false;
            if ((parameters.ConnectCulDeSacWithNonCulDeSac && nodeA.Edges.Count() > 1 && nodeB.Edges.Count() > 1) 
                || (!parameters.ConnectCulDeSacWithNonCulDeSac && (nodeA.Edges.Count() > 1 || nodeB.Edges.Count() > 1))) return false; // Too many nodes are not cul-de-sacs
            // TODO check if nodeA and nodeB are connected already.
            
            if(!IsRoadWithinBudget(world, nodeA.Position, nodeB.Position, parameters)) return false;
            
            if (!world.StreetGraph.CreateEdge(nodeA, nodeB.Position, out _, out var toNode, out var isToNodeNew, out var isEdgeNew, failIfIntersection: true)) {
                // Debug.Log("Could not connect the cul-de-sacs.");
                return false;
            }
            GraphDebugUtils.AssertStreetGraphConnectivity(world);
            DecreasePOIBudget(world, nodeA.Position, nodeB.Position, parameters);
            Debug.Assert(!isToNodeNew, $"PoIDeveloperAgent: Cul-de-sac connection created a new node at {toNode}.");
            // Debug.Log($"PoIDeveloperAgent: Connected cul-de-sacs {nodeA.Position} and {nodeB.Position}.");
            return isEdgeNew;
        }

        private static Vector3 GetRandomConnectedNodePosition(IStreetNode node, Random random) {
            if (node.ConnectedEdgesCount == 0) {
                Debug.Log("PoIDeveloperAgent: Node has no connected edges.");
                return node.Position + new Vector3(1, 0, 0);
            } // TODO Random direction
            return node.GetNeighbors()[random.Next(0, node.ConnectedEdgesCount)].Position;
        }

        private static Vector3 GetAverageInPosition(IStreetNode node) {
            if (node.ConnectedEdgesCount == 0) return new Vector3(1, 0, 0); // TODO Random direction
            var average = new Vector3(0, 0, 0);
            foreach (var otherNode in node.GetNeighbors()) {
                average += otherNode.Position;
            }
            return average / node.ConnectedEdgesCount;
        }

        private static IStreetNode GetRandomNode(IReadOnlyList<IStreetNode> nodes, Random random) {
            Debug.Assert(nodes.Count > 0, "PoIDeveloperAgent: No nodes to choose from.");
            return nodes[random.Next(0, nodes.Count)];
        }

        [Serializable]
        public class SdaParameters {
            [field: SerializeField] public int time = -1;
            [field: SerializeField] public AgentVariableFloat MinStreetLength { get; set; } = new("Min Street Length", 2f, 0.5f, 20f);
            [field: SerializeField] public AgentVariableFloat MaxStreetLength { get; set; } = new("Max Street Length", 5f, 1, 50f);
            [Tooltip("Minimum distance a new node has to have to an edge.")]
            [field: SerializeField] public AgentVariableFloat MinNodeEdgeDistance { get; set; } = new("Min Distance Node Edge", 0.7f, 0.1f, 10f);
            [field: SerializeField] public AgentVariableFloat AngleOffset { get; set; } = new("Angle Offset", Mathf.Deg2Rad * 90f, -(Mathf.Deg2Rad * 180f), Mathf.Deg2Rad * 180f);
            [field: SerializeField] public AgentVariableFloat AngleRandomMax { get; set; } = new("Ange Randomness", Mathf.Deg2Rad * 0f, 0, Mathf.Deg2Rad * 180f);
            [Tooltip("Maximum connection distance for cul-de-sacs to be connected.")]
            [field: SerializeField] public AgentVariableFloat MaxConnectionDistance { get; set; } = new("Max Connection Distance", 3.5f, 0.5f, 20f);
            [field: SerializeField] public AgentVariableBool SnapToGrid { get; set; } = new("Snap to Grid", false);
            [field: SerializeField] public AgentVariableEnum<ConnectionHandling> ConnectCulDeSacs { get; set; } = new("Cul de Sacs Connection Version", ConnectionHandling.ConnectSlowly);
            [field: SerializeField] public AgentVariableBool ConnectCulDeSacWithNonCulDeSac { get; set; } = new("Connect Cul de Sacs with non-Cul de sacs", true);
            [field: SerializeField] public AgentVariableBool AngleInBothDirections { get; set; } = new("Angle in Both Directions", true);
            
            public List<IAgentVariable> AllVariables => new() {
                MinStreetLength, MaxStreetLength, MinNodeEdgeDistance, AngleOffset, AngleRandomMax, MaxConnectionDistance, SnapToGrid, ConnectCulDeSacs, ConnectCulDeSacWithNonCulDeSac, AngleInBothDirections
            };
            /// <summary>
            /// Each POI has a radius which is needed to sample nodes from. While the POI grows,
            /// the radius will so too, but the radius needs to be a bit bigger than the real dimension of the POI.
            /// If the radius would be exactly as the farthest point (this parameter = 0), growing outwards
            /// might come to a halt, because the new points lie outside the radius.
            /// </summary>
            [field: SerializeField] public float GrowRadiusAddition { get; set; } = 2f;

            /// <summary>
            /// Maximum slope allowed for building a road. If the slope exceed this value, the cost of the road will
            /// be inifinity. A value of 0.12f means 12%. Thus a value of 1f means 100% (=45 degrees).
            /// </summary>
            [field: SerializeField] public float MaxSlope { get; set; } = 0.2f;
            


            public SdaParameters() { }

            public SdaParameters(SdaParameters? other) {
                if (other == null) {
                    return;
                }
                MinStreetLength.Value = other.MinStreetLength;
                MaxStreetLength.Value = other.MaxStreetLength;
                MinNodeEdgeDistance.Value = other.MinNodeEdgeDistance;
                AngleOffset.Value = other.AngleOffset;
                AngleRandomMax.Value = other.AngleRandomMax;
                MaxConnectionDistance.Value = other.MaxConnectionDistance;
                SnapToGrid.Value = other.SnapToGrid;
                ConnectCulDeSacs.Value = other.ConnectCulDeSacs;
                ConnectCulDeSacWithNonCulDeSac.Value = other.ConnectCulDeSacWithNonCulDeSac;
                AngleInBothDirections.Value = other.AngleInBothDirections;
            }

            public SdaParameters(float minStreetLength, float maxStreetLength, float minNodeEdgeDistance, float angleOffset, float angleRandomMax, float maxConnectionDistance, bool snapToGrid, ConnectionHandling connectCulDeSacs, bool connectCulDeSacWithNonCulDeSac, bool angleInBothDirections) {
                MinStreetLength.Value = minStreetLength;
                MaxStreetLength.Value = maxStreetLength;
                MinNodeEdgeDistance.Value = minNodeEdgeDistance;
                AngleOffset.Value = angleOffset;
                AngleRandomMax.Value = angleRandomMax;
                MaxConnectionDistance.Value = maxConnectionDistance;
                SnapToGrid.Value = snapToGrid;
                ConnectCulDeSacs.Value = connectCulDeSacs;
                ConnectCulDeSacWithNonCulDeSac.Value = connectCulDeSacWithNonCulDeSac;
                AngleInBothDirections.Value = angleInBothDirections;
            }

            public enum ConnectionHandling {
                ConnectNone,
                ConnectSlowly,
                ConnectAll
            }
        }
        
    }
    
}