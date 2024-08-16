using System.Collections.Generic;
using UnityEngine;

namespace AgentSystem.UI {
    /// <summary>
    /// This class is responsible for managing the IMGUI for all the agents.
    /// </summary>
    public class AgentIMGUIManager : MonoBehaviour {
        
        [SerializeField] private IMGUISettings imguiSettings = new();

        private readonly List<AgentIMGUIElement> agentIMGUIElements = new();
        
        private Vector2 scrollPosition;
        
        private void Awake() {
            // this needs to be in Awake, because AgentManager is initialized in Start, so we might miss the event for the initial agents
            FindObjectOfType<AgentManager>().AgentCreated += OnAgentCreated;
        }

        private void OnAgentCreated(IAgent agent) {
            agentIMGUIElements.Add(new AgentIMGUIElement(agent));
        }

        private void OnGUI() {
            
            GUILayout.BeginArea(new Rect(Screen.width - imguiSettings.width, 10, imguiSettings.width, Screen.height - 10));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Agents");
            foreach (var agentIMGUI in agentIMGUIElements) {
                agentIMGUI.AgentEntryGUI();
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}