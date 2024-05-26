using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SUMO {
    
    /// <summary>
    /// Connects to a Python server running a SUMO simulation and receives data.
    /// TODO: WIP
    /// </summary>
    public class SumoClient : MonoBehaviour
    {
        private TcpClient socketConnection;
        private NetworkStream stream;

        private void Start()
        {
            ConnectToPython();
        }

        private void Update()
        {
            if (socketConnection == null) return;

            try
            {
                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Received: " + response);

                // Example: Process data (e.g., update vehicle speed in Unity)
                var data = response.Split(';');
                var vehicleId = data[0];
                var speed = float.Parse(data[1]);
                Debug.Log($"Vehicle {vehicleId}, speed: {speed}, position: {data[2]}");
                
                // Update Unity objects based on the received data
            }
            catch (Exception e)
            {
                Debug.Log("Error: " + e);
            }
        }

        private void ConnectToPython()
        {
            try
            {
                socketConnection = new TcpClient("localhost", 9999);
                stream = socketConnection.GetStream();
                Debug.Log("Connected to Python server.");
            }
            catch (Exception e)
            {
                Debug.Log("Socket error: " + e);
            }
        }

        private void Cleanup() {
            if (socketConnection == null) return;
            
            stream.Close();
            socketConnection.Close();
        }

        private void OnDestroy() {
            Cleanup();
        }

        void OnApplicationQuit()
        {
            Cleanup();
        }
    }
}