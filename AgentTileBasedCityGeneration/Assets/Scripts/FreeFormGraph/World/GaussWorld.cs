using System;
using UnityEngine;
using FreeFormGraph.Agent;
using FreeFormGraph.LineBased;

namespace FreeFormGraph.World {
    public class GaussWorld: WorldGameObject, IWorld {
        private float[,] heights;
        
        private float maxHeight = float.MinValue;
        private float minHeight = float.MaxValue;
        private int width;
        private int height;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeight;

        public override float MinHeight => minHeight;

        private IStreetGraph graph;


        public void Start() {
            width = 100;
            height = 100;
            heights = new float[Width, Height];

            PlaceGauss(Width/2, Height/2, spread: 5);
            PlaceGauss(Width/2+20, Height/2-20, spread: 15);
            PlaceGauss(Width/2, Height/2+20, spread: 15);

            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    minHeight = Mathf.Min(MinHeight, heights[x,y]);
                    maxHeight = Mathf.Max(MaxHeight, heights[x,y]);
                }
            }

            graph = FindObjectOfType<StreetGraphGameObject>();
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
            var dist = e.GetDistanceEdgeToPosition(new Vector3(10f, 10f, 0), out var posOnEdge);
            Debug.Log($"pos on edge {posOnEdge} dist: {dist}");

            agent.AStar(new Vector3(50,97,0), new Vector3(50,70 ,0)); // jagged street going up the mountain
            agent.AStar(new Vector3(40,80,0), new Vector3(80,75 ,0));
            agent.AStar(new Vector3(20,60,0), new Vector3(90,90 ,0));
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

        public override float GetHeightAt(float x, float y)
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