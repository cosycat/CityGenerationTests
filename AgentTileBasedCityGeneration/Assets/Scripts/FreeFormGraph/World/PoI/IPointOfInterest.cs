using UnityEngine;

namespace FreeFormGraph.World.PoI {
    
    public interface IPointOfInterest {
        
        public Vector2 Position { get; }
        public PointOfInterestType Type { get; }
        
        public bool IsPointWithinRange(Vector2 point);
        
    }
    
    public enum PointOfInterestType {
        Village,
        Town,
        City,
    }
}