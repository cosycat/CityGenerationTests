#nullable enable
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
        
        private TcpClient? socketConnection;
        private NetworkStream stream = null!; // always initialized when socketConnection is not null

        private readonly Regex singleVehicleRegex = new Regex(@"\('(?<name>\w+\d+\.\d+)', \((?<x>\d+\.\d+), (?<y>\d+\.\d+)\)\)");
        
        private bool StopRequested { get; set; }

        public void StartClient() {
            StartCoroutine(WaitForConnection());
        }

        private void Update() {
            if (socketConnection == null)
                return;
            Debug.Assert(stream != null, "Stream is null when socket connection is not null.");

            if (!socketConnection.Connected) {
                Debug.Log("Socket connection lost.");
                return;
            }

            if (stream is { CanRead: true, DataAvailable: true }) {
                HandleIncomingData();
            }
            if (stream is { CanWrite: true }) {
                HandleOutgoingData();
            }
        }

        private string answer = "";

        private void HandleIncomingData() {
            try {
                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var newAnswer = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                answer += newAnswer;
            }
            catch (Exception e) {
                Debug.LogError("Error: " + e);
            }
            if (answer.Length == 0) return;
            
            var completeJson = CheckForCompleteJson(out var lastCompleteResponse);
            if (!completeJson) return; // we need to wait for more data to arrive
            
            var skippedFrames = 0;
            while (answer.Length > 0 && CheckForCompleteJson(out var response)) {
                // if we have multiple complete JSON objects, it means we are at least one frame behind,
                // so we skip to the last one where we already received all the data
                skippedFrames++;
                lastCompleteResponse = response;
            }

            if (skippedFrames > 0) {
                switch (skippedFrames) {
                    case > 1:
                        Debug.LogWarning($"Skipped {skippedFrames} frames!");
                        break;
                    case 1:
                        Debug.Log($"Skipped {skippedFrames} frames.");
                        break;
                }
            }

            ProcessResponse(lastCompleteResponse);
            
            return;

            bool CheckForCompleteJson(out string response) {
                Debug.Assert(answer[0] == '{', $"Answer does not start with '{{' character: {answer}");
                var bracketCount = 0;
                for (var i = 0; i < answer.Length; i++) {
                    switch (answer[i]) {
                        case '{':
                            bracketCount++;
                            break;
                        case '}':
                            bracketCount--;
                            break;
                    }

                    if (bracketCount == 0) {
                        // Found a complete JSON object. Process it.
                        response = answer[..(i + 1)];
                        // Skip answer to the next character after the JSON object
                        answer = answer[(i + 1)..].TrimStart(' ', '\n', '\r', '\t');
                        Debug.Assert(answer.Length == 0 || answer[0] == '{', "Remaining answer does not start with '{' character.");
                        // Debug.Log($"Gathered response: {response}");
                        // Debug.Log($"Remaining answer: {answer}");
                        return true;
                    }
                }
                response = "";
                return false;
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
                catch (SocketException e) {
                    // Debug.Log("Socket error: " + e);
                }
                catch (Exception e) {
                    Debug.Log("other error on connection: " + e);
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