using System;
using UnityEngine;

namespace FreeFormGraph.Agents {
    
    [Serializable]
    public abstract class AgentVariable<T> {
        
        [field: SerializeField] public virtual T Value { get; set; }

        protected AgentVariable(T value) {
            Value = value;
        }
        
        public static implicit operator T(AgentVariable<T> av) => av.Value;

        
    }
    
    public abstract class AgentVariableRange<T> : AgentVariable<T> where T : System.IComparable<T> {
        
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
        
        public T Min { get; private set; }
        public T Max { get; private set; }

        protected AgentVariableRange(T value, T min, T max) : base(value) {
            Min = min;
            Max = max;
        }
        
    }
    
    public class AgentVariableInt : AgentVariableRange<int> {
        
        public AgentVariableInt(int value, int min, int max) : base(value, min, max) {
            
        }
        
        // public static implicit operator int(AgentVariableInt d) => d.Value;
        
    }
    
    public class AgentVariableFloat : AgentVariableRange<float> {
        
        public AgentVariableFloat(float value, float min, float max) : base(value, min, max) {
            
        }
        
    }
    
    public class AgentVariableBool : AgentVariable<bool> {
        
        public AgentVariableBool(bool value) : base(value) {
            
        }
        
    }
    
    public class AgentVariableEnum<T> : AgentVariable<T> where T : Enum {
        
        public AgentVariableEnum(T value) : base(value) {
            
        }
        
    }

    // public abstract class AgentVariables {
    //     
    //     public AgentVariableInt WorkFrequency { get; set; }
    //     
    // }
}