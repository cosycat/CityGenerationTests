using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Graph.World.PoI.TestScripts {
    public class PointOfInterestDebugVisualizer : MonoBehaviour {
        [SerializeField] private bool showBoundaries;
        [SerializeField] private bool showLabel;
        private IPointOfInterestCollection pointsOfInterest;

        private IWorld world;

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
            }
            catch (Exception) {
                return;
            }

            foreach (var pointOfInterest in POIs) {
                if (showBoundaries) pointOfInterest.DebugVisualize(world);

#if UNITY_EDITOR
                if (showLabel) {
                    Handles.Label(pointOfInterest.Position, $"{pointOfInterest}");
                    Handles.Label(new Vector3(pointOfInterest.Position.x,
                            world.GetHeightAt(pointOfInterest.Position.x, pointOfInterest.Position.y) + 10f,
                            pointOfInterest.Position.y), $"{pointOfInterest}");
                }
#endif
            }
        }
    }
}