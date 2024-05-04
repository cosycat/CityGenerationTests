
using System;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.Agents {

    public class PointOfInterestAgent {
        
        public IWorld World { get; set; }
        
        public PointOfInterestAgent(IWorld world) {
            World = world;
        }
        
        public IPointOfInterest CreateNewPointOfInterest() {
            // for now just randomly create a spherical point of interest
            var type = (PointOfInterestType)UnityEngine.Random.Range(0, 3);
            var radiusRange = GetRadiusForPointOfInterestType(type);
            var radius = UnityEngine.Random.Range(radiusRange.min, radiusRange.max);
            if (radius > World.Width / 2f) {
                radius = World.Width / 2f - 0.1f;
            }
            var x = UnityEngine.Random.Range(radius, World.Width - radius);
            var y = UnityEngine.Random.Range(radius, World.Height - radius);
            
            var pointOfInterest = new SpherePointOfInterest(new UnityEngine.Vector2(x, y), type, radius);
            return pointOfInterest;
        }
        
        /// <summary>
        /// Returns the min and max radius for a point of interest of the given type.
        /// </summary>
        /// <param name="type"> The type of the point of interest. </param>
        /// <returns> The min and max radius for the point of interest. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the type is not a valid PointOfInterestType value. </exception>
        public static (float min, float max) GetRadiusForPointOfInterestType(PointOfInterestType type) {
            // TODO this should be moved to a more appropriate place
            return type switch {
                PointOfInterestType.Village => (10, 20),
                PointOfInterestType.Town => (20, 40),
                PointOfInterestType.City => (40, 60),
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, "Invalid PointOfInterestType value.")
            };
        }
        
        
    }
    
}