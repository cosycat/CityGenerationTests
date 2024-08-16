#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graph.World.PoI {
    /// <summary>
    ///     Handles a collection of <see cref="IPointOfInterest" />.
    ///     Useful, so that not every world has to handle its own collection of points of interest, but can simply use this
    ///     class to handle it.
    /// </summary>
    public interface IPointOfInterestCollection {
        public List<IPointOfInterest> PointsOfInterest { get; }
        public int Count => PointsOfInterest.Count;
        public void AddPointOfInterest(IPointOfInterest pointOfInterest);
        public void RemovePointOfInterest(IPointOfInterest pointOfInterest);
        public IPointOfInterest? FindClosestPointOfInterest(Vector2 point);

        public bool GetPointOfInterestFromNode(IStreetNode node, out IPointOfInterest? pointOfInterest);
        public IReadOnlyList<IStreetNode> GetNodesFromPointOfInterest(IPointOfInterest pointOfInterest);
        public void AddNodeRelationToPointOfInterest(IStreetNode node, IPointOfInterest pointOfInterest);

        public event EventHandler<PointOfInterestEventArgs> PointOfInterestAddedEvent;
        public event EventHandler<PointOfInterestEventArgs> PointOfInterestRemovedEvent;
    }

    public class PointOfInterestEventArgs : EventArgs {
        public PointOfInterestEventArgs(IPointOfInterest pointOfInterest) {
            PointOfInterest = pointOfInterest;
        }

        public IPointOfInterest PointOfInterest { get; }
    }
}