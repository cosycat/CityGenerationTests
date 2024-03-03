using UnityEngine;

namespace Simulation {
    
    public class AgentRepresentation : Representation<Agent> {
        private Agent Agent { get; set; }

        protected override void OnInitialize(Agent agent) {
            Agent = agent;
            SpriteRenderer.color = Agent.AgentUsageType switch {
                LandUsage.None => new Color(0.11f, 0.62f, 0.1f),
                LandUsage.Road => Color.gray,
                LandUsage.Residential => Color.yellow,
                LandUsage.Commercial => Color.red,
                LandUsage.Industrial => Color.blue,
                LandUsage.Park => Color.green,
                LandUsage.Water => new Color(0.38f, 0.67f, 0.84f),
                _ => throw new System.ArgumentOutOfRangeException()
            };
            agent.SiteChanged += OnAgentPositionChanged;
            OnAgentPositionChanged(agent.CurrTile);
        }

        private void OnAgentPositionChanged(Tile newSite) {
            transform.position = new Vector3(newSite.Position.x, newSite.Position.y);
        }
        
    }
}