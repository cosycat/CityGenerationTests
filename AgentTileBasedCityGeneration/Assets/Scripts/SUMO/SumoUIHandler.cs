using System;
using FreeFormGraph;
using FreeFormGraph.Agents;
using UnityEngine;

namespace SUMO {
    [RequireComponent(typeof(SumoNetworkGenerator))]
    public class SumoUIHandler : MonoBehaviour {
        
        [SerializeField] private bool runSimulationAfterGeneration = false;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = false;
        
        private SumoNetworkGenerator networkGenerator;
        private IStreetGraph graph;
        
        private void Awake() {
            networkGenerator = GetComponent<SumoNetworkGenerator>();
            graph = FindObjectOfType<StreetGraphGameObject>();
        }

        private void OnGUI() {
            GUILayout.BeginArea(new Rect(10, 500, 150, 100));
            if (GUILayout.Button("Convert to Sumo")) {
                AgentManager.Instance.RequestStopAgents(() => {
                    networkGenerator.GenerateNetwork(graph, 
                        convertToSumoNetwork: true,
                        runSimulationAfterGeneration: runSimulationAfterGeneration, 
                        openFolderAfterGeneration: openFolderAfterGeneration, 
                        openSumoGUIAfterGeneration: openSumoGUIAfterGeneration,
                        onDone: () => {
                            Debug.Log("Done generating SUMO network.");
                            // AgentManager.Instance.RestartAgents();
                        });
                });
            }

            if (!networkGenerator.IsNetworkGenerated) {
                GUILayout.EndArea();
                return;
            }

            if (!networkGenerator.IsSimulationRunning) {
                if (GUILayout.Button("Connect Simulation")) {
                    networkGenerator.StartClient();
                }
            }
            else {
                if (GUILayout.Button("Stop Simulation")) {
                    networkGenerator.RequestStopSimulation();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}