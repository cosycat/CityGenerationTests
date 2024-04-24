using UnityEngine;

namespace FreeFormGraph.World {
    public class WorldRenderer: MonoBehaviour {
        public IWorld World;
        [SerializeField]
        public Material material;
        private bool didCreateMesh = false;

        void Update() {
            if(World != null) {
                if(!didCreateMesh) {
                    RenderHeightmap();
                    didCreateMesh = true; //ugh
                }
            } else {
                World = FindObjectOfType<GaussWorld>();
            }
        }

        //chatgpt...
        void RenderHeightmap()
        {
            var width = World.Width;
            var height = World.Height;
            var minHeight = World.MinHeight;
            var maxHeight = World.MaxHeight;

            var mesh = new Mesh();
            var vertices = new Vector3[width * height];
            var colors = new Color[width * height];
            var normals = new Vector3[width * height];
            var triangles = new int[(width - 1) * (height - 1) * 6];
            var triangleIndex = 0;

            var startColor = new Color(0, 0.83f, 0.57f, 1f);
            var endColor = new Color(0.839f, 0.533f, 0, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var heightValue = Mathf.InverseLerp(minHeight, maxHeight, World.GetHeightAt(x, y));
                    vertices[index] = new Vector3(x, y, 0);
                    normals[index] = new Vector3(0, 0, 1);
                    colors[index] = Color.Lerp(startColor, endColor, heightValue);

                    // Add triangles if not at the border
                    if (x < width - 1 && y < height - 1)
                    {
                        triangles[triangleIndex++] = index;
                        triangles[triangleIndex++] = index + width;
                        triangles[triangleIndex++] = index + width + 1;

                        triangles[triangleIndex++] = index;
                        triangles[triangleIndex++] = index + width + 1;
                        triangles[triangleIndex++] = index + 1;
                    }
                }
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