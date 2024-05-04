using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    public class PointOfInterestCollection : IPointOfInterestCollection {
        public List<IPointOfInterest> PointsOfInterest { get; } = new();


        public void AddPointOfInterest(IPointOfInterest pointOfInterest) {
            PointsOfInterest.Add(pointOfInterest);
        }

        public void RemovePointOfInterest(IPointOfInterest pointOfInterest) {
            PointsOfInterest.Remove(pointOfInterest);
        }

        [CanBeNull]
        public IPointOfInterest FindClosestPointOfInterest(Vector2 point) {
            IPointOfInterest closestPoint = null;
            var closestDistance = float.MaxValue;
            foreach (var pointOfInterest in PointsOfInterest) {
                var distance = Vector2.Distance(pointOfInterest.Position, point);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestPoint = pointOfInterest;
                }
            }

            return closestPoint;
        }

        public bool IsPointWithinAPointOfInterest(Vector2 point, out List<IPointOfInterest> pointsOfInterest) {
            pointsOfInterest = new List<IPointOfInterest>();
            foreach (var pointOfInterest in PointsOfInterest) {
                if (pointOfInterest.IsPointWithinRange(point)) {
                    pointsOfInterest.Add(pointOfInterest);
                }
            }

            return pointsOfInterest.Count > 0;
        }
    }
}