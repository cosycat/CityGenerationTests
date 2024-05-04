using UnityEngine;
using FreeFormGraph.Agent;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.World {
    public class HeightmapWorld: WorldGameObject {
        private float[,] heights;
        private int width;
        private int height;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => MaxHeightMeters / Constants.METERS_PER_UNIT;
        
        public override float MinHeight => MinHeightMeters / Constants.METERS_PER_UNIT;

        public float MaxHeightMeters = 1000;
        public float MinHeightMeters = 100;

        public override IPointOfInterestCollection PointsOfInterest { get; } = new PointOfInterestCollection();

        [SerializeField]
        public Texture2D Heightmap;

        private IStreetGraph graph;

        public void Start() {
            if(Heightmap != null) {
                var pixels = Heightmap.GetPixels();  
                width = Heightmap.width;
                height = Heightmap.height;
                heights = new float[Width, Height];

                float maxHeightTexture = float.MinValue;
                float minHeightTexture = float.MaxValue;

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        var pixelValue = pixels[x + y * Width].grayscale;
                        if(pixelValue > maxHeightTexture) maxHeightTexture = pixelValue;
                        if(pixelValue < minHeightTexture) minHeightTexture = pixelValue;
                        heights[x,y] = pixelValue;
                    }
                }
                Debug.Log($"Max pixel value of heightmap: {maxHeightTexture}");
                Debug.Log($"Min pixel value of heightmap: {minHeightTexture}");

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        heights[x,y] = Mathf.Lerp(MinHeightMeters, MaxHeightMeters, Mathf.InverseLerp(minHeightTexture, maxHeightTexture, heights[x,y])) / Constants.METERS_PER_UNIT;
                    }
                }

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        Debug.Assert(heights[x,y] <= MaxHeight);
                        Debug.Assert(heights[x,y] >= MinHeight);
                    }
                }
            }

            graph = FindObjectOfType<StreetGraphGameObject>();
            var agent = new Pathfinding() {
                StreetGraph = graph,
                World = this
            };

            agent.AStar(new Vector3(10, 185,0), new Vector3(20, 192 ,0));
            agent.AStar(new Vector3(10, 200-10,0), new Vector3(140,200-150 ,0));
            agent.AStar(new Vector3(190, 200-55,0), new Vector3(80,200-180 ,0));
            agent.AStar(new Vector3(4, 4,0), new Vector3(140, 200-150 ,0));
            agent.AStar(new Vector3(4, 4,0), new Vector3(80,20,0));
        }

        public override float GetHeightAt(float x, float y)
        {
            return heights[(int)x, (int)y];
        }
    }

}