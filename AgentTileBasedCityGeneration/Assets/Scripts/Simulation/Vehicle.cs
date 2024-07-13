using UnityEngine;
using UnityEngine.Serialization;

namespace Simulation {
    public class Vehicle : MonoBehaviour {
        
        [SerializeField] private VehicleSignal blinkerRightGO = null!;
        [SerializeField] private VehicleSignal blinkerLeftGO = null!;
        [SerializeField] private VehicleSignal brakeLightGO = null!;
        
        public string ID { get; internal set; }
        
        public void UpdatePosition(Vector3 position, Quaternion rotation) {
            transform.position = position;
            transform.rotation = rotation;
        }

        public void UpdateSignals(bool blinkerRight, bool blinkerLeft, bool brakeLight) {
            this.blinkerRightGO.SetSignal(blinkerRight);
            this.blinkerLeftGO.SetSignal(blinkerLeft);
            this.brakeLightGO.SetSignal(brakeLight);
        }
    }
}