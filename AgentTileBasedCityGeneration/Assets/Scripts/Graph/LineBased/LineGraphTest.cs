using System.Linq;
using UnityEngine;
using Utils;

namespace Graph.LineBased {
    public class LineGraphTest : ITestable {
        public string Name => "Line Graph Test";
        
        public TestResult TestLineGraphInitialization() {
            var graph = new GameObject("TestLineGraphInitialization").AddComponent<LineGraph>();
            var result = TestResult.BuildResult("Line Graph Initialization",
                (graph.NodeCount == 0, "Line graph should be empty after initialization."),
                (graph.EdgeCount == 0, "Line graph should have no edges after initialization."));
            
            Object.DestroyImmediate(graph.gameObject);
            
            return result;
        }
        
        public TestResult TestAddingTwoNodesAndAnEdge() {
            var graph = new GameObject("TestAddingTwoNodesAndAnEdge").AddComponent<LineGraph>();
            
            var result = TestResult.BuildResult("Adding Two Nodes and an Edge",
                (graph.CreateUnconnectedNode(Vector3.zero, out var node1), "Node 1 should be created."),
                (graph.CreateEdge(node1, new Vector3(1, 0, 0), out var edge1, out var node2, out var isToNodeNew, out var isEdgeNew), "Edge 1 and Node 2 should be created."),
                (isToNodeNew, "Node 2 should be new."),
                (isEdgeNew, "Edge 1 should be new."),
                (graph.NodeCount == 2, "Line graph should have 2 nodes."),
                (graph.EdgeCount == 1, "Line graph should have 1 edge."),
                (node1.Edges.Contains(edge1), "Node 1 should have edge 1."),
                (node2.Edges.Contains(edge1), "Node 2 should have edge 1."),
                (edge1.NodeA == node1 || edge1.NodeB == node1, "Edge 1 should be connected to Node 1."),
                (edge1.NodeA == node2 || edge1.NodeB == node2, "Edge 1 should be connected to Node 2."),
                (edge1.NodeA != edge1.NodeB, "Edge 1 should connect two different nodes."),
                (edge1.NodeA == node1 && edge1.NodeB == node2 || edge1.NodeA == node2 && edge1.NodeB == node1, "Edge 1 should connect Node 1 and Node 2."),
                (Mathf.Approximately(edge1.Length(), 1), "Edge 1 should be of length 1."));
            
            Object.DestroyImmediate(graph.gameObject);
            
            return result;
        }
        
        
    }
}