using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Simulation;
using UnityEngine;
using UnityEngine.Splines;

namespace Visualisation {
    
    public class RoadSplineVisualizer : MonoBehaviour {
        
        [CanBeNull]
        private SplineContainer _splineContainer;
        
        public void VisualizeRoadsFromCurrentWorld() {
            VisualizeRoads(World.Instance.GetAllTilesOfType(LandUsage.Road));
        }

        public void VisualizeRoads(List<Tile> allRoadTiles) {
            GenerateGraph(allRoadTiles);
            VisualizeGraph(GenerateGraph(allRoadTiles));
        }

        private Graph GenerateGraph(IReadOnlyCollection<Tile> allRoadTiles) {
            var graph = new Graph();
            Debug.Assert(allRoadTiles.Count > 0);
            var nextTiles = new Queue<Tile>();
            // find a crossing, or if there is none, the beginning of a road, or if it is just a circle, any tile
            var startTile =
                allRoadTiles.FirstOrDefault(t => t.GetNeighbors(false).Count(n => n.UsageType == LandUsage.Road) > 2)
                ?? allRoadTiles.FirstOrDefault(t => t.GetNeighbors(false).Count(n => n.UsageType == LandUsage.Road) < 2)
                ?? allRoadTiles.FirstOrDefault();
            if (startTile == null) {
                Debug.LogError("No road tiles found");
                return graph;
            }
            nextTiles.Enqueue(startTile);
            while (nextTiles.Count > 0) {
                var currTile = nextTiles.Dequeue();
                var currNode = graph.GetOrAddNode(currTile, out var currTileWasAdded);
                Debug.Assert(!currTileWasAdded || currTile == startTile); // startNode is the only one that should be added in the first iteration. All other nodes are added in the inner loop.
                
                foreach (var neighbor in currTile.GetNeighbors(false).Where(t => t.UsageType == LandUsage.Road)) {
                    Debug.Assert(currNode.Edges.Count == 0 // current Node has no edges yet, or:
                                 || (currNode.Edges.All(e => e.NodeA == currNode || e.NodeB == currNode) // all edges are connected to currNode
                                     && currNode.Edges.All(e => e.Tiles[0] == currTile || e.Tiles[^1] == currTile) // all edges start or end at currTile
                                     && currNode.Edges.All(e => e.NodeA != e.NodeB) // no self-edges
                                 ));
                    var edgeFoundAlready = currNode.Edges.Any(currNodeEdge => currNodeEdge.Tiles.Contains(neighbor));
                    if (edgeFoundAlready)
                        continue;
                    
                    // walk along until next crossing
                    var roadEdge = new List<Tile> {
                        currTile,
                        neighbor
                    };
                    while (roadEdge[^1].GetNeighbors(false).Count(n => n.UsageType == LandUsage.Road) == 2) {
                        var nextTile = roadEdge[^1].GetNeighbors(false).First(n => n.UsageType == LandUsage.Road && n != roadEdge[^2]);
                        roadEdge.Add(nextTile);
                    }
                    // Now we added all tiles of the road without a crossing, _including_ the next tile which is either a crossing or a dead end
                    
                    var nextNodeTile = roadEdge[^1];
                    Debug.Assert(nextNodeTile.GetNeighbors(false).Count(n => n.UsageType == LandUsage.Road) != 2); // sanity check. We now have either another crossing, or a dead end.
                    var nextNode = graph.GetOrAddNode(nextNodeTile, out var wasAdded);
                    if (wasAdded) {
                        // In the outer loop we found a new crossing to start from
                        nextTiles.Enqueue(nextNodeTile);
                    }
                    var edge = new Edge(currNode, nextNode, roadEdge, true);
                    graph.Edges.Add(edge);
                    Debug.Assert(edge.Tiles.Count >= 2); // sanity check
                }
            }
            return graph;
        }
        
        private void VisualizeGraph(Graph graph) {
            if (_splineContainer != null) {
                Destroy(_splineContainer.gameObject);
            }
            _splineContainer = new GameObject("RoadSplines").AddComponent<SplineContainer>();
            
            // connect all the tiles
            foreach (var edge in graph.Edges) {
                var spline = _splineContainer.AddSpline();
                foreach (var tile in edge.Tiles) {
                    // only add a tile if it is not in a corner. But add crossings and dead ends
                    var roadNeighbors = tile.GetNeighbors(false).Where(n => n.UsageType == LandUsage.Road).ToList();
                    if (roadNeighbors.Count == 2
                        && !(roadNeighbors[0].Position.x == roadNeighbors[1].Position.x || roadNeighbors[0].Position.y == roadNeighbors[1].Position.y)) {
                        continue; // They form an L-shape instead of an I-shape
                    }
                    var bezierKnot = new BezierKnot(new Vector3(tile.Position.x, tile.Position.y, 0));
                    spline.Add(bezierKnot, TangentMode.AutoSmooth);
                }
                
            }
            
            // connect only the nodes
            // foreach (var node in graph.Nodes) {
            //     foreach (var edge in node.Edges) {
            //         var spline = _splineContainer.AddSpline();
            //         spline.Add(new BezierKnot(new Vector3(edge.Tiles[0].Position.x, edge.Tiles[0].Position.y, 0)));
            //         spline.Add(new BezierKnot(new Vector3(edge.Tiles[^1].Position.x, edge.Tiles[^1].Position.y, 0)));
            //     }
            // }
        }

        private void OnGUI() {
            // if (GUILayout.Button("Visualize Roads")) {
            //     VisualizeRoadsFromCurrentWorld();
            // }
            // Button at the top right
            if (GUI.Button(new Rect(Screen.width - 100, 0, 100, 50), "Visualize Roads")) {
                VisualizeRoadsFromCurrentWorld();
            }
        }
    }

    public class Graph {
        public HashSet<Node> Nodes { get; } = new();
        public HashSet<Edge> Edges { get; } = new();

        public Node GetOrAddNode(Tile nodeTile, out bool wasAdded) {
            var node = Nodes.FirstOrDefault(n => n.Tile == nodeTile);
            if (node == null) {
                wasAdded = true;
                return AddNode(nodeTile);
            }
            wasAdded = false;
            return node;
        }

        private Node AddNode(Tile nodeTile) {
            Debug.Assert(Nodes.All(n => n.Tile != nodeTile));
            var node = new Node(nodeTile);
            Nodes.Add(node);
            return node;
        }
    }

    public class Node {
        public Tile Tile { get; }
        public HashSet<Edge> Edges { get; } = new();

        public Node(Tile tile) {
            Tile = tile;
        }
        
        public void AddEdge(Edge edge) {
            Edges.Add(edge);
        }

    }

    public class Edge {
        public Edge(Node nodeA, Node nodeB, List<Tile> tiles, bool addToNodes) {
            NodeA = nodeA;
            NodeB = nodeB;
            Tiles = tiles;
            if (addToNodes) {
                nodeA.AddEdge(this);
                nodeB.AddEdge(this);
            }
        }

        public Node NodeA { get; }
        public Node NodeB { get; }
        public List<Tile> Tiles { get; }
        
    }
}