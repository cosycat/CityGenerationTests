using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation {
    
    public class TertiaryRoadExtender : RoadDeveloperAgent {

        private readonly int _dMin = 7; // TODO where should this be defined? locally or globally?
        private readonly int _dMax = 10; // TODO where should this be defined? locally or globally? Probably per agent (so difference for primary and tertiary roads and from settlement to settlement)
        
        public TertiaryRoadExtender(LandUsage agentUsageType, Tile currTile) : base(agentUsageType, currTile) {
            
        }
        
        protected override void MoveToNewLocation() {
            // TODO keep list of visited patches, and don't visit them again (and forget them after teleporting)
            var largestDistance = CurrTile.GetDistanceTo(AgentUsageType);
            Tile newTile = null;
            foreach (var neighbor in CurrTile.GetNeighbors(includeDiagonals: false)) {
                // get a random neighbour out of all the neighbours with largest distance (but never more than dMax)
                var distance = neighbor.GetDistanceTo(AgentUsageType);
                if (distance <= _dMax &&
                        (distance > largestDistance ||
                        (distance == largestDistance && Random.Range(0, 2) == 1)) && MeetsConstraints(neighbor)) {
                    largestDistance = distance;
                    newTile = neighbor;
                }
            }

            if (newTile != null) {
                CurrTile = newTile;
            }
            else {
                // Move to a random location (with road)
                Debug.Log($"No valid neighbours found for {this} at {CurrTile.Position}. Moving to random location.");
                var allRoads = World.AllTiles.Where(t => t.UsageType == LandUsage.Road && MeetsConstraints(t)).ToList();
                CurrTile = allRoads[Random.Range(0, allRoads.Count)];
            }
        }

        protected override bool LocationNeedsRoad() {
            return CurrTile.GetDistanceTo(LandUsage.Road) >= _dMin;
        }

        protected override RoadSegment BuildRoad() {
            var possibleRoad = new List<Tile> {CurrTile};
            while (possibleRoad[^1].UsageType != LandUsage.Road) {
                var currTile = possibleRoad[^1];
                var neighbors = currTile.GetNeighbors(false);
                var currRoadDistance = currTile.GetDistanceTo(AgentUsageType);
                Debug.Assert(!neighbors.Any(n => n.GetDistanceTo(AgentUsageType) < currRoadDistance - 1 || n.GetDistanceTo(AgentUsageType) > currRoadDistance + 1), $"Neighbours should always have a distance between +1 and -1 to currTile: {currTile}, neighbors: {string.Join(", ", neighbors)}");
                var possibleNextTiles = neighbors.Where(MeetsConstraints).ToList();
                if (possibleNextTiles.Count == 0) {
                    return null;
                }
                var minDistance = possibleNextTiles.Min(n => n.GetDistanceTo(AgentUsageType));
                var nextTile = ApplyTiebreakers(possibleNextTiles.Where(t => t.GetDistanceTo(AgentUsageType) <= minDistance + 0.1f).ToList(), possibleRoad[^1]);
                possibleRoad.Add(nextTile);
            }
            // Don't add the last road tile, as it's already part of another RoadSegment.
            possibleRoad.RemoveAt(possibleRoad.Count - 1);
            return new RoadSegment(World, AgentUsageType, possibleRoad, World.Tick, RoadType.Tertiary);
        }

        protected override bool IsValidRoad(RoadSegment newRoadSegment) {
            return isRoadDensityOk(newRoadSegment);
        }

        private bool MeetsConstraints(Tile arg) {
            bool isOnGridLine(int x, int gx, int gdx) {
                int xMax = ((x + gx - 1) / gx) * gx;
                int xMin = (x / gx) * gx; //rounding down
                return !(xMax - x > gdx && x - xMin > gdx);
            }

            //TODO why is gx saved in tile?
            bool onXAxis = isOnGridLine(arg.Position.x, arg.gx, arg.gdx);
            bool onYAxis = isOnGridLine(arg.Position.y, arg.gy, arg.gdy);

            return onXAxis || onYAxis;
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