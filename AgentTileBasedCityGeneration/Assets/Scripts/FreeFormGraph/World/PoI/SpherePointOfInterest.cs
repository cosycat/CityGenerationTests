using UnityEngine;

namespace FreeFormGraph.World.PoI {
    
    public class SpherePointOfInterest: IPointOfInterest {
        
        public Vector2 Position { get; }


        public float Budget {get; set;}
        public float Radius {get; set;} = 20f;

        public SpherePointOfInterest(Vector2 position, float budget) {
            Position = position;
            Budget = budget;
        }

        public IStreetNode[] FindAllNodes(IWorld world) {
            return world.StreetGraph.FindAllNodesWithinRange(Position, Radius);
        }

        public override string ToString() {
            return $"POI at {Position} with budget left: {Budget}";
        }

        public bool IsPointWithinRange(Vector2 point)
        {
            return Vector2.Distance(Position, point) <= Radius;
        }
    }
}