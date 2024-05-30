using System.Collections.Generic;
using SUMO;
using UnityEngine;

namespace Simulation {
    public class SimulationManager : MonoBehaviour {
        
        private readonly Dictionary<string, Vehicle> vehicles = new();
        
        [SerializeField] private Vehicle vehiclePrefab;
        
        public void StartSimulation() {
            Debug.Log("Starting simulation...");
            var sumoClient = FindObjectOfType<SumoClient>() ?? new GameObject("SumoClient").AddComponent<SumoClient>();
            
            sumoClient.VehicleDataReceived += SumoClientOnVehicleDataReceived;
            sumoClient.StartClient();
        }

        private void SumoClientOnVehicleDataReceived(object sender, VehicleEventArgs e) {
            Debug.Log($"Received {e.VehicleInfo.Length} vehicle data.");
            foreach (var vehicleInfo in e.VehicleInfo) {
                if (vehicles.TryGetValue(vehicleInfo.ID, out var vehicle)) {
                    vehicle.transform.position = new Vector3(vehicleInfo.X, 0, vehicleInfo.Y);
                }
                else {
                    var newVehicle = Instantiate(vehiclePrefab, new Vector3(vehicleInfo.X, 0, vehicleInfo.Y), Quaternion.identity);
                    newVehicle.ID = vehicleInfo.ID;
                    vehicles.Add(vehicleInfo.ID, newVehicle);
                }
            }
        }
    }
}