using System;

namespace Simulation {
    public abstract class Agent {
        
        public World World => CurrTile.World;
        
        private Tile _currTile;
        public Tile CurrTile {
            get => _currTile;
            set {
                _currTile = value;
                OnSiteChanged(value);
            }
        }

        public LandUsage AgentUsageType { get; }

        
        protected Agent(LandUsage agentUsageType, Tile currTile) {
            AgentUsageType = agentUsageType;
            CurrTile = currTile;
        }

        
        protected internal abstract void UpdateTick();

        public event Action<Tile> SiteChanged;
        
        protected virtual void OnSiteChanged(Tile obj) {
            SiteChanged?.Invoke(obj);
        }

        public override string ToString() {
            return $"Agent ({AgentUsageType}) at {CurrTile}";
        }
    }
    
}