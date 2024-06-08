#nullable enable
using System.Collections.Generic;
using FreeFormGraph.World;
using SUMO;
using UnityEngine;

namespace Simulation {
    public class SimulationManager : MonoBehaviour {
        
        private readonly Dictionary<string, Vehicle> vehicles = new();
        
        [SerializeField] private Vehicle? vehiclePrefab;

        private IWorld? world;
        
        public void StartSimulation() {
            Debug.Log("Starting simulation...");
            var sumoClient = FindObjectOfType<SumoClient>() ?? new GameObject("SumoClient").AddComponent<SumoClient>();
            world = FindObjectOfType<WorldGameObject>();

            if (!CheckSimulationValidity()) return;
            
            sumoClient.VehicleDataReceived += SumoClientOnVehicleDataReceived;
            sumoClient.StartClient();
        }

        private void SumoClientOnVehicleDataReceived(object sender, VehicleEventArgs e) {
            if (!CheckSimulationValidity()) return;
            
            Debug.Log($"Received {e.VehicleInfo.Length} vehicle data.");
            foreach (var vehicleInfo in e.VehicleInfo) {
                var position2D = new Vector2(vehicleInfo.x, vehicleInfo.y);
                var worldHeight = world!.GetHeightAt(position2D.x, position2D.y);
                if (vehicles.TryGetValue(vehicleInfo.id, out var vehicle)) {
                    vehicle.transform.position = new Vector3(vehicleInfo.x, worldHeight, vehicleInfo.y);
                }
                else {
                    var newVehicle = Instantiate(vehiclePrefab, new Vector3(vehicleInfo.x, worldHeight, vehicleInfo.y), Quaternion.identity);
                    newVehicle!.ID = vehicleInfo.id;
                    vehicles.Add(vehicleInfo.id, newVehicle);
                }
            }
        }

        private bool CheckSimulationValidity() {
            if (world != null && vehiclePrefab != null) {
                return true;
            }
            
            Debug.LogError($"World not found or vehicle prefab not set. Aborting...");
            StopSimulation();
            return false;

        }

        public void StopSimulation() {
            Debug.Log("Stopping simulation...");
            var sumoClient = FindObjectOfType<SumoClient>();
            sumoClient?.StopClient();
        }
    }
}