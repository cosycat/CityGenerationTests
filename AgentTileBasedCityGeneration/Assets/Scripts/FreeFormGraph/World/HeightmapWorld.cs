using UnityEngine;
using FreeFormGraph.Agents;
using FreeFormGraph.World.PoI;
using Utils;

namespace FreeFormGraph.World {
    public class HeightmapWorld: WorldGameObject {
        
        private float[,] heights;
        private int width;
        private int height;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeightMeters / Constants.METERS_PER_UNIT;
        
        public override float MinHeight => minHeightMeters / Constants.METERS_PER_UNIT;

        public float maxHeightMeters = 1000;
        public float minHeightMeters = 100;

        public override IPointOfInterestCollection PointsOfInterest { get; } = new PointOfInterestCollection();

        [SerializeField] public Texture2D heightmap;

        private IStreetGraph streetGraph;
        public override IStreetGraph StreetGraph => streetGraph;

        public void Start() {
            if(heightmap != null) {
                var pixels = heightmap.GetPixels();  
                width = heightmap.width;
                height = heightmap.height;
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
                        heights[x,y] = Mathf.Lerp(minHeightMeters, maxHeightMeters, Mathf.InverseLerp(minHeightTexture, maxHeightTexture, heights[x,y])) / Constants.METERS_PER_UNIT;
                    }
                }

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        Debug.Assert(heights[x,y] <= MaxHeight);
                        Debug.Assert(heights[x,y] >= MinHeight);
                    }
                }
            }

            streetGraph = FindObjectOfType<StreetGraphGameObject>();
        }

        public override float GetHeightAt(float x, float y)
        {
            return heights[(int)x, (int)y];
        }
    }

}