using UnityEngine;

namespace Simulation {
    
    [RequireComponent(typeof(SpriteRenderer))]
    public class AgentRepresentation : MonoBehaviour {
        private Agent Agent { get; set; }
        private SpriteRenderer SpriteRenderer { get; set; }

        public void Initialize(Agent agent) {
            Agent = agent;
            SpriteRenderer = GetComponent<SpriteRenderer>();
            SpriteRenderer.color = Agent.UsageType switch {
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
            OnAgentPositionChanged(agent.CurrSite);
        }

        private void OnAgentPositionChanged(ISite newSite) {
            transform.position = new Vector3(newSite.Position.x, newSite.Position.y);
        }
        
    }
}