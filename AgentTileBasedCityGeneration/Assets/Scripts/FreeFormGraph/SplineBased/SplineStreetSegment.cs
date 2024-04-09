using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Splines;

namespace FreeFormGraph.SplineBased {

    public class SplineStreetSegment : IStreetEdge {
        
        private readonly SplineStreetNode _nodeA;
        private readonly SplineStreetNode _nodeB;
        
        public IStreetNode NodeA => _nodeA;
        public IStreetNode NodeB => _nodeB;
        
        public Spline Spline { get; internal set; }

        public float StreetWidth { get; } = 0.3f;
        public IEnumerable<Vector3> SplitIntoPoints(float stepSize = 0.1f) {
            throw new System.NotImplementedException();
        }

        private SplineStreetSegment(SplineStreetNode nodeA, SplineStreetNode nodeB) {
            _nodeA = nodeA;
            _nodeB = nodeB;
        }
        
        public (int startIndex, int endIndex) GetIndices() {
            var startPos = NodeA.Position;
            var endPos = NodeB.Position;
            var indexStart = -1;
            var indexEnd = -1;
            var startDist = float.MaxValue;
            var endDist = float.MaxValue;
            for (int i = 0; i < Spline.Count; i++) {
                var distStart = Vector3.Distance(Spline[i].Position, startPos);
                if (distStart < startDist) {
                    startDist = distStart;
                    indexStart = i;
                }
                var distEnd = Vector3.Distance(Spline[i].Position, endPos);
                if (distEnd < endDist) {
                    endDist = distEnd;
                    indexEnd = i;
                }
            }
            return (indexStart, indexEnd);
        }

        /// <summary>
        /// Generates a new StreetSegment between two nodes and adds it to the nodes.
        /// Sets the spline of the segment to the given spline if <paramref name="existingSpline"/> is not null.
        /// 
        /// Does not change splines, this has to be done manually before or after calling this method.
        /// (If done before, make sure to remove it again if this method returns false)
        /// </summary>
        /// <param name="from"> The first node </param>
        /// <param name="to"> The second node </param>
        /// <param name="existingSpline"> The spline that should be used for the segment, or null if a new spline will be generated and set afterwards </param>
        /// <param name="newSegment"> The newly generated segment </param>
        /// <returns> True if the segment was added successfully, false otherwise </returns>
        internal static bool GenerateStreetSegment(SplineStreetNode from, SplineStreetNode to, [CanBeNull] Spline existingSpline, out SplineStreetSegment newSegment) {
            newSegment = new SplineStreetSegment(from, to);
            if (!from.AddSegment(newSegment) || !to.AddSegment(newSegment)) {
                from.RemoveSegment(newSegment);
                to.RemoveSegment(newSegment); // probably not needed, but to be sure
                return false;
            }

            if (existingSpline != null) {
                newSegment.Spline = existingSpline;
            }

            return true;
        }

        public override string ToString() {
            return $"Segment from {NodeA.Position} to {NodeB.Position}";
        }
    }
}