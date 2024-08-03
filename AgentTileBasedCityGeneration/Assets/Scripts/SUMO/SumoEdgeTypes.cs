#nullable enable
using System;
using System.Collections.Generic;
using FreeFormGraph;

namespace SUMO {
    
    /// <summary>
    /// Class defining edge types for SUMO.
    /// Descriptions directly taken from https://sumo.dlr.de/docs/SUMO_edge_type_file.html
    /// </summary>
    [Serializable]
    public class SumoEdgeTypes {
        /// <summary>
        /// The name of the road type. This is the only mandatory attribute.
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// The default (implicit) speed limit in m/s.
        /// </summary>
        public float Speed { get; }
        /// <summary>
        /// The number of lanes on an edge. This is the default number of lanes per direction.
        /// </summary>
        public int NumLanes { get; }
        /// <summary>
        /// A number, which determines the priority between different road types. netconvert derives the right-of-way rules at junctions from the priority. The number starts with one; higher numbers represent more important roads.
        /// </summary>
        public int Priority { get; }

        public SumoEdgeTypes(string id, float speed, int numLanes, int priority) {
            Id = id;
            Speed = speed;
            NumLanes = numLanes;
            Priority = priority;
        }
        
        /// <summary>
        /// A fallback edge type to use if no edge type is defined.
        /// </summary>
        public static readonly SumoEdgeTypes DefaultSumoEdgeType = new("defaultRoad", 10, 1, 1);
    }
}