using System.Collections.Generic;
using UnityEngine;

namespace Graph.Bezier {
    public class BezierStreetNode : IStreetNode {
        private readonly List<BezierStreetEdge> edges = new();

        public BezierStreetNode(Vector3 position) {
            Position = position;
        }

        internal IEnumerable<BezierStreetEdge> BezierEdges => edges;

        public float? EntranceAngle { get; internal set; }

        public Vector3? EntranceDirection =>
            EntranceAngle.HasValue ? Quaternion.Euler(0, 0, EntranceAngle.Value) * Vector3.right : null;

        public Vector3 Position { get; }

        public IEnumerable<IStreetEdge> Edges => edges;

        public int ConnectedEdgesCount => edges.Count;

        public int MaxConnectedEdges => 4;
        public float MinAngleBetweenEdges { get; }

        /// <summary>
        ///     Adds an edge to the node.
        /// </summary>
        /// <param name="edge"> The edge to add. </param>
        /// <returns> True if the edge was added, false if the maximum number of edges has been reached. </returns>
        internal bool AddEdge(BezierStreetEdge edge) {
            Debug.Assert(edge.NodeA == this || edge.NodeB == this);
            if (ConnectedEdgesCount >= MaxConnectedEdges) return false;
            edges.Add(edge);
            if (EntranceAngle.HasValue == false) {
                var edgeDir = edge.BezierNodeA == this ? edge.TangentA : edge.TangentB;
                EntranceAngle = Vector3.Angle(Vector3.right, edgeDir);
            }

            return true;
        }

        internal void RemoveEdge(BezierStreetEdge edge) {
            edges.Remove(edge);
        }
    }
}