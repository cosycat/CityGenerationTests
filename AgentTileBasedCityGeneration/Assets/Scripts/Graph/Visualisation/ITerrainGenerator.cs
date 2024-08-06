using System.Collections.Generic;
using Graph.World;

namespace Graph.Visualisation {
    public interface ITerrainGenerator {
        /// <summary>
        ///     Setting a certain height in the world based on world units (see Constants.METERS_PER_UNIT).
        ///     Updating is costly (vertices are reset, normals recalculated), so try to batch things if possible.
        ///     This is also the reason why this method expects a list.
        /// </summary>
        void SetHeightAt(IWorld world, IList<(float height, int x, int z)> heights);

        /// <summary>
        ///     Create terrain out of 3D meshes.
        /// </summary>
        void Render(IWorld World);
    }
}