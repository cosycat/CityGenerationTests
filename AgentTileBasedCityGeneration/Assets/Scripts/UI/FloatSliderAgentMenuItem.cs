using FreeFormGraph.Agents;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class FloatSliderAgentMenuItem : AgentMenuItem<AgentVariableFloat, float> {
        [SerializeField] private Slider slider;
        
        public override void Initialize(AgentVariableFloat variable, string variableName) {
            Variable = variable;
            label.text = variableName;
            slider.minValue = Variable.Min;
            slider.maxValue = Variable.Max;
            slider.value = Variable.Value;
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            
        }

        private void OnSliderValueChanged(float newValue) {
            Variable.Value = newValue;
        }
    }
}