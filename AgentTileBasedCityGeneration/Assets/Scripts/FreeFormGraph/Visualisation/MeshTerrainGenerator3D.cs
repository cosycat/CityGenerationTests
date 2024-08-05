using UnityEngine;
using FreeFormGraph.World;
using System.Collections.Generic;

namespace FreeFormGraph.Visualisation {
    public class MeshTerrainGenerator3D : Terrain3DGameObject {
        [SerializeField] public Material material;

        /// <summary>
        /// Gradient use for color map according to elevation data.
        /// </summary>
        [SerializeField] public Gradient gradient;

        /// <summary>
        /// Terrain will be made out of multiple meshes, where each
        /// mesh will have a maxium size of meshSize*meshSize.
        /// It will be smaller on the edges if the world is not a multiple of 
        /// meshSize
        /// </summary>
        [SerializeField] public int meshSize = 200;

        /// <summary>
        /// Defines the maximum height in world unity at which trees can be placed.
        /// </summary>
        [SerializeField] public float treeCutoffHeight = 70;

        /// <summary>
        /// Defines the maximum number of objects (trees, stones) which can be placed in the world.
        /// </summary>
        [SerializeField] public int maxObjectCount = 50000;

        /// <summary>
        /// Frequency adjustment for the noise used in object placement.
        /// </summary>
        [SerializeField] public float forestNoiseFrequency = 1;

        /// <summary>
        /// Defines which step is used to iterate over the world and place objects.
        /// Using a step size of 1 means at every position in the world, object placement
        /// will be evaluated.
        /// </summary>
        [SerializeField] public int forestStepSize = 2;

        private int objectCount = 0;

        [SerializeField] public List<GameObject> treePrefabs;

        [SerializeField] public List<GameObject> rockPrefabs;

        //Needed for road placement, when placing new roads we need to remove potential objects which
        //are in the way.
        private Dictionary<(int x, int y), GameObject> objectDetailMap = new();

        private readonly List<Mesh> meshes = new();

        //For each corresponding entry in meshes, this list saves the size of the mesh in vertex count.
        private readonly List<(int xSize, int zSize)> meshIndexSizes = new();

        private (int meshIndexX, int meshIndexZ) calculateMeshIndex(int x, int z) {
            var outX = 0;
            var outZ = 0;

            for (var i = 0; x > i + meshSize - 1; i += meshSize - 1) outX++;
            for (var i = 0; z > i + meshSize - 1; i += meshSize - 1) outZ++;
            return (outX, outZ);
        }

        //private int calcIndexInMesh(int pos, int dimSize)

        public override void SetHeightAt(IWorld world, IList<(float height, int x, int z)> heights) {
            var changedMeshes = new Dictionary<int, Vector3[]>();

            foreach (var (height, x, z) in heights) {
                Debug.Assert(x >= 0);
                Debug.Assert(z >= 0);
                Debug.Assert(x < world.Width);
                Debug.Assert(z < world.Height);

                if (objectDetailMap.ContainsKey((x, z))) {
                    Destroy(objectDetailMap[(x, z)]);
                    objectDetailMap.Remove((x, z));
                }

                var terrainWidth = world.Width;
                var (meshIndexX, meshIndexZ) = calculateMeshIndex(x, z);
                var numMeshesXDir = Mathf.CeilToInt((float)terrainWidth / (float)meshSize);
                var meshIndex = meshIndexX + meshIndexZ * numMeshesXDir;
                var (meshSizeX, meshSizeZ) = meshIndexSizes[meshIndex];

                var indexXInsideMesh = (x % meshSize + meshIndexX) % meshSize;
                var indexzInsideMesh = (z % meshSize + meshIndexZ) % meshSize;

                var indexInMesh = indexXInsideMesh + indexzInsideMesh * meshSizeX;
                Debug.Assert(indexInMesh <= meshSizeX * meshSizeZ - 1,
                    $"{indexInMesh} {x} {z} {meshIndexX} {meshIndexZ} {meshSizeX} {meshSizeZ}");

                var changeMeshes = new List<(int meshIndex, int vertexIndex)>();
                changeMeshes.Add((meshIndex, indexInMesh));

                //if point in world lies on a mesh boundary, multiple edges have to be updated
                //in the worst case 4 meshes!
                //top edge
                if (indexzInsideMesh == meshSizeZ - 1 && z < world.Height - 1)
                    changeMeshes.Add((meshIndex + numMeshesXDir, indexXInsideMesh));
                //right edge
                if (indexXInsideMesh == meshSizeX - 1 && x < world.Width - 1)
                    changeMeshes.Add((meshIndex + 1, indexzInsideMesh * meshIndexSizes[meshIndex + 1].xSize));
                //top right corner
                if (indexXInsideMesh == meshSizeX - 1
                    && indexzInsideMesh == meshSizeZ - 1
                    && x < world.Width - 1
                    && z < world.Height - 1)
                    changeMeshes.Add((meshIndex + numMeshesXDir + 1, 0));

                foreach (var (meshIdx, idxInMesh) in changeMeshes) {
                    if (!changedMeshes.ContainsKey(meshIdx)) changedMeshes[meshIdx] = meshes[meshIdx].vertices;
                    changedMeshes[meshIdx][idxInMesh].y = height;
                }
            }

            foreach (var (meshIdx, vertices) in changedMeshes) {
                meshes[meshIdx].SetVertices(vertices);
                meshes[meshIdx].RecalculateNormals();
            }
        }

        public override void Render(IWorld World) {
            for (var y = 0; y < World.Height; y += meshSize - 1) {
                for (var x = 0; x < World.Width; x += meshSize - 1)
                    RenderHeightmap(World, x, x + meshSize - 1, y, y + meshSize - 1);
            }

            AddForests(World);
        }

        private void AddForests(IWorld World) {
            var freq = forestNoiseFrequency;
            var step = forestStepSize;

            void placeObjectAt(int x, int y, List<GameObject> prefabs, float elevation, string name) {
                if (objectDetailMap.ContainsKey((x, y))) return;
                var treeIndex = x * y % prefabs.Count;
                var g = Instantiate(prefabs[treeIndex]);
                g.transform.position = new Vector3(x, elevation, y);
                g.transform.Rotate(new Vector3(0, x * y, 0));
                g.transform.SetParent(transform);
                objectDetailMap[(x, y)] = g;
            }

            //var maxTreeElevation = Mathf.Lerp(World.MinHeight, World.MaxHeight, treeCutoffPercentageHeight);
            for (var y = 0; y < World.Height; y += step) {
                for (var x = 0; x < World.Width; x += step) {
                    if (objectCount > maxObjectCount) return;

                    var elevation = World.GetHeightAt(x, y);
                    if (elevation > treeCutoffHeight) continue;
                    //var noiseValue = Mathf.PerlinNoise((float)x/freq, (float)y/freq);
                    var noiseValue = Mathf.Cos((float)x / freq) + Mathf.Sin((float)y / freq);
                    if (noiseValue > 0.5f && noiseValue < 0.9f) {
                        placeObjectAt(x, y, treePrefabs, elevation, $"Tree {objectCount}");
                    }
                    else if (noiseValue > 0.9f && noiseValue < 1.6f) {
                        objectCount++;
                        placeObjectAt(x, y, rockPrefabs, elevation, $"Rock {objectCount}");
                    }
                }
            }
        }

        private void RenderHeightmap(IWorld World, int startX, int endX, int startY, int endY) {
            Debug.Assert(startX < endX);
            Debug.Assert(startY < endY);
            endX = (int)Mathf.Min(endX, World.Width - 1);
            endY = (int)Mathf.Min(endY, World.Height - 1);
            var width = endX - startX + 1;
            var height = endY - startY + 1;
            var minHeight = World.MinHeight;
            var maxHeight = World.MaxHeight;

            var mesh = new Mesh();
            var vertices = new Vector3[width * height];
            var colors = new Color[width * height];
            var triangles = new int[width * height * 6];
            var triangleIndex = 0;

            var worldX = startX;
            var worldY = startY;
            for (var meshY = 0; meshY < height; meshY++) {
                for (var meshX = 0; meshX < width; meshX++) {
                    var index = meshY * width + meshX;
                    var heightValue = World.GetHeightAt(worldX, worldY);
                    vertices[index] = new Vector3(worldX, heightValue, worldY);
                    colors[index] = gradient.Evaluate(Mathf.InverseLerp(minHeight, maxHeight, heightValue));

                    // Add triangles if not at the border
                    if (meshX < width - 1 && meshY < height - 1) {
                        triangles[triangleIndex++] = index;
                        triangles[triangleIndex++] = index + width;
                        triangles[triangleIndex++] = index + width + 1;


                        triangles[triangleIndex++] = index;
                        triangles[triangleIndex++] = index + width + 1;
                        triangles[triangleIndex++] = index + 1;
                    }

                    worldX++;
                }

                worldX = startX;
                worldY++;
            }

            meshIndexSizes.Add((width, height));

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            var terrainObject = new GameObject($"3d Terrain {startX} {startY}");
            terrainObject.transform.position = Vector3.zero;
            terrainObject.AddComponent<MeshFilter>().mesh = mesh;
            terrainObject.AddComponent<MeshRenderer>().material = material;
            terrainObject.transform.SetParent(gameObject.transform);

            meshes.Add(mesh);
        }
    }
}