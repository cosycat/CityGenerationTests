using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FreeFormGraph.World.PoI {
    
    public class PointOfInterestDebugVisualizer : MonoBehaviour {

        [SerializeField] private bool showBoundaries = false;
        [SerializeField] private bool showLabel = false;
        
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
            
            foreach (var pointOfInterest in POIs) {
                if (showBoundaries) {
                    if (pointOfInterest is SpherePointOfInterest spherePointOfInterest) {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(spherePointOfInterest.Position, spherePointOfInterest.Radius);
                    }
                    else {
                        Debug.LogWarning("Unknown point of interest type");
                    }
                }

                if (showLabel) {
                    Handles.Label(pointOfInterest.Position, $"{pointOfInterest}");
                }
            }
        }
    }
}