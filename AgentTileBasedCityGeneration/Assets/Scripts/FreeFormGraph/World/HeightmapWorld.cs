using UnityEngine;
using FreeFormGraph.Agents;
using FreeFormGraph.World.PoI;
using Utils;

namespace FreeFormGraph.World {
    public class HeightmapWorld: WorldGameObject {
        [SerializeField] private bool doPathfinding;
        [SerializeField] private bool doPointOfInterest;
        
        private float[,] heights;
        private int width;
        private int height;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeightMeters / Constants.METERS_PER_UNIT;
        
        public override float MinHeight => minHeightMeters / Constants.METERS_PER_UNIT;

        public float maxHeightMeters = 1000;
        public float minHeightMeters = 100;

        public override IPointOfInterestCollection PointsOfInterest { get; } = new PointOfInterestCollection();

        [SerializeField] public Texture2D heightmap;

        private IStreetGraph graph;
        public override IStreetGraph StreetGraph => graph;

        private void Awake() {
            graph = FindObjectOfType<StreetGraphGameObject>();
        }

        public void Start() {
            if(heightmap != null) {
                var pixels = heightmap.GetPixels();  
                width = heightmap.width;
                height = heightmap.height;
                heights = new float[Width, Height];

                float maxHeightTexture = float.MinValue;
                float minHeightTexture = float.MaxValue;

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        var pixelValue = pixels[x + y * Width].grayscale;
                        if(pixelValue > maxHeightTexture) maxHeightTexture = pixelValue;
                        if(pixelValue < minHeightTexture) minHeightTexture = pixelValue;
                        heights[x,y] = pixelValue;
                    }
                }
                Debug.Log($"Max pixel value of heightmap: {maxHeightTexture}");
                Debug.Log($"Min pixel value of heightmap: {minHeightTexture}");

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        heights[x,y] = Mathf.Lerp(minHeightMeters, maxHeightMeters, Mathf.InverseLerp(minHeightTexture, maxHeightTexture, heights[x,y])) / Constants.METERS_PER_UNIT;
                    }
                }

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        Debug.Assert(heights[x,y] <= MaxHeight);
                        Debug.Assert(heights[x,y] >= MinHeight);
                    }
                }
            }

            var agent = new Pathfinding(streetGraph: graph, world: this);
            
            // var poiAgent = new PointOfInterestAgent(this);
            // if (doPointOfInterest) {
            //     poiAgent.CreateNewPointOfInterest(registerInWorld: true);
            //     poiAgent.CreateNewPointOfInterest(registerInWorld: true);
            //     poiAgent.CreateNewPointOfInterest(registerInWorld: true);
            // }
            
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

                    agent.AStar(new Vector3(10, 185, 0), new Vector3(20, 192, 0));
                    agent.AStar(new Vector3(10, 200 - 10, 0), new Vector3(140, 200 - 150, 0));
                    agent.AStar(new Vector3(190, 200 - 55, 0), new Vector3(80, 200 - 180, 0));
                    agent.AStar(new Vector3(4, 4, 0), new Vector3(140, 200 - 150, 0));
                    agent.AStar(new Vector3(4, 4, 0), new Vector3(80, 20, 0));
                    cancellationToken.ThrowIfCancellationRequested();
                }, () => {
                    Debug.Log("Pathfinding done");
                    // TODO: restart all other pathfinding tasks, as the world has changed. Maybe with a flag?
                });
                
            }
            
        }

        public override float GetHeightAt(float x, float y)
        {
            return heights[(int)x, (int)y];
        }
    }

}