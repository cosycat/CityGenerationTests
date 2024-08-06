using System.Collections.Generic;
using System.Linq;
using Graph;
using Graph.World;
using UnityEngine;

namespace DebugUtils {
    public class GraphDebugUtils {
        public static bool AssertStreetGraphConnectivity(IWorld world, string msg = "", bool fail = true) {
            var nodes = world.StreetGraph.Nodes;
            if (nodes.Count() == 0) return true;
            var queue = new Queue<IStreetNode>();
            var queueSet = new HashSet<IStreetNode>();


            queue.Enqueue(nodes.First());
            queueSet.Add(nodes.First());
            while (queue.Count != 0) {
                var current = queue.Dequeue();
                var neighbors = current.GetNeighbors();

                foreach (var neighbor in neighbors) {
                    if (!queueSet.Contains(neighbor)) {
                        queue.Enqueue(neighbor);
                        queueSet.Add(neighbor);
                    }
                }
            }

            var isOk = queueSet.Count() == nodes.Count();
            if (fail)
                Debug.Assert(isOk,
                    $"queueSet count: {queueSet.Count}, node count: {nodes.Count()}; custom error message: {msg}");
            return isOk;
        }
    }
}