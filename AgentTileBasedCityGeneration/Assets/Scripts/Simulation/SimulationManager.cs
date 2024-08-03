#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FreeFormGraph;
using FreeFormGraph.World;
using SUMO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Simulation {
    public class SimulationManager : MonoBehaviour {
        
        private readonly Dictionary<string, Vehicle> vehicles = new();
        private GameObject vehicleParent = null!;
        
        [SerializeField] private Vehicle defaultVehiclePrefab = null!;

        public Vehicle? PlayerVehicle { get; private set; }

        private IWorld? world;
        
        private SumoClient? sumoClient;
        public bool IsPaused => sumoClient?.IsPaused ?? false;
        
        // Eventually this could be moved to a general options object, but for now, they just share the options.
        private SumoSimulationOptions? simulationOptions;
        public SumoSimulationOptions SimulationOptions => simulationOptions ?? FindObjectOfType<SumoNetworkConverter>()?.SimulationOptions ?? new SumoSimulationOptions();

        private void Awake() {
            vehicleParent = new GameObject("Vehicles");
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
            
            sumoClient.SimulationAdvancedOneStep += OnSimulationAdvancedOneStep;
            sumoClient.StartClient(this);
        }
        
        // Lock to prevent multiple updates interfering with each other.
        private readonly object sumoStepLock = new();
        private void OnSimulationAdvancedOneStep(object sender, VehicleEventArgs e) {
            if (!CheckSimulationValidity()) return;
            
            lock (sumoStepLock) {
                var idsStillActive = new HashSet<string>();
                foreach (var vehicleInfo in e.VehicleInfo) {
                    UpdateOrCreateVehicle(vehicleInfo);
                    idsStillActive.Add(vehicleInfo.id);
                }

                var keys = new List<string>(vehicles.Keys);
                
                foreach (var id in keys) {
                    if (idsStillActive.Contains(id)) continue;
                    
                    if (PlayerVehicle?.ID == id) {
                        SetPlayerVehicle(null);
                    }
                    Destroy(vehicles[id].gameObject, 0.2f);
                    vehicles.Remove(id);
                }
            }

        }

        private void UpdateOrCreateVehicle(VehicleInfo vehicleInfo) {
            var position = new Vector3(vehicleInfo.positionX / Constants.METERS_PER_UNIT, vehicleInfo.positionZ!, vehicleInfo.positionY / Constants.METERS_PER_UNIT);
            var id = vehicleInfo.id;
            if (!vehicles.TryGetValue(id, out var vehicle)) {
                var prefab = SimulationOptions.GetVehiclePrefab(vehicleInfo.vehicleType) ?? defaultVehiclePrefab;
                vehicle = Instantiate(prefab, position, Quaternion.Euler(0, vehicleInfo.rotation, 0));
                vehicle.ID = id;
                vehicle.transform.parent = vehicleParent.transform;
                vehicles.Add(id, vehicle);
            }
            else {
                vehicle.UpdatePosition(position, Quaternion.Euler(0, vehicleInfo.rotation, 0));
                vehicle.UpdateSignals(vehicleInfo.BlinkerRight, vehicleInfo.BlinkerLeft, vehicleInfo.BrakeLight);
            }
        }

        private bool CheckSimulationValidity() {
            if (world != null) {
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
                PlayerVehicle.transform.position.z / Constants.METERS_PER_UNIT, PlayerVehicle.transform.position.y, PlayerVehicle.transform.rotation.eulerAngles.y, 0, 0,
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