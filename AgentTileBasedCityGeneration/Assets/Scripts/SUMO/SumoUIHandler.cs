using System;
using FreeFormGraph;
using FreeFormGraph.Agents;
using UnityEngine;

namespace SUMO {
    [RequireComponent(typeof(SumoNetworkGenerator))]
    public class SumoUIHandler : MonoBehaviour {
        
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
                    networkGenerator.GenerateNetwork(graph, true, true, true, () => {
                        Debug.Log("Done generating SUMO network.");
                        AgentManager.Instance.RestartAgents();
                    });
                });
            }
            GUILayout.EndArea();
        }
    }
}