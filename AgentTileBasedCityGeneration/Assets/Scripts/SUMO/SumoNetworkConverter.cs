#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using FreeFormGraph;
using FreeFormGraph.World;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SUMO {
    /// <summary>
    /// Generates a SUMO network from a given graph.
    /// </summary>
    public class SumoNetworkConverter : MonoBehaviour {
        private const string
            NETCONVERT_PATH_HOMEBREW =
                "/opt/homebrew/bin/netconvert"; // TODO Add more systems and installations. This only works on macOS with Homebrew installation of SUMO. alternatives: "/usr/local/bin/netconvert"

        private const string SUMO_GUI_PATH_HOMEBREW = "/opt/homebrew/bin/sumo-gui";
        private const string SUMO_EXECUTION_PATH_HOMEBREW = "/opt/homebrew/bin/sumo";
        private const string PYTHON_PATH = "/opt/homebrew/bin/python3";

        private const string SUMO_PYTHON_SIMULATION_SCRIPT_PATH = "Resources/Sumo/traciConnector.py";
        private const string SUMO_FILES_TO_COPY_PATH = "Resources/Sumo";

        [field: SerializeField] public SumoSimulationOptions SimulationOptions { get; private set; } = new();

        private string SumoGeneratedFilesPath { get; set; } = null!;
        private string SumoPythonSimulationScriptPath { get; set; } = null!;

        private SumoFileGenerator? sumoFileGenerator;
        private SumoClient sumoClient = null!;

        public bool IsNetworkGenerated => sumoFileGenerator != null;
        public bool IsSimulationRunning => sumoClient.IsConnected;
        private bool SimulationStopRequested { get; set; }

        private string NetconvertCommand => sumoFileGenerator == null
            ? "netconvert"
            : $"netconvert " +
              $"--node-files=\"{sumoFileGenerator.NodesFilePath}\" " +
              $"--edge-files=\"{sumoFileGenerator.EdgesFilePath}\" " +
              $"--type-files=\"{sumoFileGenerator.EdgesTypesFilePath}\" " +
              $"--output-file=\"{sumoFileGenerator.OutputNetFilePath}\" " +
              $"--offset.disable-normalization=\"true\"\n";

        private string SumoExecutionCommand => sumoFileGenerator == null
            ? "sumo"
            : $"sumo -c \"{sumoFileGenerator.ConfigurationFilePath}\" --remote-port {SumoClient.SUMO_PORT}\n";

        private void Awake() {
            SumoGeneratedFilesPath = $"{Application.persistentDataPath}/SUMO";
            SumoPythonSimulationScriptPath = $"{Application.dataPath}/{SUMO_PYTHON_SIMULATION_SCRIPT_PATH}";
            sumoClient = FindObjectOfType<SumoClient>() ??
                         new GameObject("SumoClient") { transform = { parent = transform } }.AddComponent<SumoClient>();
        }

        /// <summary>
        /// Generates a SUMO network from a given graph.
        /// </summary>
        /// <param name="graph"> The graph to generate the network from. </param>
        /// <param name="world"> The world to generate the network in. </param>
        /// <param name="convertToSumoNetwork"> Whether to convert the generated plain xml files to a SUMO network with netconvert. see https://sumo.dlr.de/docs/Networks/PlainXML.html for more info. </param>
        /// <param name="runSimulationAfterGeneration"> Whether to run the simulation via the python script after the network has been generated. </param>
        /// <param name="openFolderAfterGeneration"> Whether to open the SUMO folder where the files have been generated in, after the network has been generated. </param>
        /// <param name="openSumoGUIAfterGeneration"> Whether to open the SUMO GUI after the network has been generated. </param>
        /// <param name="onDone"> An action to be executed after all tasks have been completed. </param>
        public void GenerateNetwork(IStreetGraph graph, IWorld world, bool convertToSumoNetwork = true,
            bool runSimulationAfterGeneration = true, bool openFolderAfterGeneration = false,
            bool openSumoGUIAfterGeneration = false, Action? onDone = null) {
            Debug.Log(
                $"Generating SUMO network from graph with {graph.NodeCount} nodes and {graph.EdgeCount} edges in {SumoGeneratedFilesPath}..");
            sumoFileGenerator = SumoFileGenerator.Create(SumoGeneratedFilesPath, graph, SimulationOptions, world);
            CopyFilesToFolder();
            StartCoroutine(DoBackgroundTasks(convertToSumoNetwork, runSimulationAfterGeneration,
                openFolderAfterGeneration, openSumoGUIAfterGeneration, onDone));
        }

        private void CopyFilesToFolder() {
            var files = System.IO.Directory.GetFiles($"{Application.dataPath}/{SUMO_FILES_TO_COPY_PATH}");
            foreach (var file in files) {
                if (file.EndsWith(".meta")) continue;
                var destinationPath = $"{SumoGeneratedFilesPath}/{System.IO.Path.GetFileName(file)}";
                if (System.IO.File.Exists(destinationPath)) System.IO.File.Delete(destinationPath);
                System.IO.File.Copy(file, destinationPath);
            }

            CreateShellScript();
        }

        private void CreateShellScript() {
            if (sumoFileGenerator == null) {
                Debug.LogError("No SumoFileGenerator found. Did you call GenerateNetwork first?");
                return;
            }

            var scriptPath = $"{SumoGeneratedFilesPath}/run_sumo.sh";
            var script =
                $"#!/bin/bash\n" +
                $"\n" +
                $"# Run this script to generate the SUMO network and run the simulation.\n" +
                $"# Alternatively, you can copy the following commands and run them in your terminal.\n" +
                $"# Make sure to have sumo and netconvert installed.\n" +
                $"#\n" +
                $"# run with:" +
                $"# chmod u+x run_sumo.sh && ./run_sumo.sh\n" +
                $"\n" +
                $"# Convert the graph to a SUMO network\n" +
                NetconvertCommand +
                $"# Run the simulation\n" +
                SumoExecutionCommand;
            System.IO.File.WriteAllText(scriptPath, script);
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
        private IEnumerator DoBackgroundTasks(bool convertToSumoNetwork, bool runSimulationAfterGeneration,
            bool openFolderAfterGeneration, bool openSumoGUIAfterGeneration, Action? onDone) {
            if (convertToSumoNetwork) yield return ConvertToSumoNetwork();

            if (openFolderAfterGeneration) yield return OpenSUMOFolder();

            if (openSumoGUIAfterGeneration) yield return OpenSumoGUI();

            onDone?.Invoke();
        }

        /// <summary>
        /// Converts the generated plain xml files to a SUMO network with netconvert.
        /// Currently not used, as it only works on macOS with Homebrew installation of SUMO.
        /// </summary>
        /// <returns> An enumerator for the coroutine. </returns>
        private IEnumerator ConvertToSumoNetwork() {
            Debug.Log("Converting to SUMO network not implemented, use manual command in README.");
            yield return null;
        }

        public void RequestStopSimulation() {
            SimulationStopRequested = true;
        }

        private IEnumerator OpenSUMOFolder() {
            using var process = new Process();
            process.StartInfo.FileName = "open";
            process.StartInfo.Arguments = $"\"{SumoGeneratedFilesPath}\"";

            process.Start();

            while (!process.HasExited) yield return null;

            if (process.ExitCode != 0)
                Debug.LogError($"Failed to open folder with exit code {process.ExitCode}");
            else
                Debug.Log($"Opened folder {SumoGeneratedFilesPath}.");
        }

        /// <summary>
        /// Opens the SUMO GUI with the generated configuration file.
        /// Currently not used, as it only works on macOS with Homebrew installation of SUMO.
        /// </summary>
        /// <returns> An enumerator for the coroutine. </returns>
        private IEnumerator OpenSumoGUI() {
            Debug.Log("Opening SUMO GUI not implemented.");
            yield return null;
        }
    }
}