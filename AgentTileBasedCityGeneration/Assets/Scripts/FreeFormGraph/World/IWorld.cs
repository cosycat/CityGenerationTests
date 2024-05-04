using System.Collections.Generic;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.World {
    public interface IWorld {
        public float GetHeightAt(float x, float y);
        public int Width {get;}
        public int Height {get;}

        public float MaxHeight {get;} // TODO: What is the difference between MaxHeight and height? Maybe add documentation?
        public float MinHeight {get;}
        public IPointOfInterestCollection PointsOfInterest {get;}
    }
}
