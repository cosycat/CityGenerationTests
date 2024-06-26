using TMPro;
using UnityEngine;

namespace UI {
    public class AgentCanvas : MonoBehaviour {
        
        [SerializeField] private TextMeshProUGUI title;
        
        public void Initialize(string titleText) {
            title.text = titleText;
        }
        
    }
}