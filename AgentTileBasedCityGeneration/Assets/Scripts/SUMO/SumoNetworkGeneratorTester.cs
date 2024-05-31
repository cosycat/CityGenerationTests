using FreeFormGraph.LineBased;
using UnityEngine;

namespace SUMO {
    public class SumoNetworkGeneratorTester : MonoBehaviour {
        private SumoNetworkManager sumoNetworkManager;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = true;

        private void Awake() {
            sumoNetworkManager = FindObjectOfType<SumoNetworkManager>();
            if (sumoNetworkManager == null) {
                Debug.LogError("No SumoNetworkGenerator found in scene!");
                enabled = false;
            }
        }

        private void Start() {
            sumoNetworkManager.GenerateNetwork(LineGraphTestCreator.GenerateHShapedGraph(),
                convertToSumoNetwork: false, 
                runSimulationAfterGeneration: true, 
                openFolderAfterGeneration: openFolderAfterGeneration,
                openSumoGUIAfterGeneration: openSumoGUIAfterGeneration);
        }
    }
}