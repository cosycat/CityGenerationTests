using FreeFormGraph.Agents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public abstract class AgentMenuItem<T, TV> : MonoBehaviour where T : AgentVariable<TV> {
        
        [SerializeField] protected TextMeshProUGUI label;

        public T Variable { get; protected set; }
        public string VariableName => label.text;

        public abstract void Initialize(T variable, string variableName);

    }
}