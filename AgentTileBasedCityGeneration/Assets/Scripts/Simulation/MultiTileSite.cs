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
        
        public Vector2Int Position {
            get {
                if (Tiles.Count == 0) {
                    Debug.LogError($"MultiTileSite {this} has no tiles.");
                    return Vector2Int.zero;
                }
                return Tiles[0].Position;
            }
        }

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
            return CalcValueForType(UsageType);
        }

        public float CalcValueForType(LandUsage usage) {
            Debug.Assert(Tiles.Count > 0, $"This MultiTileSite {this} has no tiles.");
            return Tiles.Average(tile => tile.CalcValueForType(usage));
        }

        public void TileWasRemoved(Tile tile) {
            if (Tiles.Contains(tile)) {
                Debug.Log($"Tile {tile} was removed from {this}.");
                if (Tiles.Count == 1) {
                    // ignore
                    Debug.Log($"MultiTileSite {this} has no tiles left. Last tile was {tile}.");
                    OnMultiTileSiteRemoved(tile);
                }
                else {
                    // I have no idea why, but Unity just crashes on this line... (At least if it is the last remaining tile, not sure about other cases)
                    Tiles.Remove(tile);
                }
            }
            else {
                Debug.LogError($"Tile {tile} was removed from {this} but it was not in the list of tiles.");
            }
            // // Debug.Assert(Tiles.Count(t => t == tile) == 1, $"Tile {tile} exists {Tiles.Count(t => t == tile)} times in {this} instead of exactly once.");
            // // Tiles.Remove(tile);
            // if (Tiles.Count == 0) {
            //     Debug.Log($"MultiTileSite {this} has no tiles left. Last tile was {tile}.");
            //     return;
            //     OnMultiTileSiteDestroyed(tile);
            // }
        }
        
        public static List<LandUsage> ConvertibleTo(LandUsage type) {
            return type switch {
                LandUsage.Residential => new List<LandUsage> {LandUsage.Residential, LandUsage.Commercial},
                LandUsage.Commercial => new List<LandUsage> {LandUsage.Commercial, LandUsage.Residential, LandUsage.Industrial},
                LandUsage.Industrial => new List<LandUsage> {LandUsage.Industrial, LandUsage.Commercial},
                LandUsage.Park => new List<LandUsage> {LandUsage.Park},
                LandUsage.Road => new List<LandUsage> {LandUsage.Road},
                LandUsage.Water => throw new Exception("A multi-tile site cannot have a usage type of Water."),
                LandUsage.None => throw new Exception("A multi-tile site cannot have a usage type of None."),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public event Action<(LandUsage oldUsageType, LandUsage newUsageType)> MultiTileUsageChanged;

        protected virtual void OnMultiTileUsageChanged(LandUsage oldUsageType, LandUsage newUsageType) {
            MultiTileUsageChanged?.Invoke((oldUsageType, newUsageType));
        }
        
        public event Action<MultiTileSite, Tile> MultiTileSiteRemoved;
        
        protected virtual void OnMultiTileSiteRemoved(Tile lastTile) {
            MultiTileSiteRemoved?.Invoke(this, lastTile);
        }


        public override string ToString() {
            return $"MultiTileSite({GetType()}) {Position} ({UsageType})";
        }
    }
}