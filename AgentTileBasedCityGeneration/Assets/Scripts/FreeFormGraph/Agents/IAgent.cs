using System.Collections.Generic;
using System.Threading;
using FreeFormGraph.World;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph.Agents {
    
    public interface IAgent {
        
        /// <summary>
        /// A work package for an agent, that is run on a thread in the background.
        /// 
        /// Check periodically if the agent should cancel with the <see cref="CancellationToken"/>.
        /// (https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="world"></param>
        void DoWork(CancellationToken cancellationToken, IWorld world);

    }

    public class PathfindingAgent : IAgent {
        
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