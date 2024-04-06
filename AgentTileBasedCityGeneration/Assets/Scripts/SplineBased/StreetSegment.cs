using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Splines;

namespace SplineBased {

    public class StreetSegment {
        public StreetNode From { get; }
        public StreetNode To { get; }
        
        public Spline Spline { get; internal set; }

        private StreetSegment(StreetNode from, StreetNode to) {
            From = from;
            To = to;
        }
        
        public (int startIndex, int endIndex) GetIndices() {
            var startPos = From.Position;
            var endPos = To.Position;
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
        internal static bool GenerateStreetSegment(StreetNode from, StreetNode to, [CanBeNull] Spline existingSpline, out StreetSegment newSegment) {
            newSegment = new StreetSegment(from, to);
            if (!from.AddSegment(newSegment) || !to.AddSegment(newSegment)) {
                from.ConnectedSegments.Remove(newSegment);
                to.ConnectedSegments.Remove(newSegment); // probably not needed, but to be sure
                return false;
            }

            if (existingSpline != null) {
                newSegment.Spline = existingSpline;
            }

            return true;
        }
        
        internal bool SplitInsertNode(StreetNode node, out StreetSegment newLeftSegment, out StreetSegment newRightSegment) {
            From.ConnectedSegments.Remove(this);
            To.ConnectedSegments.Remove(this);
            newLeftSegment = new StreetSegment(From, node);
            newRightSegment = new StreetSegment(node, To);
            From.ConnectedSegments.Add(newLeftSegment);
            node.ConnectedSegments.Add(newLeftSegment);
            To.ConnectedSegments.Add(newRightSegment);
            node.ConnectedSegments.Add(newRightSegment);
            return true; //TODO error handling
        }

        public override string ToString() {
            return $"Segment from {From.Position} to {To.Position}";
        }
    }
}