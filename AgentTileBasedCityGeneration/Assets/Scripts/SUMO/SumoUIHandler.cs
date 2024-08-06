#nullable enable
using System;
using FreeFormGraph;
using FreeFormGraph.Agents;
using FreeFormGraph.World;
using Simulation;
using UnityEngine;

namespace SUMO {
    [RequireComponent(typeof(SumoNetworkConverter))]
    public class SumoUIHandler : MonoBehaviour {
        
        [SerializeField] private bool runSimulationAfterGeneration = false;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = false;
        
        private SumoNetworkConverter networkConverter = null!;
        private IStreetGraph graph = null!;
        private IWorld world = null!;
        private SimulationManager simulationManager = null!;
        
        private void Awake() {
            networkConverter = GetComponent<SumoNetworkConverter>();
            graph = FindObjectOfType<StreetGraphGameObject>();
            world = FindObjectOfType<WorldGameObject>();
            simulationManager = FindObjectOfType<SimulationManager>();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Return)) {
                ConvertToSumo();
            }
        }

        private void OnGUI() {
            GUILayout.BeginArea(new Rect(10, 500, 150, 100));
            if (GUILayout.Button("Convert to Sumo")) {
                ConvertToSumo();
            }

            if (!networkConverter.IsNetworkGenerated) {
                GUILayout.EndArea();
                return;
            }

            if (networkConverter.IsSimulationRunning) {
                if (simulationManager.IsPaused) {
                    if (GUILayout.Button("Resume Simulation")) {
                        simulationManager.ResumeSimulation();
                    }
                }
                else {
                    if (GUILayout.Button("Pause Simulation")) {
                        simulationManager.PauseSimulation();
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void ConvertToSumo() {
            AgentManager.Instance.RequestStopAgents(() => {
                networkConverter.GenerateNetwork(graph, world,
                    convertToSumoNetwork: false,
                    runSimulationAfterGeneration: runSimulationAfterGeneration, 
                    openFolderAfterGeneration: openFolderAfterGeneration, 
                    openSumoGUIAfterGeneration: openSumoGUIAfterGeneration,
                    onDone: () => {
                        Debug.Log("Done generating SUMO network.");
                        simulationManager.StartSimulation();
                        // AgentManager.Instance.RestartAgents();
                    });
            });
        }
    }
}