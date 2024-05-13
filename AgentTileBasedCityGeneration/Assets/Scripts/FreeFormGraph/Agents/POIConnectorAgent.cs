#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;
using UnityEngine;

namespace FreeFormGraph.Agents {

    public class POIConnectorAgent: MonoBehaviour, IAgent {

        public int WorkFrequency { get; set; } = 100;
        
        private object pathFindingLock = new();
        private Pathfinding? pathfinding;

        private HashSet<IPointOfInterest> connectedPOIs = new();

        private int frameCounter = 0;


        /// <summary>
        /// Actual best path that pathfiding has achieved (but didn't reach target yet). Set to empty when no pathfinding is active.
        /// </summary>
        private List<Pathfinding.Waypoint> currentBestPath = new();

        private Vector3 currentTarget = new();


        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {
            var worldPOIs = world.PointsOfInterest.PointsOfInterest;
            if(connectedPOIs.Count == 0 && worldPOIs.Count >= 1) {
                //initial condition: first POI in world does not need to be connected
                connectedPOIs.Add(worldPOIs[0]);
                return;
            }


            var unconnectedPoi = worldPOIs.FirstOrDefault(poi => !connectedPOIs.Contains(poi));
            if(unconnectedPoi == null) return;

            var closestPoi = connectedPOIs.ToList().MinBy(poi => Vector3.Distance(poi.Position, unconnectedPoi.Position));

            Func<bool> isCancelled = () => cancellationToken.IsCancellationRequested;
            lock(pathFindingLock) {
                pathfinding = new Pathfinding(world.StreetGraph, world);
            }
            var path = pathfinding.AStar(unconnectedPoi.Position, closestPoi.Position, isCancelled);

            if(path != null) {
                Pathfinding.BuildPath2(path, world.StreetGraph, world);
                connectedPOIs.Add(unconnectedPoi);
            } else if(!isCancelled()) {
                //isCancelled == false => AStar couldn't find a path, there is no need to test it again next time
                //isCancelled == true => AStar couldn't finish and thus returned null (but could find a path still)


                if(!isCancelled()) {
                    //path could not be found. POI can never be connected
                    worldPOIs.Remove(unconnectedPoi);
                }

            }
            lock(pathFindingLock) {
                pathfinding = null; //TODO
            }
        }

        void OnDrawGizmos() {
            if(frameCounter == 0) {
                //don't do it every frame. locking can be costly but also getting the current shortest path takes time

                lock(pathFindingLock) {

                    if(pathfinding != null) {
                        currentBestPath = pathfinding.GetShortestPath(pathfinding.startPosition, pathfinding.current);
                        currentTarget = pathfinding.target;
                    } else {
                        currentBestPath.Clear();
                    }
                }
            }
            frameCounter = (frameCounter + 1) % 60;

            if (currentBestPath.Count != 0) {
                //draw path
                Gizmos.color = Color.magenta;
                for(int i = 0; i < currentBestPath.Count - 1; i++) {
                    Gizmos.DrawLine(currentBestPath[i].Pos, currentBestPath[i+1].Pos);
                }

                //draw flags
                if (currentBestPath == null || currentBestPath.Count == 0) return;
                var currentStartPosition = currentBestPath[0].Pos; // this threw a null reference exception
                var postHeight = 4;
                var flagSize = 2;
                Gizmos.color = Color.green;
                //casting to force unwrap nullable...
                Gizmos.DrawLine(currentStartPosition, currentStartPosition + Vector3.up * postHeight);
                Gizmos.DrawCube(currentStartPosition + Vector3.up * (postHeight - (flagSize / 2)) + Vector3.right * (flagSize / 2.0f), new Vector3(flagSize, flagSize, 0));
                Gizmos.color = Color.red;
                Gizmos.DrawLine(currentTarget, currentStartPosition);
                Gizmos.DrawLine(currentTarget, currentTarget + Vector3.up * postHeight);
                Gizmos.DrawCube(currentTarget + Vector3.up * (postHeight - (flagSize / 2)) + Vector3.right * (flagSize / 2.0f), new Vector3(flagSize, flagSize, 0));
            }


        }
    }
}