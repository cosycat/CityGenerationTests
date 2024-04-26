using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace FreeFormGraph.SplineBased {
    public static class SplineIntersections {

        //DEBUG stuff
        public static List<Vector3> DbgSplineIntersectionPoints = new();
        //DEBUG stuff
        public static List<(Vector3 pos, float length)> DbgCurveSteps = new();
        

        /// <summary>
        /// Checks if a new segment has an intersection with any other segment.
        /// 
        /// Does not check segments directly attached to the new segment.
        /// </summary>
        /// <param name="newSegment"> The newly added segment to check for intersections </param>
        /// <param name="allSegments"> All segments in the graph </param>
        /// <param name="intersection"> The intersection that was found, if any </param>
        /// <returns> True if an intersection was found, false otherwise </returns>
        public static bool HasIntersection(SplineStreetSegment newSegment, List<SplineStreetSegment> allSegments, out Intersection intersection) {
            DbgCurveSteps.Clear();
            DbgSplineIntersectionPoints.Clear();
            
            var segmentSpline = newSegment.Spline;
            var curveOfNewSegment = newSegment.Curve;
            var startPoint = newSegment.NodeA.Position;
            var endPoint = newSegment.NodeB.Position;
            var segmentBounds = GetBoundsForCurve(curveOfNewSegment);
            
            var stepSize = 0.05f;
            var distanceThreshold = 0.3f;

            var pointsOnSpline = newSegment.SplitIntoEvenlySpacedPoints(stepSize);

            var foundIntersections = new List<Intersection>();

            for (int i = 0; i < allSegments.Count; i++) {
                var otherSegment = allSegments[i];
                if (otherSegment == newSegment) continue; // don't check against itself
                var otherSpline = otherSegment.Spline;
                // if (otherSpline == lastModifiedSpline) continue; // don't check against the same spline // TODO only skip the prev and next curve, instead of all.
                var otherSegmentStart = otherSegment.NodeA.Position;
                var otherSegmentEnd = otherSegment.NodeB.Position;
                
                var indices = otherSegment.GetIndices();
                var otherLowerIndex = Mathf.Min(indices.lowerIndex, indices.higherIndex);
                
                var otherCurve = otherSpline.GetCurve(otherLowerIndex);
                // check if the other curve is starting or ending at the same point as the new segment. if so, skip it.
                if (Vector3.Distance(otherCurve.P0, startPoint) <= distanceThreshold || Vector3.Distance(otherCurve.P3, startPoint) <= distanceThreshold) continue;
                
                var otherBounds = GetBoundsForCurve(otherCurve);
                if (!segmentBounds.Intersects(otherBounds)) continue; // no intersection possible
                
                // check for intersections
                var otherPointsOnSpline = otherSegment.SplitIntoEvenlySpacedPoints(stepSize);
                for (int thisI = 0; thisI < pointsOnSpline.Length; thisI++) {
                    var point = pointsOnSpline[thisI];
                    for (int otherI = 0; otherI < otherPointsOnSpline.Length; otherI++) {
                        var otherPoint = otherPointsOnSpline[otherI];
                        if (Vector3.Distance(point, otherPoint) < distanceThreshold) {
                            // found an intersection
                            // var intersectionPoint = (point + otherPoint) / 2; 
                            var intersectionPoint = point; // Don't take the average, take the exising point, to avoid changing the existing spline.
                            var otherTangent = CurveUtility.EvaluateTangent(otherCurve, otherI * 1.0f / otherPointsOnSpline.Length);
                            foundIntersections.Add(new Intersection(
                                intersectionPoint,
                                otherSegment, 
                                otherLowerIndex,
                                otherTangent
                                ));
                            // DEBUG
                            DbgSplineIntersectionPoints.Add(intersectionPoint);
                            if (foundIntersections.Count == 1) {
                                Debug.Log($"Found intersection at {intersectionPoint} between {newSegment} and {otherSegment}, otherLowerIndex: {otherLowerIndex} (indices: {indices})");
                            }
                        }
                    }
                }
            }

            if (foundIntersections.Count == 0) {
                intersection = null;
                return false;
            }
            
            // sort the intersections by distance to the start of the segment
            // TODO ability to invert the sorting order, in case the segment is built from end to start
            foundIntersections.Sort((a, b) => {
                var distanceA = Vector3.Distance(startPoint, a.IntersectionPoint);
                var distanceB = Vector3.Distance(startPoint, b.IntersectionPoint);
                return distanceA.CompareTo(distanceB);
            });
            
            intersection = foundIntersections[0];
            DbgSplineIntersectionPoints.Add(intersection.IntersectionPoint);
            Debug.Log($"Found intersection at {intersection.IntersectionPoint} between {newSegment} and {intersection.ExistingSegment}, otherLowerIndex: {intersection.ExistingBezierIndex}");
            return true;
        }

        /// <summary>
        /// Represents a found intersection between two curves.
        /// </summary>
        public class Intersection {
            
            /// <summary>
            /// The position of the intersection.
            /// </summary>
            public Vector3 IntersectionPoint { get; }
            
            /// <summary>
            /// The segment that was intersected.
            /// </summary>
            public SplineStreetSegment ExistingSegment { get; }
            
            /// <summary>
            /// The index of the curve in the segment that was intersected.
            /// </summary>
            public int ExistingBezierIndex { get; }
            
            /// <summary>
            /// The tangent of the curve at the intersection point.
            /// </summary>
            public Vector3 ExistingTangentAtIntersection { get; }

            public Intersection(Vector3 intersectionPoint, SplineStreetSegment existingSegment, int existingBezierIndex, Vector3 existingTangentAtIntersection) {
                IntersectionPoint = intersectionPoint;
                ExistingSegment = existingSegment;
                ExistingBezierIndex = existingBezierIndex;
                ExistingTangentAtIntersection = existingTangentAtIntersection;
            }

            public override string ToString() {
                return $"Intersection at {IntersectionPoint} with segment {ExistingSegment} at index {ExistingBezierIndex}";
            }
        }

        /// <summary>
        /// Calculates intersection between a given curve and all other curves. The general idea here is, that
        /// after building a new StreetSegment (which builds a single BezierCurve), this new bezier curve will be checked against
        /// all existing curves for intersections.
        /// The algorithm used is iterative, which is why there are two parameters which have to be adjusted. The default values
        /// are chosen to be a good default.
        /// 
        /// Limitations:
        /// Multiple intersections on a single bezier curve don't work. Only one intersection is found.
        /// </summary>
        /// <param name="from">The node where the curve was built from. Needed to prevent intersections with previous curve.</param>
        /// <param name="s">The spline where this curve is on</param>
        /// <param name="c">The "new" curve that will be used to check the intersections with</param>
        /// <param name="nodes">All nodes from graph. Used for extracting curves.</param>
        /// <param name="stepSize">Steping size used when walking over curve</param>
        /// <param name="minDistanceForHit">Hit is registered when this distance is met. Bigger value will trigger intersections earlier TOOD explain better</param>
        public static List<CurveIntersection> GetIntersectionsForCurve(
                SplineStreetNode from,
                Spline s,
                BezierCurve c, 
                List<SplineStreetNode> nodes, 
                float stepSize = 0.4f, 
                float minDistanceForHit = 0.4f) {

            if(s == null || c == null) return new List<CurveIntersection>();

            var allSplines = nodes
                .SelectMany(n => n.CorrespondingSplines)
                .Distinct();

            stepSize = stepSize / CurveUtility.CalculateLength(c);
            DbgCurveSteps.Clear();
            DbgSplineIntersectionPoints.Clear();

            List<CurveIntersection> intersections = new();
            Vector3 lastNearestPoint = new Vector3(float.MaxValue, float.MaxValue, 0);

            //walk along our current spline
            for(var step = 0.0f; step < 1; step += stepSize) {
                var currentPosOnSpline = step;
                var currentPosOnSplineWorldCoords = (Vector3)CurveUtility.EvaluatePosition(c, currentPosOnSpline);
                var currentTangent = CurveUtility.EvaluateTangent(c, currentPosOnSpline);
                var currentBounds = GetBoundsForCurve(c);

                //iterate through all splines...
                foreach(var targetSpline in allSplines) {

                    for(var i = 0; i < SplineUtility.GetCurveCount(targetSpline); i++) {
                        var targetCurve = targetSpline.GetCurve(i);
                        if(targetCurve == c) continue; //don't look for intersection on own curve :)

                        var bounds = GetBoundsForCurve(targetCurve);
                        if(!bounds.Intersects(currentBounds)) continue; //no intersection for sure

                        var ray = new Ray(currentPosOnSplineWorldCoords, currentTangent);
                        CurveUtility.GetNearestPoint(targetCurve, ray, out var nearest, out var nearestInterpolation);

                        DbgCurveSteps.Add((currentPosOnSplineWorldCoords, 1));

                        if(Vector3.Distance(nearest, currentPosOnSplineWorldCoords) < minDistanceForHit
                            && Vector3.Distance(nearest, lastNearestPoint) > minDistanceForHit //cheap mans non-max-supression
                            && !IsEqual(nearest, from.Position, 0.1f) //ignore intersection with previous curve we are attached on :)
                            ) { 

                            if(nearestInterpolation > 1 || nearestInterpolation < 0 ) {
                                //it's possible that we find an intersection even tough the curve itself
                                //is not directly hit (our curve moves near the start/end of the other curve)
                                nearestInterpolation = Mathf.Min(0, Mathf.Max(1, nearestInterpolation));
                                nearest = CurveUtility.EvaluatePosition(targetCurve, nearestInterpolation);
                            }

                            DbgSplineIntersectionPoints.Add(nearest);
                            lastNearestPoint = nearest;
                            var newIntersection = new CurveIntersection(
                                intersectionPosition: nearest,
                                sourceSpline: s,
                                sourceCurve: c,
                                sourceCurveInterpolation: currentPosOnSpline,
                                sourcePositionOnCurve: currentPosOnSplineWorldCoords,
                                otherBezierKnotIndex: i,
                                otherSpline: targetSpline,
                                otherCurve: targetCurve,
                                otherCurveInterpolation: nearestInterpolation,
                                otherPositionOnCurve: nearest
                            );

                            intersections.Add(newIntersection);
                        }
                    }
                }
            }

            return intersections;
        }


        // /// <summary>
        // /// Handles intersections by splitting or merging points
        // /// TODO
        // /// </summary>
        // public static void HandleIntersections(List<CurveIntersection> intersections, 
        //         List<StreetNode> nodes, 
        //         List<StreetSegment> edges
        //     ) {
        //
        //     foreach(var i in intersections) {
        //         var initialNodeCount = nodes.Count;
        //         var initialEdgeCount = edges.Count;
        //
        //         const float minDistanceToSnapToOtherEdge = 2.0f;
        //         var closestCPoint = GetClosest(i.OtherPositionOnCurve, i.OtherCurve.P0, i.OtherCurve.P3, out var distanceToCP);
        //         if(distanceToCP < minDistanceToSnapToOtherEdge) {
        //             //snap to existing node
        //             var targetPosition = closestCPoint;
        //             var existingNode = nodes.Where(n => n.Position == targetPosition).First();
        //             SplitRoadSegment(nodes, 
        //                 edges, 
        //                 i.SourceCurve, 
        //                 targetPosition,
        //                 existingNode);
        //             InsertKnotOntoSpline(i.SourceSpline, i.SourceCurve, targetPosition);
        //         } else {
        //             //create a new node
        //             var newNode = SplitRoadSegment(nodes, edges, i.OtherCurve, i.OtherPositionOnCurve);
        //             SplitRoadSegment(nodes, edges, i.SourceCurve, i.SourcePositionOnCurve, newNode);
        //             newNode.CorrespondingSplines.Add(i.SourceSpline);
        //             newNode.CorrespondingSplines.Add(i.OtherSpline);
        //             
        //             InsertKnotOntoSpline(i.OtherSpline, i.OtherCurve, i.OtherPositionOnCurve);
        //             //we want both knots to be on the same position!
        //             InsertKnotOntoSpline(i.SourceSpline, i.SourceCurve, i.OtherPositionOnCurve);
        //
        //             Debug.Assert(nodes.Count == initialNodeCount + 1);
        //             Debug.Assert(edges.Count == initialEdgeCount + 2);
        //         }
        //
        //     }
        // }

        // private static StreetNode SplitRoadSegment(
        //         List<StreetNode> nodes, 
        //         List<StreetSegment> edges,
        //         BezierCurve c,
        //         Vector3 intersectionPosition,
        //         StreetNode node = null
        //     ) {
        //     var segStart = (Vector3)c.P0;
        //     var segEnd = (Vector3)c.P3;
        //
        //     //find the street segment that corresponds to the curve where the intersection was found
        //     var roadSegment = edges
        //         .FirstOrDefault(seg => (segStart == seg.NodeA.Position && segEnd == seg.NodeB.Position)
        //             || (segStart == seg.NodeB.Position && segEnd == seg.NodeA.Position));
        //     
        //     if(roadSegment == null) {
        //         //TODO debug stuff
        //         var nearest = nodes
        //             .Select(node => {
        //                 var d1 = Vector3.Distance(node.Position, segStart);
        //                 var d2 = Vector3.Distance(node.Position, segEnd);
        //                 return (Mathf.Min(d1, d2), node);
        //             })
        //             .OrderBy(tuple => tuple.Item1)
        //             .First();
        //         Debug.Log($"nearest node: {nearest.Item2} with distance {nearest.Item1} for segstart: {segStart} segend: {segEnd}");
        //     }
        //
        //     if(node == null) {
        //         node = new StreetNode(intersectionPosition);
        //         nodes.Add(node);
        //     }
        //     //create new street segment in graph
        //     roadSegment.SplitInsertNode(node, out var newLeftSegment, out var newRightSegment);
        //     edges.Remove(roadSegment);
        //     edges.Add(newLeftSegment);
        //     edges.Add(newRightSegment);
        //     return node;
        // }
        //
        // private static void InsertKnotOntoSpline(
        //         Spline s,
        //         BezierCurve c,
        //         Vector3 insertPosition
        // ) {
        //     var segStart = (Vector3)c.P0;
        //     var segEnd = (Vector3)c.P3;
        //
        //     for(var insertionIndex = 0; insertionIndex < s.Count - 1; insertionIndex++) {
        //         var currentBezierKnotPos = (Vector3)s[insertionIndex].Position;
        //         var nextBezierKnotPos = (Vector3)s[insertionIndex+1].Position;
        //         //we need to find the index thats between point segStart and segEnd
        //         if((currentBezierKnotPos == segStart || currentBezierKnotPos == segEnd)
        //         && (nextBezierKnotPos == segStart || nextBezierKnotPos == segEnd)) {
        //                 var k = new BezierKnot(insertPosition);
        //                 s.Insert(insertionIndex+1, k, TangentMode.AutoSmooth);
        //                 break;
        //         }
        //     }
        // }

        /// <summary>
        /// Checks if two Vector3 points are approximately equal within a specified epsilon value.
        /// </summary>
        /// <param name="a">The first Vector3 point.</param>
        /// <param name="b">The second Vector3 point.</param>
        /// <param name="eps">The epsilon value for comparison (default is 0.01).</param>
        /// <returns>True if the points are approximately equal, otherwise false.</returns>
        private static bool IsEqual(Vector3 a, Vector3 b, float eps = 0.01f) {
            return Mathf.Abs(a.x - b.x) < eps &&
                Mathf.Abs(a.y - b.y) < eps &&
                Mathf.Abs(a.z - b.z) < eps; 
        }

        /// <summary>
        /// Compares two points to a source points and returns the nearer one.
        /// </summary>
        /// <param name="src">The source point.</param>
        /// <param name="dst1">The first destination point.</param>
        /// <param name="dst2">The second destination point.</param>
        /// <param name="distance">Out parameter to return the distance to the closest point.</param>
        /// <returns>The closest destination point to the source point.</returns>
        private static Vector3 GetClosest(Vector3 src, Vector3 dst1, Vector3 dst2, out float distance) {
            var d1 = Vector3.Distance(src, dst1);
            distance = Vector3.Distance(src, dst2);
            if(d1 < distance) {
                distance = d1;
                return dst1;
            }
            return dst2;
        }

        /// <summary>
        /// Calculates the bounding box (Bounds) for a given Bezier curve. This calculate the bounding box
        /// with the help of the 4 control points
        /// </summary>
        /// <param name="curve">The Bezier curve to calculate the bounding box for.</param>
        public static Bounds GetBoundsForCurve(BezierCurve curve) {
            
            var minX = Mathf.Min(curve.P0.x, Mathf.Min(curve.P1.x, Mathf.Min(curve.P2.x, curve.P3.x)));
            var minY = Mathf.Min(curve.P0.y, Mathf.Min(curve.P1.y, Mathf.Min(curve.P2.y, curve.P3.y)));
            var maxX = Mathf.Max(curve.P0.x, Mathf.Max(curve.P1.x, Mathf.Max(curve.P2.x, curve.P3.x)));
            var maxY = Mathf.Max(curve.P0.y, Mathf.Max(curve.P1.y, Mathf.Max(curve.P2.y, curve.P3.y)));
            return new Bounds() {
                min = new Vector3(minX, minY, 0),
                max = new Vector3(maxX, maxY, 0),
            };
        }

        
    }

    /// <summary>
    /// Represents the intersection of a Bezier curve.
    /// Intersection is always represented between two curves/splines (source, ie the current curve/spline and 
    /// other, ie the curve/spline that we hit)
    /// </summary>
    public struct CurveIntersection {
        
        /// <summary>
        /// The position where the intersection was found
        /// </summary>
        public Vector3 IntersectionPosition;
        
        /// <summary>
        /// The index of the bezier knot with the lower index from the segment of the other spline that was intersected.
        /// </summary>
        public int OtherBezierKnotIndex;

        
        /// <summary>
        /// The spline which the source curve is part of
        /// </summary>
        public Spline SourceSpline;
        /// <summary>
        /// The Bezier curve which was intersected (part of SourceSpline).
        /// </summary>
        public BezierCurve SourceCurve;
        /// <summary>
        /// The ratio along the current curve at which the intersection occurs between 0 an d1.
        /// </summary>
        public float SourceCurveInterpolation;
        /// <summary>
        /// The position in world coordinates where the nearest point on the bezier curve was found
        /// </summary>
        public Vector3 SourcePositionOnCurve;

        /// <summary>
        /// The spline intersecting with this curve.
        /// </summary>
        public Spline OtherSpline;

        /// <summary>
        /// The Bezier curve which was intersected (part of OtherSpline).
        /// </summary>
        public BezierCurve OtherCurve;

        /// <summary>
        /// The ratio along the current curve at which the intersection occurs between 0 an d1.
        /// </summary>
        public float OtherCurveInterpolation;
        
        /// <summary>
        /// The position in world coordinates where the nearest point on the bezier curve was found
        /// </summary>
        public Vector3 OtherPositionOnCurve;

        


        public CurveIntersection(Vector3 intersectionPosition,
            Spline sourceSpline,
            BezierCurve sourceCurve,
            float sourceCurveInterpolation,
            Vector3 sourcePositionOnCurve,
            int otherBezierKnotIndex,
            Spline otherSpline,
            BezierCurve otherCurve,
            float otherCurveInterpolation,
            Vector3 otherPositionOnCurve) {
            IntersectionPosition = intersectionPosition;
            OtherBezierKnotIndex = otherBezierKnotIndex;
            
            SourceSpline = sourceSpline;
            SourceCurve = sourceCurve;
            SourceCurveInterpolation = sourceCurveInterpolation;
            SourcePositionOnCurve = sourcePositionOnCurve;

            OtherSpline = otherSpline;
            OtherCurve = otherCurve;
            OtherCurveInterpolation = otherCurveInterpolation;
            OtherPositionOnCurve = otherPositionOnCurve;
        }
    }
}