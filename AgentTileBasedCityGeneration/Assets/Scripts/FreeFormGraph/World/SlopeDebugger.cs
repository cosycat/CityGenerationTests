using UnityEngine;
using System.Collections.Generic;
using FreeFormGraph.Agents;

namespace FreeFormGraph.World {
    public class SlopeDebugger: MonoBehaviour {

        IWorld world;
        Vector3 lowestPoint;
        Vector3 highestPoint;

        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        List<(Vector2 a, Vector2 b)> segments = new();

        Vector2 currentSegmentStart;

        void Start() {
            world = FindObjectOfType<WorldGameObject>();

            for(int y = 0; y < world.Height; y++) {
                for(int x = 0; x < world.Width; x++) {
                    var pixelValue = world.GetHeightAt(x,y);
                    if(pixelValue > maxHeight) {
                        maxHeight = pixelValue;
                        highestPoint = new Vector3(x,y,0);
                    }
                    if(pixelValue < minHeight) {
                        minHeight = pixelValue;
                        lowestPoint = new Vector3(x,y,0);
                    }
                }
            }
        }

        void CalculateSlopes() {

        }

        void Update() {
            var mousePointerWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var roundedX = Mathf.Round(mousePointerWorldPos.x);
            var roundedY = Mathf.Round(mousePointerWorldPos.y);

            if (Input.GetMouseButtonDown(0)) {
                currentSegmentStart = new Vector2(roundedX, roundedY);
            } else if (Input.GetMouseButtonUp(0)) {
                var endpoint = new Vector2(roundedX, roundedY);
                if(Vector2.Distance(currentSegmentStart, endpoint) >= 0.1f) {
                    segments.Add((currentSegmentStart, endpoint));
                }
            }

            if(Input.GetMouseButton(0) && currentSegmentStart != null) {
                Debug.DrawLine(currentSegmentStart, mousePointerWorldPos);
            }

            foreach(var seg in segments) {
                Debug.DrawLine(seg.a, seg.b);
            }
        }

        void OnGUI() {
            if(world != null) {
                var slopeCost = 0.0f;
                var pureSlope = 0.0f;
                foreach(var seg in segments) {
                    var (a,b) = seg;
                    pureSlope += Mathf.Abs(world.GetHeightAt(a.x, a.y) - world.GetHeightAt(b.x, b.y)) / Vector2.Distance(a, b);
                    slopeCost += Pathfinding.SlopeCost(world, new Pathfinding.Waypoint(a), new Pathfinding.Waypoint(b));
                }
                GUILayout.Label($"Total slope cost: {slopeCost}, pure slope: {pureSlope}");
                if (GUILayout.Button($"Clear slope segments")) {
                    segments.Clear();
                }
            }
        }

        void OnDrawGizmos() {
            if(world != null) {
                var mousePointerWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var roundedX = Mathf.Round(mousePointerWorldPos.x);
                var roundedY = Mathf.Round(mousePointerWorldPos.y);

                if(roundedX >= 0 && roundedX < world.Width && roundedY >= 0 && roundedY < world.Height) {
                    var height = world.GetHeightAt(roundedX, roundedY);
                    UnityEditor.Handles.Label(mousePointerWorldPos, $"{height * Constants.METERS_PER_UNIT} meters");
                }


                UnityEditor.Handles.Label(highestPoint, $"Highest point: {maxHeight * Constants.METERS_PER_UNIT}", new GUIStyle() { fontSize = 30 });
                UnityEditor.Handles.Label(lowestPoint, $"Lowet point: {minHeight * Constants.METERS_PER_UNIT}", new GUIStyle() { fontSize = 30 });
            }
        }
    }
}