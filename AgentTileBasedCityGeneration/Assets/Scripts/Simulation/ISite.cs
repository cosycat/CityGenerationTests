using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace Simulation {
    
    /// <summary>
    /// A site is a location in the world that can be developed by a property developer.
    /// It is either a single tile or a collection of tiles.
    /// </summary>
    public interface ISite {
        
        public World World { get; }
        LandUsage UsageType { get; }
        Vector2Int Position { get; }
        abstract Tile CorrespondingTile { get; }

        /// <summary>
        /// Calculates the value of the site for the current usage type.
        /// </summary>
        /// <returns> The value of the site </returns>
        public float CalcValue();
        /// <summary>
        /// Calculates the value of the site if it were to be used for the given usage type.
        /// </summary>
        /// <param name="usage"> The usage type to calculate the value for </param>
        /// <returns> The value of the site for the given usage type </returns>
        public float CalcValueForType(LandUsage usage);
        
        [Pure]
        public List<ISite> GetSitesInCircle(int radius) {
            var sites = new List<ISite>();
            for (int x = -radius; x <= radius; x++) {
                for (int y = -radius; y <= radius; y++) {
                    if (!World.TryGetTileAt(Position + new Vector2Int(x, y), out var tile)) {
                        continue;
                    }
                    if (Vector2Int.Distance(Position, tile.Position) + 0.1f >= radius) continue;

                    if (tile.MultiTileSite == null) {
                        sites.Add(tile);
                    }
                    else if (!sites.Contains(tile.MultiTileSite)) {
                        sites.Add(tile.MultiTileSite);
                    }
                }
            }
            return sites;
        }
    }
}