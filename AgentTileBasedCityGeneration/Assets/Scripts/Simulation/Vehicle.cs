#nullable enable
using System;
using System.Collections.Generic;
using FreeFormGraph;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Simulation {
    public class Vehicle : MonoBehaviour {
        
        [SerializeField] private VehicleSignal blinkerRight = null!;
        [SerializeField] private VehicleSignal blinkerLeft = null!;
        [SerializeField] private VehicleSignal brakeLight = null!;
        
        private string id = null!;

        private SelectableCamera[] vehicleCameras = Array.Empty<SelectableCamera>();

        public string VehicleType { get; set; } = "car";

        public string ID {
            get => id;
            internal set {
                if (id is not null and not "") {
                    Debug.LogError($"Vehicle ID already set: {id}");
                    return;
                }
                id = value;
                name = $"Vehicle_{id}";
            }
        }

        public bool IsPlayerVehicle { get; private set; }
        
        public SelectableCamera[] VehicleCameras => vehicleCameras;

        private void Awake() {
            if (!Settings.UseVehicleSignals) {
                if (blinkerRight != null) Destroy(blinkerRight.gameObject);
                if (blinkerLeft != null) Destroy(blinkerLeft.gameObject);
                if (brakeLight != null) Destroy(brakeLight.gameObject);
            }
            else {
                if (blinkerRight == null || blinkerLeft == null || brakeLight == null) {
                    Debug.LogError($"Vehicle is set to use signals but has missing signal GameObjects: {blinkerRight}, {blinkerLeft}, {brakeLight}");
                }
            }
        }

        private void Start() {
            if (ID is null or "") {
                Debug.LogError($"Vehicle ID not set: {ID ?? "null"}");
            }
            vehicleCameras = GetComponentsInChildren<SelectableCamera>(true);
        }

        public void UpdatePosition(Vector3 position, Quaternion rotation) {
            // if (IsPlayerVehicle) return;
            transform.position = position;
            transform.rotation = rotation;
        }

        public void UpdateSignals(bool isBlinkerRightOn, bool isBlinkerLeftOn, bool isBrakeLightOn) {
            // if (IsPlayerVehicle) return;
            if (!Settings.UseVehicleSignals) return;
            blinkerRight.SetSignal(isBlinkerRightOn);
            blinkerLeft.SetSignal(isBlinkerLeftOn);
            brakeLight.SetSignal(isBrakeLightOn);
        }

        public void SetPlayerVehicle(bool isPlayerVehicle) {
            // if (IsPlayerVehicle == isPlayerVehicle) return;
            UpdateSignals(false, false, false);
            
            IsPlayerVehicle = isPlayerVehicle;
        }
    }
}