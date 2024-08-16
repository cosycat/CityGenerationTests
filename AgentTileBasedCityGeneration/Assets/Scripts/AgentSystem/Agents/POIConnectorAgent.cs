#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Graph.World;
using Graph.World.PoI;
using UnityEngine;

namespace AgentSystem.Agents {
    public class POIConnectorAgent : MonoBehaviour, IAgent {
        
        [SerializeField] private POIConnectorParameters parameters = new(100);
        public AgentParameters Parameters => parameters;


        private readonly HashSet<IPointOfInterest> connectedPOIs = new();

        private readonly object pathFindingLock = new();


        /// <summary>
        ///     Actual best path that pathfinding has achieved (but didn't reach target yet). Set to empty when no pathfinding is
        ///     active.
        /// </summary>
        private List<Pathfinding.Waypoint> currentBestPath = new();

        private Vector3 currentTarget;

        private int frameCounter;

        private Pathfinding? pathfinding;


        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {
            var worldPOIs = world.PointsOfInterest.PointsOfInterest;
            if (connectedPOIs.Count == 0 && worldPOIs.Count >= 1) {
                //initial condition: first POI in world does not need to be connected
                connectedPOIs.Add(worldPOIs[0]);
                if (parameters.PlaceSettlementDeveloper)
                    context.Manager.AddNewAgent(new SettlementDeveloperAgent((BudgetPointOfInterest)worldPOIs[0], world,
                        parameters.sdaParameters));
                return;
            }


            var unconnectedPoi = worldPOIs.FirstOrDefault(poi => !connectedPOIs.Contains(poi));
            if (unconnectedPoi == null) return;

            var closestPoi = connectedPOIs.ToList()
                .MinBy(poi => Vector3.Distance(poi.Position, unconnectedPoi.Position));

            Func<bool> isCancelled = () => cancellationToken.IsCancellationRequested;
            lock (pathFindingLock) {
                pathfinding = new Pathfinding(world.StreetGraph, world, parameters.PathfindingParameters);
            }

            var path = pathfinding.AStar(unconnectedPoi.Position, closestPoi.Position, isCancelled);

            if (path != null) {
                if (Pathfinding.BuildPath2(path, world.StreetGraph, world, parameters.PathfindingParameters)) {
                    connectedPOIs.Add(unconnectedPoi);
                    if (parameters.PlaceSettlementDeveloper)
                        context.Manager.AddNewAgent(new SettlementDeveloperAgent((BudgetPointOfInterest)unconnectedPoi,
                            world, parameters.sdaParameters));
                    Debug.Assert(world.StreetGraph.TryFindClosestNode(unconnectedPoi.Position, out var node));
                    world.PointsOfInterest.AddNodeRelationToPointOfInterest(node, unconnectedPoi);
                }
                else {
                    //path could not be built for some reason (angles, too many connections...)
                    worldPOIs.Remove(unconnectedPoi);
                }
            }
            else if (!isCancelled()) {
                //isCancelled == false => AStar couldn't find a path, there is no need to test it again next time
                //isCancelled == true => AStar couldn't finish and thus returned null (but could find a path still)


                if (!isCancelled())
                    //path could not be found. POI can never be connected
                    worldPOIs.Remove(unconnectedPoi);
            }

            lock (pathFindingLock) {
                pathfinding = null; //TODO
            }
        }

        private void OnDrawGizmos() {
            if (frameCounter == 0)
                //don't do it every frame. locking can be costly but also getting the current shortest path takes time
                lock (pathFindingLock) {
                    if (pathfinding != null) {
                        Debug.Assert(pathfinding.Current != null && pathfinding.Target != null &&
                                     pathfinding.StartPosition != null);
                        currentBestPath = pathfinding.GetShortestPath(pathfinding.StartPosition!.Value,
                            pathfinding.Current!.Value);
                        currentTarget = pathfinding.Target!.Value;
                    }
                    else {
                        currentBestPath.Clear();
                    }
                }

            frameCounter = (frameCounter + 1) % 60;

            if (currentBestPath.Count != 0) {
                //draw path
                Gizmos.color = Color.magenta;
                for (var i = 0; i < currentBestPath.Count - 1; i++)
                    Gizmos.DrawLine(currentBestPath[i].Pos, currentBestPath[i + 1].Pos);

                //draw flags
                if (currentBestPath.Count == 0) return;
                var currentStartPosition = currentBestPath[0].Pos; // this threw a null reference exception
                var postHeight = 4;
                var flagSize = 2;
                Gizmos.color = Color.green;
                //casting to force unwrap nullable...
                Gizmos.DrawLine(currentStartPosition, (Vector3)currentStartPosition + Vector3.up * postHeight);
                Gizmos.DrawCube(
                    (Vector3)currentStartPosition + Vector3.up * (postHeight - flagSize / 2f) +
                    Vector3.right * (flagSize / 2.0f), new Vector3(flagSize, flagSize, 0));
                Gizmos.color = Color.red;
                Gizmos.DrawLine(currentTarget, currentStartPosition);
                Gizmos.DrawLine(currentTarget, currentTarget + Vector3.up * postHeight);
                Gizmos.DrawCube(
                    currentTarget + Vector3.up * (postHeight - flagSize / 2f) + Vector3.right * (flagSize / 2.0f),
                    new Vector3(flagSize, flagSize, 0));
            }
        }

        [Serializable]
        public class POIConnectorParameters : AgentParameters {
            
            [field:SerializeField] public AgentVariableBool PlaceSettlementDeveloper { get; private set; } = new("Place Settlement Developer", true);
            
            [field:SerializeField] public Pathfinding.Parameters PathfindingParameters { get; private set; } = new();

            [SerializeField] public List<SettlementDeveloperAgent.SdaParameters> sdaParameters = new() {
                new SettlementDeveloperAgent.SdaParameters(angleOffset: Mathf.Deg2Rad * 20, angleRandomMax: Mathf.Deg2Rad * 30, time: 25),
                new SettlementDeveloperAgent.SdaParameters()
            };

            protected override IAgentVariable[] GetVariables() {
                return new IAgentVariable[] {WorkFrequency, PlaceSettlementDeveloper}
                    .Concat(PathfindingParameters.AllVariables)
                    // .Concat(sdaParameters.SelectMany(p => p.AllVariables)) // too chaotic if these are included. A multi-level system would be necessary.
                    .ToArray();
            }

            public POIConnectorParameters(int initialWorkFrequency) : base(initialWorkFrequency) { }
        }
    }
}