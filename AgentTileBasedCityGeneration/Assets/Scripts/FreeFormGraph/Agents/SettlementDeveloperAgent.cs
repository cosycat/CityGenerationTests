using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

namespace FreeFormGraph.Agents {
    
    public class SettlementDeveloperAgent : IAgent {
        
        // parameters
        private readonly float minStreetLength = 1f;
        private readonly float maxStreetLength = 5f;
        private readonly float angleOffset = Mathf.Deg2Rad * 90f;
        private readonly float angleRandomMax = Mathf.Deg2Rad * 0f;
        
        private IPointOfInterest pointOfInterest;
        private readonly List<IStreetNode> nodes = new();
        
        private readonly Random random = new Random();
        
        public SettlementDeveloperAgent([NotNull] IPointOfInterest pointOfInterest, [NotNull] IWorld world) {
            this.pointOfInterest = pointOfInterest;
            var startNode = world.StreetGraph.TryFindClosestNode(pointOfInterest.Position, out var closestNode, minStreetLength) 
                ? closestNode : world.StreetGraph.CreateUnconnectedNode(pointOfInterest.Position, out var newNode) ? newNode : throw new Exception("Could not find or create a node for the point of interest.");
            nodes.Add(startNode);
        }

        public void DoWork(CancellationToken cancellationToken, IWorld world) {
            GrowRoadNetwork(world);
        }

        private void GrowRoadNetwork(IWorld world) {
            var node = GetRandomNode();
            Debug.Assert(node != null, $"PoIDeveloperAgent: Node is null.");
            var averageInPosition = GetAverageInPosition(node); // TODO take a random incoming edge as direction
            var direction = node.Position - averageInPosition;
            var angle = Mathf.Atan2(direction.x, direction.y);
            var angleRandom = (float)random.NextDouble() * 2f * angleRandomMax - angleRandomMax; //UnityEngine.Random.Range(-angleRandomMax, angleRandomMax);
            var length = (float)random.NextDouble() * (maxStreetLength - minStreetLength) + minStreetLength; //UnityEngine.Random.Range(minStreetLength, maxStreetLength);
            var newPoint = new Vector2(node.Position.x + Mathf.Sin(angle + angleOffset + angleRandom) * length, node.Position.y + Mathf.Cos(angle + angleOffset + angleRandom) * length);
            // TODO check if the new angle is in a legal range for every edge
            if(world.StreetGraph.TryFindClosestNode(newPoint, out var closestNode, length)) {
                Debug.Log($"PoIDeveloperAgent: New point {newPoint} is too close to an existing node: {closestNode.Position}");
                // the new point is too close to an existing node, discard it
                return;
            }
            
            if (!pointOfInterest.IsPointWithinRange(newPoint)) {
                Debug.Log($"PoIDeveloperAgent: New point {newPoint} is outside the point of interest.");
                // the new point is outside the point of interest, discard it
                return;
            }
            
            // TODO check for intersections

            if (!world.StreetGraph.CreateUnconnectedNode(newPoint, out var newNode)) {
                Debug.LogWarning("Could not create a new node for the PoIDeveloperAgent.");
                // could not create a new node, discard the new point
                return;
            }
            world.StreetGraph.CreateEdge(node, newNode, out var newEdge);
            nodes.Add(newNode);
        }

        private Vector3 GetAverageInPosition(IStreetNode node) {
            if (node.ConnectedEdgesCount == 0) return new Vector3(1, 0, 0); // TODO Random direction
            var average = new Vector3(0, 0, 0);
            foreach (var edge in node.Edges) {
                var otherNode = edge.NodeA == node ? edge.NodeB : edge.NodeA;
                average += otherNode.Position;
            }
            return average / node.ConnectedEdgesCount;
        }

        private IStreetNode GetRandomNode() {
            return nodes[random.Next(0, nodes.Count)];
        }
    }
    
}