using UnityEngine;
using FreeFormGraph.World;

namespace FreeFormGraph.World {
    public class WorldRenderer: MonoBehaviour {
        public IWorld World;
        [SerializeField]
        public Material material;
        private bool didCreateMesh = false;

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
            for(int x = 0; x < World.Width; x+=meshSize) {
                for(int y = 0; y < World.Width; y+=meshSize) {
                    RenderHeightmap(x,x+meshSize, y, y+meshSize);
                }
            }
        }

        //chatgpt...
        void RenderHeightmap(int startX, int endX, int startY, int endY)
        {
            Debug.Log($"Render {startX} {endX} {startY} {endY}");
            Debug.Assert(startX < endX);
            Debug.Assert(startY < endY);
            endX = (int)Mathf.Min(endX, World.Width);
            endY = (int)Mathf.Min(endY, World.Height);
            var width = endX-startX;
            var height = endY-startY;
            var minHeight = World.MinHeight;
            var maxHeight = World.MaxHeight;

            var mesh = new Mesh();
            var vertices = new Vector3[width * height];
            var colors = new Color[width * height];
            var normals = new Vector3[width * height];
            var triangles = new int[(width - 1) * (height - 1) * 6];
            var triangleIndex = 0;

            var startColor = new Color(0.91f, 0.83f, 0.02f, 1f);
            var endColor = new Color(0.91f, 0, 0, 1f);

            int worldX = startX;
            int worldY = startY;
            for (int meshY = 0; meshY < height; meshY++)
            {
                for (int meshX = 0; meshX < width; meshX++)
                {
                    var index = meshY * width + meshX;
                    if(index >= vertices.Length) {
                        Debug.Log($"{index} {meshX} {meshY} {width} {height}");
                    }
                    var heightValue = Mathf.InverseLerp(minHeight, maxHeight, World.GetHeightAt(worldX, worldY));
                    vertices[index] = new Vector3(worldX, worldY, 0);
                    normals[index] = new Vector3(0, 0, 1);
                    colors[index] = Color.Lerp(startColor, endColor, heightValue);

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
            mesh.normals = normals;


            GameObject terrainObject = new GameObject("Terrain");
            terrainObject.transform.position = Vector3.zero;
            terrainObject.AddComponent<MeshFilter>().mesh = mesh;
            terrainObject.AddComponent<MeshRenderer>().material = material;
;
        }
    }

}