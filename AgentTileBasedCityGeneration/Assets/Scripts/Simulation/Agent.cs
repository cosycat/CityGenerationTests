using System;

namespace Simulation {
    public abstract class Agent {
        private ISite _currSite;
        public ISite CurrSite {
            get => _currSite;
            protected set {
                _currSite = value;
                OnSiteChanged(value);
            }
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

    public enum RoadType {
        Primary,
        Tertiary,
    }

    public class RoadSegment {
        
    }
}