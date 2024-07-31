#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodingConnected.TraCI.NET;
using CodingConnected.TraCI.NET.Types;
using FreeFormGraph;
using UnityEngine;


namespace SUMO {
    
    /// <summary>
    /// Connects to a Python server running a SUMO simulation and receives data.
    /// </summary>
    public class SumoClient : MonoBehaviour {
        private const float TIME_STEP_SECONDS = 0.03f;
        private const int SUMO_PORT = 4321;

        private static readonly List<byte> VariablesToSubscribeTo = new() {
            TraCIConstants.VAR_POSITION, TraCIConstants.VAR_ANGLE, TraCIConstants.VAR_SPEED, TraCIConstants.VAR_SIGNALS,
            TraCIConstants.VAR_TYPE
        };

        private Task? connectionTask = null;
        private TraCIClient client = new();
        private float totalSimulationTime = 0f;
        private float timeSinceLastUpdate = 0f;
        private int step = 0;
        
        
        public bool IsConnected => connectionTask is { IsCompleted: true };
        
        private bool StopRequested { get; set; }

        public void StartClient() {
            client = new TraCIClient();
            connectionTask = client.ConnectAsync("127.0.0.1", SUMO_PORT);
            connectionTask.ContinueWith(task => {
                if (task.IsFaulted) {
                    Debug.LogError("Connection failed: " + task.Exception);
                }
                Debug.Log("Connected to SUMO server.");
                
            });
            // client.VehicleSubscription += OnClientOnVehicleSubscription;
        }

        // private void OnClientOnVehicleSubscription(object sender, SubscriptionEventArgs args) {
        //     
        //     // from https://github.com/CodingConnected/CodingConnected.Traci/blob/master/TracCI.NET-Usage-example/UsageExample.cs
        //     foreach (var r in args.Responses) {
        //         /* Responses are object that can be cast to IResponseInfo, so we can retrieve
        //          the variable type. */
        //         var respInfo = r as IResponseInfo;
        //         if (respInfo == null) {
        //             Debug.LogError("respInfo is null");
        //             continue;
        //         }
        //         var variableCode = respInfo.Variable;
        //
        //         /*We can then cast to TraCIResponse to get the Content
        //          We can also use IResponseInfo.GetContentAs<> ()s*/
        //         // WARNING using TraCIResponse<> we must use the exact type (i.e for speed, accel, angle, is double and not float)
        //         switch (variableCode) {
        //             case TraCIConstants.VAR_POSITION:
        //                 var position = respInfo.GetContentAs<Position2D>();
        //                 break;
        //             case TraCIConstants.VAR_ANGLE:
        //                 
        //             
        //             default:
        //                 /* Intentionaly ommit VAR_ACCEL*/
        //                 Console.WriteLine($" Variable with code {ByteToHex(variableCode)} not handled ");
        //                 break;
        //         }
        //     }
        // }

        private void HandleTraCI() {
            // TODO maybe call this in a coroutine instead of Update
            if (connectionTask == null || !connectionTask.IsCompleted) return;
            
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
            if (stepsAdvanced == 0) Debug.LogWarning("No steps advanced.");
            if (stepsAdvanced > 1) Debug.LogWarning($"Advanced {stepsAdvanced} steps - simulation running behind.");
            step += stepsAdvanced;
            
            // // subscribe to all newly departed vehicles
            // var departedIDList = client.Simulation.GetDepartedIDList("");
            // foreach (var id in departedIDList.Content) {
            //     Debug.Log($"Vehicle {id} departed.");
            //     client.Vehicle.Subscribe(id, 0, 100_000, VariablesToSubscribeTo);
            // }
            
            
            var allVehiclesID = client.Vehicle.GetIdList();
            Debug.Log($"Number of vehicles: {allVehiclesID.Content.Count}");
            var vehicleInfoList = new List<VehicleInfo>();
            foreach (var id in allVehiclesID.Content) {
                var position = client.Vehicle.GetPosition(id);
                var angle = client.Vehicle.GetAngle(id);
                var speed = client.Vehicle.GetSpeed(id);
                var signals = client.Vehicle.GetSignals(id);
                var vehicleType = client.Vehicle.GetTypeID(id);
                vehicleInfoList.Add(new VehicleInfo(
                    id,
                    (float)position.Content.X,
                    (float)position.Content.Y,
                    (float)angle.Content,
                    signals.Content,
                    (float)speed.Content,
                    vehicleType.Content
                ));
            }
            OnSimulationAdvancedOneStep(vehicleInfoList.ToArray());
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