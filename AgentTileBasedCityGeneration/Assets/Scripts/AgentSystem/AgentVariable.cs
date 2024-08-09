#nullable enable
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
    
    [Serializable]
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
    
    [Serializable]
    public class AgentVariableInt : AgentVariableRange<int> {
        
        public AgentVariableInt(string name, int value, int min, int max, string? description = null) : base(name, value, min, max, description) { }

        public override void OnGui() {
            GUILayout.BeginHorizontal();
            if (Description != null) {
                GUILayout.Label(new GUIContent(Name, Description));
            } else {
                GUILayout.Label(Name);
            }
            GUILayout.BeginVertical();
            var newValueSlider = Mathf.RoundToInt(GUILayout.HorizontalSlider(Value, Min, Max));
            var newValueField = int.TryParse(GUILayout.TextField(Value.ToString()), out var newValue) ? newValue : Value;
            Value = newValueSlider != Value ? newValueSlider : newValueField;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
    
    [Serializable]
    public class AgentVariableFloat : AgentVariableRange<float> {
        
        public AgentVariableFloat(string name, float value, float min, float max, string? description = null) : base(name, value, min, max, description) { }

        public override void OnGui() {
            GUILayout.BeginHorizontal();
            if (Description != null) {
                GUILayout.Label(new GUIContent(Name, Description));
            } else {
                GUILayout.Label(Name);
            }
            // GUILayout.Label(Name);
            GUILayout.BeginVertical();
            var newValueSlider = GUILayout.HorizontalSlider(Value, Min, Max);
            var newValueField = float.TryParse(GUILayout.TextField(Value.ToString()), out var newValue) ? newValue : Value;
            Value = !Mathf.Approximately(newValueSlider, Value) ? newValueSlider : newValueField;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
    
    [Serializable]
    public class AgentVariableBool : AgentVariable<bool> {
        
        public AgentVariableBool(string name, bool value, string? description = null) : base(name, value, description) { }

        public override void OnGui() {
            GUILayout.BeginHorizontal();
            Value = Description != null ? GUILayout.Toggle(Value, new GUIContent(Name, Description)) : GUILayout.Toggle(Value, Name);
            GUILayout.EndHorizontal();
        }
    }
    
    [Serializable]
    public class AgentVariableEnum<T> : AgentVariable<T> where T : Enum {
        
        public AgentVariableEnum(string name, T value, string? description = null) : base(name, value, description) { }

        public override void OnGui() {
            GUILayout.BeginHorizontal();
            if (Description != null) {
                GUILayout.Label(new GUIContent(Name, Description));
            } else {
                GUILayout.Label(Name);
            }
            var selectionIndex = GUILayout.Toolbar((int) (object) Value, Enum.GetNames(typeof(T)));
            Value = (T) (object) selectionIndex;
            GUILayout.EndHorizontal();
        }
    }
    
    public class AgentVariableButton : IAgentVariable {
        
        public string Name { get; }
        public string? Description { get; }
        private readonly Action onClick;

        public AgentVariableButton(string name, Action onClick, string? description = null) {
            Name = name;
            this.onClick = onClick;
            Description = description;
        }

        public void OnGui() {
            if (Description != null) {
                if (GUILayout.Button(new GUIContent(Name, Description))) {
                    onClick();
                }
            } else {
                if (GUILayout.Button(Name)) {
                    onClick();
                }
            }
        }
    }

    public abstract class AgentVariableCollection {
        
        private IAgentVariable[]? variables;

        /// <summary>
        /// Returns all variables of type IAgentVariable.
        ///
        /// Uses lazy initialization.
        /// </summary>
        public IAgentVariable[] AllVariables {
            get { return variables ??= GetVariables(); }
        }
        
        /// <summary>
        /// Returns all IAgentVariable variables of the agent,
        /// including all variables of type IAgentVariable in subclasses of AgentVariableCollection.
        ///
        /// Could maybe be used with reflection, but that would probably be slower and this is more explicit.
        /// </summary>
        /// <returns> All variables of type IAgentVariable </returns>
        protected abstract IAgentVariable[] GetVariables();
        
    }

    public abstract class AgentParameters : AgentVariableCollection {
        
        public AgentParameters(int initialWorkFrequency) {
            WorkFrequency.Value = initialWorkFrequency;
            
        }

        public AgentVariableInt WorkFrequency { get; protected set; } = new("Work Frequency", 1, 1, 100);
        
    }
    
    
}