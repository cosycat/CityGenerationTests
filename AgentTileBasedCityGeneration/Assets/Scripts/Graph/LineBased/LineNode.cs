#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Graph.LineBased {
    public class LineNode : IStreetNode {
        private readonly List<LineEdge> edges = new();

        public LineNode(Vector3 position, float minAngleBetweenEdges = Mathf.Deg2Rad * 30f, int maxConnectedEdges = 6) {
            Position = position;
            MinAngleBetweenEdges = minAngleBetweenEdges;
            MaxConnectedEdges = maxConnectedEdges;
        }

        public Vector3 Position { get; set; }

        public IEnumerable<IStreetEdge> Edges => edges;

        public int ConnectedEdgesCount => edges.Count;

        public int MaxConnectedEdges { get; set; }

        public float MinAngleBetweenEdges { get; }

        public bool IsMaxConnectedEdgesReached => ConnectedEdgesCount >= MaxConnectedEdges;

        public override string ToString() {
            return $"LineNode at {Position} with {ConnectedEdgesCount} edges.";
        }

        public void AddEdge(LineEdge e) {
            if (!edges.Contains(e)) edges.Add(e);
        }

        public void RemoveEdge(LineEdge e) {
            edges.Remove(e);
        }
    }
}