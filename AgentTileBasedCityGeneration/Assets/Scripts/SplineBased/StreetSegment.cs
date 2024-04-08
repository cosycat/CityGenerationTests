using System;
using System.Collections.Generic;
using System.Linq;
using FreeFormGraph;
using UnityEngine;
using UnityEngine.Splines;

namespace SplineBased {

    public class StreetSegment : IStreetEdge {
        
        private readonly StreetNode _nodeA;
        private readonly StreetNode _nodeB;
        
        public IStreetNode NodeA => _nodeA;
        public IStreetNode NodeB => _nodeB;

        public float StreetWidth { get; } = 0.3f;

        private StreetSegment(StreetNode nodeA, StreetNode nodeB) {
            _nodeA = nodeA;
            _nodeB = nodeB;
        }

        /// <summary>
        /// Generates a new StreetSegment between two nodes and adds it to the nodes.
        ///
        /// Does not change splines, this has to be done manually before or after calling this method.
        /// (If done before, make sure to remove it again if this method returns false)
        /// </summary>
        /// <param name="from"> The first node </param>
        /// <param name="to"> The second node </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <returns> True if the segment was added successfully, false otherwise </returns>
        internal static bool GenerateStreetSegment(StreetNode from, StreetNode to, out StreetSegment newSegment) {
            newSegment = new StreetSegment(from, to);
            if (!from.AddSegment(newSegment) || !to.AddSegment(newSegment)) {
                from.Edges.Remove(newSegment);
                to.Edges.Remove(newSegment); // probably not needed, but to be sure
                return false;
            }

            return true;
        }

        public override string ToString() {
            return $"Segment from {NodeA.Position} to {NodeB.Position}";
        }
    }
}