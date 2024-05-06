using System;
using UnityEngine;

namespace FreeFormGraph.Agents {
    public class AgentManagerTest : MonoBehaviour {
        private AgentManager agentManager;

        private AgentTestStatus currStatus = AgentTestStatus.Running;
        
        private void Start() {
            agentManager = FindObjectOfType<AgentManager>();
        }

        private void Update() {
            // if (Time.timeSinceLevelLoad >= 10 && Time.timeSinceLevelLoad < 11 && currStatus == AgentTestStatus.Running) {
            //     agentManager.RequestStopAgents(() => {
            //         Debug.Log("we successfully stopped");
            //         currStatus = AgentTestStatus.Stopped;
            //     });
            // }
            //
            // if (Time.timeSinceLevelLoad >= 20 && currStatus == AgentTestStatus.Stopped) {
            //     Debug.Log("AgentManagerTest - Restarting");
            //     agentManager.RestartAgents();
            //     currStatus = AgentTestStatus.Running;
            // }
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