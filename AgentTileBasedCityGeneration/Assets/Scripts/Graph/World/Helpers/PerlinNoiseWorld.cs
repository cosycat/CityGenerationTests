using Graph.LineBased;
using Graph.World.PoI;
using UnityEngine;

namespace Graph.World.Helpers {
    public class PerlinNoiseWorld: WorldGameObject {
        [SerializeField] private int width = 100;
        [SerializeField] private int height = 100;
        [SerializeField] private float maxHeight = 200;
        [SerializeField] private float minHeight = 100;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeight;

        public override float MinHeight => minHeight;

        public override IPointOfInterestCollection PointsOfInterest => new PointOfInterestCollection();

        public override IStreetGraph StreetGraph =>
            FindObjectOfType<StreetGraphGameObject>() ?? LineGraphTestCreator.GenerateHShapedGraph();

        public override float GetHeightAt(float x, float y) {
            return minHeight + (maxHeight - minHeight) * Mathf.PerlinNoise(x, y);
        }
    }
}