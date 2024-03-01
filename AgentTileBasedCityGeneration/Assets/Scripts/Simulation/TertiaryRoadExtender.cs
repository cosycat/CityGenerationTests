using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation {
    
    public class TertiaryRoadExtender : RoadDeveloperAgent {

        private readonly int _dMin = 5; // TODO where should this be defined? locally or globally?
        private readonly int _dMax = 10; // TODO where should this be defined? locally or globally? Probably per agent (so difference for primary and tertiary roads and from settlement to settlement)
        
        public TertiaryRoadExtender(LandUsage usageType, ISite currSite) : base(usageType, currSite) {
            
        }
        
        protected override void MoveToNewLocation() {
            // TODO keep list of visited patches, and don't visit them again (and forget them after teleporting)
            var largestDistance = CurrTile.GetDistanceTo(UsageType);
            Tile newTile = null;
            foreach (var neighbor in CurrTile.GetNeighbors(true)) {
                if (neighbor.UsageType == UsageType) {
                    continue;
                }
                // TODO get a random neighbour out of all the neighbours with largest distance (but never more than dMax)
                var distance = neighbor.GetDistanceTo(UsageType);
                if (distance > largestDistance && distance <= _dMax) {
                    largestDistance = distance;
                    newTile = neighbor;
                }
            }

            if (newTile != null) {
                CurrTile = newTile;
            }
            else {
                // TODO what should happen if there are no valid neighbors? Move to a random location (with road) probably.
            }
        }

        protected override bool LocationNeedsRoad() {
            return CurrTile.GetDistanceTo(LandUsage.Road) >= _dMin;
        }

        protected override RoadSegment BuildRoad() {
            var possibleRoad = new List<Tile> {CurrTile};
            while (possibleRoad[^1].UsageType != LandUsage.Road) {
                var currTile = possibleRoad[^1];
                var neighbors = currTile.GetNeighbors(true);
                var currRoadDistance = currTile.GetDistanceTo(UsageType);
                Debug.Assert(!neighbors.Any(n => n.GetDistanceTo(UsageType) < currRoadDistance - 1 || n.GetDistanceTo(UsageType) > currRoadDistance + 1), $"Neighbours should always have a distance between +1 and -1 to currTile: {currTile}, neighbors: {string.Join(", ", neighbors)}");
                var possibleNextTiles = neighbors.Where(MeetsConstraints).ToList();
                if (possibleNextTiles.Count == 0) {
                    return null;
                }
                var minDistance = possibleNextTiles.Min(n => n.GetDistanceTo(UsageType));
                var nextTile = ApplyTiebreakers(possibleNextTiles.Where(t => t.GetDistanceTo(UsageType) <= minDistance + 0.1f).ToList(), possibleRoad[^1]);
                possibleRoad.Add(nextTile);
                
            }
            return new RoadSegment(World, UsageType, possibleRoad, World.Tick);
        }

        protected override bool IsValidRoad(RoadSegment newRoadSegment) {
            // check if road density is still okay
            foreach (var tile in newRoadSegment.Tiles) {
                var allSitesInCircle = ((ISite)tile).GetSitesInCircle(5).OfType<Tile>();
                var sitesInCircle = allSitesInCircle as Tile[] ?? allSitesInCircle.ToArray();
                var roadCount = sitesInCircle.Count(t => t.UsageType == LandUsage.Road);
                var acceptableRoadDensity = tile.dRoad;
                var actualRoadDensity = 1.0f * roadCount / sitesInCircle.Length;
                if (actualRoadDensity > acceptableRoadDensity) {
                    Debug.Log($"Road density too high at {tile.Position} with {roadCount} roads in circle of {sitesInCircle.Length} tiles (dRoad: {acceptableRoadDensity}, actual: {actualRoadDensity})");
                    return false;
                }
            }
            return true;
        }

        private bool MeetsConstraints(Tile arg) {
            return true; // TODO Add constrtaints
        }

        /// <summary>
        /// "Given a choice between two or more patches, an extender uses two tiebreakers.
        /// First, it will choose the patch that is on a parcel boundary.
        /// If that does not resolve the choice, the extender chooses the patch with the lowest absolute change in elevation.
        /// Otherwise, the choice is random.
        /// </summary>
        /// <param name="possibleNextTiles"> All neighbour tiles nearest to an existing road </param>
        /// <param name="prevTile"> The previous tile chosen for the road </param>
        /// <returns> The next tile to be chosen for the road </returns>
        private Tile ApplyTiebreakers(IReadOnlyList<Tile> possibleNextTiles, Tile prevTile) {
            Debug.Assert(possibleNextTiles.Any(), "possibleNextTiles.Count > 0");
            var currTile = possibleNextTiles[0];
            for (var i = 1; i < possibleNextTiles.Count; i++) {
                var nextTile = possibleNextTiles[i];
                var currTileBoundary = currTile.IsParcelBoundary();
                var nextTileBoundary = nextTile.IsParcelBoundary();
                if (!currTileBoundary && nextTileBoundary) {
                    currTile = nextTile;
                } else if (currTileBoundary && !nextTileBoundary) {
                    continue;
                } else {
                    var nextTileHeightDiff = Math.Abs(prevTile.Elevation - nextTile.Elevation);
                    var currTileHeightDiff = Math.Abs(prevTile.Elevation - currTile.Elevation);
                    if (nextTileHeightDiff < currTileHeightDiff) {
                        currTile = nextTile;
                    }
                    else {
                        if (Random.Range(0, 2) == 1) // Take random tile if heights are equal // TODO better even distribution
                            currTile = nextTile;
                    }
                }
            }
            return currTile;
        }
        
    }
}