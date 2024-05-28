#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using FreeFormGraph;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SUMO {
    
    /// <summary>
    /// Generates a SUMO network from a given graph.
    /// </summary>
    public class SumoNetworkGenerator : MonoBehaviour {
        private const string NETCONVERT_PATH_HOMEBREW = "/opt/homebrew/bin/netconvert"; // TODO Add more systems and installations. This only works on macOS with Homebrew installation of SUMO. alternatives: "/usr/local/bin/netconvert"
        private const string SUMO_GUI_PATH_HOMEBREW = "/opt/homebrew/bin/sumo-gui";
        public SumoSimulationOptions SimulationOptions { get; } = new();

        private string SumoPath { get; set; }
        
        [CanBeNull] private SumoFileGenerator sumoFileGenerator;

        private void Awake() {
            SumoPath = $"{Application.persistentDataPath}/SUMO";
        }

        public void GenerateNetwork(IStreetGraph graph, bool convertToSumoNetwork = true, bool openFolderAfterGeneration = true, bool openSumoGUIAfterGeneration = true, Action? onDone = null) {
            Debug.Log($"Generating SUMO network from graph with {graph.NodeCount} nodes and {graph.EdgeCount} edges in {SumoPath}..");
            sumoFileGenerator = SumoFileGenerator.Create(SumoPath, graph, SimulationOptions);
            if (convertToSumoNetwork || openFolderAfterGeneration || openSumoGUIAfterGeneration) {
                StartCoroutine(DoBackgroundTasks(convertToSumoNetwork, openFolderAfterGeneration, openSumoGUIAfterGeneration, onDone));
            }
        }

        /// <summary>
        /// Sequential tasks to be executed in the background.
        /// </summary>
        /// <param name="convertToSumoNetwork"> Whether to call <see cref="ConvertToSumoNetwork"/> afterwards </param>
        /// <param name="openFolderAfterGeneration"> Whether to call <see cref="OpenSUMOFolder"/> afterwards </param>
        /// <param name="openSumoGUIAfterGeneration"> Whether to call <see cref="OpenSumoGUI"/> afterwards </param>
        /// <param name="onDone"></param>
        /// <returns> An enumerator for the coroutine. </returns>
        private IEnumerator DoBackgroundTasks(bool convertToSumoNetwork, bool openFolderAfterGeneration, bool openSumoGUIAfterGeneration, Action? onDone) {
            if (convertToSumoNetwork) {
                yield return ConvertToSumoNetwork();
            }

            if (openFolderAfterGeneration) {
                yield return OpenSUMOFolder();
            }

            if (openSumoGUIAfterGeneration) {
                yield return OpenSumoGUI();
            }
            
            onDone?.Invoke();
        }

        private IEnumerator ConvertToSumoNetwork() {
            if (sumoFileGenerator == null) {
                Debug.LogError("No SumoFileGenerator found. Did you call GenerateNetwork first?");
                yield break;
            }
            var nodesFilePath = sumoFileGenerator.NodesFilePath;
            var edgesFilePath = sumoFileGenerator.EdgesFilePath;
            var connectionsFilePath = sumoFileGenerator.ConnectionsFilePath;
            var routesFilePath = sumoFileGenerator.RoutesFilePath;
            var outputNetFilePath = sumoFileGenerator.OutputNetFilePath;
            var configurationFilePath = sumoFileGenerator.ConfigurationFilePath;

            var arguments = "";
            arguments += $"--node-files=\"{nodesFilePath}\" ";
            arguments += $"--edge-files=\"{edgesFilePath}\" ";
            // arguments += $"--connection-files=\"{connectionsFilePath}\" ";
            arguments += $"--output-file=\"{outputNetFilePath}\" ";

            var startInfo = new ProcessStartInfo {
                FileName = NETCONVERT_PATH_HOMEBREW,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (_, e) => Debug.Log(e.Data);
            process.ErrorDataReceived += (_, e) => Debug.LogError($"{e.GetType()}: {e.Data}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.HasExited) {
                yield return null;
            }

            if (process.ExitCode != 0) {
                Debug.LogError($"netconvert failed with exit code {process.ExitCode}");
            }
            else {
                Debug.Log("netconvert completed successfully.");
            }

            Debug.Log("Conversion completed.");
        }

        private IEnumerator OpenSUMOFolder() {
            using var process = new Process();
            process.StartInfo.FileName = "open";
            process.StartInfo.Arguments = $"\"{SumoPath}\"";

            process.Start();

            while (!process.HasExited) {
                yield return null;
            }
                    
            if (process.ExitCode != 0) {
                Debug.LogError($"Failed to open folder with exit code {process.ExitCode}");
            }
            else {
                Debug.Log($"Opened folder {SumoPath}.");
            }
        }
        
        private IEnumerator OpenSumoGUI() {
            if (sumoFileGenerator == null) {
                Debug.LogError("No SumoFileGenerator found. Did you call GenerateNetwork first?");
                yield break;
            }

            using var process = new Process();
            process.StartInfo.FileName = SUMO_GUI_PATH_HOMEBREW;
            process.StartInfo.Arguments = $"-c \"{sumoFileGenerator.ConfigurationFilePath}\"";

            process.Start();

            while (!process.HasExited) {
                yield return null;
            }

            if (process.ExitCode != 0) {
                Debug.LogError($"Failed to open SUMO GUI with exit code {process.ExitCode}");
            }
            else {
                Debug.Log("Opened SUMO GUI.");
            }
        }
    }
}