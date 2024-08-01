#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FreeFormGraph;
using FreeFormGraph.World;
using SUMO;
using UnityEngine;

namespace Simulation {
    public class SimulationManager : MonoBehaviour {
        
        private readonly Dictionary<string, Vehicle> vehicles = new();
        private GameObject vehicleParent = null!;
        
        [SerializeField] private Vehicle vehiclePrefab = null!;

        public Vehicle? PlayerVehicle { get; private set; }

        private IWorld? world;
        
        private SumoClient? sumoClient;

        private void Awake() {
            // if (vehiclePrefab == null) {
            //     Debug.LogError("Vehicle prefab not set.");
            //     vehiclePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Vehicle>();
            //     vehiclePrefab.gameObject.SetActive(false);
            //     vehiclePrefab.name = "VehicleDebugPrefab";
            // }

            vehicleParent = new GameObject("Vehicles");
            // vehicleParent.transform.parent = transform;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                if (sumoClient == null || !sumoClient.IsConnected) {
                    StartSimulation();
                }
                else if (sumoClient.IsPaused) {
                    ResumeSimulation();
                }
                else {
                    PauseSimulation();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Escape)) {
                StopSimulation();
            }

            if (Input.GetKeyDown(KeyCode.V)) {
                if (vehicles.Count == 0) return;
                // get random vehicle
                var vehicle = vehicles.ElementAt(UnityEngine.Random.Range(0, vehicles.Count)).Value;
                SetPlayerVehicle(vehicle);
            }
        }

        public void StartSimulation() {
            Debug.Log("Starting simulation...");
            sumoClient = FindObjectOfType<SumoClient>() ?? new GameObject("SumoClient").AddComponent<SumoClient>();
            world = FindObjectOfType<WorldGameObject>();

            if (!CheckSimulationValidity()) return;
            
            sumoClient.SimulationAdvancedOneStep += SumoClientOnSimulationAdvancedOneStep;
            sumoClient.StartClient(this);
        }
        
        
        private readonly object sumoStepLock = new();
        private void SumoClientOnSimulationAdvancedOneStep(object sender, VehicleEventArgs e) {
            if (!CheckSimulationValidity()) return;
            
            lock (sumoStepLock) {
                // Debug.Log($"Received {e.VehicleInfo.Length} vehicle data.");
                var idsStillActive = new HashSet<string>();
                foreach (var vehicleInfo in e.VehicleInfo) {
                    UpdateOrCreateVehicle(vehicleInfo);
                    idsStillActive.Add(vehicleInfo.id);
                }

                var keys = new List<string>(vehicles.Keys);
                foreach (var id in keys) {
                    if (!idsStillActive.Contains(id)) {
                        if (PlayerVehicle?.ID == id) {
                            SetPlayerVehicle(null);
                        }
                        Destroy(vehicles[id].gameObject, 0.2f);
                        vehicles.Remove(id);
                    }
                }
            }

        }

        private void UpdateOrCreateVehicle(VehicleInfo vehicleInfo) {
            var position2D = new Vector2(vehicleInfo.positionX / Constants.METERS_PER_UNIT, vehicleInfo.positionY / Constants.METERS_PER_UNIT);
            var id = vehicleInfo.id;
            var worldHeight = world!.GetHeightAt(position2D.x, position2D.y);
            if (!vehicles.TryGetValue(id, out var vehicle)) {
                vehicle = Instantiate(vehiclePrefab, new Vector3(position2D.x, worldHeight, position2D.y), Quaternion.identity);
                vehicle.ID = id;
                vehicle.transform.parent = vehicleParent.transform;
                vehicle.UpdatePosition(new Vector3(position2D.x, worldHeight, position2D.y), Quaternion.Euler(0, vehicleInfo.rotation, 0));
                vehicles.Add(id, vehicle);
            }
            else {
                vehicle.UpdatePosition(new Vector3(position2D.x, worldHeight, position2D.y), Quaternion.Euler(0, vehicleInfo.rotation, 0));
                vehicle.UpdateSignals(vehicleInfo.BlinkerRight, vehicleInfo.BlinkerLeft, vehicleInfo.BrakeLight);
                
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
            sumoClient?.StopClient();
        }
        
        public void PauseSimulation() {
            Debug.Log("Pausing simulation...");
            sumoClient?.PauseClient();
        }
        
        public void ResumeSimulation() {
            Debug.Log("Resuming simulation...");
            sumoClient?.ResumeClient();
        }

        public VehicleInfo? GetPlayerVehicleInfo() {
            if (PlayerVehicle == null) return null;
            return new VehicleInfo(PlayerVehicle.ID, PlayerVehicle.transform.position.x / Constants.METERS_PER_UNIT,
                PlayerVehicle.transform.position.z / Constants.METERS_PER_UNIT, PlayerVehicle.transform.rotation.eulerAngles.y, 0, 0,
                PlayerVehicle.VehicleType);
        }
        
        public event EventHandler<PlayerVehicleChangedEventArgs>? PlayerVehicleChanged;
        
        public void SetPlayerVehicle(Vehicle? vehicle) {
            Debug.Log($"Setting player vehicle from {PlayerVehicle?.ID ?? "null"} to {vehicle?.ID ?? "null"}");
            PlayerVehicle?.SetPlayerVehicle(false);
            var oldPlayerVehicle = PlayerVehicle;
            PlayerVehicle = vehicle;
            PlayerVehicle?.SetPlayerVehicle(true);
            PlayerVehicleChanged?.Invoke(this, new PlayerVehicleChangedEventArgs(oldPlayerVehicle, PlayerVehicle));
        }
    }
    
    public class PlayerVehicleChangedEventArgs : EventArgs {
        public Vehicle? OldPlayerVehicle { get; }
        public Vehicle? NewPlayerVehicle { get; }

        public PlayerVehicleChangedEventArgs(Vehicle? oldPlayerVehicle, Vehicle? newPlayerVehicle) {
            OldPlayerVehicle = oldPlayerVehicle;
            NewPlayerVehicle = newPlayerVehicle;
        }
    }
}