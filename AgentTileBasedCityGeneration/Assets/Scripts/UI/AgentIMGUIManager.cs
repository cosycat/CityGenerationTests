using System;
using System.Collections.Generic;
using FreeFormGraph.Agents;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI {
    public class AgentIMGUIManager : MonoBehaviour {
        
        private readonly List<IAgent> agents = new();
        
        private void Awake() {
            // this needs to be in Awake, because AgentManager is initialized in Start, so we might miss the event for the initial agents
            FindObjectOfType<AgentManager>().AgentCreated += OnAgentCreated;
            
        }

        private void OnAgentCreated(IAgent obj) {
            agents.Add(obj);
        }

        private void OnGUI() {
            
        }
    }
    
    public static class AgentIMGUIElements {
        public static void AgentEntry(IAgent agent) {
            
        }
    }
}