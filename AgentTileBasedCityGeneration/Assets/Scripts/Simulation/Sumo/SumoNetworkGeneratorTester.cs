using Graph.LineBased;
using Graph.World;
using UnityEngine;

namespace Simulation.Sumo {
    public class SumoNetworkGeneratorTester : MonoBehaviour {
        [SerializeField] private bool openFolderAfterGeneration = true;
        [SerializeField] private bool openSumoGUIAfterGeneration = true;
        private SumoNetworkConverter sumoNetworkConverter;

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