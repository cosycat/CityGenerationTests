using System;
using AgentSystem.Agents;
using UnityEditor;
using UnityEngine;

namespace AgentSystem.UI {
    /// <summary>
    /// This class is responsible for displaying one agent in the IMGUI.
    /// </summary>
    public class AgentIMGUIElement {
        
        private readonly IAgent agent;
        private bool isFoldout = false;
        
        public AgentIMGUIElement(IAgent agent) {
            this.agent = agent;
        }
        
        public void AgentEntryGUI() {
            isFoldout = GUILayout.Toggle(isFoldout, agent.GetType().Name);

            if (!isFoldout) return;
            
            GUILayout.BeginVertical();
            foreach (var variable in agent.Parameters.AllVariables) {
                variable.OnGui();
            }
            GUILayout.Label("--------------------");
            GUILayout.EndVertical();
        }
        
    }
}