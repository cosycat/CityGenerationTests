using System;
using FreeFormGraph.Agents;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI {
    public static class AgentMenuItemFactory {

        public static VisualElement Initialize(IAgentVariable variable, string variableName, UISettings uiSettings) {
            switch (variable) {
                case AgentVariableFloat agentVariableFloat:
                    return InitializeSliderFloat(agentVariableFloat, variableName, uiSettings);
                case AgentVariableInt agentVariableInt:
                    return InitializeSliderInt(agentVariableInt, variableName, uiSettings);
                case AgentVariableBool agentVariableBool:
                    return InitializeToggle(agentVariableBool, variableName, uiSettings);
                case AgentVariableEnum<Enum> agentVariableEnum:
                    return InitializeEnum(agentVariableEnum, variableName, uiSettings);
                default:
                    Debug.Log($"AgentMenuItemFactory.Initialize not implemented for this type of AgentVariable: {variable.GetType()}.");
                    return new Label("Not implemented.");
            }
        }

        private static Slider InitializeSliderFloat(AgentVariableFloat variable, string variableName, UISettings uiSettings) {
            var slider = new Slider {
                label = variableName,
                lowValue = variable.Min,
                highValue = variable.Max,
                value = variable.Value,
                showInputField = true,
                tooltip = variable.Description,
                direction = SliderDirection.Horizontal,
                style = {
                    fontSize = uiSettings.fontSize,
                }
            };

            slider.RegisterValueChangedCallback(evt => slider.value = evt.newValue);

            return slider;
        }
        
        private static Slider InitializeSliderInt(AgentVariableInt variable, string variableName, UISettings uiSettings) {
            var slider = new Slider {
                label = variableName,
                lowValue = variable.Min,
                highValue = variable.Max,
                value = variable.Value,
                showInputField = true,
                tooltip = variable.Description,
                direction = SliderDirection.Horizontal,
                style = {
                    fontSize = uiSettings.fontSize,
                }
            };

            slider.RegisterValueChangedCallback(evt => {
                var newValue = Mathf.RoundToInt(evt.newValue);
                variable.Value = newValue;
                slider.value = newValue;
            });

            return slider;
        }
        
        private static Toggle InitializeToggle(AgentVariableBool variable, string variableName, UISettings uiSettings) {
            var toggle = new Toggle {
                label = variableName,
                value = variable.Value,
                tooltip = variable.Description,
                style = {
                    fontSize = uiSettings.fontSize,
                }
            };

            toggle.RegisterValueChangedCallback(evt => toggle.value = evt.newValue);

            return toggle;
        }
        
        private static EnumField InitializeEnum<T>(AgentVariableEnum<T> variable, string variableName,
            UISettings uiSettings) where T : Enum {
            var enumField = new EnumField {
                label = variableName,
                value = variable.Value,
                tooltip = variable.Description,
                style = {
                    fontSize = uiSettings.fontSize,
                }
            };
        
            enumField.RegisterValueChangedCallback(evt => enumField.value = evt.newValue);
        
            return enumField;
        }

    }
}