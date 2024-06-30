using System;
using System.Collections.Generic;
using FreeFormGraph.Agents;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI {
    public class AgentUIManager : MonoBehaviour {
        
        private UIDocument uiDocument;
        private TreeView agentsList;
        
        private List<IAgent> agents = new();
        
        private void Awake() {
            // this needs to be in Awake, because AgentManager is initialized in Start, so we might miss the event for the initial agents
            FindObjectOfType<AgentManager>().AgentCreated += OnAgentCreated;
            uiDocument = GetComponent<UIDocument>();
            agentsList = uiDocument.rootVisualElement.Q<TreeView>("agents-list");
            Debug.Log($"Found agents list: {agentsList}");
        }

        private void Start() {
            agentsList.makeItem = () => {
                var agentEntry = new VisualElement();

                // create title
                var agentTitle = new Label();
                agentEntry.Add(agentTitle);

                // create menu items for each agent variable
                
                return agentEntry;
            };

            agentsList.bindItem = (element, index) => {
                var agent = agents[index];
                var agentTitle = element.Q<Label>();
                agentTitle.text = agent.GetType().Name;
                
                foreach (var agentVariable in agent.AgentVariables) {
                    var variableElement = AgentMenuItemFactory.Initialize(agentVariable, agentVariable.Name);
                    element.Add(variableElement);
                }
            };
        }

        private void OnAgentCreated(IAgent agent) {
            Debug.Log($"Creating UI for new {agent.GetType()} agent.");
            agents.Add(agent);
            // create agent entry
            // agentsList.Add(agentEntry);

            // var agentCanvas = Instantiate(agentCanvasPrefab, agentsCanvas.transform);
            // agentCanvas.Initialize(agent.GetType().Name);
            // agentCanvases.Add(agentCanvas);
            // // create menu items for each agent variable
            // foreach (var agentVariable in agent.AgentVariables) {
            //     if (agentVariable is AgentVariableFloat agentVariableFloat) {
            //         Debug.Log($"Creating float slider for {agentVariableFloat.Name}.");
            //         var floatSliderAgentMenuItem = Instantiate(floatSliderAgentMenuItemPrefab, agentCanvas.transform);
            //         floatSliderAgentMenuItem.Initialize(agentVariableFloat, agentVariableFloat.Name);
            //     }
            // }
        }
        
    }
}
