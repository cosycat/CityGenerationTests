using FreeFormGraph.LineBased;
using FreeFormGraph.World;
using UnityEngine;

namespace SUMO {
    public class SumoNetworkGeneratorTester : MonoBehaviour {
        private SumoNetworkConverter sumoNetworkConverter;
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = true;

        private void Awake() {
            sumoNetworkConverter = FindObjectOfType<SumoNetworkConverter>();
            if (sumoNetworkConverter == null) {
                Debug.LogError("No SumoNetworkGenerator found in scene!");
                enabled = false;
            }
        }

        private void Start() {
            sumoNetworkConverter.GenerateNetwork(LineGraphTestCreator.GenerateHShapedGraph(),
                FindObjectOfType<WorldGameObject>(),
                false,
                false,
                openFolderAfterGeneration,
                openSumoGUIAfterGeneration);
        }
    }
}