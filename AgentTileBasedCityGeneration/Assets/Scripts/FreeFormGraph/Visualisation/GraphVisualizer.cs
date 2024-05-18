#nullable enable
using System;
using System.Collections.Generic;
using FreeFormGraph.World;
using UnityEngine;

namespace FreeFormGraph.Visualisation {
    
    public class GraphVisualizer : MonoBehaviour {
        
        private readonly Dictionary<IStreetEdge, GameObject> edgeVisualisations = new();
        
        private readonly List<IStreetEdge> edgesToAdd = new();
        private readonly List<IStreetEdge> edgesToRemove = new();
        private readonly object edgeLock = new();
        
        private void Start() {
            var world = FindObjectOfType<WorldGameObject>();
            var graph = world.StreetGraph;
            
            // Initialise the visualisation
            lock (edgeLock) {
                VisualizeWholeGraph(graph);
            }
            graph.EdgeAdded += OnEdgeAdded;
            graph.EdgeRemoved += OnEdgeRemoved;
        }

        private void Update() {
            if (edgesToAdd.Count == 0 && edgesToRemove.Count == 0) return;
            lock (edgeLock) {
                foreach (var edge in edgesToAdd) {
                    AddEdgeVisualisation(edge);
                }
                edgesToAdd.Clear();
                
                foreach (var edge in edgesToRemove) {
                    RemoveEdgeVisualisation(edge);
                }
                edgesToRemove.Clear();
            }
        }

        private void VisualizeWholeGraph(IStreetGraph graph) {
            foreach (var edge in graph.Edges) {
                AddEdgeVisualisation(edge);
            }
        }

        private void AddEdgeVisualisation(IStreetEdge edge) {
            var edgeGameObject = new GameObject($"Edge {edge}");
            edgeGameObject.transform.SetParent(transform);
            var meshFilter = edgeGameObject.AddComponent<MeshFilter>();
            var meshRenderer = edgeGameObject.AddComponent<MeshRenderer>();
            // create a mesh for the edge
            var mesh = new Mesh();
            meshFilter.mesh = mesh;
            meshRenderer.material = new Material(Shader.Find("Standard"));
            edgeVisualisations[edge] = edgeGameObject;

            var points = edge.SplitIntoEvenlySpacedPoints(out var tangents);
            var verticesLeft = new Vector3[points.Length];
            var verticesRight = new Vector3[points.Length];
            for (var i = 0; i < points.Length; i++) {
                var point = points[i];
                var tangent = tangents[i];
                var right = Vector3.Cross(tangent, Vector3.forward).normalized * (edge.StreetWidth / 2f) / Constants.METERS_PER_UNIT;
                verticesLeft[i] = point + right; // TODO maybe this should be -right?
                verticesRight[i] = point - right;
            }

            var verticesList = new List<Vector3>();
            var trianglesList = new List<int>();
            for (var i = 0; i < points.Length - 1; i++) {
                var p1 = verticesLeft[i];
                var p2 = verticesRight[i];
                var p3 = verticesLeft[i + 1];
                var p4 = verticesRight[i + 1];
            
                var offset = 4 * i;
                var t1 = offset + 0;
                var t2 = offset + 3;
                var t3 = offset + 2;
                var t4 = offset + 3;
                var t5 = offset + 0;
                var t6 = offset + 1;
                
                verticesList.AddRange(new[] {p1, p2, p3, p4});
                trianglesList.AddRange(new[] {t1, t2, t3, t4, t5, t6});
            }
            mesh.SetVertices(verticesList.ToArray());
            mesh.SetTriangles(trianglesList.ToArray(), 0);
            
        }
        
        private void RemoveEdgeVisualisation(IStreetEdge edge) {
            if (!edgeVisualisations.TryGetValue(edge, out var edgeGameObject)) {
                Debug.LogWarning($"Edge {edge} not found in visualisations");
                return;
            }

            Destroy(edgeGameObject);
            edgeVisualisations.Remove(edge);
        }
        
        private void OnEdgeAdded(object sender, EdgeEventArgs e) {
            lock (edgeLock) {
                edgesToAdd.Add(e.Edge);
            }
        }
        
        private void OnEdgeRemoved(object sender, EdgeEventArgs e) {
            lock (edgeLock) {
                edgesToRemove.Add(e.Edge);
            }
        }
        
    }
    
}