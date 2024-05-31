using System;
using System.Collections;
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

        private void HandleIncomingData() {
            try {
                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Received: " + response);
                
                ProcessResponse(response);
                // TODO Update Unity objects based on the received data
            }
            catch (Exception e) {
                Debug.LogError("Error: " + e);
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
            Debug.Log("Sent: testMessage");
            
        }

        private void ProcessResponse(string response) {

            Debug.Log(response);
            
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
    
    public struct VehicleInfo {
        private readonly string id;
        private readonly float positionX;
        private readonly float positionY;
        private readonly float rotation;

        public readonly string ID => id;

        public readonly float X => positionX;
        public readonly float Y => positionY;
        public readonly float Rotation => rotation;

        public VehicleInfo(string id, float x, float y, float rotation) {
            this.id = id;
            this.positionX = x;
            this.positionY = y;
            this.rotation = rotation;
        }
        
        public VehicleInfo VehicleInfoFromJson(string json) {
            return JsonUtility.FromJson<VehicleInfo>(json);
        }
    }
}