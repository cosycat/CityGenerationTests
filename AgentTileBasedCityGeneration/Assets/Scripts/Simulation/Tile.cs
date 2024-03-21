using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Simulation {

    /// <summary>
    /// A tile is the smallest unit of land in the simulation. It can be combined with other tiles to form a parcel by a property developer.
    /// </summary>
    [Serializable]
    public sealed class Tile : ISite {

        #region AgentParameters
        
        /// <summary>
        /// Grid size in x direction
        /// </summary>
        public int gx { get; } = 10;
        /// <summary>
        /// Grid size in y direction
        /// </summary>
        public int gy { get; } = 10;
        /// <summary>
        /// Tightness of the grid in x direction
        /// If gdx = gdy = 0, road patches must be directly on the grid.
        /// If gdx >= gx and gdy >= gy, the grid does not constrain road layout at all,
        /// so the local road network will be completely ungridded and "organic"
        /// </summary>
        public int gdx { get; } = 5;
        /// <summary>
        /// Tightness of the grid in y direction
        /// If gdx = gdy = 0, road patches must be directly on the grid.
        /// If gdx >= gx and gdy >= gy, the grid does not constrain road layout at all,
        /// so the local road network will be completely ungridded and "organic"
        /// </summary>
        public int gdy { get; } = 5;
        /// <summary>
        /// Limiting road density (number of road patches within each local neighborhood circle(5))
        /// </summary>
        public float Dt { get; } = 0.5f;
        /// <summary>
        /// Road density
        /// Number of road patches within each local neighborhood circle(5)
        /// </summary>
        public float dRoad {
            get {
                var thisSite = this as ISite;
                var sitesInCircle = thisSite.GetSitesInCircle(5);
                var roadCount = 0;
                foreach (var site in sitesInCircle) {
                    if (site.UsageType == LandUsage.Road) roadCount++;
                }
                return 1f * roadCount / sitesInCircle.Count;
            }
        }
        
        #endregion

        #region WorldInfo

        public World World { get; }

        public Vector2Int Position { get; }
        public float Elevation { get; }
        public Tile CorrespondingTile => this;
        
        public bool IsWater { get; }
        public bool IsBuildable => !IsWater; // TODO maybe add more conditions like if it is too steep.
        
        public bool IsRoadAdjacent => GetNeighbors().Exists(tile => tile.UsageType == LandUsage.Road);
        
        #endregion
        

        #region LandUsage and Parcel

        [CanBeNull] private MultiTileSite _multiTileSite;

        [CanBeNull]
        public MultiTileSite MultiTileSite {
            get => _multiTileSite;
            internal set {
                if (_multiTileSite == value) return;
                if (_multiTileSite != null) { _multiTileSite.MultiTileUsageChanged -= OnTileUsageChanged;
                    // TODO if a Tile changes its MultiTileSite, it should also update the MultiTileSite's Tiles
                    _multiTileSite?.TileWasRemoved(this);
                }
                var currUsageType = UsageType;
                _multiTileSite = value;
                if (currUsageType != UsageType) OnTileUsageChanged((currUsageType, UsageType));
                if (_multiTileSite != null) _multiTileSite.MultiTileUsageChanged += OnTileUsageChanged;
            }
        }

        public LandUsage UsageType => IsWater ? LandUsage.Water : MultiTileSite?.UsageType ?? LandUsage.None;
        

        #endregion
        
        

        public Tile(World world, bool isWater, Vector2Int position, float elevation) {
            World = world; 
            Position = position;
            IsWater = isWater;
            Elevation = elevation;
        }

        public List<Tile> GetNeighbors(bool includeDiagonals = false) {
            var neighbors = new List<Tile>();
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    if (x == 0 && y == 0) continue;
                    if (!includeDiagonals && x != 0 && y != 0) continue;
                    if (World.TryGetTileAt(Position + new Vector2Int(x, y), out var tile)) {
                        neighbors.Add(tile);
                    }
                }
            }
            Debug.Assert(includeDiagonals || neighbors.Count <= 4, $"Tile {Position} has {neighbors.Count} neighbors but should have at most 4.");
            return neighbors;
            
        }
        
        public float CalcValue() {
            return CalcValueForType(UsageType);
        }

        public float CalcValueForType(LandUsage usage) {
            switch (usage) {
                case LandUsage.Residential:
                case LandUsage.Commercial:
                case LandUsage.Industrial:
                case LandUsage.Park:
                    var attributeValue = new Attribute(this).Calculate(usage);
                    return attributeValue;
                
                case LandUsage.Road:
                case LandUsage.Water:
                case LandUsage.None:
                    return 0f;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }


        public event Action<(LandUsage oldUsageType, LandUsage newUsageType)> TileUsageChanged;

        private void OnTileUsageChanged((LandUsage oldUsageType, LandUsage newUsageType) usageChange) {
            if (usageChange.oldUsageType == usageChange.newUsageType) return;
            // if (usageChange.oldUsageType != LandUsage.None)
                PropagateOldTileUsageDistances(usageChange.oldUsageType);
            // if (usageChange.newUsageType != LandUsage.None)
                PropagateNewTileUsageDistances(usageChange.newUsageType);
            TileUsageChanged?.Invoke(usageChange);
        }

        private void PropagateOldTileUsageDistances(LandUsage oldUsageType) {
            // BFS until the distance is not going upwards again, then we found a tile which has another tile closer to it
            // There might be a more efficient way to do this.
            var edgeTiles = new HashSet<Tile>();
            var queue = new Queue<(Tile tile, int distance)>();
            queue.Enqueue((this, 0));
            while (queue.Count > 0) {
                var (tile, distance) = queue.Dequeue();
                Debug.Assert(distance > 0 || tile == this, "Only distance to self can be 0");
                if (tile._distancesToUsageType[oldUsageType] < distance) {
                    // Another tile of oldUsageType is closer to the current tile than this
                    // This is an edge tile from where we have to newly propagate.
                    edgeTiles.Add(tile);
                    continue;
                }

                if (tile._distancesToUsageType[oldUsageType] == MaxDist) {
                    // we already visited this tile
                    continue;
                }

                tile._distancesToUsageType[oldUsageType] = MaxDist;
                
                foreach (var neighbor in tile.GetNeighbors()) {
                    queue.Enqueue((neighbor, distance + 1));
                }
            }
            
            foreach (var edgeTile in edgeTiles) {
                edgeTile.PropagateNewTileUsageDistances(oldUsageType, initialDistance: edgeTile._distancesToUsageType[oldUsageType]);
            }
        }

        private void PropagateNewTileUsageDistances(LandUsage newUsageType, bool initialRun = false, int initialDistance = 0) {
            var queue = new Queue<(Tile tile, int distance)>();
            queue.Enqueue((this, initialDistance));
            while (queue.Count > 0) {
                var (tile, distance) = queue.Dequeue();
                if (tile._distancesToUsageType[newUsageType] <= distance) continue;
                Debug.Assert((tile.UsageType != newUsageType && tile._distancesToUsageType[newUsageType] > 0) || tile.UsageType == newUsageType, $"Tile {tile.Position} has a distance of {tile._distancesToUsageType[newUsageType]} to {newUsageType} but is of type {tile.UsageType}");
                tile._distancesToUsageType[newUsageType] = distance;
                foreach (var neighbor in tile.GetNeighbors()) {
                    if (initialRun && neighbor.UsageType == newUsageType) {
                        queue.Enqueue((neighbor, 0)); // This is for the initial initialisation of the whole map.
                        return;
                    }
                    queue.Enqueue((neighbor, distance + 1));
                }
            }
        }

        /// <summary>
        /// Used for initialization
        /// </summary>
        internal void InitializeCurrentTileUsageDistances() {
            PropagateNewTileUsageDistances(UsageType, true);
        }

        private const int MaxDist = 100_000;
        // TODO should distances be Manhattan or Euclidean? Currently Manhattan
        private readonly Dictionary<LandUsage, int> _distancesToUsageType = new() {
            {LandUsage.Residential, MaxDist},
            {LandUsage.Commercial, MaxDist},
            {LandUsage.Industrial, MaxDist},
            {LandUsage.Park, MaxDist},
            {LandUsage.Road, MaxDist},
            {LandUsage.Water, MaxDist},
            {LandUsage.None, MaxDist}
        };

        /// <summary>
        /// Returns the (precalculated) distance to the nearest tile of a given usage type.
        /// Currently in Manhattan distance.
        /// </summary>
        /// <param name="usageType"> The usage type to calculate the distance to </param>
        /// <returns> The distance to the nearest tile of the given usage type </returns>
        public int GetDistanceTo(LandUsage usageType) {
            Debug.Assert(
                _distancesToUsageType[usageType] == 0 && UsageType == usageType ||
                _distancesToUsageType[usageType] != 0 && UsageType != usageType,
                $"Tile {Position} has a distance of {_distancesToUsageType[usageType]} to {usageType} but is of type {UsageType}");
            return _distancesToUsageType[usageType];
        }

        public override string ToString() {
            return $"Tile {Position} ({UsageType})";
        }

        public bool IsParcelBoundary() {
            return MultiTileSite == null 
                ? GetNeighbors().Any(n => n.MultiTileSite != null) 
                : GetNeighbors().Any(n => n.MultiTileSite != null || n.MultiTileSite != MultiTileSite);
            // If this tile is part of a parcel, but the other tile is not, then only the other tile is a boundary tile.
        }
    }
    
    
    
    public class Attribute {
        private readonly Tile _tile;

        public Attribute(Tile tile) { 
            _tile = tile;
        }


        private float ElevationAdvantage => 0;
        private float VariationElevationNegative => 0;
        private float VariationElevationPositive => 0;
        private float FloodPlainElevation => 0;
        private float ProximityToPrimaryRoads => 0;
        private float ProximityToWater => Mathf.Pow(1 + _tile.GetDistanceTo(LandUsage.Water), -2);
        private float ProximityToMarket => 0;
        private float ResidentialDensity => 0;
        private float CommercialDensity => 0;
        private float IndustrialDensity => 0;
        private float CommercialClustering => 0;
        private float AntiWorth => 0;
        private float DistanceToPark => _tile.GetDistanceTo(LandUsage.Park);
        private float DistanceToCommercial => _tile.GetDistanceTo(LandUsage.Commercial);
        
        public float Calculate(LandUsage usage) {
            var weights = GetWeights(usage);
            var values = CalculateAttribute();
            Debug.Assert(weights.Count() == values.Count());
            return weights.Zip(values, (w, v) => w * v).Sum();
        }

        private List<float> CalculateAttribute() {
            var values = new List<float>();
            values.Add(ElevationAdvantage);
            values.Add(VariationElevationNegative);
            values.Add(VariationElevationPositive);
            values.Add(FloodPlainElevation);
            values.Add(ProximityToWater);
            values.Add(ResidentialDensity);
            values.Add(IndustrialDensity);
            values.Add(DistanceToPark);
            values.Add(ProximityToPrimaryRoads);
            values.Add(ProximityToMarket);
            values.Add(DistanceToCommercial);
            values.Add(AntiWorth);
            return values;
        }

        private List<float> GetWeights(LandUsage usage) {
            switch (usage) {
                case LandUsage.Residential:
                    return new List<float> { .3f, 0, 0, 0, .3f, .4f, 0, 0, 0, 0, 0, 0 };
                case LandUsage.Commercial:
                    return new List<float> { 0, .2f, 0, 0, .15f, .15f, 0, 0, 0, .4f, .1f, 0 };
                case LandUsage.Industrial:
                    return new List<float> { 0, .5f, 0, 0, .3f, 0, .1f, 0, .1f, 0, 0, 0 };
                case LandUsage.Park:
                    throw new NotImplementedException();
                case LandUsage.Road:
                    Debug.LogWarning("Roads should not have attributes");
                    return new List<float> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                case LandUsage.Water:
                    Debug.LogWarning("Water should not have attributes");
                    return new List<float> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                case LandUsage.None:
                    Debug.LogWarning("None should not have attributes");
                    return new List<float> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                default:
                    throw new ArgumentOutOfRangeException();
            };
        }
    }

}