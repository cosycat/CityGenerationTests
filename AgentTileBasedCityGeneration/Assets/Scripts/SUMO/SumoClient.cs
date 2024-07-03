using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using FreeFormGraph;
using UnityEngine;


namespace SUMO {
    
    /// <summary>
    /// Connects to a Python server running a SUMO simulation and receives data.
    /// </summary>
    public class SumoClient : MonoBehaviour {
        
        private TcpClient socketConnection;
        private NetworkStream stream;

        private readonly Regex singleVehicleRegex = new Regex(@"\('(?<name>\w+\d+\.\d+)', \((?<x>\d+\.\d+), (?<y>\d+\.\d+)\)\)");
        
        private bool StopRequested { get; set; }

        public void StartClient() {
            StartCoroutine(WaitForConnection());
        }

        private void Update() {
            if (socketConnection == null)
                return;
            if (!socketConnection.Connected) {
                Debug.Log("Socket connection lost.");
                return;
            }
            if (stream == null) {
                Debug.Log("Stream is null.");
                return;
            }

            if (stream.CanRead && stream.DataAvailable) {
                HandleIncomingData();
            }
            
            if (stream.CanWrite) {
                HandleOutgoingData();
            }
        }

        private string answer = "";

        private void HandleIncomingData() {
            try {
                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // Debug.Log("Received: " + response);
                answer += response;
                
                // ProcessResponse(response);
            }
            catch (Exception e) {
                Debug.LogError("Error: " + e);
            }
            if (answer.Length == 0) return;
            Debug.Assert(answer[0] == '{');
            
            var shouldContinueChecking = true;
            while (shouldContinueChecking) {
                CheckForCompleteJson(out var isOnlyIncomplete);
                shouldContinueChecking = !isOnlyIncomplete;
            }
            return;

            void CheckForCompleteJson(out bool isOnlyIncomplete) {
                var bracketCount = 0;
                for (var i = 0; i < answer.Length; i++) {
                    var c = answer[i];
                    if (c == '{') {
                        bracketCount++;
                    }
                    else if (c == '}') {
                        bracketCount--;
                    }

                    if (bracketCount == 0) { // Found a complete JSON object. Process it.
                        var response = answer[..(i + 1)];
                        answer = answer[(i + 1)..].TrimStart(' ', '\n', '\r', '\t');
                        // Debug.Log($"Gathered response: {response}");
                        // Debug.Log($"Remaining answer: {answer}");
                        Debug.Assert(answer.Length == 0 || answer[0] == '{', "Remaining answer does not start with '{' character.");
                        ProcessResponse(response);
                        isOnlyIncomplete = false; // Continue checking for more JSON objects.
                        return;
                    }
                }
                isOnlyIncomplete = true;
            }
        }

        private void HandleOutgoingData() {
            if (StopRequested) {
                var stopMessage = Encoding.UTF8.GetBytes("stop");
                stream.Write(stopMessage, 0, stopMessage.Length);
                Debug.Log("Sent: stop");
                StopRequested = false;
                Cleanup();
                return;
            }
            
            var message = Encoding.UTF8.GetBytes("testMessage");
            stream.Write(message, 0, message.Length);
            // Debug.Log("Sent: testMessage");
            
        }

        private void ProcessResponse(string response) {
            Debug.Assert(response[0] == '{', "Response does not start with '{' character.");
            Debug.Assert(response[^1] == '}', "Response does not end with '}' character.");
            // Debug.Log(response);
            try {
                var simulationStepInfo = JsonUtility.FromJson<SimulationStepInfo>(response);
                // Debug.Log($"simulationStepInfo: {simulationStepInfo}");
                Debug.Assert(simulationStepInfo != null, "Could not parse JSON.");
                Debug.Assert(simulationStepInfo.vehicleList != null, "Could not parse JSON vehicle list.");
                var vehicleInfo = simulationStepInfo.vehicleList;
                OnVehicleDataReceived(vehicleInfo.ToArray());
            }
            catch (Exception e) {
                Debug.LogError("Error: " + e);
            }
            
            
            // // Parse with regex:
            // var matches = singleVehicleRegex.Matches(response);
            // var vehicleInfo = new VehicleInfo[matches.Count];
            // for (var i = 0; i < matches.Count; i++) {
            //     var match = matches[i];
            //     var vehicleID = match.Groups["name"].Value;
            //     var x = float.Parse(match.Groups["x"].Value) / Constants.METERS_PER_UNIT;
            //     var y = float.Parse(match.Groups["y"].Value) / Constants.METERS_PER_UNIT;
            //     // Debug.Log($"Vehicle {vehicleID} at ({x}, {y})");
            //     vehicleInfo[i] = new VehicleInfo(vehicleID, x, y);
            // }
            //
            // OnVehicleDataReceived(vehicleInfo);
        }

        private IEnumerator WaitForConnection() {
            while (socketConnection is not { Connected: true }) {
                yield return new WaitForSeconds(1f);
                try {
                    socketConnection = new TcpClient("localhost", 9999);
                    stream = socketConnection.GetStream();
                    Debug.Log("Connected to Python server.");
                    break;
                }
                catch (Exception e) {
                    Debug.Log("Socket error: " + e);
                }
            }
        }

        private void Cleanup() {
            if (socketConnection == null) return;
            
            stream.Flush();
            stream.Close();
            socketConnection.Close();
            socketConnection = null;
        }

        private void OnDestroy() {
            Cleanup();
        }

        private void OnApplicationQuit() {
            Cleanup();
        }
        
        public event EventHandler<VehicleEventArgs> VehicleDataReceived;
        
        protected virtual void OnVehicleDataReceived(VehicleInfo[] vehicleInfo) {
            VehicleDataReceived?.Invoke(this, new VehicleEventArgs(vehicleInfo));
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
        public int signals;
        public float speed;
        public string vehicleType;

        // public VehicleInfo(string id, float positionX, float positionY, float rotation, int signals, float speed, string vehicleType) {
        //     this.id = id;
        //     this.positionX = positionX;
        //     this.positionY = positionY;
        //     this.rotation = rotation;
        //     this.signals = signals;
        //     this.speed = speed;
        //     this.vehicleType = vehicleType;
        // }

        // public VehicleInfo VehicleInfoFromJson(string json) {
        //     return JsonUtility.FromJson<VehicleInfo>(json);
        // }
    }
}