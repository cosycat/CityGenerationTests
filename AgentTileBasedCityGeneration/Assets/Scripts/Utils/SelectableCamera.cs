#nullable enable
using UnityEngine;

namespace Utils {
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(AudioListener))]
    public class SelectableCamera : MonoBehaviour {
        private Camera cam = null!;
        private AudioListener audioListener = null!;

        private void Awake() {
            cam = GetComponent<Camera>();
            audioListener = GetComponent<AudioListener>();
        }

        public void SetActive(bool active) {
            cam.enabled = active;
            audioListener.enabled = active;
        }
    }
}