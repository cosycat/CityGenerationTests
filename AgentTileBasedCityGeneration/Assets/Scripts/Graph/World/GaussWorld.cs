using Graph.World.PoI;
using UnityEngine;

namespace Graph.World {
    public class GaussWorld : WorldGameObject {
        [SerializeField] private int width = 100;
        [SerializeField] private int height = 100;
        private float[,] heights;

        private float maxHeight = float.MinValue;
        private float minHeight = float.MaxValue;

        private IStreetGraph streetGraph;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeight;

        public override float MinHeight => minHeight;
        public override IPointOfInterestCollection PointsOfInterest { get; } = new PointOfInterestCollection();
        public override IStreetGraph StreetGraph => streetGraph;

        private void Awake() {
            streetGraph = FindObjectOfType<StreetGraphGameObject>();

            heights = new float[Width, Height];

            PlaceGauss(Width / 2, Height / 2, spread: 5);
            PlaceGauss(Width / 2 + 20, Height / 2 - 20, spread: 15);
            PlaceGauss(Width / 2, Height / 2 + 20, spread: 15);

            for (var y = 0; y < Height; y++) {
                for (var x = 0; x < Width; x++) {
                    minHeight = Mathf.Min(MinHeight, heights[x, y]);
                    maxHeight = Mathf.Max(MaxHeight, heights[x, y]);
                }
            }
        }

        private void PlaceGauss(int centerX, int centerY, float amplitude = 10, float spread = 5.0f) {
            for (var y = 0; y < Height; y++) {
                for (var x = 0; x < Width; x++) {
                    float distanceX = (x - centerX) * (x - centerX);
                    float distanceY = (y - centerY) * (y - centerY);

                    heights[x, y] += amplitude * Mathf.Exp(-((distanceX + distanceY) / (2 * spread * spread)));
                }
            }
        }

        public override float GetHeightAt(float x, float y) {
            if (x < 0 || x >= Width || y < 0 || y >= Height) {
                Debug.LogWarning($"Requested height at {x}, {y} outside of world bounds.");
                return 0;
            }
            return heights[(int)x, (int)y];
        }
    }
}