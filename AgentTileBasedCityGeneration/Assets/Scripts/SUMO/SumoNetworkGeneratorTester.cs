using FreeFormGraph.LineBased;
using UnityEngine;

namespace SUMO {
    public class SumoNetworkGeneratorTester : MonoBehaviour {
        private SumoNetworkGenerator sumoNetworkGenerator;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = true;

        private void Awake() {
            sumoNetworkGenerator = FindObjectOfType<SumoNetworkGenerator>();
            if (sumoNetworkGenerator == null) {
                Debug.LogError("No SumoNetworkGenerator found in scene!");
                enabled = false;
            }
        }

        private void Start() {
            sumoNetworkGenerator.GenerateNetwork(LineGraphTestCreator.GenerateHShapedGraph(), 
                true, 
                openFolderAfterGeneration, 
                openSumoGUIAfterGeneration);
        }
    }
}