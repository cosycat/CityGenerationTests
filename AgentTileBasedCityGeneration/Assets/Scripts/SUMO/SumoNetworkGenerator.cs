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
        private const string SUMO_EXECUTION_PATH_HOMEBREW = "/opt/homebrew/bin/sumo";
        private const string PYTHON_PATH = "/opt/homebrew/bin/python3";

        private const string SUMO_PYTHON_SIMULATION_SCRIPT_PATH = "Resources/Sumo/traciConnector.py";
        private const string SUMO_FILES_TO_COPY_PATH = "Resources/Sumo";
        
        public SumoSimulationOptions SimulationOptions { get; } = new();

        private string SumoGeneratedFilesPath { get; set; } = null!;
        private string SumoPythonSimulationScriptPath { get; set; } = null!;
        
        private SumoFileGenerator? sumoFileGenerator;
        private SumoClient sumoClient = null!;

        public bool IsNetworkGenerated => sumoFileGenerator != null;
        public bool IsSimulationRunning { get; private set; } = false;
        private bool SimulationStopRequested { get; set; }

        private void Awake() {
            SumoGeneratedFilesPath = $"{Application.persistentDataPath}/SUMO";
            SumoPythonSimulationScriptPath = $"{Application.dataPath}/{SUMO_PYTHON_SIMULATION_SCRIPT_PATH}";
            sumoClient = FindObjectOfType<SumoClient>() ?? new GameObject("SumoClient"){transform = { parent = this.transform }}.AddComponent<SumoClient>();
        }

        /// <summary>
        /// Generates a SUMO network from a given graph.
        /// </summary>
        /// <param name="graph"> The graph to generate the network from. </param>
        /// <param name="convertToSumoNetwork"> Whether to convert the generated plain xml files to a SUMO network with netconvert. see https://sumo.dlr.de/docs/Networks/PlainXML.html for more info. </param>
        /// <param name="runSimulationAfterGeneration"> Whether to run the simulation via the python script after the network has been generated. </param>
        /// <param name="openFolderAfterGeneration"> Whether to open the SUMO folder where the files have been generated in, after the network has been generated. </param>
        /// <param name="openSumoGUIAfterGeneration"> Whether to open the SUMO GUI after the network has been generated. </param>
        /// <param name="onDone"> An action to be executed after all tasks have been completed. </param>
        public void GenerateNetwork(IStreetGraph graph, bool convertToSumoNetwork = true, bool runSimulationAfterGeneration = true, bool openFolderAfterGeneration = false, bool openSumoGUIAfterGeneration = false, Action? onDone = null) {
            Debug.Log($"Generating SUMO network from graph with {graph.NodeCount} nodes and {graph.EdgeCount} edges in {SumoGeneratedFilesPath}..");
            sumoFileGenerator = SumoFileGenerator.Create(SumoGeneratedFilesPath, graph, SimulationOptions);
            CopyFilesToFolder();
            if (convertToSumoNetwork || openFolderAfterGeneration || openSumoGUIAfterGeneration) {
                StartCoroutine(DoBackgroundTasks(convertToSumoNetwork, runSimulationAfterGeneration, openFolderAfterGeneration, openSumoGUIAfterGeneration, onDone));
            }
        }
        
        private void CopyFilesToFolder() {
            var files = System.IO.Directory.GetFiles($"{Application.dataPath}/{SUMO_FILES_TO_COPY_PATH}");
            foreach (var file in files) {
                if (file.EndsWith(".meta")) continue;
                var destinationPath = $"{SumoGeneratedFilesPath}/{System.IO.Path.GetFileName(file)}";
                if (System.IO.File.Exists(destinationPath)) {
                    System.IO.File.Delete(destinationPath);
                }
                System.IO.File.Copy(file, destinationPath);
            }
            // var destinationPath = $"{SumoGeneratedFilesPath}/traciConnector.py";
            // if (System.IO.File.Exists(destinationPath)) {
            //     System.IO.File.Delete(destinationPath);
            // }
            // System.IO.File.Copy(SumoPythonSimulationScriptPath, destinationPath);
        }

        /// <summary>
        /// Sequential tasks to be executed in the background.
        /// </summary>
        /// <param name="convertToSumoNetwork"> Whether to call <see cref="ConvertToSumoNetwork"/> afterwards </param>
        /// <param name="runSimulationAfterGeneration"> Whether to call the python script to run the simulation afterwards </param>
        /// <param name="openFolderAfterGeneration"> Whether to call <see cref="OpenSUMOFolder"/> afterwards </param>
        /// <param name="openSumoGUIAfterGeneration"> Whether to call <see cref="OpenSumoGUI"/> afterwards </param>
        /// <param name="onDone"> An action to be executed after all tasks have been completed. </param>
        /// <returns> An enumerator for the coroutine. </returns>
        private IEnumerator DoBackgroundTasks(bool convertToSumoNetwork, bool runSimulationAfterGeneration, bool openFolderAfterGeneration, bool openSumoGUIAfterGeneration, Action? onDone) {
            if (convertToSumoNetwork) {
                yield return ConvertToSumoNetwork();
            }
            
            if (runSimulationAfterGeneration) {
                yield return RunSimulation();
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

        }

        private IEnumerator RunSimulation() {
            Debug.LogWarning("Does not work. Probably just run the python script manually.");
            if (sumoFileGenerator == null) {
                Debug.LogError("No SumoFileGenerator found. Did you call GenerateNetwork first?");
                yield break;
            }

            var arguments = $"\"{SumoPythonSimulationScriptPath}\" \"{sumoFileGenerator.ConfigurationFilePath}\"";
            var startInfo = new ProcessStartInfo {
                FileName = PYTHON_PATH,
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
            IsSimulationRunning = true;

            while (!process.HasExited) {
                if (SimulationStopRequested) {
                    process.Kill();
                    Debug.Log("Killed python script.");
                    SimulationStopRequested = false;
                }
                yield return null;
            }
            
            IsSimulationRunning = false;

            if (process.ExitCode != 0) {
                Debug.LogError($"Python script failed with exit code {process.ExitCode}");
            }
            else {
                Debug.Log("Python script completed successfully.");
            }
        }
        
        public void RequestStopSimulation() {
            SimulationStopRequested = true;
        }

        private IEnumerator OpenSUMOFolder() {
            using var process = new Process();
            process.StartInfo.FileName = "open";
            process.StartInfo.Arguments = $"\"{SumoGeneratedFilesPath}\"";

            process.Start();

            while (!process.HasExited) {
                yield return null;
            }
                    
            if (process.ExitCode != 0) {
                Debug.LogError($"Failed to open folder with exit code {process.ExitCode}");
            }
            else {
                Debug.Log($"Opened folder {SumoGeneratedFilesPath}.");
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

        public void StartClient() {
            sumoClient.StartClient();
        }
    }
}