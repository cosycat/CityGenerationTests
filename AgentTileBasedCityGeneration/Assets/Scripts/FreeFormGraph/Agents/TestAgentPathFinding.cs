using System.Collections.Generic;
using System.Threading;
using FreeFormGraph.World;
using UnityEngine;

namespace FreeFormGraph.Agents {
    public class TestAgentPathFinding : IAgent {
        
        public void DoWork(CancellationToken cancellationToken, IWorld world) {
            var path = FindPath(cancellationToken, world);
            cancellationToken.ThrowIfCancellationRequested();
            CommitPath(world);
        }

        private List<Pathfinding.Waypoint> FindPath(CancellationToken cancellationToken, IWorld world) {
            for (int i = 0; i < 50_000_000; i++) {
                if (i % 100_000 == 0) {
                    if (cancellationToken.IsCancellationRequested) {
                        Debug.Log($"IsCancellationRequested with i: {i}");
                        // cleanup
                        cancellationToken.ThrowIfCancellationRequested();
                        // never executed
                    }
                }
            }

            Debug.Log("Path found");
            return new List<Pathfinding.Waypoint>();
        }

        private void CommitPath(IWorld world) {
            Debug.Log("Path commited");
            // TODO
        }
        
    }
}