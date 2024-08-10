using UnityEngine;

namespace Graph.LineBased {
    /// <summary>
    ///     Helper class to create graphs for testing purposes.
    /// </summary>
    public static class LineGraphTestCreator {
        public static LineGraph GenerateSquareGraph() {
            var graph = new GameObject("TestGraph").AddComponent<LineGraph>();
            graph.CreateUnconnectedNode(new Vector3(0, 0, 0), out var n0);

            graph.CreateEdge(n0, new Vector3(100, 0, 0), out var e0, out var n1, out var isToNodeNew,
                out var isEdgeNew, RoadType.Secondary);
            graph.CreateEdge(n1, new Vector3(100, 100, 0), out var e1, out var n2, out var isToNodeNew1,
                out var isEdgeNew1, RoadType.Secondary);
            graph.CreateEdge(n2, new Vector3(0, 100, 0), out var e2, out var n3, out var isToNodeNew2,
                out var isEdgeNew2, RoadType.Secondary);
            graph.CreateEdge(n3, new Vector3(0, 0, 0), out var e3, out var n02, out var isToNodeNew3,
                out var isEdgeNew3, RoadType.Secondary);

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
                out var isEdgeNew, RoadType.Secondary);
            graph.CreateEdge(nLeftMiddle, new Vector3(0, 200, 0), out var e1, out var nLeftTop, out var isToNodeNew1,
                out var isEdgeNew1, RoadType.Secondary);

            // Middle
            graph.CreateEdge(nLeftMiddle, new Vector3(100, 100, 0), out var e2, out var nRightMiddle,
                out var isToNodeNew2, out var isEdgeNew2, RoadType.Secondary);

            // Right Side
            graph.CreateEdge(nRightMiddle, new Vector3(100, 200, 0), out var e3, out var nRightTop,
                out var isToNodeNew3, out var isEdgeNew3, RoadType.Secondary);
            graph.CreateEdge(nRightMiddle, new Vector3(100, 0, 0), out var e4, out var nRightBottom,
                out var isToNodeNew4, out var isEdgeNew4, RoadType.Secondary);

            return graph;
        }
    }
}