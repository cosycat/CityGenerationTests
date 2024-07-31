#nullable enable
using System;
using FreeFormGraph;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Simulation {
    public class Vehicle : MonoBehaviour {
        
        [SerializeField] protected VehicleSignal blinkerRight = null!;
        [SerializeField] protected VehicleSignal blinkerLeft = null!;
        [SerializeField] protected VehicleSignal brakeLight = null!;
        
        public string ID { get; internal set; } = null!;

        protected virtual void Awake() {
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
        }

        public virtual void UpdatePosition(Vector3 position, Quaternion rotation) {
            transform.position = position;
            transform.rotation = rotation;
        }

        public virtual void UpdateSignals(bool isBlinkerRightOn, bool isBlinkerLeftOn, bool isBrakeLightOn) {
            if (!Settings.UseVehicleSignals) return;
            blinkerRight.SetSignal(isBlinkerRightOn);
            blinkerLeft.SetSignal(isBlinkerLeftOn);
            brakeLight.SetSignal(isBrakeLightOn);
        }
    }
}