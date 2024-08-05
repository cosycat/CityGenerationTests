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
        public const int SUMO_PORT = 4339;

        private static readonly List<byte> VariablesToSubscribeTo = new() {
            TraCIConstants.VAR_POSITION3D, TraCIConstants.VAR_ANGLE, TraCIConstants.VAR_SPEED,
            TraCIConstants.VAR_SIGNALS, TraCIConstants.VAR_TYPE
        };

        private Task? connectionTask = null;
        private TraCIClient client = new();
        private float totalSimulationTime = 0f;
        private float timeSinceLastUpdate = 0f;
        private int step = 0;

        private SimulationManager simulationManager = null!;

        public bool IsConnected => connectionTask is { IsCompletedSuccessfully: true };

        public bool IsPaused { get; private set; }

        private bool StopRequested { get; set; }

        public void StartClient(SimulationManager correspondingSimulationManager) {
            simulationManager = correspondingSimulationManager;
            client = new TraCIClient();
            connectionTask = client.ConnectAsync("127.0.0.1", SUMO_PORT);
            connectionTask.ContinueWith(task => {
                if (task.IsFaulted) Debug.LogWarning("Connection failed: " + task.Exception);
                else Debug.Log($"Connected to SUMO server: {task.IsCompletedSuccessfully}");
            });
            client.VehicleSubscription += OnVehicleChangedSubscription;
        }

        private readonly object updatedVehicleInfoListLock = new();
        private readonly List<VehicleInfo> updatedVehicleInfoList = new();

        private void OnVehicleChangedSubscription(object sender, SubscriptionEventArgs args) {
            // var vehicleInfo = new VehicleInfo(args.ObjectId);
            var id = args.ObjectId;
            Position3D? position3D = null;
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
                    case TraCIConstants.VAR_POSITION3D:
                        position3D = respInfo.GetContentAs<Position3D>();
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

            Debug.Assert(position3D != null && angle != null && speed != null && signals != null && vehicleType != null,
                $"Some values are null: {position3D}, {angle}, {speed}, {signals}, {vehicleType}");
            var vehicleInfo = new VehicleInfo(id, (float)position3D!.X, (float)position3D.Y, (float)position3D.Z,
                angle!.Value, signals!.Value, speed!.Value, vehicleType!);
            lock (updatedVehicleInfoListLock) {
                updatedVehicleInfoList.Add(vehicleInfo);
            }
        }

        private void UpdateTraCI() {
            // TODO maybe call this in a coroutine instead of Update
            if (connectionTask == null) return; // we have not yet tried to connect
            if (!connectionTask.IsCompleted) return; // we are still trying to connect
            if (!connectionTask.IsCompletedSuccessfully) { // The connection has failed or was cancelled, retry
                if (connectionTask.IsCanceled) return; // The connection was cancelled, do nothing
                if (connectionTask.IsFaulted) { // The connection has failed, retry
                    Debug.LogWarning("Connection failed, retrying...");
                    StartClient(simulationManager);
                    return;
                }

                Debug.LogError("Connection task is in an invalid state.");
            }

            if (!IsConnected) return; // we are connected, but not yet ready to start the simulation
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
            foreach (var id in departedIDList.Content) client.Vehicle.Subscribe(id, 0, 100_000, VariablesToSubscribeTo);

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
            UpdateTraCI();
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
}