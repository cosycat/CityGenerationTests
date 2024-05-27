using FreeFormGraph.LineBased;
using UnityEngine;

namespace SUMO {
    public class SumoNetworkGeneratorTester : MonoBehaviour {
        private SumoNetworkGenerator sumoNetworkGenerator;

        private void Awake() {
            sumoNetworkGenerator = FindObjectOfType<SumoNetworkGenerator>();
            if (sumoNetworkGenerator == null) {
                Debug.LogError("No SumoNetworkGenerator found in scene!");
                enabled = false;
            }
        }

        private void Start() {
            sumoNetworkGenerator.GenerateNetwork(LineGraphTestCreator.GenerateSquareGraph());
        }
    }
}