using System.Linq;
using Graph.World.PoI;
using UnityEngine;

namespace Graph.World {
    public class HeightmapWorld : WorldGameObject {
        public float maxHeightMeters = 1000;
        public float minHeightMeters = 100;

        [SerializeField] public Texture2D heightmap;
        private int height;
        private float[,] heights;

        private IStreetGraph streetGraph;
        private int width;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeightMeters / Constants.METERS_PER_UNIT;

        public override float MinHeight => minHeightMeters / Constants.METERS_PER_UNIT;

        public override IPointOfInterestCollection PointsOfInterest { get; } = new PointOfInterestCollection();
        public override IStreetGraph StreetGraph => streetGraph;

        private void Awake() {
            streetGraph = FindObjectOfType<StreetGraphGameObject>();

            if (heightmap != null) {
                var pixels = heightmap.GetPixels();
                width = heightmap.width;
                height = heightmap.height;
                heights = new float[Width, Height];

                var maxHeightTexture = float.MinValue;
                var minHeightTexture = float.MaxValue;

                for (var y = 0; y < Height; y++) {
                    for (var x = 0; x < Width; x++) {
                        var pixelValue = pixels[x + y * Width].grayscale;
                        if (pixelValue > maxHeightTexture) maxHeightTexture = pixelValue;
                        if (pixelValue < minHeightTexture) minHeightTexture = pixelValue;
                        heights[x, y] = pixelValue;
                    }
                }

                Debug.Log($"Max pixel value of heightmap: {maxHeightTexture}");
                Debug.Log($"Min pixel value of heightmap: {minHeightTexture}");

                for (var y = 0; y < Height; y++) {
                    for (var x = 0; x < Width; x++) {
                        heights[x, y] =
                            Mathf.Lerp(minHeightMeters, maxHeightMeters,
                                Mathf.InverseLerp(minHeightTexture, maxHeightTexture, heights[x, y])) /
                            Constants.METERS_PER_UNIT;
                    }
                }

                // for (var y = 0; y < Height; y++) {
                //     for (var x = 0; x < Width; x++) {
                //         Debug.Assert(heights[x, y] <= MaxHeight);
                //         Debug.Assert(heights[x, y] >= MinHeight);
                //     }
                // }
                Debug.Assert(heights.Cast<float>().All(h => h <= MaxHeight && h >= MinHeight), "HeightmapWorld: Heights are not within the expected range");
            }
        }

        public override float GetHeightAt(float x, float y) {
            var lowerX = Mathf.FloorToInt(x);
            var upperX = (int)Mathf.Min(Mathf.Ceil(x), width - 1);
            var lowerY = Mathf.FloorToInt(y);
            var upperY = (int)Mathf.Min(Mathf.Ceil(y), height - 1);

            if (lowerX < 0 || upperX < 0 || lowerY < 0 || upperY < 0) {
                Debug.LogWarning($"HeightmapWorld: GetHeightAt called with negative coordinates: {x}, {y}");
                return 0;
            }

            if (lowerX >= width || upperX >= width || lowerY >= height || upperY >= height) {
                Debug.LogWarning($"HeightmapWorld: GetHeightAt called with coordinates outside of heightmap: {x}, {y}");
                return 0;
            }

            //bilinear interpolation 

            var h1 = heights[lowerX, lowerY];
            var h2 = heights[upperX, lowerY];
            var h3 = heights[lowerX, upperY];
            var h4 = heights[upperX, upperY];

            var interpolation1 = Mathf.Lerp(h1, h2, x % 1); //modulo to get part after point
            var interpolation2 = Mathf.Lerp(h3, h4, x % 1); //modulo to get part after point
            var interpolationFinal = Mathf.Lerp(interpolation1, interpolation2, y % 1);
            return interpolationFinal;
        }
    }
}