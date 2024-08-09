using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Graph.World;
using Graph.World.PoI;
using UnityEngine;

namespace AgentSystem.Agents {
    public class SimulateTrafficAgent : MonoBehaviour, IAgent {
        [SerializeField] private SimulateTrafficParameters parameters = new(100);

        public AgentParameters Parameters => parameters;

        public void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context) {
            var random = context.Random;
            var nodes = world.StreetGraph.Nodes;

            //delay sim because?
            if (world.StreetGraph.NodeCount < 50) return;

            for (var i = 0; i < parameters.NumCarsToSimulate; i++) {
                var startIndex = random.Next(world.StreetGraph.NodeCount);
                var endIndex = startIndex;
                while (startIndex == endIndex) endIndex = random.Next(world.StreetGraph.NodeCount);

                var startNode = nodes.ElementAt(startIndex);
                var endNode = nodes.ElementAt(endIndex);

                var waypoints = Pathfinding.AStarStreetOnly(
                    world,
                    new Pathfinding.Waypoint(startNode),
                    new Pathfinding.Waypoint(endNode),
                    () => false);

                if (waypoints == null) {
                    Debug.LogWarning(
                        $"(SIM) no path found for {startNode} {endNode}! This is an error (we always want a connected graph) unless pathfinding was interrupted by user.");
                    continue;
                }

                var passedPOISet = new HashSet<IPointOfInterest>();
                foreach (var wp in waypoints) {
                    Debug.Assert(wp.GraphNode != null);
                    Debug.Log(wp);
                    if (world.PointsOfInterest.GetPointOfInterestFromNode(wp.GraphNode, out var poi) &&
                        !passedPOISet.Contains(poi))
                        if (poi is BudgetPointOfInterest budgetPoi) { //eww...
                            budgetPoi.Budget += parameters.BudgetIncreasePerPass;
                            passedPOISet.Add(budgetPoi);
                        }
                }
            }
        }

        [Serializable]
        private class SimulateTrafficParameters : AgentParameters {
            
            [field:SerializeField] public AgentVariableInt NumCarsToSimulate = new("Number of cars to simulate", 10, 1, 100);
            [field:SerializeField] public AgentVariableFloat BudgetIncreasePerPass = new("Budget increase per pass", 10, 1, 1000);
            
            protected override IAgentVariable[] GetVariables() {
                return new IAgentVariable[] {
                    WorkFrequency,
                    NumCarsToSimulate,
                    BudgetIncreasePerPass
                };
            }

            public SimulateTrafficParameters(int initialWorkFrequency) : base(initialWorkFrequency) { }
        }
    }
}