#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Graph;
using UnityEngine;

namespace Simulation.Sumo {
    [Serializable]
    public class SumoSimulationOptions {
        [field: Tooltip("The length of each simulation step in seconds.")]
        [field: SerializeField]
        public float SimulationStepLengthSeconds { get; set; } = 0.03f;

        [field: SerializeField] public int RandomTripCount { get; set; } = 10;
        [field: SerializeField] public int RandomFlowCount { get; set; } = 10;
        [field: SerializeField] public float FlowPeriod { get; set; } = 30f;
        [field: SerializeField] public SumoVehicleType[] VehicleTypes { get; set; } = Array.Empty<SumoVehicleType>();
        [field: SerializeField] private Vehicle defaultVehiclePrefab = null!;

        public Dictionary<RoadType, SumoEdgeTypes> RoadTypeToEdgeType = new() {
            { RoadType.Primary, new SumoEdgeTypes("primary", 22.22f, 1, 3) },
            { RoadType.Secondary, new SumoEdgeTypes("secondary", 13.89f, 1, 2) },
            { RoadType.Tertiary, new SumoEdgeTypes("tertiary", 8.33f, 1, 1) },
            { RoadType.CountryRoad, new SumoEdgeTypes("countryRoad", 41.67f, 1, 5) }
        };

        public Vehicle GetVehiclePrefab(string vehicleType) {
            return VehicleTypes.FirstOrDefault(v => v.Id == vehicleType)?.Prefab ?? defaultVehiclePrefab;
        }
    }
}