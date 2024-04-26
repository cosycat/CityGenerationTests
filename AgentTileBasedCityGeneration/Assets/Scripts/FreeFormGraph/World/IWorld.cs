namespace FreeFormGraph.World {
    public interface IWorld {
        public float GetHeightAt(float x, float y);
        public int Width {get;}
        public int Height {get;}

        public float MaxHeight {get;}
        public float MinHeight {get;}
    }
}
