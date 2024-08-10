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

        public TestResult TestAddingTwoEdgesWithAnIntersection() {
            var graph = new GameObject("TestAddingTwoEdgesWithAnIntersection").AddComponent<LineGraph>();

            var result = TestResult.BuildResult("Adding Two Edges with an Intersection",
                (graph.CreateUnconnectedNode(new Vector3(-1, 0, 0), out var nodeLeft), "Node Left should be created."),
                (graph.CreateEdge(nodeLeft, new Vector3(1, 0, 0), out var edgeLeftRight, out var nodeRight, out var isNodeRightNew, out var isEdgeLeftRightNew),
                    "Edge Left-Right and Node Right should be created."),
                (isNodeRightNew, "Node Right should be new."),
                (isEdgeLeftRightNew, "Edge Left-Right should be new."),

                (graph.CreateEdge(nodeLeft, new Vector3(0, 1, 0), out var edgeLeftTop, out var nodeTop, out var isNodeTopNew, out var isEdgeLeftTopNew),
                    "Edge Left-Top and Node Top should be created."),
                (isNodeTopNew, "Node Top should be new."),
                (isEdgeLeftTopNew, "Edge Left-Top should be new."),

                (graph.CreateEdge(nodeTop, new Vector3(0, -1, 0), out var edgeTopDown, out var nodeDown, out var isNodeDownNew, out var isEdgeTopDownNew, false),
                    "Edge Top-Down and Node Down should be created."),
                (isNodeDownNew, "Node Down should be new."),
                (isEdgeTopDownNew, "Edge Top-Down should be new."),
                (graph.Nodes.Contains(nodeDown), "Node Down should be added to the graph."),
                (nodeDown.Position == new Vector3(0, 0, 0), "Node Down should be at position (0, 0, 0)."),
                (nodeDown.ConnectedEdgesCount == 3, "Node Down should be connected to 3 edges."),
                (graph.Edges.Contains(edgeTopDown), "Edge Top-Down should be added to the graph."),

                (graph.NodeCount == 4, $"Line graph should have 4 nodes but has {graph.NodeCount}."),
                (graph.EdgeCount == 4, $"Line graph should have 4 edges but has {graph.EdgeCount}."),
                (!graph.Edges.Contains(edgeLeftRight), "Edge Left-Right should not be in the graph anymore."),
                (graph.Edges.Contains(edgeLeftTop), "Edge Left-Top should still be in the graph."),
                (graph.Edges.Contains(edgeTopDown), "Edge Top-Down should still be in the graph."),
                (nodeLeft.Edges.Contains(edgeLeftTop), "Node Left should have Edge Left-Top."),
                (!nodeLeft.Edges.Contains(edgeLeftRight), "Node Left should not have Edge Left-Right anymore."),
                (nodeTop.Edges.Contains(edgeLeftTop), "Node Top should have Edge Left-Top."),
                (nodeTop.Edges.Contains(edgeTopDown), "Node Top should have Edge Top-Down."))
                
                .AppendErrorMessageIfFalse($"Nodes: {string.Join(", ", graph.Nodes.Select(n => n.ToString()).ToArray())}")
                .AppendErrorMessageIfFalse($"Edges: {string.Join(", ", graph.Edges.Select(e => e.ToString()).ToArray())}");

            
            Object.DestroyImmediate(graph.gameObject);
            
            return result;
        }
        
        
    }
}