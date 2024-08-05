#nullable enable
using System;

namespace SUMO {
    [Serializable]
    public class VehicleInfo {
        public string id;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float rotation;

        /// <summary>
        /// A vehicle's signals are encoded in an integer, each by one bit, encoding whether the according signal/... is on or off.
        /// The signals are encoded as follows:
        /// VEH_SIGNAL_BLINKER_RIGHT: bit 0
        /// VEH_SIGNAL_BLINKER_LEFT: bit 1
        /// VEH_SIGNAL_BRAKELIGHT: bit 3
        /// </summary>
        public int signals;

        public float speed;
        public string vehicleType;

        public bool BlinkerRight => (signals & 1) == 1;
        public bool BlinkerLeft => (signals & 2) == 2;
        public bool BrakeLight => (signals & 8) == 8;

        public VehicleInfo(string id, float positionX, float positionY, float positionZ, float rotation, int signals,
            float speed, string vehicleType) {
            this.id = id;
            this.positionX = positionX;
            this.positionY = positionY;
            this.rotation = rotation;
            this.signals = signals;
            this.speed = speed;
            this.vehicleType = vehicleType;
            this.positionZ = positionZ;
        }
    }
}