using System;
using FreeFormGraph;
using FreeFormGraph.Agents;
using UnityEngine;

namespace SUMO {
    [RequireComponent(typeof(SumoNetworkManager))]
    public class SumoUIHandler : MonoBehaviour {
        
        [SerializeField] private bool runSimulationAfterGeneration = false;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = false;
        
        private SumoNetworkManager networkManager;
        private IStreetGraph graph;
        
        private void Awake() {
            networkManager = GetComponent<SumoNetworkManager>();
            graph = FindObjectOfType<StreetGraphGameObject>();
        }

        private void OnGUI() {
            GUILayout.BeginArea(new Rect(10, 500, 150, 100));
            if (GUILayout.Button("Convert to Sumo")) {
                AgentManager.Instance.RequestStopAgents(() => {
                    networkManager.GenerateNetwork(graph, 
                        convertToSumoNetwork: true,
                        runSimulationAfterGeneration: runSimulationAfterGeneration, 
                        openFolderAfterGeneration: openFolderAfterGeneration, 
                        openSumoGUIAfterGeneration: openSumoGUIAfterGeneration,
                        onDone: () => {
                            Debug.Log("Done generating SUMO network.");
                            networkManager.StartClient();
                            // AgentManager.Instance.RestartAgents();
                        });
                });
            }

            if (!networkManager.IsNetworkGenerated) {
                GUILayout.EndArea();
                return;
            }

            if (!networkManager.IsSimulationRunning) {
                if (GUILayout.Button("Connect Simulation")) {
                    networkManager.StartClient();
                }
            }
            else {
                if (GUILayout.Button("Stop Simulation")) {
                    networkManager.RequestStopSimulation();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}