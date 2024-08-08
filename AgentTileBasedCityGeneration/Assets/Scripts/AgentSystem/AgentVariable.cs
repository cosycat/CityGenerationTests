#nullable enable
using System;
using UnityEngine;

namespace AgentSystem {

    public interface IAgentVariable {
        public string Name { get; }
        public string? Description { get; }
        public void OnGui();
    }
    
    [Serializable]
    public abstract class AgentVariable<T> : IAgentVariable {
        [SerializeField] private T value;
        
        public virtual T Value {
            get => value;
            set => this.value = value;
        }

        protected AgentVariable(string name, T value, string? description = null) {
            this.value = value;
            Description = description;
            Name = name;
        }
        
        public abstract void OnGui();
        
        public static implicit operator T(AgentVariable<T> av) => av.Value;

        public string Name { get; }
        public string? Description { get; }
        
        public override string ToString() {
            return $"{Name}: {Value}";
        }
    }
    
    public abstract class AgentVariableRange<T> : AgentVariable<T> where T : IComparable<T> {
        
        public override T Value {
            get => base.Value;
            set {
                if (value.CompareTo(Min) < 0) {
                    base.Value = Min;
                } else if (value.CompareTo(Max) > 0) {
                    base.Value = Max;
                } else {
                    base.Value = value;
                }
            }
        }
        
        public T Min { get; }
        public T Max { get; }

        protected AgentVariableRange(string name, T value, T min, T max, string? description = null) : base(name, value, description) {
            Min = min;
            Max = max;
        }
        
    }
    
    public class AgentVariableInt : AgentVariableRange<int> {
        
        public AgentVariableInt(string name, int value, int min, int max, string? description = null) : base(name, value, min, max, description) { }

        public override void OnGui() {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Name);
            GUILayout.BeginVertical();
            var newValueSlider = Mathf.RoundToInt(GUILayout.HorizontalSlider(Value, Min, Max));
            var newValueField = int.TryParse(GUILayout.TextField(Value.ToString()), out var newValue) ? newValue : Value;
            Value = newValueSlider != Value ? newValueSlider : newValueField;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
    
    public class AgentVariableFloat : AgentVariableRange<float> {
        
        public AgentVariableFloat(string name, float value, float min, float max, string? description = null) : base(name, value, min, max, description) { }

        public override void OnGui() {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Name);
            GUILayout.BeginVertical();
            var newValueSlider = GUILayout.HorizontalSlider(Value, Min, Max);
            var newValueField = float.TryParse(GUILayout.TextField(Value.ToString()), out var newValue) ? newValue : Value;
            Value = !Mathf.Approximately(newValueSlider, Value) ? newValueSlider : newValueField;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
    
    public class AgentVariableBool : AgentVariable<bool> {
        
        public AgentVariableBool(string name, bool value, string? description = null) : base(name, value, description) { }

        public override void OnGui() {
            GUILayout.BeginHorizontal();
            Value = GUILayout.Toggle(Value, Name);
            GUILayout.EndHorizontal();
        }
    }
    
    public class AgentVariableEnum<T> : AgentVariable<T> where T : Enum {
        
        public AgentVariableEnum(string name, T value, string? description = null) : base(name, value, description) { }

        public override void OnGui() {
            GUILayout.BeginVertical();
            var selectionIndex = GUILayout.Toolbar((int) (object) Value, Enum.GetNames(typeof(T)));
            Value = (T) (object) selectionIndex;
            GUILayout.EndVertical();
        }
    }

    // public abstract class AgentVariables {
    //     
    //     public AgentVariableInt WorkFrequency { get; set; }
    //     
    // }
}