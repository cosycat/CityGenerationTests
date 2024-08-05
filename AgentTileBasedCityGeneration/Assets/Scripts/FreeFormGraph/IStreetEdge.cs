using UnityEngine;

namespace FreeFormGraph {
    public interface IStreetEdge {
        /// <summary>
        /// The position of the first node of the street.
        /// </summary>
        public Vector3 PositionNodeA => NodeA.Position;

        /// <summary>
        /// The position of the second node of the street.
        /// </summary>
        public Vector3 PositionNodeB => NodeB.Position;

        /// <summary>
        /// The first node of the street.
        /// </summary>
        public IStreetNode NodeA { get; }

        /// <summary>
        /// The second node of the street.
        /// </summary>
        public IStreetNode NodeB { get; }

        /// <summary>
        /// The street width measured from the center of the street to the edge of the street.
        /// </summary>
        public float StreetWidth { get; }

        /// <summary>
        /// The type of the road.
        /// </summary>
        public RoadType Type { get; set; }

        /// <summary>
        /// Returns a string representation of the edge for debugging purposes.
        /// </summary>
        /// <returns> A debug string representation of the edge. </returns>
        public string DebugString() {
            return $"Edge from {NodeA}\nto {NodeB}";
        }

        /// <summary>
        /// Splits the edge into evenly spaced points.
        ///
        /// The last point is always the position of <see cref="NodeB"/> and the first point is always the position of <see cref="NodeA"/>.
        /// The last point disregards the step size.
        /// The number of points is always >= 2
        /// The tangents are the tangents of the points in the direction from <see cref="NodeA"/> to <see cref="NodeB"/>.
        /// </summary>
        /// <param name="tangents"> The tangents of the points in the direction from <see cref="NodeA"/> to <see cref="NodeB"/>. </param>
        /// <param name="stepSize"> The distance between the points. </param>
        /// <returns> The evenly spaced points. </returns>
        Vector3[] SplitIntoEvenlySpacedPoints(out Vector3[] tangents, float stepSize = 0.1f);

        /// <summary>
        /// Returns the distance from the given position to the edge.
        /// </summary>
        /// <param name="position"> The position to measure the distance from. </param>
        /// <param name="positionOnEdge"> The position on the edge that is closest to the given position. </param>
        /// <returns> The distance from the position to the edge. </returns>
        public float GetDistanceEdgeToPosition(Vector3 position, out Vector3 positionOnEdge);

        /// <summary>
        /// Returns the length of this street segment which can be different depending on the geometry used.
        /// </summary>
        /// <returns> Length of this road segment. </returns>
        public float Length();
    }
}