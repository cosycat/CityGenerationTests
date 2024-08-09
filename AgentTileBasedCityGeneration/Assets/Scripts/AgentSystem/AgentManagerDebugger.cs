using UnityEngine;

namespace AgentSystem {
    public class AgentManagerDebugger : MonoBehaviour {
        [SerializeField] private int minTargetFPS = 1;
        [SerializeField] private int maxTargetFPS = 120;

        private AgentManager agentManager;

        private AgentTestStatus currStatus = AgentTestStatus.Running;

        private void Start() {
            agentManager = FindObjectOfType<AgentManager>();
        }

        private void OnGUI() {
            GUILayout.BeginArea(new Rect(250, 10, 200, 300));

            GUILayout.Label("Agent Manager");
            GUILayout.Label($"Is Running: {agentManager.IsAgentRunning}");

            GUILayout.Label($"Target FPS: {agentManager.TargetFramesPerSecond}");
            agentManager.TargetFramesPerSecond =
                Mathf.RoundToInt(GUILayout.HorizontalSlider(agentManager.TargetFramesPerSecond, minTargetFPS,
                    maxTargetFPS));

            GUILayout.Label($"Current Agent: {agentManager.CurrAgent}");
            GUILayout.Label($"Frames since last work: {agentManager.CurrAgentFramesSinceWorked}");

            if (currStatus == AgentTestStatus.Running) {
                if (GUILayout.Button("Stop Agents")) {
                    agentManager.RequestStopAgents(() => { Debug.Log("Stopped with button"); });
                    currStatus = AgentTestStatus.Stopped;
                }
            }
            else {
                if (GUILayout.Button("Start Agents")) {
                    Debug.Log("Starting agents with button");
                    agentManager.RestartAgents();
                    currStatus = AgentTestStatus.Running;
                }
            }

            GUILayout.EndArea();
        }
    }

    public enum AgentTestStatus {
        Running,
        Stopped
    }
}