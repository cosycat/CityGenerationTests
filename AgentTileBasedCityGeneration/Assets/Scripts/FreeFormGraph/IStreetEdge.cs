using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FreeFormGraph {
    public interface IStreetEdge {
        
        /// <summary>
        /// The position of the first node of the street.
        /// </summary>
        public Vector3 PosA => NodeA.Position;
        /// <summary>
        /// The position of the second node of the street.
        /// </summary>
        public Vector3 PosB => NodeB.Position;

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
        /// Returns a string representation of the edge for debugging purposes.
        /// </summary>
        /// <returns> A debug string representation of the edge. </returns>
        public string DebugString() {
            return $"Edge from {NodeA}\nto {NodeB}";
        }

        Vector3[] SplitIntoEvenlySpacedPoints(float stepSize = 0.1f);
    }
}