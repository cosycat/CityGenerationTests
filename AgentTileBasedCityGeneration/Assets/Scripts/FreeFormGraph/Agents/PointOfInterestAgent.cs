using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.Agents {

    public class PointOfInterestAgent : MonoBehaviour, IAgent {
        
        public int WorkFrequency { get; set; } = 100;
        
        [Tooltip("The parameters for used for the next generated settlement and its settlement developer agent."),
         SerializeField] private SettlementDeveloperAgent.SdaParameters settlementDeveloperAgentParameters = new();
        [Tooltip("The type of point of interest that will be generated next."),
         SerializeField] private PointOfInterestType pointOfInterestType = PointOfInterestType.Village;
        
        private IStreetGraph streetGraph;

        [SerializeField]
        public POIAgentParameters agentParameters = new();
        
        /// <summary>
        /// Returns the min and max radius for a point of interest of the given type.
        /// </summary>
        /// <param name="type"> The type of the point of interest. </param>
        /// <returns> The min and max radius for the point of interest. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the type is not a valid PointOfInterestType value. </exception>
        public static (float min, float max) GetRadiusForPointOfInterestType(PointOfInterestType type) {
            // TODO this should be moved to a more appropriate place
            return type switch {
                PointOfInterestType.Village => (5, 10),
                PointOfInterestType.Town => (10, 15),
                PointOfInterestType.City => (15, 20),
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, "Invalid PointOfInterestType value.")
            };
        }

        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {
            if(streetGraph == null) return;

            var random = context.Random;
            if (world.PointsOfInterest.PointsOfInterest.Count < agentParameters.DesiredNumberOfPoints)  {
                CreatePOI(world, streetGraph, context);
            }
        }

        private void CreatePOI(IWorld world, IStreetGraph streetGraph, AgentManager.Context context) {
            var random = context.Random;
            var poiSeedPosition = new Vector2(world.Width/2, world.Height/2);
            var worldPoiCount = world.PointsOfInterest.PointsOfInterest.Count;
            if(worldPoiCount > 0) {
                poiSeedPosition = world.PointsOfInterest.PointsOfInterest[random.Next(worldPoiCount)].Position;
            }

            var minDistanceToRoad = agentParameters.MinDistanceToRoad;
            var radius = agentParameters.RadiusGeneration; // Shouldn't this be dependent on the world size?
            var randomDir = new Vector2(random.Next(-radius, radius), random.Next(-radius, radius));
            var newPoiPos = poiSeedPosition + randomDir;

            if(newPoiPos.x < 0 || newPoiPos.x >= world.Width || newPoiPos.y < 0 || newPoiPos.y >= world.Height) return;

            newPoiPos = MinimizeCostOfPOI(world, newPoiPos);

            if(streetGraph.TryFindClosestNode(newPoiPos, out _, minDistanceToRoad)) {
                return;
            } else if(streetGraph.TryFindClosestEdge(newPoiPos, out _, out _, minDistanceToRoad)) {
                return;
            }

            //TODO check validity?
            var radiusPoi = DetermineRadiusOfPoi(newPoiPos, world);
            var pointOfInterest = new SpherePointOfInterest(newPoiPos, pointOfInterestType, radiusPoi);
            world.PointsOfInterest.AddPointOfInterest(pointOfInterest);
            context.Manager.AddNewAgent(new SettlementDeveloperAgent(pointOfInterest, world, settlementDeveloperAgentParameters));
        }

        /// <summary>
        /// Given a position <paramref name="pos"/> in the world, this function will move the point around
        /// until it reaches a local minima according to a cost function.
        /// </summary>
        /// <param name="pos">Initial point to move</param>
        /// <param name="world"></param>
        /// <returns>New point in local minima</returns>
        private Vector3 MinimizeCostOfPOI(IWorld world, Vector2 pos) {
            var visited = new HashSet<Vector3>();
            var current = pos;

            while(!visited.Contains(current)) {
                visited.Add(current);
                current = SelectCheapestDirection(current, world);
            }

            return current;
        }

        private float Cost(IWorld world, Vector2 currentPos, Vector3 targetPosition) {
            var heightDiff = world.GetHeightAt(targetPosition.x, targetPosition.y) - world.GetHeightAt(currentPos.x, currentPos.y);
            var gradient = heightDiff / Vector3.Distance(currentPos, targetPosition);

            var distanceClosest = 0.0f;
            if(world.PointsOfInterest.PointsOfInterest.Count >= 2) {
                distanceClosest = world.PointsOfInterest.PointsOfInterest
                    .Min(POI => Vector3.Distance(targetPosition, POI.Position));
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
        private Vector2 SelectCheapestDirection(Vector2 v, IWorld world) {
            var currentTargetPos = Vector2.zero;
            var currentCost = float.MaxValue;

            int lookDistance = 1;
            for(int dy = -lookDistance; dy <= lookDistance; dy++) {
                for(int dx = -lookDistance; dx <= lookDistance; dx++) {
                    if(dy == 0 && dx == 0) continue;

                    var testPosition = v + new Vector2(dx,dy);
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

        /// <summary>
        /// Determine the radius of a POI by growing it in steps. During each step, the slopes on the edge of the radius are
        /// evaluated, penalised and accumulated according to some parameters. If this accumulated penalty is higher than some
        /// threshold, growing stops and the radius is fixed.
        /// </summary>
        /// <param name="v">POI position</param>
        /// <param name="world"></param>
        /// <returns>Radius of POI in world units</returns>
        private float DetermineRadiusOfPoi(Vector2 v, IWorld world) {
            var startingHeight = world.GetHeightAt(v.x, v.y);
            var penalty = 0.0f;
            var threshold = agentParameters.POIRadiusFlatnessThreshold;

            var directions = new Vector2[] {
                new Vector2(0, 1), //up
                new Vector2(0, -1), //down
                new Vector2(1, 0), //left
                new Vector2(-1, 0), //right

                new Vector2(1, 1), //top right
                new Vector2(-1, -1), //bottom left
                new Vector2(1, -1), //top left
                new Vector2(-1, 1), //bottom right
            };

            //looks the same as above but 
            var dirUpdateMask = (Vector2[])directions.Clone(); //vectors are value types!
            var directionHeights = new float[directions.Count()];
            Array.Fill(directionHeights, startingHeight);

            var radius = 1; //radius could technically be inferred from directions array but lets keep that array flexible
            while(penalty < threshold) {
                penalty = 0;
                for(int i = 0; i < directions.Count(); i++) {
                    var testPos = v + directions[i];
                    if(world.IsOutOfBounds(testPos)) continue;
                    var heightAtDir = directionHeights[i];
                    var heightAtTestPos = world.GetHeightAt(testPos.x, testPos.y);

                    var slopeCost = 0.0f;
                    var slope = heightAtTestPos - heightAtDir;
                    if(slope >= agentParameters.POIRadiusMinimumSlopeForPenalty) {
                        slopeCost = Mathf.Pow(slope + 1, agentParameters.POIRadiusSlopePenaltyPower);
                    }

                    penalty += slopeCost;
                    
                    var heightPenalty = agentParameters.POIRadiusHeightPenalty.Evaluate(Mathf.InverseLerp(world.MinHeight, world.MaxHeight, heightAtDir));
                    penalty += heightPenalty * agentParameters.POIRadiusHeightPenaltyMultiplier;

                    directions[i] += dirUpdateMask[i];
                    directionHeights[i] = heightAtTestPos;
                }
                radius++;
            }
            
            return radius;
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
        }
    }
    
}