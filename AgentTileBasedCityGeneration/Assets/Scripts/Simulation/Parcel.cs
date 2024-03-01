using System;
using System.Collections.Generic;

namespace Simulation {
    /// <summary>
    /// A parcel is a <see cref="MultiTileSite"/> that has a population and a road access.
    /// Population is the number of people living in the parcel
    /// </summary>
    public class Parcel : MultiTileSite {

        public float Population { get; set; }
        
        public float Density => Population / Area;

        public Parcel(World world, LandUsage usageType, List<Tile> tiles, long tickCreated, float population) : base(world, usageType, tiles, tickCreated) {
            Population = population;
        }

        public Parcel(Parcel parcel) : base(parcel) {
            Population = parcel.Population;
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
        
    }
    
}