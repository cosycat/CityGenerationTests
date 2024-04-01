using System;
using UnityEngine;

namespace SplineBased {
    
    public class StreetGraphTest : MonoBehaviour {
        private StreetGraph _graph;
        
        private string _x = "0";
        private string _y = "0";
        private StreetNode _prevCreatedNode;
        private float X => float.TryParse(_x, out var x) ? x : 0;
        private float Y => float.TryParse(_y, out var y) ? y : 0;

        private void Start() {
            _graph = FindObjectOfType<StreetGraph>();
            if (_graph == null) {
                Debug.LogError("No StreetGraph found in Scene.");
                Destroy(this);
            }
            _prevCreatedNode = _graph.Nodes[0];
        }

        private void CreateNode(Func<StreetGraph, Vector3, StreetNode> getNode) {
            if (!float.TryParse(_x, out var x) || !float.TryParse(_y, out var y)) {
                Debug.LogError("Invalid Coordinates");
                return;
            }
            var fromNode = getNode(_graph, new Vector3(x, y));
            Debug.Log($"StreetGraphTest - Creating new node at {x}, {y} connected to {fromNode}");
            if (!_graph.AddNewSegment(fromNode, new Vector3(x, y), out _, out _prevCreatedNode)) {
                Debug.LogError("Failed to create new node.");
                return;
            }
            Debug.Log($"StreetGraphTest - Created new node at {x}, {y} from {fromNode} connected to {_prevCreatedNode}");
        }
        
        private void CreateNodeClosest() {
            CreateNode((g, newNodePos) => g.FindClosestNode(newNodePos));
        }
        
        private void CreateNodeRandom() {
            CreateNode((g, newNodePos) => g.Nodes[UnityEngine.Random.Range(0, g.Nodes.Count)]);
        }
        
        private void CreateNodePrevious() {
            CreateNode((g, newNodePos) => _prevCreatedNode);
        }

        private void OnGUI() {
            // Contents:
            // - A textfield to enter coordinates for a new node
            // - Multiple buttons to create a new node at the entered coordinates:
            //   - "Create Node Random" creates a new node connected to a random existing node
            //   - "Create Node Closest" creates a new node connected to the closest existing node
            //   - "Create Node Previous" creates a new node connected to the previously created node
            
            GUILayout.BeginArea(new Rect(10, 10, 200, 200));
            GUILayout.Label("Create New Node");
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:");
            _x = GUILayout.TextField(_x);
            GUILayout.Label("Y:");
            _y = GUILayout.TextField(_y);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Connected to Random")) {
                CreateNodeRandom();
            }
            if (GUILayout.Button("Connected to Closest")) {
                CreateNodeClosest();
            }
            if (GUILayout.Button("Connected to Previous")) {
                CreateNodePrevious();
            }
            GUILayout.EndArea();
        }

        private void OnDrawGizmos() {
            if (_graph == null) {
                return;
            }
            foreach (var node in _graph.Nodes) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(node.Position, 0.1f);
            }
            foreach (var edge in _graph.Edges) {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(edge.From.Position, edge.To.Position);
            }
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(X, Y), 0.1f);
        }
    }
}