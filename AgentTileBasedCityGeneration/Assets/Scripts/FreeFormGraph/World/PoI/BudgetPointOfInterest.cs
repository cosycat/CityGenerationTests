#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    
    public class BudgetPointOfInterest: IPointOfInterest {
        
        public Vector2 Position { get; }

        public float Budget {get; set;}

        public ICollection<IStreetNode> Nodes = new List<IStreetNode>();

        private Color? gizmoColor;

        public BudgetPointOfInterest(Vector2 position, float budget) {
            Position = position;
            Budget = budget;
        }

        public override string ToString() {
            return $"POI at {Position} with budget left: {Budget}";
        }

        public void DebugVisualize(IWorld world) {
            var nodes = world.PointsOfInterest.GetNodesFromPointOfIntereset(this);
            if(nodes.Count == 0) return;

            if(gizmoColor == null) gizmoColor = new Color(Random.Range(0,1f), Random.Range(0,1f), Random.Range(0,1f), 1f);
            Gizmos.color = (Color)gizmoColor;
            var distanceMax = nodes.Max(n => Vector2.Distance(n.Position, Position));
            Gizmos.DrawWireSphere(Position, distanceMax);
            for(int i = 0; i < nodes.Count; i++) {
                Gizmos.DrawSphere(nodes[i].Position, 1f);
            }
        }
    }
}