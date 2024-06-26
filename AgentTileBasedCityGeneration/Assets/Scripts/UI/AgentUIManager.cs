using System.Collections.Generic;
using FreeFormGraph.Agents;
using UnityEngine;

namespace UI {
    public class AgentUIManager : MonoBehaviour {
        
        [SerializeField] private Canvas agentsCanvas;
        [SerializeField] private AgentCanvas agentCanvasPrefab;
        [SerializeField] private FloatSliderAgentMenuItem floatSliderAgentMenuItemPrefab;
        
        private List<AgentCanvas> agentCanvases = new();
        
        private void Awake() {
            // this needs to be in Awake, because AgentManager is initialized in Start, so we might miss the event for the initial agents
            FindObjectOfType<AgentManager>().AgentCreated += OnAgentCreated;
        }

        private void OnAgentCreated(IAgent agent) {
            Debug.Log($"Creating UI for new {agent.GetType()} agent.");
            // create title
            var agentCanvas = Instantiate(agentCanvasPrefab, agentsCanvas.transform);
            agentCanvas.Initialize(agent.GetType().Name);
            agentCanvases.Add(agentCanvas);
            // create menu items for each agent variable
            foreach (var agentVariable in agent.AgentVariables) {
                if (agentVariable is AgentVariableFloat agentVariableFloat) {
                    Debug.Log($"Creating float slider for {agentVariableFloat.Name}.");
                    var floatSliderAgentMenuItem = Instantiate(floatSliderAgentMenuItemPrefab, agentCanvas.transform);
                    floatSliderAgentMenuItem.Initialize(agentVariableFloat, agentVariableFloat.Name);
                }
            }
        }
        
    }
}
