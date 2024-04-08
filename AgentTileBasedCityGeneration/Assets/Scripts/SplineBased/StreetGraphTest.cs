// using System;
// using System.Linq;
// using System.Collections.Generic;
// using JetBrains.Annotations;
// using UnityEngine;
// using UnityEngine.Splines;
// using UnityEngine.Serialization;
//
// namespace SplineBased {
//     
//     public class StreetGraphTest : MonoBehaviour {
//         [SerializeField] private bool allowMouseCreation = true;
//         
//         private StreetGraph _graph;
//         
//         private string _x = "0";
//         private string _y = "0";
//         private StreetNode _prevCreatedNode;
//         private float X => float.TryParse(_x, out var x) ? x : 0;
//         private float Y => float.TryParse(_y, out var y) ? y : 0;
//
//         private bool drawGrid = true;
//         private bool drawCurveBboxes = false;
//         private bool drawIntersectionLines = true;
//         private bool drawLabelsEdges = true;
//         private bool drawLabelsNodes = true;
//
//         private void Start() {
//             _graph = FindObjectOfType<StreetGraph>();
//             if (_graph == null) {
//                 Debug.LogError("No StreetGraph found in Scene.");
//                 Destroy(this);
//             }
//             _prevCreatedNode = _graph.Nodes[0];
//         }
//
//         private void Update() {
//             if (allowMouseCreation) {
//                 CheckMouseCreation();
//             }
//         }
//         
//         [CanBeNull] private StreetNode _dragStartNode;
//         
//         private void CheckMouseCreation() {
//             const float dragThreshold = 0.3f;
//             var mousePosWorld = MouseWorldPos;
//             if (Input.GetMouseButtonDown(0)) {
//                 _dragStartNode = _graph.FindClosestNode(mousePosWorld);
//                 if (_dragStartNode != null && Vector3.Distance(_dragStartNode.Position, mousePosWorld) < dragThreshold) {
//                     Debug.Log($"StreetGraphTest - Dragging from {_dragStartNode}");
//                 } else {
//                     Debug.Log($"No node found at {mousePosWorld}.");
//                     _dragStartNode = null;
//                 }
//             } else if (Input.GetMouseButtonUp(0)) {
//                 if (_dragStartNode == null) {
//                     return;
//                 }
//                 var existingEndNode = _graph.FindClosestNode(mousePosWorld);
//                 if (existingEndNode != null && Vector3.Distance(existingEndNode.Position, mousePosWorld) < dragThreshold) {
//                     Debug.Log($"StreetGraphTest - Dragging to {existingEndNode}");
//                     if (!_graph.AddNewSegment(_dragStartNode, existingEndNode, out _)) {
//                         Debug.LogError("Failed to create new segment.");
//                     }
//                 }
//                 else {
//                     if (!_graph.AddNewSegment(_dragStartNode, mousePosWorld, out _, out _prevCreatedNode)) {
//                         Debug.LogError("Failed to create new node.");
//                     }
//                 }
//
//                 _dragStartNode = null;
//             }
//         }
//
//         private static Vector2 MouseWorldPos =>
//             Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
//
//         private void CreateNode(Func<StreetGraph, Vector3, StreetNode> getNode) {
//             if (!float.TryParse(_x, out var x) || !float.TryParse(_y, out var y)) {
//                 Debug.LogError("StreetGraphTest::CreateNode - Invalid Coordinates");
//                 return;
//             }
//             var fromNode = getNode(_graph, new Vector3(x, y));
//             Debug.Log($"StreetGraphTest::CreateNode - Creating new node at {x}, {y} connected to {fromNode}");
//             if (!_graph.AddNewSegment(fromNode, new Vector3(x, y), out _, out _prevCreatedNode)) {
//                 Debug.LogError("StreetGraphTest::CreateNode - Failed to create new node.");
//                 return;
//             }
//             Debug.Log($"StreetGraphTest::CreateNode - Created new node at {x}, {y} from {fromNode} connected to {_prevCreatedNode}");
//         }
//         
//         private void CreateNodeClosest() {
//             CreateNode((g, newNodePos) => g.FindClosestNode(newNodePos));
//         }
//         
//         private void CreateNodeRandom() {
//             CreateNode((g, newNodePos) => g.Nodes[UnityEngine.Random.Range(0, g.Nodes.Count)]);
//         }
//         
//         private void CreateNodePrevious() {
//             CreateNode((g, newNodePos) => _prevCreatedNode);
//         }
//
//         private void OnGUI() {
//             // Contents:
//             // - A textfield to enter coordinates for a new node
//             // - Multiple buttons to create a new node at the entered coordinates:
//             //   - "Create Node Random" creates a new node connected to a random existing node
//             //   - "Create Node Closest" creates a new node connected to the closest existing node
//             //   - "Create Node Previous" creates a new node connected to the previously created node
//             
//             GUILayout.BeginArea(new Rect(10, 10, 200, 600));
//             GUILayout.Label($"Number of nodes: {_graph.Nodes.Count}");
//             GUILayout.Label($"Number of edges: {_graph.Edges.Count}");
//             GUILayout.Label("Create New Node");
//             GUILayout.BeginHorizontal();
//             GUILayout.Label("X:");
//             _x = GUILayout.TextField(_x);
//             GUILayout.Label("Y:");
//             _y = GUILayout.TextField(_y);
//             GUILayout.EndHorizontal();
//             if (GUILayout.Button("Connected to Random")) {
//                 CreateNodeRandom();
//             }
//             if (GUILayout.Button("Connected to Closest")) {
//                 CreateNodeClosest();
//             }
//             if (GUILayout.Button("Connected to Previous")) {
//                 CreateNodePrevious();
//             }
//             if (GUILayout.Button($"Draw grid: ({drawGrid})")) {
//                 drawGrid = !drawGrid;
//             }
//             if (GUILayout.Button($"Bezier B-Boxes: ({drawCurveBboxes})")) {
//                 drawCurveBboxes = !drawCurveBboxes;
//             }
//             if (GUILayout.Button($"Intersection Lines: ({drawIntersectionLines})")) {
//                 drawIntersectionLines = !drawIntersectionLines;
//             }
//             GUILayout.Label("Labels:");
//             GUILayout.BeginHorizontal();
//             if (GUILayout.Button($"Nodes: ({drawLabelsNodes})")) {
//                 drawLabelsNodes = !drawLabelsNodes;
//             }
//             if (GUILayout.Button($"Edges: ({drawLabelsEdges})")) {
//                 drawLabelsEdges = !drawLabelsEdges;
//             }
//             GUILayout.EndHorizontal();
//             if (allowMouseCreation) {
//                 GUILayout.Label("Drag from one node to create a new connection or node.");
//             }
//             GUILayout.EndArea();
//         }
//
//         private void OnDrawGizmos() {
//             if (_graph == null) {
//                 return;
//             }
//             foreach (var node in _graph.Nodes) {
//                 Gizmos.color = node.MaxSegmentCountReached ? Color.red : Color.green;
//                 Gizmos.DrawSphere(node.Position, 0.1f);
//             }
//             var edgeDrawingColors = new List<Color>() {Color.blue, Color.cyan};
//             var colorIndex = 0;
//
//             foreach (var edge in _graph.Edges) {
//                 Gizmos.color = edgeDrawingColors[colorIndex];
//                 colorIndex = (colorIndex + 1) % edgeDrawingColors.Count;
//                 Gizmos.DrawLine(edge.From.Position, edge.To.Position);
//             }
//             Gizmos.color = Color.green;
//             Gizmos.DrawSphere(new Vector3(X, Y), 0.1f);
//             if (Input.GetMouseButton(0) && _dragStartNode != null) {
//                 Gizmos.DrawLine(_dragStartNode.Position, MouseWorldPos);
//             }
//
//             if (drawIntersectionLines) {
//                 foreach ((var pos, var dir) in Intersections._dbg_curveSteps) {
//                     Gizmos.color = Color.grey;
//                     Gizmos.DrawRay(pos, Vector3.up * dir);
//                 }
//
//                 foreach (var intersection in Intersections._dbg_splineIntersectionPoints) {
//                     Gizmos.color = Color.magenta;
//                     Gizmos.DrawRay(intersection, Vector3.up);
//                 }
//             }
//
//             if(drawGrid) {
//                 Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
//                 for(int i = -100; i < 100; i++) {
//                     Gizmos.DrawRay(new Vector3(i, -100), Vector3.up * 200);
//                 }
//                 for(int i = -100; i < 100; i++) {
//                     Gizmos.DrawRay(new Vector3(-100, i), Vector3.right * 200);
//                 }
//             }
//
//             if(drawCurveBboxes) {
//                 foreach(var s in _graph.Nodes.SelectMany(n => n.CorrespondingSplines).Distinct()) {
//                     for(var i = 0; i < SplineUtility.GetCurveCount(s); i++) {
//                         var targetCurve = s.GetCurve(i);
//                         var bounds = Intersections.GetBoundsForCurve(targetCurve);
//                         Gizmos.DrawRay(bounds.min, Vector3.right * (bounds.max.x - bounds.min.x));
//                         Gizmos.DrawRay(bounds.min, Vector3.up* (bounds.max.y - bounds.min.y));
//                         Gizmos.DrawRay(bounds.max, Vector3.left * (bounds.max.x - bounds.min.x));
//                         Gizmos.DrawRay(bounds.max, Vector3.down* (bounds.max.y - bounds.min.y));
//                     }
//                 }
//             }
//             
//             if (drawLabelsEdges) {
//                 // UnityEditor.Handles.color = Color.yellow;
//                 foreach (var edge in _graph.Edges) {
//                     var (fromIndex, toIndex) = edge.GetIndices();
//                     UnityEditor.Handles.Label((edge.From.Position + edge.To.Position) / 2,
//                         $"EDGE\n{Vector2.one * edge.From.Position}({fromIndex}) -> {Vector2.one * edge.To.Position}({toIndex})");
//                     // drawString($"EDGE\n{Vector2.one * edge.From.Position}({fromIndex}) -> {Vector2.one * edge.To.Position}({toIndex})", (edge.From.Position + edge.To.Position) / 2, Color.yellow, Vector2.zero);
//                 }
//             }
//             if (drawLabelsNodes) {
//                 // UnityEditor.Handles.color = Color.white;
//                 foreach (var node in _graph.Nodes) {
//                     var connectedSegmentsCount = node.ConnectedSegments.Count;
//                     var nodeSplineIndices = node.SplineIndices;
//                     UnityEditor.Handles.Label(node.Position, $"NODE\n{Vector2.one * node.Position} connections: {connectedSegmentsCount}\nsplineIndices: {string.Join(", ", nodeSplineIndices.Select(t => $"{t.index}"))}");
//                     // drawString($"NODE\n{Vector2.one * node.Position} connections: {connectedSegmentsCount}\nsplineIndices: {string.Join(", ", nodeSplineIndices.Select(t => $"{t.index}"))}", node.Position, Color.white, Vector2.zero);
//                 }
//             }
//         }
//         
//         
// //         /// <summary>
// //         /// From https://gist.github.com/Arakade/9dd844c2f9c10e97e3d0?permalink_comment_id=4043513#gistcomment-4043513
// //         /// </summary>
// //         /// <param name="text"> The text to draw </param>
// //         /// <param name="worldPosition"> The world position to draw the text at </param>
// //         /// <param name="textColor"> The color of the text </param>
// //         /// <param name="anchor"> pixel-precise positioning with anchor support (e.g. Vector2.zero = center, Vector2.one = anchor to bottom-left corner, -Vector2.one = anchor to top-right corner, etc.) </param>
// //         /// <param name="textSize"> The size of the text </param>
// //         static public void drawString(string text, Vector3 worldPosition, Color textColor, Vector2 anchor, float textSize = 15f)
// //         {
// // #if UNITY_EDITOR
// //             var view = UnityEditor.SceneView.currentDrawingSceneView;
// //             if (!view)
// //                 return;
// //             Vector3 screenPosition = view.camera.WorldToScreenPoint(worldPosition);
// //             if (screenPosition.y < 0 || screenPosition.y > view.camera.pixelHeight || screenPosition.x < 0 || screenPosition.x > view.camera.pixelWidth || screenPosition.z < 0)
// //                 return;
// //             var pixelRatio = UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.right).x - UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.zero).x;
// //             UnityEditor.Handles.BeginGUI();
// //             var style = new GUIStyle(GUI.skin.label)
// //             {
// //                 fontSize = (int)textSize,
// //                 normal = new GUIStyleState() { textColor = textColor }
// //             };
// //             Vector2 size = style.CalcSize(new GUIContent(text)) * pixelRatio;
// //             var alignedPosition =
// //                 ((Vector2)screenPosition +
// //                  size * ((anchor + Vector2.left + Vector2.up) / 2f)) * (Vector2.right + Vector2.down) +
// //                 Vector2.up * view.camera.pixelHeight;
// //             GUI.Label(new Rect(alignedPosition / pixelRatio, size / pixelRatio), text, style);
// //             UnityEditor.Handles.EndGUI();
// // #endif
// //         }
//     }
//     
//     
// }