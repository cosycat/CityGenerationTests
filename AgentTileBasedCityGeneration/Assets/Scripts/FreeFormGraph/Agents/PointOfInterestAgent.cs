using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.Agents {

    public class PointOfInterestAgent : MonoBehaviour, IAgent {
        
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

            var random = context.random;
            if (world.PointsOfInterest.PointsOfInterest.Count < agentParameters.DesiredNumberOfPoints)  {
                CreatePOI(world, streetGraph, context);
            }
        }

        private void CreatePOI(IWorld world, IStreetGraph streetGraph, AgentManager.Context context) {
            var random = context.random;
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
            var sizeRange = GetRadiusForPointOfInterestType(pointOfInterestType);
            var radiusPoi = random.Next((int)sizeRange.min, (int)sizeRange.max);
            var pointOfInterest = new SpherePointOfInterest(newPoiPos, pointOfInterestType, radiusPoi);
            world.PointsOfInterest.AddPointOfInterest(pointOfInterest);
            context.manager.AddNewAgent(new SettlementDeveloperAgent(pointOfInterest, world, settlementDeveloperAgentParameters));
        }

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

        void Start() {
            streetGraph = FindObjectOfType<StreetGraphGameObject>();
        }

        [Serializable]
        public class POIAgentParameters {
            [Min(0)] public int DesiredNumberOfPoints = 35;
            [SerializeField] public float DistanceCostFactor = 35;
            [Min(0)] public int MinDistanceToRoad = 20;
            [Min(0)] public int RadiusGeneration = 100;
        }
    }
    
}