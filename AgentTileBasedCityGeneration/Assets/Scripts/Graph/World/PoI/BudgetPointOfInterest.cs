#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graph.World.PoI {
    /// <summary>
    ///     A BudgetPointOfInterest is a POI with a certain budget, which can be used to build a settlement.
    ///     The Budget can grow over time, and is used to determine the size of the settlement.
    /// </summary>
    public class BudgetPointOfInterest : IPointOfInterest {
        private Color? gizmoColor;

        public ICollection<IStreetNode> Nodes = new List<IStreetNode>();

        public BudgetPointOfInterest(Vector2 position, float budget) {
            Position = position;
            Budget = budget;
        }

        public float Budget { get; set; }
        public Vector2 Position { get; }

        public void DebugVisualize(IWorld world) {
            var nodes = world.PointsOfInterest.GetNodesFromPointOfInterest(this);
            if (nodes.Count == 0) return;

            if (gizmoColor == null)
                gizmoColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1f);
            Gizmos.color = (Color)gizmoColor;
            var distanceMax = nodes.Max(n => Vector2.Distance(n.Position, Position));
            Gizmos.DrawWireSphere(Position, distanceMax);
            for (var i = 0; i < nodes.Count; i++) Gizmos.DrawSphere(nodes[i].Position, 1f);
        }

        public override string ToString() {
            return $"POI at {Position} with budget left: {Budget}";
        }
    }
}