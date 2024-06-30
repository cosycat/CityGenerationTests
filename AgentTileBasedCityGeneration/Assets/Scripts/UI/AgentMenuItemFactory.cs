using System;
using FreeFormGraph.Agents;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI {
    public static class AgentMenuItemFactory {

        public static VisualElement Initialize(IAgentVariable variable, string variableName) {
            return variable switch {
                AgentVariableFloat agentVariableFloat => InitializeSliderFloat(agentVariableFloat, variableName),
                AgentVariableInt agentVariableInt => InitializeSliderInt(agentVariableInt, variableName),
                AgentVariableBool agentVariableBool => InitializeToggle(agentVariableBool, variableName),
                AgentVariableEnum<Enum> agentVariableEnum => InitializeEnum(agentVariableEnum, variableName),
                _ => throw new NotImplementedException(
                    $"AgentMenuItemFactory.Initialize not implemented for this type of AgentVariable: {variable.GetType()}.")
            };
        }

        private static Slider InitializeSliderFloat(AgentVariableFloat variable, string variableName) {
            var slider = new Slider {
                label = variableName,
                lowValue = variable.Min,
                highValue = variable.Max,
                value = variable.Value,
                showInputField = true,
                tooltip = variable.Description,
                direction = SliderDirection.Horizontal
            };

            slider.RegisterValueChangedCallback(evt => slider.value = evt.newValue);

            return slider;
        }
        
        private static Slider InitializeSliderInt(AgentVariableInt variable, string variableName) {
            var slider = new Slider {
                label = variableName,
                lowValue = variable.Min,
                highValue = variable.Max,
                value = variable.Value,
                showInputField = true,
                tooltip = variable.Description,
                direction = SliderDirection.Horizontal
            };

            slider.RegisterValueChangedCallback(evt => {
                var newValue = Mathf.RoundToInt(evt.newValue);
                variable.Value = newValue;
                slider.value = newValue;
            });

            return slider;
        }
        
        private static Toggle InitializeToggle(AgentVariableBool variable, string variableName) {
            var toggle = new Toggle {
                label = variableName,
                value = variable.Value,
                tooltip = variable.Description
            };

            toggle.RegisterValueChangedCallback(evt => toggle.value = evt.newValue);

            return toggle;
        }
        
        private static EnumField InitializeEnum<T>(AgentVariableEnum<T> variable, string variableName) where T : Enum {
            var enumField = new EnumField {
                label = variableName,
                value = variable.Value,
                tooltip = variable.Description
            };

            enumField.RegisterValueChangedCallback(evt => enumField.value = evt.newValue);

            return enumField;
        }

    }
}