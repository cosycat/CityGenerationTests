using System;
using System.Collections.Generic;
using FreeFormGraph.Agents;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI {
    public class AgentIMGUIManager : MonoBehaviour {
        
        [SerializeField] private IMGUISettings imguiSettings = new();

        private readonly List<AgentIMGUIElement> agentIMGUIElements = new();
        
        private void Awake() {
            // this needs to be in Awake, because AgentManager is initialized in Start, so we might miss the event for the initial agents
            FindObjectOfType<AgentManager>().AgentCreated += OnAgentCreated;
        }

        private void OnAgentCreated(IAgent agent) {
            agentIMGUIElements.Add(new AgentIMGUIElement(agent));
        }

        private void OnGUI() {
            
            GUILayout.BeginArea(new Rect(Screen.width - imguiSettings.width, 10, 150, Screen.height - 10));
            GUILayout.Label("Agents");
            foreach (var agentIMGUI in agentIMGUIElements) {
                agentIMGUI.AgentEntryGUI();
            }

            GUILayout.EndArea();
        }
    }

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
                    IMGUIEnum(connectionHandlingEnum);
                    break;
                default:
                    Debug.Log($"AgentMenuItemFactory.Initialize not implemented for this type of AgentVariable: {variable.GetType()}.");
                    GUILayout.Label("Not implemented.");
                    break;
            }
        }
        
    }

    [Serializable]
    public class IMGUISettings {
        public int width = 200;
    }
}