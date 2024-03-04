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
        
    }
    
}