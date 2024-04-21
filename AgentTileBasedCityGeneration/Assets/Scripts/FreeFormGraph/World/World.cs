using System;
using UnityEngine;
using FreeFormGraph.Agent;
using FreeFormGraph.LineBased;

namespace FreeFormGraph.World {
    public class HeightmapWorld : MonoBehaviour, IWorld {
        private float[,] heights;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public float MaxHeight {get; private set;} = float.MinValue;
        public float MinHeight {get; private set;} = float.MaxValue;

        private IStreetGraph graph;

        public void Start() {
            Width = 100;
            Height = 100;
            heights = new float[Width, Height];

            PlaceGauss(Width/2, Height/2, spread: 5);
            PlaceGauss(Width/2+20, Height/2-20, spread: 15);
            PlaceGauss(Width/2, Height/2+20, spread: 15);

            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    MinHeight = Mathf.Min(MinHeight, heights[x,y]);
                    MaxHeight = Mathf.Max(MaxHeight, heights[x,y]);
                }
            }

            graph = FindObjectOfType<StreetGraphGameObject>().graph;
            Debug.Assert(graph != null);
            //ExampleGraph();

            var agent = new LandRoadAgent() {
                StreetGraph = graph,
                World = this
            };

            var e = new LineEdge() {
                NodeA = new LineNode() { Position = new Vector3(0,0,0) },
                NodeB = new LineNode() { Position = new Vector3(5,0,0) },
            };
            var dist = graph.GetDistanceEdgeToPosition(e, new Vector3(10f, 10f, 0), out var posOnEdge);
            Debug.Log($"pos on edge {posOnEdge} dist: {dist}");

            //agent.AStar(new Vector3(2,2,0), new Vector3(99,99,0));
            graph.CreateUnconnectedNode(new Vector3(99, 99, 0), out var newNode);
            graph.CreateEdge(newNode, new Vector3(0, 95, 0), out _, out _, out _);
            agent.AStar(new Vector3(78,70,0), new Vector3(20,97,0));
        }

        private void PlaceGauss(int centerX, int centerY, float amplitude = 10, float spread = 5.0f) {
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    float distanceX = (x - centerX) * (x - centerX);
                    float distanceY = (y - centerY) * (y - centerY);
                    
                    heights[x, y] += amplitude * Mathf.Exp(-((distanceX + distanceY) / (2 * spread * spread)));
                }
            }
        }

        public void ExampleGraph() {
            var agent = new LandRoadAgent();
            agent.StreetGraph = graph;
            graph.CreateUnconnectedNode(new Vector3(0,0,0), out var newNode);
            graph.CreateEdge(newNode, new Vector3(3,5,0), out _, out newNode, out _);

            graph.CreateUnconnectedNode(new Vector3(3,0,0), out newNode);
            graph.CreateEdge(newNode, new Vector3(0,5,0), out _, out newNode, out _);
        }

        public float GetHeightAt(float x, float y)
        {
            return heights[(int)x, (int)y];
        }

        private void OnDrawGizmos() {
            /*for(int y = 0; y < Height; y++) {
                for(int x = 0; x < Width; x++) {
                    Gizmos.DrawRay(new Vector3(x,y,0), Vector3.forward * heights[x,y]);
                }
            }*/
        }
    }

}