using System.Collections.Generic;
using AgentSystem;
using UnityEditor;
using UnityEngine;

namespace Graph.World.Helpers {
    public class SlopeDebugger : MonoBehaviour {
        private Vector2 currentSegmentStart;
        private Vector3 highestPoint;
        private Vector3 lowestPoint;

        private float maxHeight = float.MinValue;
        private float minHeight = float.MaxValue;

        private readonly List<(Vector2 a, Vector2 b)> segments = new();
        private IWorld world;

        private void Start() {
            world = FindObjectOfType<WorldGameObject>();

            for (var y = 0; y < world.Height; y++) {
                for (var x = 0; x < world.Width; x++) {
                    var pixelValue = world.GetHeightAt(x, y);
                    if (pixelValue > maxHeight) {
                        maxHeight = pixelValue;
                        highestPoint = new Vector3(x, y, 0);
                    }

                    if (pixelValue < minHeight) {
                        minHeight = pixelValue;
                        lowestPoint = new Vector3(x, y, 0);
                    }
                }
            }
        }

        private void Update() {
            var mousePointerWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var roundedX = Mathf.Round(mousePointerWorldPos.x);
            var roundedY = Mathf.Round(mousePointerWorldPos.y);

            if (Input.GetMouseButtonDown(0)) {
                currentSegmentStart = new Vector2(roundedX, roundedY);
            }
            else if (Input.GetMouseButtonUp(0)) {
                var endpoint = new Vector2(roundedX, roundedY);
                if (Vector2.Distance(currentSegmentStart, endpoint) >= 0.1f)
                    segments.Add((currentSegmentStart, endpoint));
            }

            if (Input.GetMouseButton(0) && currentSegmentStart != null)
                Debug.DrawLine(currentSegmentStart, mousePointerWorldPos);

            foreach (var seg in segments) Debug.DrawLine(seg.a, seg.b);
        }

        private void OnGUI() {
            if (world != null) {
                var slopeCost = 0.0f;
                var pureSlope = 0.0f;
                foreach (var seg in segments) {
                    var (a, b) = seg;
                    var heightStart = world.GetHeightAt(a.x, a.y);
                    var heightEnd = world.GetHeightAt(b.x, b.y);
                    pureSlope += Mathf.Abs(heightStart - heightEnd) / Vector2.Distance(a, b);
                    slopeCost += Pathfinding.SlopeCost(world, new Pathfinding.Waypoint(a), new Pathfinding.Waypoint(b),
                        new Pathfinding.Parameters(), heightStart, heightEnd);
                }

                GUILayout.Label($"Total slope cost: {slopeCost}, pure slope: {pureSlope}");
                if (GUILayout.Button("Clear slope segments")) segments.Clear();
            }
        }

        private void OnDrawGizmos() {
            if (world != null) {
                var mousePointerWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var roundedX = Mathf.Round(mousePointerWorldPos.x);
                var roundedY = Mathf.Round(mousePointerWorldPos.y);

                if (roundedX >= 0 && roundedX < world.Width && roundedY >= 0 && roundedY < world.Height) {
                    var height = world.GetHeightAt(roundedX, roundedY);
                    Handles.Label(mousePointerWorldPos, $"{height * Constants.METERS_PER_UNIT} meters");
                }


                Handles.Label(highestPoint, $"Highest point: {maxHeight * Constants.METERS_PER_UNIT}",
                    new GUIStyle { fontSize = 30 });
                Handles.Label(lowestPoint, $"Lowest point: {minHeight * Constants.METERS_PER_UNIT}",
                    new GUIStyle { fontSize = 30 });
            }
        }

        private void CalculateSlopes() { }
    }
}