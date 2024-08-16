using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Utils {
    public static class SplineExtensions {
        /// <summary>
        ///     Calculates evenly spaced points on the spline, with a given spacing between the points.
        ///     Does not include the last point on the spline.
        ///     From https://youtu.be/d9k97JemYbM?si=atas47LAqDahVCo9
        /// </summary>
        /// <param name="spline"> The spline to calculate the points on. </param>
        /// <param name="spacing"> The spacing between the points. </param>
        /// <param name="resolution"> The resolution with which to sample the actual points on the spline (steps of t). </param>
        /// <returns> An array of evenly spaced points on the spline. </returns>
        public static Vector3[] CalculateEvenlySpacedPoints(this Spline spline, float spacing, float resolution = 1f) {
            var evenlySpacedPoints = new List<Vector3> { spline[0].Position };
            var prevPoint = (Vector3)spline[0].Position;
            var dstSinceLastEvenPoint = 0f;

            var divisions = Mathf.CeilToInt(spline.GetLength() * resolution * 10);
            var tStep = 1f / divisions;

            float t = 0;
            while (t < 1) {
                t += tStep;
                var pointOnSpline = (Vector3)spline.EvaluatePosition(t);
                dstSinceLastEvenPoint += Vector3.Distance(prevPoint, pointOnSpline);

                while (dstSinceLastEvenPoint >= spacing) {
                    var overshootDst = dstSinceLastEvenPoint - spacing;
                    var newEvenlySpacedPoint =
                        pointOnSpline +
                        (prevPoint - pointOnSpline).normalized * overshootDst; // TODO is this the right way around?
                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    dstSinceLastEvenPoint = overshootDst;
                    prevPoint = newEvenlySpacedPoint;
                }

                prevPoint = pointOnSpline;
            }

            return evenlySpacedPoints.ToArray();
        }

        /// <summary>
        ///     Calculates evenly spaced points on the curve, with a given spacing between the points.
        ///     Does not include the last point on the curve (P3).
        ///     From https://youtu.be/d9k97JemYbM?si=atas47LAqDahVCo9
        /// </summary>
        /// <param name="curve"> The curve to calculate the points on. </param>
        /// <param name="spacing"> The spacing between the points. </param>
        /// <param name="resolution"> The resolution with which to sample the actual points on the curve (steps of t). </param>
        /// <param name="useLengthApproximation">
        ///     Whether to use the approximate length of the curve to calculate the points, or
        ///     calculate the length more accurately with the given resolution.
        /// </param>
        /// <returns> An array of evenly spaced points on the curve. </returns>
        public static Vector3[] CalculateEvenlySpacedPoints(this BezierCurve curve, float spacing, int resolution = 10,
            bool useLengthApproximation = true) {
            var evenlySpacedPoints = new List<Vector3> { curve.P0 };
            var prevPoint = (Vector3)curve.P0;
            var dstSinceLastEvenPoint = 0f;

            var divisions = Mathf.CeilToInt(useLengthApproximation
                ? CurveUtility.ApproximateLength(curve)
                : CurveUtility.CalculateLength(curve, resolution) * resolution);
            var tStep = 1f / divisions;

            float t = 0;
            while (t < 1) {
                t += tStep;
                var pointOnSpline = (Vector3)CurveUtility.EvaluatePosition(curve, t);
                dstSinceLastEvenPoint += Vector3.Distance(prevPoint, pointOnSpline);

                while (dstSinceLastEvenPoint >= spacing) {
                    var overshootDst = dstSinceLastEvenPoint - spacing;
                    var newEvenlySpacedPoint =
                        pointOnSpline +
                        (prevPoint - pointOnSpline).normalized * overshootDst; // TODO is this the right way around?
                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    dstSinceLastEvenPoint = overshootDst;
                    prevPoint = newEvenlySpacedPoint;
                }

                prevPoint = pointOnSpline;
            }

            return evenlySpacedPoints.ToArray();
        }
    }
}