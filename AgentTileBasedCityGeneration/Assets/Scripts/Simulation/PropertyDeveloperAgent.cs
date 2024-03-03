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


        public PropertyDeveloperAgent(LandUsage type, RangeInt sizeRange, Tile startTile) : base(type, startTile) {
            _sizeRange = sizeRange;
        }

        protected internal override void UpdateTick() {
            Debug.Assert(UsageType is LandUsage.Commercial or LandUsage.Industrial or LandUsage.Residential or LandUsage.Park);
            Prospect(DevSites);
            foreach (var devSite in DevSites) {
                var newDev = Build(devSite);
                if (Profitable(newDev, devSite)) {
                    Commit(newDev, devSite);
                }
            }
        }

        private void Prospect(List<ISite> devSites) {
            Debug.Assert(!devSites.Exists(site => site is null or Tile { MultiTileSite: null, IsRoadAdjacent: false }), $"devSites: {string.Join(", ", devSites)}");
            if (devSites.Count > 0) { // TODO check for recent relocation or commit
                // move locally
                CurrTile = devSites.OrderBy(site => site.CalcValue()).First().CorrespondingTile;
            }
            else {
                // move globally
                var allDevelopmentSites = World.AllTiles
                    .Where(IsDevelopableSite);
                if (!allDevelopmentSites.Any()) {
                    Debug.Log($"No developable sites found for {this}");
                    return;
                }
                var allDevSitesOrdered = allDevelopmentSites.OrderBy(site => site.CalcValue()).ToList();
                Debug.Assert(allDevSitesOrdered.Count > 0, "No developable sites found");
                CurrTile = allDevSitesOrdered[UnityEngine.Random.Range(0, allDevSitesOrdered.Count)];
                DevTiles = new List<Tile>();
            }
            DevSites = ((ISite)CurrTile).GetSitesInCircle(5)
                .Where(IsDevelopableSite)
                .ToList();
            var allDevTiles = DevSites.OfType<Tile>().ToList();
            DevTiles = allDevTiles.OrderBy(tile => tile.CalcValue()).Take((int)(allDevTiles.Count / 10f * 9)).Union(DevTiles).ToList();
        }

        [CanBeNull]
        private Parcel Build(ISite devSite) {
            switch (devSite) {
                case Tile tile: {
                    // Build a new parcel
                    Debug.Assert(tile.UsageType == LandUsage.None && tile.MultiTileSite == null);
                    // TODO bigger size
                    var newParcel = new Parcel(tile.World, UsageType, new List<Tile> {tile}, 0, tile.World.Tick);
                    return newParcel;
                }
                // Expand or convert the parcel
                case Parcel parcel: //when parcel.UsageType == _type:
                    var copy = new Parcel(parcel);
                    if (parcel.UsageType == UsageType) {
                        copy.Population += 1;
                        return copy;
                    }
                    copy.UsageType = UsageType;
                    return copy;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool Profitable([CanBeNull] Parcel newDev, ISite oldDev) {
            if (newDev == null) return false;
            if (oldDev is not Parcel oldParcel) return true;
            var isProfitable = newDev.CalcValue() / oldParcel.CalcValue() >= 1 + ProfitabilityNeeded;
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
            return site is Parcel parcel && Parcel.ConvertibleTo(UsageType).Contains(parcel.UsageType) ||
                   site is Tile && (site as Tile).UsageType == LandUsage.None && (site as Tile).IsRoadAdjacent;
        }
        
        
        
    }
}