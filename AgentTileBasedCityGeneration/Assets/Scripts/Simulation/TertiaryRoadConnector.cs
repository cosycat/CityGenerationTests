using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation {
    
    public class TertiaryRoadConnector : RoadDeveloperAgent {

        private Tile PrevTile;

        private int SearchRadius = 7;
        private int cRatio = 2;

        public TertiaryRoadConnector(LandUsage agentUsageType, Tile currTile) : base(agentUsageType, currTile) {
            PrevTile = currTile;
        }

        protected override void MoveToNewLocation() {
            
            var neighbors = CurrTile.GetNeighbors(includeDiagonals: false).Where(t => t.UsageType == LandUsage.Road && t != PrevTile);
            //Debug.Log($"Number of roads to choose from (excluding previous): {neighbors.Count()}");
            if(neighbors.Count() == 0) {
                var x = PrevTile;
                PrevTile = CurrTile;
                CurrTile = x; //roads ends here, just move backwards
            } else {
                var nextTile = neighbors.ToList()[Random.Range(0, neighbors.Count())];
                Debug.Assert(nextTile.Position != PrevTile.Position);
                PrevTile = CurrTile;
                CurrTile = nextTile;
            }

        }

        protected override bool LocationNeedsRoad() {
            //LocationNeedsRoad depends on direct distance and shortest path to destination via roads
            //Calculating these and saving these results for use in BuildRoad() is not nice, which is why
            //it is directly calculated BuildRoad()...
            return true;
        }

        protected override RoadSegment BuildRoad() {
            var roadsInCircle = ((ISite) CurrTile).GetTilesInCircle(SearchRadius).Where(t => t != CurrTile && t.UsageType == LandUsage.Road);
            if(roadsInCircle.Count() == 0) {
                return null;
            }

            var dest = roadsInCircle.ToList()[Random.Range(0, roadsInCircle.Count())];
            var distDirect = CurrTile.Position.ManhattanDistanceTo(dest.Position);
            var currentRoadDistance = shortestPath(CurrTile, dest);

            if(currentRoadDistance / distDirect >= cRatio || currentRoadDistance == 0) {

                var possibleRoad = new List<Tile>();
                var nextPatch = CurrTile;
                var start = CurrTile;
                var prevStates = new Stack<(List<Tile>, List<Tile>)>();

                while(true) {
                    while((CurrTile.UsageType != LandUsage.Road || CurrTile == start) && nextPatch != null) {
                        CurrTile = nextPatch;
                        possibleRoad.Add(CurrTile);
                        var possibleTiles = nextPatch.GetNeighbors()
                            .Where(MeetsConstraints)
                            .Where(t => !possibleRoad.Contains(t))
                            .OrderBy(t => t.Position.ManhattanDistanceTo(dest.Position))
                            .ToList();

                        nextPatch = possibleTiles.FirstOrDefault();
                        possibleTiles.RemoveAt(0);
                        prevStates.Push((new List<Tile>(possibleRoad), possibleTiles));
                    }

                    if(CurrTile.UsageType == LandUsage.Road) {
                        var existingRoadDistance = shortestPath(possibleRoad[^1], dest);
                        var newRoadDistance = possibleRoad.Count; 

                        if(currentRoadDistance / newRoadDistance <= cRatio && currentRoadDistance != 0) {
                            //The current shortest new road is equally long as the existing road,
                            //this means the total length of the route will not be under the define cRatio threshold
                            CurrTile = start; //reset position
                            return null;
                        }

                        var totalRoadDistance = newRoadDistance + existingRoadDistance;
                        if(currentRoadDistance / totalRoadDistance <= cRatio && currentRoadDistance != 0) {
                            var (road, tiles) = prevStates.Pop();
                            possibleRoad = road;
                            nextPatch = tiles.FirstOrDefault();
                        } else {
                            //we are happy!
                            Debug.Log($"Built a road here! src {CurrTile} dest {dest}");
                            CurrTile = possibleRoad[^1];
                            possibleRoad.RemoveAt(0); //remove start tile
                            possibleRoad.RemoveAt(possibleRoad.Count - 1); //due to algorithm, last tile is already a road
                            return new RoadSegment(World, AgentUsageType, possibleRoad, World.Tick);
                        }
                    }


                    CurrTile = start; //reset position
                    return null;
                }

            }

            return null;
        }

        private float distanceFn(Tile src, Tile dest) {
            return 0;
        }

        //TODO change to A*?
        private int shortestPath(Tile src, Tile dest) {
            Debug.Assert(src.UsageType == LandUsage.Road);
            Debug.Assert(dest.UsageType == LandUsage.Road);
            if(src == dest) return 0;

            var queue = new Queue<Tile>();
            var parents = new Dictionary<Tile, Tile>();
            parents.Add(src, null);
            queue.Enqueue(src);
            var foundRoute = false;
            while(queue.Count > 0) {
                var node = queue.Dequeue();
                if(node == dest) {
                    foundRoute = true;
                    break;
                } else {
                    foreach(var neighbor in node.GetNeighbors(includeDiagonals: false).Where(t => t.UsageType == LandUsage.Road)) {
                        if(!parents.ContainsKey(neighbor)) {
                            queue.Enqueue(neighbor);
                            parents.Add(neighbor, node);
                        }
                    }
                }
            }

            if(!foundRoute) return 0; //no path was found?

            var pathLength = 0;
            var currentTile = dest;
            while(currentTile != src) {
                pathLength++;
                currentTile = parents[currentTile];
            }

            Debug.Assert(pathLength >= 1); //if src and dest are neighbors, distance is 1
            return pathLength;
        }

        protected override bool IsValidRoad(RoadSegment newRoadSegment) {
            return isRoadDensityOk(newRoadSegment);
        }

        private bool MeetsConstraints(Tile tile) {
            return this.IsRoadDensityOkTile(tile);
        }
        
    }
}