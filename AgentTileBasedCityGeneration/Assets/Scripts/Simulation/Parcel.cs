using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Simulation {
    
    /// <summary>
    /// A parcel is a collection of tiles that are considered as a single unit for the purposes of development.
    /// </summary>
    public class Parcel : ISite {

        public World World { get; }
        public float Population { get; set; }
        public long TickCreated { get; }
        public float Age => World.Tick - TickCreated;
        public float Area => Tiles.Count;
        public float Density => Population / Area;
        
        private LandUsage _usageType;
        public LandUsage UsageType {
            get => _usageType;
            set {
                if (_usageType == value) return;
                var currUsageType = _usageType;
                _usageType = value;
                if (currUsageType != UsageType)
                    OnParcelUsageChanged(currUsageType, UsageType);
            }
        }

        public List<Tile> Tiles { get; private set; }
        public Vector2Int Position => Tiles[0].Position;
        
        
        public Parcel(World world, LandUsage usageType, List<Tile> tiles, float value, long tickCreated) {
            World = world;
            UsageType = usageType;
            Tiles = tiles;
            TickCreated = tickCreated;
        }

        public Parcel(Parcel parcel) {
            TickCreated = parcel.TickCreated;
            World = parcel.World;
            UsageType = parcel.UsageType;
            Tiles = new List<Tile>(parcel.Tiles);
        }

        
        public static List<LandUsage> ConvertibleTo(LandUsage type) {
            return type switch {
                LandUsage.Residential => new List<LandUsage> {LandUsage.Residential, LandUsage.Commercial},
                LandUsage.Commercial => new List<LandUsage> {LandUsage.Commercial, LandUsage.Residential, LandUsage.Industrial},
                LandUsage.Industrial => new List<LandUsage> {LandUsage.Industrial, LandUsage.Commercial},
                LandUsage.Park => new List<LandUsage> {LandUsage.Park},
                LandUsage.Road => new List<LandUsage> {LandUsage.Road},
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public float CalcValue() {
            return Tiles.Average(tile => tile.CalcValue());
        }
        
        public event Action<(LandUsage oldUsageType, LandUsage newUsageType)> ParcelUsageChanged;
        
        protected virtual void OnParcelUsageChanged(LandUsage oldUsageType, LandUsage newUsageType) {
            ParcelUsageChanged?.Invoke((oldUsageType, newUsageType));
        }
    }
    
}