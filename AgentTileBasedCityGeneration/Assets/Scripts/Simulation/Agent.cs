using System;

namespace Simulation {
    public abstract class Agent {
        
        public World World => CurrSite.World;
        
        private ISite _currSite; // TODO this should probably just be a tile
        public ISite CurrSite {
            get => _currSite;
            protected set {
                _currSite = value;
                OnSiteChanged(value);
            }
        }
        public Tile CurrTile {
            get => CurrSite.CorrespondingTile;
            set => CurrSite = value;
        }

        public LandUsage UsageType { get; }

        
        protected Agent(LandUsage usageType, ISite currSite) {
            UsageType = usageType;
            CurrSite = currSite;
        }

        
        protected internal abstract void UpdateTick();

        public event Action<ISite> SiteChanged;
        
        protected virtual void OnSiteChanged(ISite obj) {
            SiteChanged?.Invoke(obj);
        }
        
    }
    
}