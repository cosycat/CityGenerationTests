using UnityEngine;
using FreeFormGraph.World;
using System.Collections.Generic;

namespace FreeFormGraph.Visualisation {

    public abstract class Terrain3DGameObject: MonoBehaviour, ITerrainGenerator {
        public abstract void SetHeightAt(IWorld world, IList<(float height, int x, int z)> heights);
        public abstract void Render(IWorld World);
    }
}