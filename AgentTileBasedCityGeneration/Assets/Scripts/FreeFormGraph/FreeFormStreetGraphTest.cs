using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using FreeFormGraph.World;

namespace FreeFormGraph {
    
    public class FreeFormStreetGraphTest : MonoBehaviour {

        private IStreetGraph _streetGraph;
        private IWorld World;

        [CanBeNull] private IStreetNode _dragStartNode;
        
        private bool _drawGrid = true;
        private bool _drawCurveBoxes = false;
        private bool _drawIntersectionLines = true;
        private bool _drawLabelsEdges = true;
        private bool _drawLabelsNodes = true;

        private Dictionary<IStreetEdge, Color> edgeColors = new();

        private void Start() {
            _streetGraph ??= FindObjectOfType<StreetGraphGameObject>().graph;
            World = FindObjectOfType<World.WorldGameObject>() as IWorld;
            if (_streetGraph == null) {
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
            var mousePosWorld = MouseWorldPos;
            if (Input.GetMouseButtonDown(0)) {
                if (_streetGraph.TryFindClosestNode(mousePosWorld, out var closestNode, dragThreshold)) {
                    _dragStartNode = closestNode;
                    Debug.Log($"FreeFormStreetGraphTest - Dragging from {_dragStartNode}");
                } else {
                    Debug.Log($"No node found at {mousePosWorld}.");
                    _dragStartNode = null;
                }
            } else if (Input.GetMouseButtonUp(0)) {
                if (_dragStartNode == null) {
                    return;
                }
                if (!_streetGraph.CreateEdge(_dragStartNode, mousePosWorld, out _, out _, out _)) {
                    Debug.LogWarning("FreeFormStreetGraphTest - Failed to create new edge.");
                }

                _dragStartNode = null;
            }
        }

        private static Vector2 MouseWorldPos => Camera.main!.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
        
        private void OnGUI() {
            // Contents:
            // - A textfield to enter coordinates for a new node
            // - Multiple buttons to create a new node at the entered coordinates:
            //   - "Create Node Random" creates a new node connected to a random existing node
            //   - "Create Node Closest" creates a new node connected to the closest existing node
            //   - "Create Node Previous" creates a new node connected to the previously created node
            
            GUILayout.BeginArea(new Rect(10, 10, 200, 600));
            GUILayout.Label($"Number of nodes: {_streetGraph.NodeCount}");
            GUILayout.Label($"Number of edges: {_streetGraph.EdgeCount}");
            if (GUILayout.Button($"Draw grid: ({_drawGrid})")) {
                _drawGrid = !_drawGrid;
            }
            if (GUILayout.Button($"Bezier B-Boxes: ({_drawCurveBoxes})")) {
                _drawCurveBoxes = !_drawCurveBoxes;
            }
            if (GUILayout.Button($"Intersection Lines: ({_drawIntersectionLines})")) {
                _drawIntersectionLines = !_drawIntersectionLines;
            }
            if (GUILayout.Button($"Graph color: Recalculate colors")) {
                GraphColoring();
            }
            GUILayout.Label("Labels:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Nodes: ({_drawLabelsNodes})")) {
                _drawLabelsNodes = !_drawLabelsNodes;
            }
            if (GUILayout.Button($"Edges: ({_drawLabelsEdges})")) {
                _drawLabelsEdges = !_drawLabelsEdges;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Drag from one node to create a new connection or node.");
            GUILayout.EndArea();
        }

        private void GraphColoring() {
            //BFS
            //I know there are propably better algorithms for this...
            var colors = new List<Color>(){Color.red, Color.blue, Color.yellow, Color.green, Color.cyan, Color.magenta};
            var queue = new Queue<IStreetNode>();
            var visited = new HashSet<IStreetNode>();
            queue.Enqueue(_streetGraph.Nodes.ToList()[0]);
            visited.Add(_streetGraph.Nodes.ToList()[0]);

            while(queue.Count != 0) {
                var current = queue.Dequeue();
                visited.Add(current);
                var neighborColors = new HashSet<Color>();
                //find colors of neighbors
                foreach(var edge in current.Edges) {
                    if(edgeColors.ContainsKey(edge)) {
                        neighborColors.Add(edgeColors[edge]);
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

                //enqueu new nodes
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
            if (_streetGraph == null) {
                return;
            }
            foreach (var node in _streetGraph.Nodes) {
                Gizmos.color = node.IsMaxConnectedEdgesReached ? Color.red : node.ConnectedEdgesCount > 2 ? Color.yellow : Color.green;
                Gizmos.DrawSphere(node.Position, 0.1f);
                Gizmos.DrawLine(node.Position, node.Position +
                                               (Quaternion.Euler(0, 0, node.EntranceAngle ?? 0) *
                                                (Vector3.right * 0.3f)));
            }

            foreach (var edge in _streetGraph.Edges) {
                if(edgeColors.ContainsKey(edge)) {
                    Gizmos.color = edgeColors[edge];
                } else {
                    Gizmos.color = Color.grey;
                }
                Gizmos.DrawLine(edge.PosA, edge.PosB);
                foreach (var point in edge.SplitIntoEvenlySpacedPoints()) {
                    Gizmos.DrawSphere(point, 0.05f);
                }
            }
            
            Gizmos.color = Color.green;
            if (Input.GetMouseButton(0) && _dragStartNode != null) {
                Gizmos.DrawLine(_dragStartNode.Position, MouseWorldPos);
            }

            // if (_drawIntersectionLines) {
            //     foreach ((var pos, var dir) in Intersections._dbg_curveSteps) {
            //         Gizmos.color = Color.grey;
            //         Gizmos.DrawRay(pos, Vector3.up * dir);
            //     }
            //
            //     foreach (var intersection in Intersections._dbg_splineIntersectionPoints) {
            //         Gizmos.color = Color.magenta;
            //         Gizmos.DrawRay(intersection, Vector3.up);
            //     }
            // }

            if(_drawGrid) {
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                for(int i = 0; i < World.Width; i++) {
                    Gizmos.DrawRay(new Vector3(i, 0), Vector3.up * World.Height);
                }
                for(int i = 0; i < World.Height; i++) {
                    Gizmos.DrawRay(new Vector3(0, i), Vector3.right * World.Width);
                }
            }

            // if(_drawCurveBoxes) {
            //     foreach(var s in _streetGraph.Nodes.SelectMany(n => n.CorrespondingSplines).Distinct()) {
            //         for(var i = 0; i < SplineUtility.GetCurveCount(s); i++) {
            //             var targetCurve = s.GetCurve(i);
            //             var bounds = Intersections.GetBoundsForCurve(targetCurve);
            //             Gizmos.DrawRay(bounds.min, Vector3.right * (bounds.max.x - bounds.min.x));
            //             Gizmos.DrawRay(bounds.min, Vector3.up* (bounds.max.y - bounds.min.y));
            //             Gizmos.DrawRay(bounds.max, Vector3.left * (bounds.max.x - bounds.min.x));
            //             Gizmos.DrawRay(bounds.max, Vector3.down* (bounds.max.y - bounds.min.y));
            //         }
            //     }
            // }
            
            if (_drawLabelsEdges) {
                // UnityEditor.Handles.color = Color.yellow;
                foreach (var edge in _streetGraph.Edges) {
                    UnityEditor.Handles.Label((edge.PosA + edge.PosB) / 2, edge.DebugString());
                }
            }
            if (_drawLabelsNodes) {
                // UnityEditor.Handles.color = Color.white;
                foreach (var node in _streetGraph.Nodes) {
                    UnityEditor.Handles.Label(node.Position, node.DebugString());
                }
            }
        }
    }
}