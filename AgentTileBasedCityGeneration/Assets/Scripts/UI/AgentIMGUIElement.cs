using System;
using AgentSystem;
using AgentSystem.Agents;
using UnityEditor;
using UnityEngine;

namespace UI {
    /// <summary>
    /// This class is responsible for displaying one agent in the IMGUI.
    /// </summary>
    public class AgentIMGUIElement {
        
        private IAgent agent;
        private bool isFoldout = false;
        
        public AgentIMGUIElement(IAgent agent) {
            this.agent = agent;
        }
        
        public void AgentEntryGUI() {
            GUILayout.BeginHorizontal();
            isFoldout = GUILayout.Toggle(isFoldout, agent.GetType().Name);
            GUILayout.EndHorizontal();

            if (!isFoldout) return;
            GUILayout.BeginVertical();
            foreach (var variable in agent.AgentVariables) {
                IMGUIMenuItem(variable);
            }
            GUILayout.Label("--------------------");
            GUILayout.EndVertical();
        }

        private static void IMGUISliderFloat(AgentVariableFloat variable) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(variable.Name);
            GUILayout.BeginVertical();
            variable.Value = GUILayout.HorizontalSlider(variable.Value, variable.Min, variable.Max);
            variable.Value = EditorGUILayout.FloatField(variable.Value);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void IMGUISliderInt(AgentVariableInt variable) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(variable.Name);
            GUILayout.BeginVertical();
            variable.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(variable.Value, variable.Min, variable.Max));
            variable.Value = EditorGUILayout.IntField(variable.Value);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void IMGUIToggle(AgentVariableBool variable) {
            GUILayout.BeginHorizontal();
            variable.Value = GUILayout.Toggle(variable.Value, variable.Name);
            GUILayout.EndHorizontal();
        }

        private static void IMGUIEnum<T>(AgentVariableEnum<T> variable) where T : Enum {
            GUILayout.BeginHorizontal();
            variable.Value = (T) EditorGUILayout.EnumPopup(variable.Name, variable.Value);
            GUILayout.EndHorizontal();
        }
        
        public static void IMGUIMenuItem(IAgentVariable variable) {
            switch (variable) {
                case AgentVariableFloat variableFloat:
                    IMGUISliderFloat(variableFloat);
                    break;
                case AgentVariableInt variableInt:
                    IMGUISliderInt(variableInt);
                    break;
                case AgentVariableBool variableBool:
                    IMGUIToggle(variableBool);
                    break;
                case AgentVariableEnum<Enum> variableEnum:
                    IMGUIEnum(variableEnum);
                    break;
                case AgentVariableEnum<SettlementDeveloperAgent.SdaParameters.ConnectionHandling> connectionHandlingEnum:
                    IMGUIEnum(connectionHandlingEnum); // A bit hacky that this is needed, but ok for now
                    break;
                default:
                    Debug.Log($"AgentMenuItemFactory.Initialize not implemented for this type of AgentVariable: {variable.GetType()}.");
                    GUILayout.Label("Not implemented.");
                    break;
            }
        }
        
    }
}