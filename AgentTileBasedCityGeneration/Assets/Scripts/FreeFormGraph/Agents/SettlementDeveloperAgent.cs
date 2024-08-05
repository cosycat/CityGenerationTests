#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;
using UnityEngine;
using Random = System.Random;
using DebugUtils;
using SUMO;

namespace FreeFormGraph.Agents {
    public class SettlementDeveloperAgent : IAgent {
        public int WorkFrequency { get; set; } = 1;

        private readonly List<SdaParameters> parameters;
        private int currentParameterSetIndex = 0;

        private readonly BudgetPointOfInterest pointOfInterest;

        private int age = 0;

        public SettlementDeveloperAgent(BudgetPointOfInterest pointOfInterest, IWorld world,
            List<SdaParameters> parameters) {
            Debug.Assert(pointOfInterest != null);
            this.pointOfInterest = pointOfInterest!;
            this.parameters = parameters ?? new List<SdaParameters> { new SdaParameters() };
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
                Debug.LogWarning(
                    "PoIDeveloperAgent: No nodes found for the point of interest. Creating a new one at the center. PS: This should not happen because this agent is always placed after building a road.");
                var newNode = CreateStartNode(world, GetCurrentParamaterSet());
                nodes = new[] { newNode };
            }

            var currentParameterSet = GetCurrentParamaterSet();
            var roadNetworkGrown = GrowRoadNetwork(world, nodes, context.Random, currentParameterSet);
            cancellationToken.ThrowIfCancellationRequested();

            var newConnections =
                ConnectRoadNetwork(world, nodes, cancellationToken, context.Random, currentParameterSet);
            var didChange = roadNetworkGrown || newConnections > 0;
            SelectParameterSet(didChange);
        }

        private void SelectParameterSet(bool didChange) {
            if (!didChange
                || currentParameterSetIndex == parameters.Count - 1 //we are in last timeline
                || parameters[currentParameterSetIndex].time == -1) return;
            age++;
            if (age > parameters[currentParameterSetIndex].time) {
                currentParameterSetIndex++;
                age = 0;
                Debug.Log("Settlement developer: switched to a new timeline!");
            }
        }

        private SdaParameters GetCurrentParamaterSet() {
            return parameters[currentParameterSetIndex];
        }

        private bool GrowRoadNetwork(IWorld world, IReadOnlyList<IStreetNode> nodes, Random random,
            SdaParameters parameters) {
            var node = GetRandomNode(nodes, random);
            // Debug.Assert(node != null, $"PoIDeveloperAgent: Node is null.");
            // var averageInPosition = GetAverageInPosition(node); // TODO take a random incoming edge as direction
            var randomInPosition = GetRandomConnectedNodePosition(node, random);
            Debug.Assert(node.Position != randomInPosition);
            var direction = node.Position - randomInPosition;
            var inStreetAngle = Mathf.Atan2(direction.y, direction.x);
            var angleRandom =
                (float)random.NextDouble() * 2f * parameters.AngleRandomMax -
                parameters.AngleRandomMax; //UnityEngine.Random.Range(-angleRandomMax, angleRandomMax);
            var angleOffset = parameters.AngleInBothDirections
                ? random.Next(2) == 1 ? parameters.AngleOffset : -parameters.AngleOffset
                : parameters.AngleOffset;
            var combinedAngle = inStreetAngle + angleOffset + angleRandom;
            var length = (float)random.NextDouble() * (parameters.MaxStreetLength - parameters.MinStreetLength) +
                         parameters.MinStreetLength; //UnityEngine.Random.Range(minStreetLength, maxStreetLength);
            var newPointPosition = new Vector2(node.Position.x + Mathf.Cos(combinedAngle) * length,
                node.Position.y + Mathf.Sin(combinedAngle) * length);
            var newPoint = parameters.SnapToGrid
                ? new Vector2(Mathf.Round(newPointPosition.x), Mathf.Round(newPointPosition.y))
                : newPointPosition;

            // TODO check for steepness with parameter
            // TODO check if the new angle is in a legal range for every edge
            if (node.IsMaxConnectedEdgesReached)
                //Debug.Log($"PoIDeveloperAgent: Node {node.Position} has too many connected edges.");
                return false;
            if (!IsRoadWithinBudget(world, node.Position, newPoint, parameters))
                //Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too expensive.");
                return false;
            if (world.StreetGraph.TryFindClosestNode(newPoint, out _, length))
                //Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too close to an existing node: ");
                return false;
            if (world.StreetGraph.TryFindClosestEdge(newPoint, out _, out _, parameters.MinNodeEdgeDistance))
                //Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too close to an existing edge: ");
                return false;
            if (!world.StreetGraph.CreateEdge(node, newPoint, out var newEdge, out var newNode, out _, out _, true))
                //Debug.LogWarning($"Could not create a new node for the PoIDeveloperAgent at {newPoint} from {node.Position} {newNode.Position} edge: ({newEdge == null}).");
                return false;
            newEdge.Type = parameters.GrowRoadType;

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
            if (slope > parameters.MaxSlope) return float.PositiveInfinity;
            return poiDistanceCost + roadLength;
        }

        private int ConnectRoadNetwork(IWorld world,
            IReadOnlyList<IStreetNode> nodes,
            CancellationToken cancellationToken,
            Random random,
            SdaParameters parameters) {
            if (nodes.Count < 2) return 0;
            switch (parameters.ConnectCulDeSacs) {
                case SdaParameters.ConnectionHandling.ConnectNone:
                    return 0;

                case SdaParameters.ConnectionHandling.ConnectSlowly: {
                    var nodeA = GetRandomNode(nodes, random);
                    var nodeB = GetRandomNode(nodes, random);
                    while (nodeA == nodeB) {
                        nodeB = GetRandomNode(nodes, random);
                        if (nodes.Count < 2) {
                            // sanity check and in case some parallel code modifies the nodes list (which it shouldn't)
                            Debug.LogError(
                                "PoIDeveloperAgent: Not enough nodes to connect. Nodes modified during connection.");
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
                            if (cancellationToken.IsCancellationRequested) return connections;
                            if (nodeA != nodeB && ConnectCulDeSacs(nodeA, nodeB, world, parameters)) connections++;
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
                || (!parameters.ConnectCulDeSacWithNonCulDeSac && (nodeA.Edges.Count() > 1 || nodeB.Edges.Count() > 1)))
                return false; // Too many nodes are not cul-de-sacs
            // TODO check if nodeA and nodeB are connected already.

            if (!IsRoadWithinBudget(world, nodeA.Position, nodeB.Position, parameters)) return false;

            if (!world.StreetGraph.CreateEdge(nodeA, nodeB.Position, out var newEdge, out var toNode,
                    out var isToNodeNew, out var isEdgeNew, true))
                // Debug.Log("Could not connect the cul-de-sacs.");
                return false;
            newEdge.Type = parameters.ConnectRoadType;

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
            foreach (var otherNode in node.GetNeighbors()) average += otherNode.Position;
            return average / node.ConnectedEdgesCount;
        }

        private static IStreetNode GetRandomNode(IReadOnlyList<IStreetNode> nodes, Random random) {
            Debug.Assert(nodes.Count > 0, "PoIDeveloperAgent: No nodes to choose from.");
            return nodes[random.Next(0, nodes.Count)];
        }

        [Serializable]
        public class SdaParameters {
            [field: SerializeField] public int time = -1;
            [field: SerializeField] public float MinStreetLength { get; set; } = 2f;
            [field: SerializeField] public float MaxStreetLength { get; set; } = 5f;

            [Tooltip("Minimum distance a new node has to have to an edge.")]
            [field: SerializeField]
            public float MinNodeEdgeDistance { get; set; } = 0.7f;

            [field: SerializeField] public float AngleOffset { get; set; } = Mathf.Deg2Rad * 90f;
            [field: SerializeField] public float AngleRandomMax { get; set; } = Mathf.Deg2Rad * 0f;

            [Tooltip("Maximum connection distance for cul-de-sacs to be connected.")]
            [field: SerializeField]
            public float MaxConnectionDistance { get; set; } = 3.5f;

            [field: SerializeField] public bool SnapToGrid { get; set; } = false;

            [field: SerializeField]
            public ConnectionHandling ConnectCulDeSacs { get; set; } = ConnectionHandling.ConnectSlowly;

            [field: SerializeField] public bool ConnectCulDeSacWithNonCulDeSac { get; set; } = true;
            [field: SerializeField] public bool AngleInBothDirections { get; set; } = true;

            /// <summary>
            /// The type of roads for new roads.
            /// </summary>
            [field: SerializeField]
            public RoadType GrowRoadType { get; set; } = RoadType.Tertiary;

            /// <summary>
            /// The type of road to connect cul-de-sacs with.
            /// </summary>
            [field: SerializeField]
            public RoadType ConnectRoadType { get; set; } = RoadType.Tertiary;

            /// <summary>
            /// Each POI has a radius which is needed to sample nodes from. While the POI grows,
            /// the radius will so too, but the radius needs to be a bit bigger than the real dimension of the POI.
            /// If the radius would be exactly as the farthest point (this parameter = 0), growing outwards
            /// might come to a halt, because the new points lie outside the radius.
            /// </summary>
            [field: SerializeField]
            public float GrowRadiusAddition { get; set; } = 2f;

            /// <summary>
            /// Maximum slope allowed for building a road. If the slope exceed this value, the cost of the road will
            /// be inifinity. A value of 0.12f means 12%. Thus a value of 1f means 100% (=45 degrees).
            /// </summary>
            [field: SerializeField]
            public float MaxSlope { get; set; } = 0.2f;


            public SdaParameters() { }

            public SdaParameters(SdaParameters? other) {
                if (other == null) return;
                MinStreetLength = other.MinStreetLength;
                MaxStreetLength = other.MaxStreetLength;
                MinNodeEdgeDistance = other.MinNodeEdgeDistance;
                AngleOffset = other.AngleOffset;
                AngleRandomMax = other.AngleRandomMax;
                MaxConnectionDistance = other.MaxConnectionDistance;
                SnapToGrid = other.SnapToGrid;
                ConnectCulDeSacs = other.ConnectCulDeSacs;
                ConnectCulDeSacWithNonCulDeSac = other.ConnectCulDeSacWithNonCulDeSac;
                AngleInBothDirections = other.AngleInBothDirections;
                GrowRoadType = other.GrowRoadType;
                ConnectRoadType = other.ConnectRoadType;
                GrowRadiusAddition = other.GrowRadiusAddition;
                MaxSlope = other.MaxSlope;
            }

            public SdaParameters(float minStreetLength, float maxStreetLength, float minNodeEdgeDistance,
                float angleOffset, float angleRandomMax, float maxConnectionDistance, bool snapToGrid,
                ConnectionHandling connectCulDeSacs, bool connectCulDeSacWithNonCulDeSac, bool angleInBothDirections,
                RoadType growRoadType, RoadType connectRoadType, float growRadiusAddition, float maxSlope) {
                MinStreetLength = minStreetLength;
                MaxStreetLength = maxStreetLength;
                MinNodeEdgeDistance = minNodeEdgeDistance;
                AngleOffset = angleOffset;
                AngleRandomMax = angleRandomMax;
                MaxConnectionDistance = maxConnectionDistance;
                SnapToGrid = snapToGrid;
                ConnectCulDeSacs = connectCulDeSacs;
                ConnectCulDeSacWithNonCulDeSac = connectCulDeSacWithNonCulDeSac;
                AngleInBothDirections = angleInBothDirections;
                GrowRoadType = growRoadType;
                ConnectRoadType = connectRoadType;
                GrowRadiusAddition = growRadiusAddition;
                MaxSlope = maxSlope;
            }

            public enum ConnectionHandling {
                ConnectNone,
                ConnectSlowly,
                ConnectAll
            }
        }
    }
}