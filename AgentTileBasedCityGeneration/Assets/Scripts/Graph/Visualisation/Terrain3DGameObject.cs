using System.Collections.Generic;
using Graph.World;
using UnityEngine;

namespace Graph.Visualisation {
    public abstract class Terrain3DGameObject : MonoBehaviour, ITerrainGenerator {
        public abstract void SetHeightAt(IWorld world, IList<(float height, int x, int z)> heights);
        public abstract void Render(IWorld World);
    }
}