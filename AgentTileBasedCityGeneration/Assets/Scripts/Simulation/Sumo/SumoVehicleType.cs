#nullable enable
using System;
using UnityEngine;

namespace Simulation.Sumo {
    [Serializable]
    public class SumoVehicleType {
        /// <summary>
        ///     The ID, i.e. the name of the vehicle type.
        /// </summary>
        [field: SerializeField]
        public string Id { get; private set; } = null!;

        [field: SerializeField] public float MaxSpeed { get; private set; }
        [field: SerializeField] public float Length { get; private set; }
        [field: SerializeField] public float Accel { get; private set; }
        [field: SerializeField] public float Decel { get; private set; }

        [field: SerializeField] public SumoVehicleClass VehicleClass { get; private set; } = SumoVehicleClass.Passenger;

        [SerializeField] private Vehicle? prefab;

        public Vehicle? Prefab {
            get => prefab;
            private set => prefab = value;
        }

        // public SumoVehicleType(string id, float maxSpeed, float length, float accel, float decel, Vehicle prefab) {
        //     Id = id;
        //     MaxSpeed = maxSpeed;
        //     Length = length;
        //     Accel = accel;
        //     Decel = decel;
        //     Prefab = prefab;
        // }
    }

    public enum SumoVehicleClass {
        Passenger,
        Bus,
        Truck,
        Emergency,
        Taxi,
        Bicycle,
        Pedestrian,
        Motorcycle
    }
}