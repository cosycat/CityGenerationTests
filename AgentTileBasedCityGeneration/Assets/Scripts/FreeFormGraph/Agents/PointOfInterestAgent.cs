using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.Agents {

    public class PointOfInterestAgent : MonoBehaviour, IAgent {
        
        public AgentVariableInt WorkFrequency { get; set; } = new("Work Frequency", 100, 1, 1000);
        
        public List<IAgentVariable> AgentVariables => new() { WorkFrequency }; // TODO: add parameters

        [Tooltip("The parameters for used for the next generated settlement and its settlement developer agent."),
         SerializeField] private SettlementDeveloperAgent.SdaParameters settlementDeveloperAgentParameters = new();
        
        private IStreetGraph streetGraph;

        [SerializeField]
        public POIAgentParameters agentParameters = new();

        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {
            if(streetGraph == null) return;

            if (world.PointsOfInterest.PointsOfInterest.Count < agentParameters.DesiredNumberOfPoints)  {
                CreatePOI(world, streetGraph, context);
            }
        }

        private void CreatePOI(IWorld world, IStreetGraph streetGraph, AgentManager.Context context) {
            var random = context.Random;
            var poiSeedPosition = new Vector2Int(world.Width/2, world.Height/2);
            var worldPoiCount = world.PointsOfInterest.PointsOfInterest.Count;
            if(worldPoiCount > 0) {
                poiSeedPosition = Vector2Int.RoundToInt(world.PointsOfInterest.PointsOfInterest[random.Next(worldPoiCount)].Position);
            }

            var minDistanceToRoad = agentParameters.MinDistanceToRoad;
            var radius = agentParameters.RadiusGeneration; // Shouldn't this be dependent on the world size?
            var randomDir = Vector2Int.RoundToInt(new Vector2((float)random.NextDouble(), (float)random.NextDouble()).normalized * ((float)random.NextDouble()-0.5f)*2f*radius);
            var newPoiPos = poiSeedPosition + randomDir;

            if(newPoiPos.x < 0 || newPoiPos.x >= world.Width || newPoiPos.y < 0 || newPoiPos.y >= world.Height) return;

            newPoiPos = MinimizeCostOfPOI(world, newPoiPos);

            if(streetGraph.TryFindClosestNode((Vector3Int)newPoiPos, out _, minDistanceToRoad)) {
                return;
            } else if(streetGraph.TryFindClosestEdge((Vector3Int)newPoiPos, out _, out _, minDistanceToRoad)) {
                return;
            }

            //TODO check validity?
            var pointOfInterest = new BudgetPointOfInterest(newPoiPos, agentParameters.InitialBudgetForPOI);
            world.PointsOfInterest.AddPointOfInterest(pointOfInterest);
        }

        /// <summary>
        /// Given a position <paramref name="pos"/> in the world, this function will move the point around
        /// until it reaches a local minima according to a cost function.
        /// </summary>
        /// <param name="pos">Initial point to move</param>
        /// <param name="world"></param>
        /// <returns>New point in local minima</returns>
        private Vector2Int MinimizeCostOfPOI(IWorld world, Vector2Int pos) {
            var visited = new HashSet<Vector2Int>();
            var current = pos;

            while(!visited.Contains(current)) {
                visited.Add(current);
                current = SelectCheapestDirection(current, world);
            }

            return current;
        }

        private float Cost(IWorld world, Vector2Int currentPos, Vector2Int targetPosition) {
            var heightDiff = world.GetHeightAt(targetPosition.x, targetPosition.y) - world.GetHeightAt(currentPos.x, currentPos.y);
            var gradient = heightDiff / Vector2.Distance(currentPos, targetPosition);

            var distanceClosest = 0.0f;
            if(world.PointsOfInterest.PointsOfInterest.Count >= 2) {
                distanceClosest = world.PointsOfInterest.PointsOfInterest
                    .Min(POI => Vector2.Distance(targetPosition, POI.Position));
            }
            var distanceCost = agentParameters.DistanceCostFactor * 1.0f/(distanceClosest+1);

            var totalCost = gradient + distanceCost;
            return totalCost;
        }

        /// <summary>
        /// Given a position <paramref name="v"/> in the world, this function will return the neighboring point
        /// with the lowest cost function value. Neighboring is implemented as 8-neighborhood.
        /// If <paramref name="v"/> is already in a local minima, <paramref name="v"/> is returned.
        /// </summary>
        /// <param name="v">Initial position</param>
        /// <param name="world"></param>
        /// <returns>New position or v if local minima is reached</returns>
        private Vector2Int SelectCheapestDirection(Vector2Int v, IWorld world) {
            var currentTargetPos = Vector2Int.zero;
            var currentCost = float.MaxValue;

            int lookDistance = 1;
            for(int dy = -lookDistance; dy <= lookDistance; dy++) {
                for(int dx = -lookDistance; dx <= lookDistance; dx++) {
                    if(dy == 0 && dx == 0) continue;

                    var testPosition = v + new Vector2Int(dx,dy);
                    if(testPosition.y < 0 || testPosition.y >= world.Height || testPosition.x < 0 || testPosition.x >= world.Width) continue;

                    var newCost = Cost(world, v, testPosition);

                    if(newCost < currentCost) {
                        currentCost = newCost;
                        currentTargetPos = testPosition; 
                    }
                }
            }
            if(currentCost == float.MaxValue) return v;
            return currentTargetPos;
        }

        void Start() {
            streetGraph = FindObjectOfType<StreetGraphGameObject>();
        }

        [Serializable]
        public class POIAgentParameters {
            /// <summary>
            /// Maximum number of POIs in world. If this threshold is reached, no new POIs are generated.
            /// </summary>
            [Min(0)] public int DesiredNumberOfPoints = 35;

            /// <summary>
            /// Factor which influences cost function of POI generation. Specifically, it influences the cost
            /// of the distance to the nearest existing POI. This part of the cost function is calculated as 
            /// DistanceCostFactor * 1 / (distanceToNearestPOI + 1). Increasing this factor will make the POI be farther apart
            /// </summary>
            [SerializeField] public float DistanceCostFactor = 35;

            /// <summary>
            /// Minimum distance to a road that needs to exist to build a POI
            /// </summary>
            [Min(0)] public int MinDistanceToRoad = 20;

            /// <summary>
            /// Radius which is used for selection of new POI. An initial position for a new POI is based
            /// on a random POI's position + radius. The final POI might be outside of the radius due to the cost function.
            /// </summary>
            [Min(0)] public int RadiusGeneration = 100;

            /// <summary>
            /// Threshold which determines when POI-Radius-growing stops. Radius growing stops when accumulated penalty
            /// is above this threshold.
            /// </summary>
            [Min(0)] public float POIRadiusFlatnessThreshold = 20.0f;

            /// <summary>
            /// Defines at which point a penalty for the slope is applied. 0.1 equals to 10% slope.
            /// </summary>
            [Min(0)] public float POIRadiusMinimumSlopeForPenalty = 0.06f;

            /// <summary>
            /// Defines how slopes > POIRadiusMinimumSlopeForPenalty are penalised in terms of power:
            /// slope penalty = slope ^ POIRadiusSlopePenaltyPower
            /// </summary>
            [Min(0)] public int POIRadiusSlopePenaltyPower = 3;
            
            /// <summary>
            /// Curve which describes how height penalty is applied to the radius. A linear curve means that
            /// the penalty for height is linear with increasing height.
            /// </summary>
            [SerializeField] public AnimationCurve POIRadiusHeightPenalty = AnimationCurve.Linear(0,0,1,1);
            
            /// <summary>
            /// Multiplier for the height penalty.
            /// </summary>
            [SerializeField] public float POIRadiusHeightPenaltyMultiplier = 3.5f;

            [Min(0)] public int InitialBudgetForPOI = 500;
        }
    }
    
}