using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Simulation {
    
    public abstract class RoadDeveloperAgent : Agent {
        
        public RoadDeveloperAgent(LandUsage agentUsageType, Tile currTile) : base(agentUsageType, currTile) { }
        
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

        protected bool isRoadDensityOk(RoadSegment newRoadSegment) {
            foreach (var tile in newRoadSegment.Tiles) {
                if(!IsRoadDensityOkTile(tile)) return false;
            }
            return true;
        }

        protected bool IsRoadDensityOkTile(Tile tile) {
            var allSitesInCircle = ((ISite)tile).GetTilesInCircle(5).OfType<Tile>();
            var sitesInCircle = allSitesInCircle as Tile[] ?? allSitesInCircle.ToArray();
            var roadCount = sitesInCircle.Count(t => t.UsageType == LandUsage.Road);
            var acceptableRoadDensity = tile.Dt;
            var actualRoadDensity = 1.0f * roadCount / sitesInCircle.Length;
            if (actualRoadDensity > acceptableRoadDensity) {
                //Debug.Log($"Road density too high at {tile.Position} with {roadCount} roads in circle of {sitesInCircle.Length} tiles (dRoad: {acceptableRoadDensity}, actual: {actualRoadDensity})");
                return false;
            }
            return true;
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