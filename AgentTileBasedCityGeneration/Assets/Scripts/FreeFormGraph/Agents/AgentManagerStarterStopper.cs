using System;
using UnityEngine;

namespace FreeFormGraph.Agents {
    public class AgentManagerStarterStopper : MonoBehaviour {
        private AgentManager agentManager;

        private AgentTestStatus currStatus = AgentTestStatus.Running;
        
        private void Start() {
            agentManager = FindObjectOfType<AgentManager>();
        }
        
        private void OnGUI() {
            if (GUI.Button(new Rect(100, 10, 50, 30), "Stop Agents")) {
                agentManager.RequestStopAgents(() => { Debug.Log("Stopped with button");});
            }
            if (GUI.Button(new Rect(100, 40, 50, 30), "Start Agents")) {
                agentManager.RestartAgents();
            }
        }
    }

    public enum AgentTestStatus {
        Running,
        Stopped,
    }
}