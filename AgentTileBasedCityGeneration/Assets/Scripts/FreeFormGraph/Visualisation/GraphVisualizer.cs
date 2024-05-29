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
        private IWorld world;
        private ITerrainGenerator terrainGenerator;

        /// <summary>
        /// Road thickness describes how thick the road in world units is.
        /// The thickness will grow downwards: if the road height is set to 50.0,
        /// with a RoadThickness of 0.5f, the lowest point of the road will be at 45.5, the
        /// highest at 50.0.
        /// </summary>
        [SerializeField]
        public float RoadThickness = 0.5f;
        
        private void Start() {
            world = FindObjectOfType<WorldGameObject>();
            var graph = world.StreetGraph;
            terrainGenerator = FindObjectOfType<Terrain3DGameObject>() as ITerrainGenerator;
            terrainGenerator.Render(world);
            
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
            var verticesList = new Vector3[points.Length*4];

            var firstPointHeight = world.GetHeightAt(points[0].x, points[0].y);
            var lastPointHeight = world.GetHeightAt(points[^1].x, points[^1].y);

            for (var i = 0; i < points.Length; i++) {
                var point = points[i];
                point.z = point.y;
                point.y = Mathf.Lerp(firstPointHeight, lastPointHeight, (float)i/(float)(points.Length-1));
                var tangent = tangents[i];
                tangent.z = tangent.y;
                tangent.y = 0;
                var right = Vector3.Cross(tangent, Vector3.down).normalized * (edge.StreetWidth / 2f) / Constants.METERS_PER_UNIT;

                //vertices for top surface, "top plane"
                verticesList[i] = point + right; //top plane right vertex
                verticesList[points.Length * 2 - 1 - i] = point - right; //top plane left vertex

                //vertices for side surfaces, "bottom plane"
                verticesList[i + points.Length * 2] = point + right - Vector3.up * RoadThickness; //bottom plane right vertex
                verticesList[points.Length * 4 - 1 - i] = point - right - Vector3.up * RoadThickness; //bottom plane left vertex
            }

            /*
                #top triangles: (points.Length - 1) * 2
                #left triangles: (points.Length - 1) * 2
                #right triangles: (points.Length - 1) * 2
                front and backface triangles: 2 + 2
            */
            var triangleList = new int[((points.Length - 1) * 2 * 2 * 2 + 4) * 3]; //sorry
            var triangleIdx = 0;

            //triangles for top surface
            var offset = 2 * points.Length - 1;
            for (var i = 0; i < points.Length - 1; i++) {
                var t1 = i;
                var t2 = i + offset - 1;
                var t3 = i + 1;
                var t4 = i;
                var t5 = i + offset;
                var t6 = i + offset - 1;
                
                triangleList[triangleIdx++] = t1;
                triangleList[triangleIdx++] = t2;
                triangleList[triangleIdx++] = t3;
                triangleList[triangleIdx++] = t4;
                triangleList[triangleIdx++] = t5;
                triangleList[triangleIdx++] = t6;
                offset -= 2;
            }

            int numTopVertices = points.Length * 2;            
            //triangles for thickness surface
            offset = numTopVertices;
            for (var i = 0; i < numTopVertices; i++) {
                var t1 = i;
                var t2 = ((i + 1) % numTopVertices) + offset;
                var t3 = i + offset;
                var t4 = i;
                var t5 = (i + 1) % numTopVertices;
                var t6 = ((i + 1) % numTopVertices) + offset;
                triangleList[triangleIdx++] = t1;
                triangleList[triangleIdx++] = t2;
                triangleList[triangleIdx++] = t3;
                triangleList[triangleIdx++] = t4;
                triangleList[triangleIdx++] = t5;
                triangleList[triangleIdx++] = t6;
            }

            mesh.SetVertices(verticesList);
            mesh.SetTriangles(triangleList, 0);
            EmbedIntoTerrain(mesh, numTopVertices);
            
        }

        /// <summary>
        /// Embed a mesh into the terrain by lowering/heightening the vertecies in the terrain.
        /// Consider this example:
        ///
        /// x------x
        /// |......|
        /// |......|
        /// |...y--|--y
        /// |......|
        /// x------x
        /// 
        /// x marks the terrain verticies
        /// y marks the mesh vertices
        /// The left y vertex lies inside a "quad" of the terrain. This quad is made out of
        /// two triangles. As a result, all 4 terrain vertices will be adjusted to the y
        /// vertex height. The same procedure will apply to the second y vertex.
        /// 
        /// </summary>
        /// <param name="m">The mesh to be embedded into the terrain.</param>
        /// <param name="n">The first n vertices of this mesh will be used for embedding.</param>
        private void EmbedIntoTerrain(Mesh m, int n) {

            var updateHeights = new (float height, int x, int y)[n * 4];

            for(int i = 0; i < n; i++) {
                var vertex = m.vertices[i];
                var roundedX = Mathf.FloorToInt(vertex.x);
                var roundedZ = Mathf.FloorToInt(vertex.z);

                updateHeights[i*4] = (vertex.y - (RoadThickness + 0.1f), roundedX, roundedZ);
                updateHeights[i*4+1] = (vertex.y - (RoadThickness + 0.1f), roundedX+1, roundedZ);
                updateHeights[i*4+2] = (vertex.y - (RoadThickness + 0.1f), roundedX, roundedZ+1);
                updateHeights[i*4+3] = (vertex.y - (RoadThickness + 0.1f), roundedX+1, roundedZ+1);
            }

            terrainGenerator.SetHeightAt(world, updateHeights);
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