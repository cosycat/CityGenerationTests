using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    public class PointOfInterestCollection : IPointOfInterestCollection {
        public List<IPointOfInterest> PointsOfInterest { get; } = new();

        public IDictionary<IStreetNode, IPointOfInterest> NodePOIMapping = new Dictionary<IStreetNode, IPointOfInterest>();
        public IDictionary<IPointOfInterest, List<IStreetNode>> POINodeMapping = new Dictionary<IPointOfInterest, List<IStreetNode>>();

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

        public IReadOnlyList<IStreetNode> GetNodesFromPointOfIntereset(IPointOfInterest pointOfInterest) {
            return POINodeMapping.ContainsKey(pointOfInterest) ? POINodeMapping[pointOfInterest] : new();
        }

        public void AddNodeRelationToPointOfInterest(IStreetNode node, IPointOfInterest pointOfInterest)
        {
            NodePOIMapping[node] = pointOfInterest;

            List<IStreetNode> nodes;
            if(!POINodeMapping.ContainsKey(pointOfInterest)) {
                nodes = new();
                POINodeMapping[pointOfInterest] = nodes;
            } else {
                nodes = POINodeMapping[pointOfInterest];
            }
            Debug.Assert(!nodes.Contains(node));
            nodes.Add(node);
        }
    }
}