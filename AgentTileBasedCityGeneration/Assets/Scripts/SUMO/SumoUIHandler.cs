#nullable enable
using System;
using FreeFormGraph;
using FreeFormGraph.Agents;
using FreeFormGraph.World;
using Simulation;
using UnityEngine;

namespace SUMO {
    [RequireComponent(typeof(SumoNetworkManager))]
    public class SumoUIHandler : MonoBehaviour {
        
        [SerializeField] private bool runSimulationAfterGeneration = false;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = false;
        
        private SumoNetworkManager networkManager = null!;
        private IStreetGraph graph = null!;
        private IWorld world = null!;
        private SimulationManager simulationManager = null!;
        
        private void Awake() {
            networkManager = GetComponent<SumoNetworkManager>();
            graph = FindObjectOfType<StreetGraphGameObject>();
            world = FindObjectOfType<WorldGameObject>();
            simulationManager = FindObjectOfType<SimulationManager>();
        }

        private void OnGUI() {
            GUILayout.BeginArea(new Rect(10, 500, 150, 100));
            if (GUILayout.Button("Convert to Sumo")) {
                AgentManager.Instance.RequestStopAgents(() => {
                    networkManager.GenerateNetwork(graph, world,
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

            if (!networkManager.IsNetworkGenerated) {
                GUILayout.EndArea();
                return;
            }

            if (!networkManager.IsSimulationRunning) {
                if (GUILayout.Button("Start Simulation")) {
                    simulationManager.StartSimulation();
                }
            }
            else {
                if (GUILayout.Button("Stop Simulation")) {
                    simulationManager.StopSimulation();
                    networkManager.RequestStopSimulation();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}