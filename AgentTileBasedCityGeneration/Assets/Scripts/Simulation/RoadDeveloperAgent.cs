using System.Collections.Generic;
using JetBrains.Annotations;

namespace Simulation {
    
    public abstract class RoadDeveloperAgent : Agent {
        
        public RoadDeveloperAgent(LandUsage usageType, ISite currSite) : base(usageType, currSite) { }
        
        protected internal override void UpdateTick() {
            MoveToNewLocation();
            if (LocationMeetsConstraint() && LocationNeedsRoad()) {
                var newRoadSegment = BuildRoad();
                if (newRoadSegment != null && IsValidRoad(newRoadSegment)) {
                    Commit(newRoadSegment);
                }
            }
        }

        protected abstract void MoveToNewLocation();
        
        protected virtual bool LocationMeetsConstraint() {
            return true; // TODO
        }

        protected abstract bool LocationNeedsRoad();

        [CanBeNull] protected abstract RoadSegment BuildRoad();

        /// <summary>
        /// After finding a new road segment, the extender checks again if the whole road is valid. 
        /// </summary>
        /// <param name="newRoadSegment"> The new road segment to be checked </param>
        /// <returns> True if the road is valid, false otherwise </returns>
        protected abstract bool IsValidRoad(RoadSegment newRoadSegment);
        
        protected virtual void Commit(RoadSegment newRoadSegment) {
            foreach (var tile in newRoadSegment.Tiles) {
                tile.MultiTileSite = newRoadSegment;
            }
        }
        
    }

    public class RoadSegment : MultiTileSite {
        
        public RoadSegment(World world, LandUsage usageType, List<Tile> tiles, long tickCreated) : base(world, usageType, tiles, tickCreated) { }
        public RoadSegment(MultiTileSite site) : base(site) { }
        
    }
    
    public enum RoadType {
        Primary,
        Tertiary,
    }
}