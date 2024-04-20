using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using FreeFormGraph.LineBased;

namespace FreeFormGraph {
    public class StreetGraphGameObject : MonoBehaviour {
        public IStreetGraph graph = null!;

        public void Awake() {
            //graph = new FreeFormGraph.SplineBased.SplineStreetGraph(this.gameObject); 
            graph = new LineGraph();
        }
    }
}