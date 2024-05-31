using System;
using System.Net.Sockets;
using JetBrains.Annotations;
using UnityEngine;

namespace SUMO {
    public class SumoDirectClient : MonoBehaviour {
        private string Host { get; } = "localhost";
        private int Port { get; } = 7654;

        [CanBeNull] private SumoDirectConnector connector = new SumoDirectConnector();
        private CommandBuilder commandBuilder = new CommandBuilder();

        public void SimulationStep() {
            if (connector == null) {
                Debug.LogError("Connector is null.");
                return;
            }

            var command = commandBuilder.GetSimulationStepCommand();
            connector.SendCommand(command);
            var response = connector.ReceiveResponse();
            Debug.Log("Received: " + BitConverter.ToString(response));
        }

        public void StartConnection() {
            connector = new SumoDirectConnector();
            connector.Connect(Host, Port);
        }

        public void StopConnection() {
            connector?.Disconnect();
        }

        private void OnDestroy() {
            StopConnection();
        }
    }

    public class CommandBuilder {
        public byte[] GetSimulationStepCommand() {
            return new byte[] { 0x02 };
        }
    }


    public class SumoDirectConnector {
        private TcpClient client;
        private NetworkStream stream;

        public void Connect(string host, int port) {
            if (client != null) {
                Debug.LogError("Client already connected.");
                return;
            }

            client = new TcpClient(host, port);
            stream = client.GetStream();
        }

        public void Disconnect() {
            stream?.Close();
            client?.Close();
        }

        public void SendCommand(byte[] command) {
            stream!.Write(command, 0, command.Length);
        }

        public byte[] ReceiveResponse() {
            var buffer = new byte[4096]; // Adjust buffer size as needed
            var bytesRead = stream!.Read(buffer, 0, buffer.Length);
            var response = new byte[bytesRead];
            Array.Copy(buffer, response, bytesRead);
            return response;
        }
    }
}