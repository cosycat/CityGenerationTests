#nullable enable
using System;
using UnityEngine;

namespace SUMO {
    [Serializable]
    public class SumoSimulationOptions {
        [field:Tooltip("The length of each simulation step in seconds.")]
        [field:SerializeField] public float SimulationStepLengthSeconds { get; set; } = 0.03f;
        [field:SerializeField] public int RandomTripCount { get; set; } = 10;
        [field:SerializeField] public int RandomFlowCount { get; set; } = 10;
        [field:SerializeField] public float FlowPeriod { get; set; } = 30f;
    }
}