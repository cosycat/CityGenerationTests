#nullable enable
using UnityEngine;

namespace Utils.Cameras {
    /// <summary>
    ///    A camera that can be selected to be active or inactive.
    ///    Used to cycle through different cameras of a vehicle.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(AudioListener))]
    public class SelectableCamera : MonoBehaviour {
        private AudioListener audioListener = null!;
        private Camera cam = null!;

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