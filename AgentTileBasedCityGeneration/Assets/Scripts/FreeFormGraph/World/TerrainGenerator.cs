using UnityEngine;
using FreeFormGraph.LineBased;

namespace FreeFormGraph.World {
    public class TerrainGenerator: MonoBehaviour {
        
        public IWorld World;
        [SerializeField]
        public Material material;
        private bool didCreateMesh = false;

        [SerializeField]
        public Gradient gradient;

        void Start() {
        }

        void Update() {
            if(World != null) {
                if(!didCreateMesh) {
                    RenderSubmaps();
                    didCreateMesh = true; //ugh
                }
            } else {
                World = FindObjectOfType<WorldGameObject>() as IWorld;
            }
        }

        void RenderSubmaps() {
            int meshSize = 200;
            for(int x = 0; x < World.Width; x+=meshSize-1) {
                for(int y = 0; y < World.Width; y+=meshSize-1) {
                    RenderHeightmap(x,x+meshSize-1, y, y+meshSize-1);
                }
            }
        }

        void RenderHeightmap(int startX, int endX, int startY, int endY)
        {
            Debug.Log($"Render {startX} {endX} {startY} {endY}");
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

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();


            var terrainObject = new GameObject($"3d Terrain {startX} {startY}");
            terrainObject.transform.position = Vector3.zero;
            terrainObject.AddComponent<MeshFilter>().mesh = mesh;
            terrainObject.AddComponent<MeshRenderer>().material = material;
            terrainObject.transform.SetParent(this.gameObject.transform);
        }
    }

}