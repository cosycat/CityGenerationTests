using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph.LineBased {
    public static class LineGraphTestCreator {
        public static LineGraph GenerateSquareGraph() {
            var graph = new GameObject("TestGraph").AddComponent<LineGraph>();
            graph.CreateUnconnectedNode(new Vector3(0, 0, 0), out var n0);

            graph.CreateEdge(n0, new Vector3(100, 0, 0), out var e0, out var n1, out var isToNodeNew,
                out var isEdgeNew);
            graph.CreateEdge(n1, new Vector3(100, 100, 0), out var e1, out var n2, out var isToNodeNew1,
                out var isEdgeNew1);
            graph.CreateEdge(n2, new Vector3(0, 100, 0), out var e2, out var n3, out var isToNodeNew2,
                out var isEdgeNew2);
            graph.CreateEdge(n3, new Vector3(0, 0, 0), out var e3, out var n02, out var isToNodeNew3,
                out var isEdgeNew3);

            Debug.Assert(n0 == n02, "n0 == n02");
            Debug.Assert(!isToNodeNew3);
            Debug.Assert(isEdgeNew3);

            return graph;
        }

        public static LineGraph GenerateHShapedGraph() {
            var graph = new GameObject("TestGraph").AddComponent<LineGraph>();
            graph.CreateUnconnectedNode(new Vector3(0, 0, 0), out var nLeftBottom);

            // Left Side
            graph.CreateEdge(nLeftBottom, new Vector3(0, 100, 0), out var e0, out var nLeftMiddle, out var isToNodeNew,
                out var isEdgeNew);
            graph.CreateEdge(nLeftMiddle, new Vector3(0, 200, 0), out var e1, out var nLeftTop, out var isToNodeNew1,
                out var isEdgeNew1);

            // Middle
            graph.CreateEdge(nLeftMiddle, new Vector3(100, 100, 0), out var e2, out var nRightMiddle,
                out var isToNodeNew2, out var isEdgeNew2);

            // Right Side
            graph.CreateEdge(nRightMiddle, new Vector3(100, 200, 0), out var e3, out var nRightTop,
                out var isToNodeNew3, out var isEdgeNew3);
            graph.CreateEdge(nRightMiddle, new Vector3(100, 0, 0), out var e4, out var nRightBottom,
                out var isToNodeNew4, out var isEdgeNew4);

            return graph;
        }
    }
}