using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Simulation {
    
    /// <summary>
    /// A <see cref="MultiTileSite"/> is a collection of tiles that are considered as a single unit for the purposes of development.
    ///  TODO find a better name for this class
    /// </summary>
    public class MultiTileSite : ISite {
        public World World { get; }
        
        public long TickCreated { get; }
        public float Age => World.Tick - TickCreated;
        public float Area => Tiles.Count;
        
        private LandUsage _usageType;
        public LandUsage UsageType {
            get => _usageType;
            set {
                if (_usageType == value) return;
                var currUsageType = _usageType;
                _usageType = value;
                if (currUsageType != UsageType)
                    OnMultiTileUsageChanged(currUsageType, UsageType);
            }
        }
        
        public List<Tile> Tiles { get; private set; }
        
        public Vector2Int Position => Tiles[0].Position;
        
        public Tile CorrespondingTile => Tiles[0];

        public MultiTileSite(World world, LandUsage usageType, List<Tile> tiles, long tickCreated) {
            World = world;
            TickCreated = tickCreated;
            Tiles = tiles;
            _usageType = usageType;
            
        }
        
        public MultiTileSite(MultiTileSite site) {
            World = site.World;
            TickCreated = site.TickCreated;
            Tiles = site.Tiles;
        }

        public float CalcValue() {
            return Tiles.Average(tile => tile.CalcValue());
        }

        public void TileWasRemoved(Tile tile) {
            throw new NotImplementedException();
        }

        public event Action<(LandUsage oldUsageType, LandUsage newUsageType)> MultiTileUsageChanged;

        protected virtual void OnMultiTileUsageChanged(LandUsage oldUsageType, LandUsage newUsageType) {
            MultiTileUsageChanged?.Invoke((oldUsageType, newUsageType));
        }
    }
}