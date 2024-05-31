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
            // in the form [('flow0.0', (408.96483244156576, 993.5172486623486)), ('flow1.0', (1193.75, 96.292)), ('flow2.0', (1116.7817870872352, 749.2766081813702)), ('flow3.0', (813.2101233616037, 1225.5954109483423)), ('flow4.0', (1115.2254206744058, 75.44626202321726)), ('flow5.0', (1049.082758926685, 930.4783794633424)), ('flow6.0', (698.0927562127802, 1261.2860066123935)), ('flow7.0', (1109.1564639105732, 193.31633598780542)), ('flow8.0', (1127.1794682184423, 717.1429364368845)), ('flow9.0', (1106.9339902700074, 788.6468805643397)), ('trip0', (1108.291257072772, 69.00098569092397)), ('trip1', (1172.4955085714287, 597.1553866666666)), ('trip2', (682.4060290030122, 1291.63733936702)), ('trip3', (702.8526220990964, 1268.4785592503058)), ('trip4', (740.4439934533551, 1249.8210441898527)), ('trip5', (1132.0121627994356, 67.8395650323575)), ('trip6', (1142.359200610998, 54.63827902240325)), ('trip7', (693.3992914644351, 1296.7432674058578)), ('trip8', (710.8347109558676, 1277.7576068999801)), ('trip9', (1070.8437477381585, 899.3501362426823))]

            // Parse with regex:
            var matches = singleVehicleRegex.Matches(response);
            var vehicleInfo = new VehicleInfo[matches.Count];
            for (var i = 0; i < matches.Count; i++) {
                var match = matches[i];
                var vehicleID = match.Groups["name"].Value;
                var x = float.Parse(match.Groups["x"].Value) / Constants.METERS_PER_UNIT;
                var y = float.Parse(match.Groups["y"].Value) / Constants.METERS_PER_UNIT;
                // Debug.Log($"Vehicle {vehicleID} at ({x}, {y})");
                vehicleInfo[i] = new VehicleInfo(vehicleID, x, y);
            }
            
            OnVehicleDataReceived(vehicleInfo);
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
        public string ID { get; }
        public float X { get; }
        public float Y { get; }
        
        public VehicleInfo(string id, float x, float y) {
            ID = id;
            X = x;
            Y = y;
        }
    }
}