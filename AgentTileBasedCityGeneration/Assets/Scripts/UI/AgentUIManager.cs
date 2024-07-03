using System;
using System.Collections.Generic;
using System.Linq;
using FreeFormGraph.Agents;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UI {
    
    [Serializable]
    public class UISettings {
        public int fontSize = 12;
    }
    public class AgentUIManager : MonoBehaviour {
        
        [SerializeField] private UISettings uiSettings = new();
        
        private UIDocument uiDocument;
        private TreeView agentsTreeView;
        
        private readonly List<IAgent> agents = new();
        
        private void Awake() {
            // this needs to be in Awake, because AgentManager is initialized in Start, so we might miss the event for the initial agents
            FindObjectOfType<AgentManager>().AgentCreated += OnAgentCreated;
            uiDocument = GetComponent<UIDocument>();
            // agentsTreeView = uiDocument.rootVisualElement.Q<TreeView>("agents-list");
            Debug.Log($"Found agents list: {agentsTreeView}");
        }

        private void Start() {
            var background = uiDocument.rootVisualElement.Q<VisualElement>("background");
            background.Add(new Label("Hello World!"));
            
            agentsTreeView = new TreeView();
            background.Add(agentsTreeView);
            agentsTreeView.style.flexGrow = 1;
            agentsTreeView.style.flexShrink = 1;
            agentsTreeView.style.flexBasis = 0;
            agentsTreeView.style.flexDirection = FlexDirection.Column;
            
            agentsTreeView.style.width = new StyleLength(500);
            agentsTreeView.style.height = new StyleLength(400);
            
            agentsTreeView.style.fontSize = uiSettings.fontSize;
            
            
            agentsTreeView.makeItem = () => {
                Debug.Log("agentsTreeView.makeItem");
                var agentEntry = new VisualElement();
                
                return agentEntry;
            };

            agentsTreeView.bindItem = (agentEntry, index) => {
                Debug.Log($"agentsTreeView.bindItem {index}");
                agentEntry.Clear();
                var agent = agents[index];
                // var agentTitle = element.Q<Label>();
                // agentTitle.text = agent.GetType().Name;
                
                // create title
                var agentTitle = new Label {
                    text = agent.GetType().Name,
                    style = {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        fontSize = uiSettings.fontSize
                    }
                };
                agentEntry.Add(agentTitle);
                
                // create menu items for each agent variable
                foreach (var agentVariable in agent.AgentVariables) {
                    var variableElement = AgentMenuItemFactory.Initialize(agentVariable, agentVariable.Name, uiSettings);
                    agentEntry.Add(variableElement);
                }
                
                // var agentTitle = element.Q<Label>();
                // agentTitle.text = agent.GetType().Name;
                
            };
            
            agentsTreeView.unbindItem = (agentEntry, index) => {
                Debug.Log($"agentsTreeView.unbindItem {index}");
                agentEntry.Clear();
            };
        }

        private void OnAgentCreated(IAgent agent) {
            Debug.Log($"Creating UI for new {agent.GetType()} agent.");
            agents.Add(agent);
            
            agentsTreeView.SetRootItems(treeRoots);

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

        // https://docs.unity3d.com/Manual/UIE-ListView-TreeView.html
        protected IList<TreeViewItemData<IAgent>> treeRoots {
            get {
                var id = 0;
                var roots = new List<TreeViewItemData<IAgent>>(agents.Count);
                foreach (var agent in agents) {
                    roots.Add(new TreeViewItemData<IAgent>(id++, agent));
                    // var planetsInGroup = new List<TreeViewItemData<IPlanetOrGroup>>(group.planets.Count);
                    // foreach (var planet in group.planets)
                    // {
                    //     planetsInGroup.Add(new TreeViewItemData<IPlanetOrGroup>(id++, planet));
                    // }
                    //
                    // roots.Add(new TreeViewItemData<IPlanetOrGroup>(id++, group, planetsInGroup));
                }

                return roots;
            }
        }
        
    }
}
