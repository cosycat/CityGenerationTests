using System.Collections.Generic;
using UnityEngine;
using FreeFormGraph.World;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph {
    public class GeneratePOIs: MonoBehaviour {

        [SerializeField]
        public List<Vector2> poiPositions;

        public void Start() {
            var world = FindObjectOfType<WorldGameObject>();

            foreach(var pos in poiPositions) {
                world.PointsOfInterest.AddPointOfInterest(new BudgetPointOfInterest(pos, 0));
            }

            foreach(var pos in world.PointsOfInterest.PointsOfInterest) {
                Debug.Log(pos.Position);
            }
        }
    }
}