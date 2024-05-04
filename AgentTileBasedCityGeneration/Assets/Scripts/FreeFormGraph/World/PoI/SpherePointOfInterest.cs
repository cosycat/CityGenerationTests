using UnityEngine;

namespace FreeFormGraph.World.PoI {
    
    public class SpherePointOfInterest : IPointOfInterest {
        
        public Vector2 Position { get; }
        public PointOfInterestType Type { get; }

        public float Radius { get; }
        
        public SpherePointOfInterest(Vector2 position, PointOfInterestType type, float radius) {
            Position = position;
            Type = type;
            Radius = radius;
        }
        
        public bool IsPointWithinRange(Vector2 point) {
            return Vector2.Distance(Position, point) <= Radius;
        }

        public override string ToString() {
            return $"Sphere POI (Position: {Position}, Type: {Type}, Radius: {Radius})";
        }
    }
}