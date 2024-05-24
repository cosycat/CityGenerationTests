using UnityEngine;
using FreeFormGraph.LineBased;
using System.Collections.Generic;

namespace FreeFormGraph.World {
    public interface ITerrainGenerator {
        
        /// <summary>
        /// Setting a certain height in the world based on world units (see Constants.METERS_PER_UNIT).
        /// Updating is costly (vertices are reset, normals recalculated), so try to batch things if possible.
        /// This is also the reason why this method expects a list.
        /// </summary>
        void SetHeightAt(IWorld world, List<(float height, int x, int z)> heights);
        /// <summary>
        /// Create terrain out of 3D meshes.
        /// </summary>
        void Render(IWorld World);
    }

    public class MeshTerrainGenerator: MonoBehaviour, ITerrainGenerator {
        
        [SerializeField]
        public Material material;
        private bool didCreateMesh = false;

        [SerializeField]
        public Gradient gradient;

        [SerializeField]
        public int meshSize = 200;

        private readonly List<Mesh> meshes = new();
        private readonly List<(int xSize, int zSize)> meshIndexSizes = new();

        public void SetHeightAt(IWorld world, List<(float height, int x, int z)> heights) {
            
            var changedMeshes = new Dictionary<int, Vector3[]>();
            
            foreach((var height, var x, var z) in heights) {
                var terrainWidth = world.Width;
                var meshIndexX = Mathf.FloorToInt(x/meshSize);
                var meshIndexZ = Mathf.FloorToInt(z/meshSize);
                var numMeshesXDir = Mathf.CeilToInt((float)terrainWidth/(float)meshSize);
                var meshIndex = meshIndexX + meshIndexZ * numMeshesXDir;

                var indexXInsideMesh = x % meshSize + meshIndexX;
                var indexzInsideMesh = z % meshSize + meshIndexZ;

                var indexInMesh = indexXInsideMesh + indexzInsideMesh * meshSize;

                var changeMeshes = new List<(int meshIndex, int vertexIndex)>();
                changeMeshes.Add((meshIndex, indexInMesh));

                //if point in world lies on a mesh boundary, multiple edges have to be updated
                //in the worst case 4 meshes!
                //top edge
                if(indexzInsideMesh == meshSize - 1){
                    changeMeshes.Add((meshIndex + numMeshesXDir, indexXInsideMesh));
                }
                //right edge
                if(indexXInsideMesh == meshSize - 1){
                    changeMeshes.Add((meshIndex + 1, indexzInsideMesh * meshSize));
                }
                //top right corner
                if(indexXInsideMesh == meshSize - 1 && indexzInsideMesh == meshSize -1) {
                    changeMeshes.Add((meshIndex + numMeshesXDir + 1, 0));
                }

                foreach((var meshIdx, var idxInMesh) in changeMeshes) {
                    if(!changedMeshes.ContainsKey(meshIdx)) {
                        changedMeshes[meshIdx] = meshes[meshIdx].vertices;
                    }
                    changedMeshes[meshIdx][idxInMesh].y = height;
                }
            }

            foreach((var meshIdx, var vertices) in changedMeshes) {
                meshes[meshIdx].SetVertices(vertices);
                meshes[meshIdx].RecalculateNormals();
            }

        }

        public void Render(IWorld World) {
            for(int y = 0; y < World.Width; y+=meshSize-1) {
                for(int x = 0; x < World.Width; x+=meshSize-1) {
                    RenderHeightmap(World, x,x+meshSize-1, y, y+meshSize-1);
                }
            }
        }

        void RenderHeightmap(IWorld World, int startX, int endX, int startY, int endY)
        {
            Debug.Assert(startX < endX);
            Debug.Assert(startY < endY);
            endX = (int)Mathf.Min(endX, World.Width-1);
            endY = (int)Mathf.Min(endY, World.Height-1);
            var width = endX-startX + 1;
            var height = endY-startY + 1;
            var minHeight = World.MinHeight;
            var maxHeight = World.MaxHeight;

            var mesh = new Mesh();
            var vertices = new Vector3[(width+1) * (height+1)];
            var colors = new Color[(width+1) * (height+1)];
            var triangles = new int[(width) * (height) * 6];
            var triangleIndex = 0;
            
            int worldX = startX;
            int worldY = startY;
            for (int meshY = 0; meshY < height; meshY++)
            {
                for (int meshX = 0; meshX < width; meshX++)
                {
                    var index = meshY * width + meshX;
                    var heightValue = World.GetHeightAt(worldX, worldY);
                    vertices[index] = new Vector3(worldX, heightValue, worldY);
                    colors[index] = gradient.Evaluate(Mathf.InverseLerp(minHeight, maxHeight, heightValue));

                    // Add triangles if not at the border
                    if (meshX < width - 1 && meshY < height - 1)
                    {
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
            terrainObject.transform.SetParent(this.gameObject.transform);

            meshes.Add(mesh);
        }
    }

}