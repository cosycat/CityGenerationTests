using System;
using System.Threading;
using System.Collections.Generic;
using FreeFormGraph.World;
using UnityEngine;

namespace FreeFormGraph.Agents {

    public class PathbuilderVisualizingAgent: MonoBehaviour, IAgent {
        

        private Pathfinding pathfinding;
        private IStreetGraph streetGraph;
        public Queue<(Vector3 start, Vector3 end)> pathsToBuild = new();

        private int frameCounter = 0;
        private List<Pathfinding.Waypoint> currentBestPath = new();

        public void DoWork(CancellationToken cancellationToken, IWorld world) {
            if(pathsToBuild.Count == 0) return;

            Func<bool> isCancelled = () => cancellationToken.IsCancellationRequested;
            pathfinding = new Pathfinding(streetGraph, world);

            var pathToBuild = pathsToBuild.Peek();
            Debug.Log($"Iscancelled {isCancelled()}");
            var path = pathfinding.AStar(pathToBuild.start, pathToBuild.end, isCancelled);
            Debug.Log("Pathfinding finished");

            if(path != null) {
                Pathfinding.BuildPath2(path, streetGraph, world);
                pathsToBuild.Dequeue();
            } else if(!isCancelled()) {
                //isCancelled == false => AStar couldn't find a path, there is no need to test it again next time
                //isCancelled == true => AStar couldn't finish and thus returned null (but could find a path still)
                pathsToBuild.Dequeue();
            }
            pathfinding = null; //TODO


            Debug.Log($"Agent finished, cancelled: {isCancelled()}");
        }

        void Start() {
            streetGraph = FindObjectOfType<StreetGraphGameObject>(); // TODO maybe get this from the IWorld, so it's not dependent on the scene?

            var width = 200;
            var height = 200;
            pathsToBuild.Enqueue((new Vector3(10, 10), new Vector3(width - 10, height - 10)));
            pathsToBuild.Enqueue((new Vector3(width - 10, 10), new Vector3(10, height - 10)));

            pathsToBuild.Enqueue((new Vector3(10, 10), new Vector3(width - 10, 10)));
            pathsToBuild.Enqueue((new Vector3(10, 10), new Vector3(10, height - 10)));
            pathsToBuild.Enqueue((new Vector3(10, height - 10), new Vector3(width - 10, height - 10)));
            pathsToBuild.Enqueue((new Vector3(width - 10, 10), new Vector3(width - 10, height - 10)));

            pathsToBuild.Enqueue((new Vector3(10, 185, 0), new Vector3(20, 192, 0)));
            pathsToBuild.Enqueue((new Vector3(10, 200 - 10, 0), new Vector3(140, 200 - 150, 0)));
            pathsToBuild.Enqueue((new Vector3(190, 200 - 55, 0), new Vector3(80, 200 - 180, 0)));
            pathsToBuild.Enqueue((new Vector3(4, 4, 0), new Vector3(140, 200 - 150, 0)));
            pathsToBuild.Enqueue((new Vector3(4, 4, 0), new Vector3(80, 20, 0)));
        }

        void OnDrawGizmos() {
            if(pathfinding != null && frameCounter == 0) {
                currentBestPath = pathfinding.GetShortestPath(pathfinding.startPosition, pathfinding.current);
            }
            if(pathfinding != null) {
                var postHeight = 4;
                var flagSize = 2;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pathfinding.startPosition.Pos, pathfinding.startPosition.Pos + Vector3.up*postHeight);
                Gizmos.DrawCube(pathfinding.startPosition.Pos + Vector3.up*(postHeight - (flagSize/2)) + Vector3.right * (flagSize/2.0f), new Vector3(flagSize, flagSize,0));
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pathfinding.target, pathfinding.startPosition.Pos);
                Gizmos.DrawLine(pathfinding.target, pathfinding.target + Vector3.up*postHeight);
                Gizmos.DrawCube(pathfinding.target + Vector3.up*(postHeight - (flagSize/2)) + Vector3.right * (flagSize/2.0f), new Vector3(flagSize, flagSize,0));
            }
            frameCounter = (frameCounter + 1) % 30;
            Gizmos.color = Color.magenta;
            for(int i = 0; i < currentBestPath.Count - 1; i++) {
                Gizmos.DrawLine(currentBestPath[i].Pos, currentBestPath[i+1].Pos);
            }


        }
    }
}