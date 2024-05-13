using UnityEngine;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.World {
    public interface IWorld {
        public float GetHeightAt(float x, float y);
        public int Width {get;}
        public int Height {get;}
        public bool IsOutOfBounds(Vector2 pos) => pos.x < 0 || pos.x >= Width || pos.y < 0 || pos.y >= Height;

        public float MaxHeight {get;} // TODO: What is the difference between MaxHeight and height? Maybe add documentation?
        public float MinHeight {get;}
        public IPointOfInterestCollection PointsOfInterest {get;}
        public IStreetGraph StreetGraph {get;}
    }
}
