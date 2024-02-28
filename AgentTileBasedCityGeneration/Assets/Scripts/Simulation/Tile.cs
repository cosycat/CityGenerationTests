using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Simulation {

    /// <summary>
    /// A tile is the smallest unit of land in the simulation. It can be combined with other tiles to form a parcel by a property developer.
    /// </summary>
    public sealed class Tile : ISite {
        [CanBeNull] private Parcel _parcel;

        [CanBeNull]
        public Parcel Parcel {
            get => _parcel;
            internal set {
                _parcel = value;
                OnTileChanged(this);
            }
        }

        public World World { get; }

        public Vector2Int Position { get; }

        public LandUsage UsageType => IsWater ? LandUsage.Water :
            Parcel?.UsageType ?? LandUsage.None;

        public bool IsWater { get; }
        public bool IsRoadAdjacent => GetNeighbors().Exists(tile => tile.UsageType == LandUsage.Road);

        public Tile(World world, bool isWater, Vector2Int position) {
            World = world; 
            Position = position;
            IsWater = isWater;
        }

        private List<Tile> GetNeighbors(bool includeDiagonals = false) {
            var neighbors = new List<Tile>();
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    if (x == 0 && y == 0) continue;
                    if (includeDiagonals && x != 0 && y != 0) continue;
                    if (World.TryGetTile(Position + new Vector2Int(x, y), out var tile)) {
                        neighbors.Add(tile);
                    }
                }
            }
            return neighbors;
            
        }

        public float CalcValue() {
            var attributeValue = new Attribute(this).Calculate();
            return attributeValue;
        }


        public event Action<Tile> TileChanged;

        private void OnTileChanged(Tile obj) {
            TileChanged?.Invoke(obj);
        }

        public float CalcDistanceTo(LandUsage usageType) {
            if (this.UsageType == usageType) return 0;
            if (World.AllTiles.All(tile => tile.UsageType != usageType))
                return 100000;
            return World.AllTiles
                .Where(tile => tile.UsageType == usageType)
                .Min(tile => Vector2Int.Distance(Position, tile.Position));
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
        private float ProximityToWater => Mathf.Pow(1 + _tile.CalcDistanceTo(LandUsage.Water), -2);
        private float ProximityToMarket => 0;
        private float ResidentialDensity => 0;
        private float CommercialDensity => 0;
        private float IndustrialDensity => 0;
        private float CommercialClustering => 0;
        private float AntiWorth => 0;
        private float DistanceToPark => _tile.CalcDistanceTo(LandUsage.Park);
        private float DistanceToCommercial => _tile.CalcDistanceTo(LandUsage.Commercial);
        
        public float Calculate() {
            var weights = GetWeights();
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

        private List<float> GetWeights() {
            switch (_tile.UsageType) {
                case LandUsage.Residential:
                    return new List<float> { .3f, 0, 0, 0, .3f, .4f, 0, 0, 0, 0, 0, 0 };
                case LandUsage.Commercial:
                    return new List<float> { 0, .2f, 0, 0, .15f, .15f, 0, 0, 0, .4f, .1f, 0 };
                case LandUsage.Industrial:
                    return new List<float> { 0, .5f, 0, 0, .3f, 0, .1f, 0, .1f, 0, 0, 0 };
                case LandUsage.Park:
                    throw new NotImplementedException();
                case LandUsage.Road:
                    throw new NotImplementedException();
                case LandUsage.Water:
                    return new List<float> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                case LandUsage.None:
                    return new List<float> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                default:
                    throw new ArgumentOutOfRangeException();
            };
        }
    }

}