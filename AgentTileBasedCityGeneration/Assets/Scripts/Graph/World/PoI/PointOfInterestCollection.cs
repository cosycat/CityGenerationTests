#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Graph.World.PoI {
    public class PointOfInterestCollection : IPointOfInterestCollection {
        public readonly IDictionary<IStreetNode, IPointOfInterest> NodePOIMapping =
            new Dictionary<IStreetNode, IPointOfInterest>();

        public readonly IDictionary<IPointOfInterest, List<IStreetNode>> POINodeMapping =
            new Dictionary<IPointOfInterest, List<IStreetNode>>();

        public List<IPointOfInterest> PointsOfInterest { get; } = new();

        public void AddPointOfInterest(IPointOfInterest pointOfInterest) {
            PointsOfInterest.Add(pointOfInterest);
            OnPointOfInterestAdded(pointOfInterest);
        }

        public void RemovePointOfInterest(IPointOfInterest pointOfInterest) {
            PointsOfInterest.Remove(pointOfInterest);
            OnPointOfInterestRemoved(pointOfInterest);
        }

        public IPointOfInterest? FindClosestPointOfInterest(Vector2 point) {
            IPointOfInterest? closestPoint = null;
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

        public event EventHandler<PointOfInterestEventArgs>? PointOfInterestAddedEvent;
        public event EventHandler<PointOfInterestEventArgs>? PointOfInterestRemovedEvent;

        public bool GetPointOfInterestFromNode(IStreetNode node, out IPointOfInterest? pointOfInterest) {
            return NodePOIMapping.TryGetValue(node, out pointOfInterest);
        }

        public IReadOnlyList<IStreetNode> GetNodesFromPointOfInterest(IPointOfInterest pointOfInterest) {
            return POINodeMapping.TryGetValue(pointOfInterest, out var nodesFromPOI)
                ? nodesFromPOI
                : new List<IStreetNode>();
        }

        public void AddNodeRelationToPointOfInterest(IStreetNode node, IPointOfInterest pointOfInterest) {
            NodePOIMapping[node] = pointOfInterest;

            if (!POINodeMapping.TryGetValue(pointOfInterest, out var nodes)) {
                nodes = new List<IStreetNode>();
                POINodeMapping[pointOfInterest] = nodes;
            }

            Debug.Assert(!nodes.Contains(node));
            nodes.Add(node);
        }

        protected virtual void OnPointOfInterestAdded(IPointOfInterest poi) {
            PointOfInterestAddedEvent?.Invoke(this, new PointOfInterestEventArgs(poi));
        }

        protected virtual void OnPointOfInterestRemoved(IPointOfInterest poi) {
            PointOfInterestRemovedEvent?.Invoke(this, new PointOfInterestEventArgs(poi));
        }
    }
}