using System;
using UnityEditor;
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    public class PointOfInterestTest : MonoBehaviour {

        private IWorld world;
        private IPointOfInterestCollection pointsOfInterest;

        private void Start() {
            world = FindObjectOfType<WorldGameObject>();
            pointsOfInterest = world.PointsOfInterest;
        }


        private void OnDrawGizmos() {
            if (pointsOfInterest == null)
                return;
            
            foreach (var pointOfInterest in pointsOfInterest.PointsOfInterest) {
                if (pointOfInterest is SpherePointOfInterest spherePointOfInterest) {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(spherePointOfInterest.Position, spherePointOfInterest.Radius);
                }
                else {
                    Debug.LogWarning("Unknown point of interest type");
                }

                Handles.Label(pointOfInterest.Position, $"{pointOfInterest}");
            }
        }
    }
}