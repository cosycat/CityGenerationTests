using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Simulation {
    public class PropertyDeveloperAgent : Agent {
        private const float ProfitabilityNeeded = 0.1f;
        
        private List<ISite> DevSites { get; set; } = new();
        private List<Tile> DevTiles { get; set; } = new();
        private readonly RangeInt _sizeRange;
        
        private int _ticksSinceCreatedNewSite = 0;
        private int _ticksSinceRelocated = 0;
        /// <summary>
        /// How many ticks without any newly created sites before relocating to a new area
        /// </summary>
        private int TicksUntilRelocate { get; } = 10;


        public PropertyDeveloperAgent(LandUsage type, RangeInt sizeRange, Tile startTile) : base(type, startTile) {
            _sizeRange = sizeRange;
        }

        protected internal override void UpdateTick() {
            _ticksSinceCreatedNewSite++;
            Debug.Assert(AgentUsageType is LandUsage.Commercial or LandUsage.Industrial or LandUsage.Residential or LandUsage.Park);
            Prospect(DevSites);
            foreach (var devSite in DevSites) {
                var (newDev, buildType) = Build(devSite);
                if (Profitable(newDev, devSite)) {
                    Commit(newDev, devSite);
                    if (buildType == BuildType.New) {
                        _ticksSinceCreatedNewSite = 0;
                    }
                }
            }
        }

        private void Prospect(List<ISite> devSites) {
            Debug.Assert(!devSites.Exists(site => site is null or Tile { MultiTileSite: null, IsRoadAdjacent: false }), $"devSites: {string.Join(", ", devSites)}");
            if (devSites.Count > 0 && _ticksSinceCreatedNewSite < TicksUntilRelocate) {
                // move locally
                CurrTile = devSites.OrderBy(site => site.CalcValueForType(AgentUsageType)).First().CorrespondingTile;
            }
            else {
                // move globally
                var allDevelopmentSites = World.AllTiles
                    .Where(IsDevelopableSite);
                if (!allDevelopmentSites.Any()) {
                    // Debug.Log($"No developable sites found for {this}");
                    return;
                }
                var allDevSitesOrdered = allDevelopmentSites.OrderBy(site => site.CalcValueForType(AgentUsageType)).ToList();
                if (allDevSitesOrdered.Count == 0) {
                    Debug.LogWarning($"No developable sites to relocate found for {this}");
                    return;
                }
                Debug.Assert(allDevSitesOrdered.Count > 0, "No developable sites found");
                CurrTile = allDevSitesOrdered[UnityEngine.Random.Range(0, allDevSitesOrdered.Count)];
                DevTiles = new List<Tile>();
                _ticksSinceRelocated = 0;
                _ticksSinceCreatedNewSite = 0;
            }
            // We take the best 90% of the tiles and add them to the list of tiles to develop
            DevSites = ((ISite)CurrTile).GetSitesInCircle(5)
                .Where(IsDevelopableSite)
                .ToList();
            var allDevTiles = DevSites.OfType<Tile>().ToList(); 
            DevTiles = allDevTiles.OrderBy(tile => tile.CalcValueForType(AgentUsageType)).Take((int)(allDevTiles.Count / 10f * 9)).Union(DevTiles).ToList();
        }

        private enum BuildType {
            AddPopulation,
            Convert,
            New,
            FailedBuild
        }
        
        private (Parcel builtParcel, BuildType buildType) Build(ISite devSite) {
            switch (devSite) {
                case Tile tile: {
                    // Build a new parcel
                    Debug.Assert(tile.UsageType == LandUsage.None && tile.MultiTileSite == null);
                    // TODO bigger size
                    var newParcel = new Parcel(tile.World, AgentUsageType, new List<Tile> {tile}, 0, tile.World.Tick);
                    return (newParcel, BuildType.New);
                }
                // TODO don't copy the parcel, just change the usage type or population directly in here. Copying just adds potential bugs.
                // Expand or convert the parcel
                case Parcel parcel: //when parcel.UsageType == _type:
                    var copy = new Parcel(parcel);
                    if (parcel.UsageType == AgentUsageType) {
                        copy.Population += 1;
                        return (copy, BuildType.AddPopulation);
                    }
                    copy.UsageType = AgentUsageType;
                    return (copy, BuildType.Convert);
                default:
                    throw new NotImplementedException();
            }
        }

        private bool Profitable([CanBeNull] Parcel newDev, ISite oldDev) {
            if (newDev == null) return false;
            if (oldDev is not Parcel oldParcel) return true;
            var isProfitable = newDev.CalcValueForType(AgentUsageType) / oldParcel.CalcValueForType(AgentUsageType) >= 1 + ProfitabilityNeeded;
            // TODO better implementation
            return isProfitable;
        }

        private static void Commit(Parcel newDev, ISite oldDev) {
            Debug.Assert(oldDev is Tile || (oldDev is Parcel oldSiteParcel && oldSiteParcel.Tiles.SequenceEqual(newDev.Tiles)));
            switch (oldDev) {
                case Tile: {
                    // Just add the new parcel to the world
                    foreach (var newDevTile in newDev.Tiles) {
                        newDevTile.MultiTileSite = newDev;
                    }
                    break;
                }
                case Parcel parcel:
                    // Update the existing parcel
                    parcel.Population = newDev.Population;
                    parcel.UsageType = newDev.UsageType;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool IsDevelopableSite(ISite site) {
            return site is Parcel parcel && MultiTileSite.ConvertibleTo(AgentUsageType).Contains(parcel.UsageType) ||
                   site is Tile { UsageType: LandUsage.None, IsRoadAdjacent: true };
        }
        
        
        
    }
}