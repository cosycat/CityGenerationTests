#nullable enable
using System;
using System.Collections.Generic;
using FreeFormGraph;
using FreeFormGraph.World;
using SUMO;
using UnityEngine;

namespace Simulation {
    public class SimulationManager : MonoBehaviour {
        
        private readonly Dictionary<string, Vehicle> vehicles = new();
        private GameObject vehicleParent = null!;
        
        [SerializeField] private Vehicle vehiclePrefab = null!;

        private IWorld? world;

        private void Awake() {
            if (vehiclePrefab == null) {
                Debug.LogError("Vehicle prefab not set.");
                vehiclePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Vehicle>();
                vehiclePrefab.gameObject.SetActive(false);
                vehiclePrefab.name = "VehicleDebugPrefab";
            }

            vehicleParent = new GameObject("Vehicles");
            // vehicleParent.transform.parent = transform;
        }

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
            var idsStillActive = new HashSet<string>();
            foreach (var vehicleInfo in e.VehicleInfo) {
                UpdateOrCreateVehicle(vehicleInfo);
                idsStillActive.Add(vehicleInfo.id);
            }
            
            var keys = new List<string>(vehicles.Keys);
            foreach (var id in keys) {
                if (!idsStillActive.Contains(id)) {
                    Destroy(vehicles[id].gameObject, 0.2f);
                    vehicles.Remove(id);
                }
            }
            
        }

        private void UpdateOrCreateVehicle(VehicleInfo vehicleInfo) {
            var position2D = new Vector2(vehicleInfo.positionX / Constants.METERS_PER_UNIT, vehicleInfo.positionY / Constants.METERS_PER_UNIT);
            var id = vehicleInfo.id;
            var worldHeight = world!.GetHeightAt(position2D.x, position2D.y);
            if (!vehicles.TryGetValue(id, out var vehicle)) {
                vehicle = Instantiate(vehiclePrefab, new Vector3(position2D.x, worldHeight, position2D.y), Quaternion.identity);
                vehicle.transform.parent = vehicleParent.transform;
                vehicle.ID = id;
                vehicles.Add(id, vehicle);
            }
            vehicle.UpdatePosition(new Vector3(position2D.x, worldHeight, position2D.y), Quaternion.Euler(0, vehicleInfo.rotation + 180, 0));
            vehicle.UpdateSignals(vehicleInfo.BlinkerRight, vehicleInfo.BlinkerLeft, vehicleInfo.BrakeLight);
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