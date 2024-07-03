#nullable enable
using System;
using System.Collections.Generic;
using FreeFormGraph;
using FreeFormGraph.World;
using SUMO;
using UnityEngine;

namespace Simulation {

    public class DebugJsonTest {
        public int id;
        public string name;
        public List<int> list;
        public List<TestElement> testElements;
        // public DebugJsonTest(int id) {
        //     this.id = id;
        // }
    }
    [Serializable]
    public class TestElement {
        public int id;
        public string name;
    }
    public class SimulationManager : MonoBehaviour {
        
        private readonly Dictionary<string, Vehicle> vehicles = new();
        
        [SerializeField] private Vehicle? vehiclePrefab;

        private IWorld? world;
        
        private void Start() {
            var debugJsonString = "{\n   \"id\": 5,\n    \"name\": \"testName\",\n   \"list\": [1, 2, 3],\n   \"testElements\": [\n      {\n         \"id\": 1,\n         \"name\": \"Test1\"\n      },\n      {\n         \"id\": 2,\n         \"name\": \"Test2\"\n      }\n   ]\n}";
            var debugJson = JsonUtility.FromJson<DebugJsonTest>(debugJsonString);
            Debug.Log($"DebugJsonTest: {debugJson.id}");
            Debug.Log($"DebugJsonTest: {debugJson.name}");
            foreach (var i in debugJson.list) {
                Debug.Log($"List element: {i}");
            }
            foreach (var testElement in debugJson.testElements) {
                Debug.Log($"TestElement: {testElement.id}, {testElement.name}");
            }
            // var debugJsonString = "{\n    \"id\": \"trip8\",\n    \"positionX\": 7033.562003662933,\n    \"positionY\": 5165.739671987797,\n    \"rotation\": 431.77751910124357,\n    \"signals\": 2,\n    \"speed\": 2.22,\n    \"vehicleType\": \"passenger\"\n}";
            // var debugJson = JsonUtility.FromJson<VehicleInfo>(debugJsonString);
            // Debug.Log($"VehicleInfo: {debugJson.id}");
// "
// {
//     "id": "trip8",
//     "positionX": 7033.562003662933,
//     "positionY": 5165.739671987797,
//     "rotation": 431.77751910124357,
//     "signals": 2,
//     "speed": 2.22,
//     "vehicleType": "passenger"
// }
// "
            // var debugJsonString2 = "{\n\"step\": 74,\n\"vehicleList\": [\n    {\n        \"id\": \"flow0.0\",\n        \"positionX\": 7069.7447748391705,\n        \"positionY\": 5188.419849892781,\n        \"rotation\": 416.3099324740275,\n        \"signals\": 2,\n        \"speed\": 2.22,\n        \"vehicleType\": \"passenger\"\n    },\n    {\n        \"id\": \"flow1.0\",\n        \"positionX\": 6669.715000628299,\n        \"positionY\": 4894.529997486806,\n        \"rotation\": 345.96375653205456,\n        \"signals\": 0,\n        \"speed\": 2.22,\n        \"vehicleType\": \"passenger\"\n    }\n]\n}";
            // var debugJson2 = JsonUtility.FromJson<SimulationStepInfo>(debugJsonString2);
            // Debug.Log($"SimulationStepInfo: {debugJson2.step}");
            // foreach (var vehicleInfo in debugJson2.vehicleList) {
            //     Debug.Log($"VehicleInfo: {vehicleInfo.id}");
            // }
// """
// {
// "step": 74,
// "vehicleList": [
//     {
//         "id": "flow0.0",
//         "positionX": 7069.7447748391705,
//         "positionY": 5188.419849892781,
//         "rotation": 416.3099324740275,
//         "signals": 2,
//         "speed": 2.22,
//         "vehicleType": "passenger"
//     },
//     {
//         "id": "flow1.0",
//         "positionX": 6669.715000628299,
//         "positionY": 4894.529997486806,
//         "rotation": 345.96375653205456,
//         "signals": 0,
//         "speed": 2.22,
//         "vehicleType": "passenger"
//     }
// ]
// }
// """
        }

        public void StartSimulation() {
            Debug.Log("Starting simulation...");
            var sumoClient = FindObjectOfType<SumoClient>() ?? new GameObject("SumoClient").AddComponent<SumoClient>();
            world = FindObjectOfType<WorldGameObject>();

            if (!CheckSimulationValidity()) return;
            
            sumoClient.VehicleDataReceived += SumoClientOnVehicleDataReceived;
            sumoClient.StartClient();
        }

        private void SumoClientOnVehicleDataReceived(object sender, VehicleEventArgs e) {
            if (!CheckSimulationValidity()) return;
            
            Debug.Log($"Received {e.VehicleInfo.Length} vehicle data.");
            foreach (var vehicleInfo in e.VehicleInfo) {
                var position2D = new Vector2(vehicleInfo.x, vehicleInfo.y);
                var worldHeight = world!.GetHeightAt(position2D.x, position2D.y);
                if (vehicles.TryGetValue(vehicleInfo.id, out var vehicle)) {
                    vehicle.transform.position = new Vector3(vehicleInfo.x, worldHeight, vehicleInfo.y);
                }
                else {
                    var newVehicle = Instantiate(vehiclePrefab, new Vector3(vehicleInfo.x, worldHeight, vehicleInfo.y), Quaternion.identity);
                    newVehicle!.ID = vehicleInfo.id;
                    vehicles.Add(vehicleInfo.id, newVehicle);
                }
            }
        }

        private bool CheckSimulationValidity() {
            if (world != null && vehiclePrefab != null) {
                return true;
            }
            
            Debug.LogError($"World not found or vehicle prefab not set. Aborting...");
            StopSimulation();
            return false;

        }

        public void StopSimulation() {
            Debug.Log("Stopping simulation...");
            var sumoClient = FindObjectOfType<SumoClient>();
            sumoClient?.StopClient();
        }
    }
}