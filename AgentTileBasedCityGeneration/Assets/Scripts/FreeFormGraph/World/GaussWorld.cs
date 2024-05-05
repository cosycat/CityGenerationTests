using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using FreeFormGraph.Agents;
using FreeFormGraph.World.PoI;
using Utils;

namespace FreeFormGraph.World {
    public class GaussWorld: WorldGameObject {
        [SerializeField] private bool doPathfinding;
        [SerializeField] private bool doPointOfInterest;
        
        private float[,] heights;
        
        private float maxHeight = float.MinValue;
        private float minHeight = float.MaxValue;
        [SerializeField] private int width = 100;
        [SerializeField] private int height = 100;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeight;

        public override float MinHeight => minHeight;
        public override IPointOfInterestCollection PointsOfInterest { get; } = new PointOfInterestCollection();


        private IStreetGraph graph;


        public void Start() {
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

            var agent = new Pathfinding(streetGraph: graph, world: this);
            
            var poiAgent = new PointOfInterestAgent(this);
            if (doPointOfInterest) {
                poiAgent.CreateNewPointOfInterest(registerInWorld: true);
                poiAgent.CreateNewPointOfInterest(registerInWorld: true);
                poiAgent.CreateNewPointOfInterest(registerInWorld: true);
            }
            

            if (doPathfinding) {
                BackgroundCodeExecutor.ExecuteInBackground((cancellationToken) => {
                    // diagonal
                    agent.AStar(new Vector3(10, 10), new Vector3(width - 10, height - 10));
                    agent.AStar(new Vector3(width - 10, 10), new Vector3(10, height - 10));
                    cancellationToken.ThrowIfCancellationRequested();

                    // straight
                    agent.AStar(new Vector3(10, 10), new Vector3(width - 10, 10));
                    agent.AStar(new Vector3(10, 10), new Vector3(10, height - 10));
                    agent.AStar(new Vector3(10, height - 10), new Vector3(width - 10, height - 10));
                    agent.AStar(new Vector3(width - 10, 10), new Vector3(width - 10, height - 10));
                    cancellationToken.ThrowIfCancellationRequested();

                    // jagged street going up the mountain
                    agent.AStar(new Vector3(50,97,0), new Vector3(50,70 ,0)); 
                    agent.AStar(new Vector3(40,80,0), new Vector3(80,75 ,0));
                    agent.AStar(new Vector3(20,60,0), new Vector3(90,90 ,0));
                    cancellationToken.ThrowIfCancellationRequested();
                }, () => {
                    Debug.Log("Pathfinding done");
                    // TODO: restart all other pathfinding tasks, as the world has changed. Maybe with a flag?
                });
                
            }
            
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

        public override float GetHeightAt(float x, float y)
        {
            return heights[(int)x, (int)y];
        }
        
    }

}