using UnityEngine;

public static class DistanceUtils {
    public static int ManhattanDistanceTo(this Vector2Int src, Vector2Int dest) {
        return Mathf.Abs(src.x - dest.x) + Mathf.Abs(src.y - dest.y);
    }
}