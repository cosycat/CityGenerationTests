using System;
using System.Collections.Generic;
using System.Linq;
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
            
            //handle concurrency issues
            List<IPointOfInterest> POIs;
            try {
                POIs = pointsOfInterest.PointsOfInterest.ToList();
            } catch (Exception) {
                return;
            }
            
            for (int i = 0; i < POIs.Count; i++) {
                var pointOfInterest = POIs[i];
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