using Graph.World.PoI;
using UnityEngine;

namespace Graph.World {
    public interface IWorld {
        public int Width { get; }
        public int Height { get; }

        public float
            MaxHeight { get; } // TODO: What is the difference between MaxHeight and height? Maybe add documentation?

        public float MinHeight { get; }
        public IPointOfInterestCollection PointsOfInterest { get; }
        public IStreetGraph StreetGraph { get; }
        public float GetHeightAt(float x, float y);

        public bool IsOutOfBounds(Vector2 pos) {
            return pos.x < 0 || pos.x >= Width || pos.y < 0 || pos.y >= Height;
        }
    }
}