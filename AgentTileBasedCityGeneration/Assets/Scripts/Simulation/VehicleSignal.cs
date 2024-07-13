using System;
using UnityEngine;

namespace Simulation {
    public class VehicleSignal : MonoBehaviour {
        
        private const float BLINK_INTERVAL_SECONDS = 0.5f;
        
        [Tooltip("Whether the signal should blink if on, or just stay on. If true, one single MeshRenderer component is required."),
         SerializeField] private bool isBlinking;
        
        private bool isOn = false;
        private float timeSinceLastBlink = 0f;
        
        private MeshRenderer meshRenderer = null!;
        
        public void SetSignal(bool on) {
            if (isOn == on) return;
            isOn = on;
            if (isBlinking) {
                meshRenderer.enabled = isOn;
                timeSinceLastBlink = 0f;
            }
            else {
                gameObject.SetActive(isOn);
            }
        }

        private void Start() {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) {
                if (isBlinking) Debug.LogError("VehicleSignal is set to blink but has no MeshRenderer component!");
                isBlinking = false;
                return;
            }
            meshRenderer.enabled = isOn;
        }

        private void Update() {
            if (!isBlinking || !isOn) return;
            timeSinceLastBlink += Time.deltaTime;
            if (timeSinceLastBlink >= BLINK_INTERVAL_SECONDS) {
                timeSinceLastBlink = 0f;
                meshRenderer.enabled = !meshRenderer.enabled;
            }
        }
    }
}