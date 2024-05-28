using System;
using System.Collections.Generic;
using System.Linq;
using FreeFormGraph.World;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph {
    
    public class FreeFormStreetGraphTest : MonoBehaviour {

        private IStreetGraph streetGraph;
        private IWorld world;

        [CanBeNull] private IStreetNode dragStartNode;
        
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private bool drawCurveBoxes;
        [SerializeField] private bool drawIntersectionLines = true;
        [SerializeField] private bool drawLabelsEdges = false;
        [SerializeField] private bool drawLabelsNodes = false;
        [SerializeField] private bool drawMouseLabel = true;
        [SerializeField] private float mouseNodeDistanceThreshold = 0.3f;

        private readonly Dictionary<IStreetEdge, Color> edgeColors = new();

        private static Vector2 MouseWorldPosition => Camera.main!.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));

        private void Start() {
            streetGraph ??= FindObjectOfType<StreetGraphGameObject>();
            world = FindObjectOfType<WorldGameObject>();
            if (streetGraph == null) {
                Debug.LogError("No StreetGraph found in Scene.");
                Destroy(this);
            }
            if (FindObjectsOfType<StreetGraphGameObject>().Length > 1) {
                Debug.LogWarning("Multiple StreetGraphs found in Scene. Using the first one found.");
            }
        }

        private void Update() {
            CheckMouseCreation();
        }

        private void CheckMouseCreation() {
            const float dragThreshold = 0.3f;
            var mousePositionWorld = MouseWorldPosition;
            if (Input.GetMouseButtonDown(0)) {
                if (streetGraph.TryFindClosestNode(mousePositionWorld, out var closestNode, dragThreshold)) {
                    dragStartNode = closestNode;
                    Debug.Log($"FreeFormStreetGraphTest - Dragging from {dragStartNode}");
                } else {
                    Debug.Log($"No node found at {mousePositionWorld}.");
                    dragStartNode = null;
                }
            } else if (Input.GetMouseButtonUp(0)) {
                if (dragStartNode == null) {
                    return;
                }
                if (!streetGraph.CreateEdge(dragStartNode, mousePositionWorld, out _, out _, out _, out _)) {
                    Debug.LogWarning("FreeFormStreetGraphTest - Failed to create new edge.");
                }

                dragStartNode = null;
            }
        }

        private void OnGUI() {
            // Contents:
            // - A textfield to enter coordinates for a new node
            // - Multiple buttons to create a new node at the entered coordinates:
            //   - "Create Node Random" creates a new node connected to a random existing node
            //   - "Create Node Closest" creates a new node connected to the closest existing node
            //   - "Create Node Previous" creates a new node connected to the previously created node
            
            GUILayout.BeginArea(new Rect(10, 10, 200, 600));
            GUILayout.Label($"Number of nodes: {streetGraph.NodeCount}");
            GUILayout.Label($"Number of edges: {streetGraph.EdgeCount}");
            if (GUILayout.Button($"Draw grid: ({drawGrid})")) {
                drawGrid = !drawGrid;
            }
            if (GUILayout.Button($"Bezier B-Boxes: ({drawCurveBoxes})")) {
                drawCurveBoxes = !drawCurveBoxes;
            }
            if (GUILayout.Button($"Intersection Lines: ({drawIntersectionLines})")) {
                drawIntersectionLines = !drawIntersectionLines;
            }
            if (GUILayout.Button($"Graph color: Recalculate colors")) {
                GraphColoring();
            }
            GUILayout.Label("Labels:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Nodes ({drawLabelsNodes})")) {
                drawLabelsNodes = !drawLabelsNodes;
            }
            if (GUILayout.Button($"Edges ({drawLabelsEdges})")) {
                drawLabelsEdges = !drawLabelsEdges;
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"Mouse Label ({drawMouseLabel})")) {
                drawMouseLabel = !drawMouseLabel;
            }
            GUILayout.Label("Drag from one node to create a new connection or node.");
            GUILayout.EndArea();
        }

        private void GraphColoring() {
            //BFS
            //I know there are probably better algorithms for this...
            var colors = new List<Color>(){Color.red, Color.blue, Color.yellow, Color.green, Color.cyan, Color.magenta};
            var queue = new Queue<IStreetNode>();
            var visited = new HashSet<IStreetNode>();
            queue.Enqueue(streetGraph.Nodes.ToList()[0]);
            visited.Add(streetGraph.Nodes.ToList()[0]);

            while(queue.Count != 0) {
                var current = queue.Dequeue();
                visited.Add(current);
                var neighborColors = new HashSet<Color>();
                //find colors of neighbors
                foreach(var edge in current.Edges) {
                    if(edgeColors.TryGetValue(edge, out var color)) {
                        neighborColors.Add(color);
                    }
                }

                //color edges
                var usableColors = colors.Except(neighborColors).ToList();
                var colorIndex = 0;
                foreach(var edge in current.Edges) {
                    if(!edgeColors.ContainsKey(edge)) {
                        edgeColors[edge] = usableColors[colorIndex];
                        colorIndex++;
                    }
                }

                //enqueue new nodes
                foreach(var edge in current.Edges) {
                    var nextNode = edge.NodeA;
                    if(nextNode == current) nextNode = edge.NodeB;
                    if(!visited.Contains(nextNode)) {
                        queue.Enqueue(nextNode);
                    }
                }
            }
        }

        private void OnDrawGizmos() {
            if (streetGraph == null) {
                return;
            }

            try {
                var nodes = streetGraph.Nodes.ToArray();
                var edges = streetGraph.Edges.ToArray();

                var mouseWorldPosition = MouseWorldPosition;

                foreach (var node in nodes) {
                    Gizmos.color = node.IsMaxConnectedEdgesReached ? Color.red :
                        node.ConnectedEdgesCount > 2 ? Color.yellow : Color.green;
                    Gizmos.DrawSphere(node.Position, 0.1f);
                }

                foreach (var edge in edges) {
                    Gizmos.color = edgeColors.TryGetValue(edge, out var color) ? color : Color.grey;
                    Gizmos.DrawLine(edge.PositionNodeA, edge.PositionNodeB);
                }

                Gizmos.color = Color.green;
                if (Input.GetMouseButton(0) && dragStartNode != null) {
                    Gizmos.DrawLine(dragStartNode.Position, mouseWorldPosition);
                }

                if (drawGrid) {
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    for (int i = 0; i < world.Width; i++) {
                        Gizmos.DrawRay(new Vector3(i, 0), Vector3.up * world.Height);
                    }

                    for (int i = 0; i < world.Height; i++) {
                        Gizmos.DrawRay(new Vector3(0, i), Vector3.right * world.Width);
                    }
                }

                if (drawLabelsEdges) {
                    // UnityEditor.Handles.color = Color.yellow;
                    foreach (var edge in edges) {
                        UnityEditor.Handles.Label((edge.PositionNodeA + edge.PositionNodeB) / 2, edge.DebugString());
                    }
                }

                if (drawLabelsNodes) {
                    // UnityEditor.Handles.color = Color.white;
                    DrawLabelNode(nodes.ToArray());
                }

                if (drawMouseLabel) {
                    Gizmos.DrawWireSphere(mouseWorldPosition, mouseNodeDistanceThreshold);
                    var nodesWithinRange =
                        streetGraph.FindAllNodesWithinRange(mouseWorldPosition, mouseNodeDistanceThreshold);
                    DrawLabelNode(nodesWithinRange);
                    var foundANode = streetGraph.TryFindClosestNode(mouseWorldPosition, out var closestNode,
                        mouseNodeDistanceThreshold);
                    UnityEditor.Handles.Label(mouseWorldPosition,
                        $"{mouseWorldPosition}{(foundANode ? $" {closestNode}" : "")}");
                }
            }
            catch (InvalidOperationException) {
                
            }

        }

        private static void DrawLabelNode(IStreetNode[] nodes) {
            for (var i = 0; i < nodes.Length; i++) {
                var node = nodes[i];
                UnityEditor.Handles.Label(node.Position, $"(Idx: {i}) {node.DebugString()}");
            }
        }
    }
}