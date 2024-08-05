#nullable enable
using AgentSystem;
using Graph;
using Graph.World;
using Simulation;
using UnityEngine;

namespace SUMO {
    [RequireComponent(typeof(SumoNetworkConverter))]
    public class SumoUIHandler : MonoBehaviour {
        [SerializeField] private bool runSimulationAfterGeneration;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration;
        private IStreetGraph graph = null!;

        private SumoNetworkConverter networkConverter = null!;
        private SimulationManager simulationManager = null!;
        private IWorld world = null!;

        private void Awake() {
            networkConverter = GetComponent<SumoNetworkConverter>();
            graph = FindObjectOfType<StreetGraphGameObject>();
            world = FindObjectOfType<WorldGameObject>();
            simulationManager = FindObjectOfType<SimulationManager>();
        }

        private void OnGUI() {
            GUILayout.BeginArea(new Rect(10, 500, 150, 100));
            if (GUILayout.Button("Convert to Sumo"))
                AgentManager.Instance.RequestStopAgents(() => {
                    networkConverter.GenerateNetwork(graph, world,
                        false,
                        runSimulationAfterGeneration,
                        openFolderAfterGeneration,
                        openSumoGUIAfterGeneration,
                        () => {
                            Debug.Log("Done generating SUMO network.");
                            simulationManager.StartSimulation();
                            // AgentManager.Instance.RestartAgents();
                        });
                });

            if (!networkConverter.IsNetworkGenerated) {
                GUILayout.EndArea();
                return;
            }

            if (networkConverter.IsSimulationRunning) {
                if (simulationManager.IsPaused) {
                    if (GUILayout.Button("Resume Simulation")) simulationManager.ResumeSimulation();
                }
                else {
                    if (GUILayout.Button("Pause Simulation")) simulationManager.PauseSimulation();
                }
            }

            GUILayout.EndArea();
        }
    }
}