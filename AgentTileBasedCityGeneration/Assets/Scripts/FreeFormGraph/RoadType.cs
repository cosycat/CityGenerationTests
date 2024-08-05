using System;

namespace FreeFormGraph {
    /// <summary>
    /// The different types of roads that can be represented in the graph.
    /// </summary>
    [Serializable]
    public enum RoadType {
        Primary,
        Secondary,
        Tertiary,
        Highway
    }
}