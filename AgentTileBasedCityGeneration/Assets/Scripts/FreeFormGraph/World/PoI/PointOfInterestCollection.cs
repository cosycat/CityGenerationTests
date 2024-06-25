using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    public class PointOfInterestCollection : IPointOfInterestCollection {
        public List<IPointOfInterest> PointsOfInterest { get; } = new();

        public IDictionary<IStreetNode, IPointOfInterest> NodePOIMapping = new Dictionary<IStreetNode, IPointOfInterest>();

        public void AddPointOfInterest(IPointOfInterest pointOfInterest) {
            PointsOfInterest.Add(pointOfInterest);
            OnPointOfInterestAdded(pointOfInterest);
        }

        public void RemovePointOfInterest(IPointOfInterest pointOfInterest) {
            PointsOfInterest.Remove(pointOfInterest);
            OnPointOfInterestRemoved(pointOfInterest);
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

        public event EventHandler<PointOfInterestEventArgs> PointOfInterestAddedEvent;
        public event EventHandler<PointOfInterestEventArgs> PointOfInterestRemovedEvent;

        protected virtual void OnPointOfInterestAdded(IPointOfInterest poi) {
            PointOfInterestAddedEvent?.Invoke(this, new PointOfInterestEventArgs(poi));
        }

        protected virtual void OnPointOfInterestRemoved(IPointOfInterest poi) {
            PointOfInterestRemovedEvent?.Invoke(this, new PointOfInterestEventArgs(poi));
        }

        public bool GetPointOfInterestFromNode(IStreetNode node, out IPointOfInterest? pointOfInterest)
        {
            pointOfInterest = null;
            if(!NodePOIMapping.ContainsKey(node)) return false;
            pointOfInterest = NodePOIMapping[node];
            return true;
        }

        public void AddNodeRelationToPointOfInterest(IStreetNode node, IPointOfInterest pointOfInterest)
        {
            NodePOIMapping[node] = pointOfInterest;
        }
    }
}