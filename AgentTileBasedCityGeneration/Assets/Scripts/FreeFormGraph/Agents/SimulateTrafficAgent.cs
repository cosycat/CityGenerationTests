using System;
using System.Linq;
using System.Collections.Generic;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;
using UnityEngine;
using System.Threading;

namespace FreeFormGraph.Agents {

    public class SimulateTrafficAgent: MonoBehaviour, IAgent {

        public int WorkFrequency { get; set; } = 100;

        [SerializeField]
        public int numCarsToSimulate = 10;

        [SerializeField]
        public float budgetIncreasePerPass = 1;

        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {

            var random = context.Random;
            var nodes = world.StreetGraph.Nodes;

            //delay sim because?
            if(world.StreetGraph.NodeCount < 50) return;
        
            for(int i = 0; i < numCarsToSimulate; i++) {
                var startIndex = random.Next(world.StreetGraph.NodeCount);
                var endIndex = startIndex;
                while(startIndex == endIndex) endIndex = random.Next(world.StreetGraph.NodeCount);

                var startNode = nodes.ElementAt(startIndex);
                var endNode = nodes.ElementAt(endIndex);

                var waypoints = Pathfinding.AStarStreetOnly(
                    world,
                    new Pathfinding.Waypoint(startNode),
                    new Pathfinding.Waypoint(endNode),
                    () => false);

                if(waypoints == null) {
                    Debug.LogWarning($"(SIM) no path found for {startNode} {endNode}! This is an error (we always want a connected graph) unless pathfinding was interrupted by user.");
                    continue;
                }
                var passedPOISet = new HashSet<IPointOfInterest>();
                foreach(var wp in waypoints) {
                    Debug.Assert(wp.GraphNode != null);
                    Debug.Log(wp);
                    if(world.PointsOfInterest.GetPointOfInterestFromNode(wp.GraphNode, out var poi) && !passedPOISet.Contains(poi)) {
                        if(poi is BudgetPointOfInterest budgetPoi) { //eww...
                            budgetPoi.Budget += budgetIncreasePerPass;
                            passedPOISet.Add(budgetPoi);
                        }
                    }
                }

            }
        }
    }
}