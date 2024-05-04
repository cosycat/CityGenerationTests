using System.Collections.Generic;
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    public interface IPointOfInterestCollection {
        public List<IPointOfInterest> PointsOfInterest { get; }
        public void AddPointOfInterest(IPointOfInterest pointOfInterest);
        public void RemovePointOfInterest(IPointOfInterest pointOfInterest);
        public IPointOfInterest FindClosestPointOfInterest(Vector2 point);
        public bool IsPointWithinAPointOfInterest(Vector2 point, out List<IPointOfInterest> pointsOfInterest);
    }
}