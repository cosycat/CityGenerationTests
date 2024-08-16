#if UNITY_EDITOR
using UnityEditor;
#endif
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
            GUIStyle style;
#if UNITY_EDITOR
            style = EditorStyles.foldout;
#else
            style = new GUIStyle();
#endif
            isFoldout = GUILayout.Toggle(isFoldout, agent.GetName(), style);

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