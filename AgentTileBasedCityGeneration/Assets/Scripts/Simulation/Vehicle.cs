#nullable enable
using System;
using FreeFormGraph;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Simulation {
    public class Vehicle : MonoBehaviour {
        
        [FormerlySerializedAs("blinkerRightGO")] [SerializeField] private VehicleSignal? blinkerRight;
        [FormerlySerializedAs("blinkerLeftGO")] [SerializeField] private VehicleSignal? blinkerLeft;
        [FormerlySerializedAs("brakeLightGO")] [SerializeField] private VehicleSignal? brakeLight;
        
        public string ID { get; internal set; } = null!;

        private void Start() {
            if (!Settings.UseVehicleSignals) {
                if (blinkerRight != null) Destroy(blinkerRight.gameObject);
                if (blinkerLeft != null) Destroy(blinkerLeft.gameObject);
                if (brakeLight != null) Destroy(brakeLight.gameObject);
            }
            else {
                if (blinkerRight == null || blinkerLeft == null || brakeLight == null) {
                    Debug.LogError("Vehicle is set to use signals but has missing signal GameObjects!");
                }
            }
            
            if (ID is null or "") {
                Debug.LogError("Vehicle ID not set.");
            }
            
            
        }

        public void UpdatePosition(Vector3 position, Quaternion rotation) {
            transform.position = position;
            transform.rotation = rotation;
        }

        public void UpdateSignals(bool isBlinkerRightOn, bool isBlinkerLeftOn, bool isBrakeLightOn) {
            if (!Settings.UseVehicleSignals) return;
            blinkerRight?.SetSignal(isBlinkerRightOn);
            blinkerLeft?.SetSignal(isBlinkerLeftOn);
            brakeLight?.SetSignal(isBrakeLightOn);
        }
    }
}