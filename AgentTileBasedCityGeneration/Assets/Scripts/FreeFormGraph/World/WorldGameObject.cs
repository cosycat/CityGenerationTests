using UnityEngine;

namespace FreeFormGraph.World {

    public abstract class WorldGameObject: MonoBehaviour, IWorld {
        public abstract float GetHeightAt(float x, float y);
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract float MaxHeight { get; }
        public abstract float MinHeight { get; }
    }
}