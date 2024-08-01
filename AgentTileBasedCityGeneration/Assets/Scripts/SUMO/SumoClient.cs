#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodingConnected.TraCI.NET;
using CodingConnected.TraCI.NET.Types;
using Simulation;
using UnityEngine;


namespace SUMO {
    
    /// <summary>
    /// Starts a connection to a SUMO server and forwards the simulation.
    /// </summary>
    public class SumoClient : MonoBehaviour {
        public const float TIME_STEP_SECONDS = 0.03f;
        public const int SUMO_PORT = 4321;

        private static readonly List<byte> VariablesToSubscribeTo = new() {
            TraCIConstants.VAR_POSITION, TraCIConstants.VAR_ANGLE, TraCIConstants.VAR_SPEED, TraCIConstants.VAR_SIGNALS,
            TraCIConstants.VAR_TYPE
        };

        private Task? connectionTask = null;
        private TraCIClient client = new();
        private float totalSimulationTime = 0f;
        private float timeSinceLastUpdate = 0f;
        private int step = 0;
        
        private SimulationManager simulationManager = null!;
        
        public bool IsConnected => connectionTask is { IsCompleted: true };
        
        public bool IsPaused { get; private set; }
        
        private bool StopRequested { get; set; }

        public void StartClient(SimulationManager correspondingSimulationManager) {
            simulationManager = correspondingSimulationManager;
            client = new TraCIClient();
            connectionTask = client.ConnectAsync("127.0.0.1", SUMO_PORT);
            connectionTask.ContinueWith(task => {
                if (task.IsFaulted) {
                    Debug.LogError("Connection failed: " + task.Exception);
                }
                Debug.Log("Connected to SUMO server.");
                
            });
            client.VehicleSubscription += OnVehicleChangedSubscription;
        }

        private readonly object updatedVehicleInfoListLock = new();
        private readonly List<VehicleInfo> updatedVehicleInfoList = new();
        private void OnVehicleChangedSubscription(object sender, SubscriptionEventArgs args) {

            // var vehicleInfo = new VehicleInfo(args.ObjectId);
            string id = args.ObjectId;
            Position2D? position = null;
            float? angle = null;
            float? speed = null;
            int? signals = null;
            string? vehicleType = null;
            
            // from https://github.com/CodingConnected/CodingConnected.Traci/blob/master/TracCI.NET-Usage-example/UsageExample.cs
            foreach (var responseObject in args.Responses) {
                /* Responses are object that can be cast to IResponseInfo, so we can retrieve
                 the variable type. */
                if (responseObject is not IResponseInfo respInfo) {
                    Debug.LogError("respInfo is null");
                    continue;
                }
                var variableCode = respInfo.Variable;
        
                /*We can then cast to TraCIResponse to get the Content
                 We can also use IResponseInfo.GetContentAs<> ()s*/
                // WARNING using TraCIResponse<> we must use the exact type (i.e for speed, accel, angle, is double and not float)
                switch (variableCode) {
                    case TraCIConstants.VAR_POSITION:
                        position = respInfo.GetContentAs<Position2D>();
                        break;
                    case TraCIConstants.VAR_ANGLE:
                        angle = respInfo.GetContentAs<float>();
                        break;
                    case TraCIConstants.VAR_SPEED:
                        speed = respInfo.GetContentAs<float>();
                        break;
                    case TraCIConstants.VAR_SIGNALS:
                        signals = respInfo.GetContentAs<int>();
                        break;
                    case TraCIConstants.VAR_TYPE:
                        vehicleType = respInfo.GetContentAs<string>();
                        break;
                    
                    default:
                        Console.WriteLine($" Variable with code {variableCode} not handled ");
                        break;
                }
            }
            Debug.Assert(position != null && angle != null && speed != null && signals != null && vehicleType != null, $"Some values are null: {position}, {angle}, {speed}, {signals}, {vehicleType}");
            var vehicleInfo = new VehicleInfo(id, (float)position!.X, (float)(position.Y), angle!.Value, signals!.Value, speed!.Value, vehicleType!);
            lock (updatedVehicleInfoListLock) {
                updatedVehicleInfoList.Add(vehicleInfo);
            }
        }

        private void HandleTraCI() {
            // TODO maybe call this in a coroutine instead of Update
            if (connectionTask == null || !connectionTask.IsCompleted) return;
            if (IsPaused) return;
            
            totalSimulationTime += Time.deltaTime;
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate < TIME_STEP_SECONDS) return;
            
            // forwards the simulation by one step
            var stepsAdvanced = 0;
            while (timeSinceLastUpdate >= TIME_STEP_SECONDS) {
                timeSinceLastUpdate -= TIME_STEP_SECONDS;
                stepsAdvanced++;
                client.Control.SimStep();
            }
            Debug.Assert(stepsAdvanced > 0, "No steps advanced.");
            if (stepsAdvanced > 1) Debug.LogWarning($"Advanced {stepsAdvanced} steps - simulation running behind.");
            step += stepsAdvanced;
            
            // Update the player vehicle in the simulation
            // var playerVehicleInfo = simulationManager.GetPlayerVehicleInfo();
            // if (playerVehicleInfo != null) {
            //     client.Vehicle.MoveToXY(playerVehicleInfo.id, "-1", -1, playerVehicleInfo.positionX, playerVehicleInfo.positionY, playerVehicleInfo.rotation, 2);
            //     client.Vehicle.SetSpeed(playerVehicleInfo.id, playerVehicleInfo.speed);
            // }

            // subscribe to all newly departed vehicles
            var departedIDList = client.Simulation.GetDepartedIDList("");
            foreach (var id in departedIDList.Content) {
                // Debug.Log($"Vehicle {id} departed.");
                client.Vehicle.Subscribe(id, 0, 100_000, VariablesToSubscribeTo);
            }
            
            // Inform about all vehicles that have been updated
            var vehicleInfoList = new List<VehicleInfo>();
            lock (updatedVehicleInfoListLock) {
                vehicleInfoList.AddRange(updatedVehicleInfoList);
                updatedVehicleInfoList.Clear();
            }
            OnSimulationAdvancedOneStep(vehicleInfoList.ToArray());
            
            if (StopRequested) {
                client.Control.Close();
                Cleanup();
            }
        }
        

        private void Update() {
            HandleTraCI();
        }
        
        
        private void Cleanup() {
            client.Dispose();
        }

        private void OnDestroy() {
            Cleanup();
        }

        public event EventHandler<VehicleEventArgs>? SimulationAdvancedOneStep;
        
        protected virtual void OnSimulationAdvancedOneStep(VehicleInfo[] vehicleInfo) {
            SimulationAdvancedOneStep?.Invoke(this, new VehicleEventArgs(vehicleInfo));
        }

        public void StopClient() {
            StopRequested = true;
        }

        public void PauseClient() {
            IsPaused = true;
        }

        public void ResumeClient() {
            IsPaused = false;
        }
    }
    
    public class VehicleEventArgs : EventArgs {
        public VehicleInfo[] VehicleInfo { get; }
        
        public VehicleEventArgs(VehicleInfo[] vehicleInfo) {
            VehicleInfo = vehicleInfo;
        }
    }
    
    [Serializable]
    public class SimulationStepInfo {
        // public readonly float step;

        public int step;
        public List<VehicleInfo> vehicleList;
        
        // public SimulationStepInfo(List<VehicleInfo> vehicleList, int step) {
        //     this.vehicleList = vehicleList;
        //     this.step = step;
        // }
    }
    
    [Serializable]
    public class VehicleInfo {

        public string id;
        public float positionX;
        public float positionY;
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

        public VehicleInfo(string id, float positionX, float positionY, float rotation, int signals, float speed, string vehicleType) {
            this.id = id;
            this.positionX = positionX;
            this.positionY = positionY;
            this.rotation = rotation;
            this.signals = signals;
            this.speed = speed;
            this.vehicleType = vehicleType;
        }
        
    }
}