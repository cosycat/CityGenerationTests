using System.Collections.Generic;
using UnityEngine;
using FreeFormGraph.Agent;
using FreeFormGraph.World.PoI;

namespace FreeFormGraph.World {
    public class HeightmapWorld: WorldGameObject {
        private float[,] heights;
        private float maxHeight = float.MinValue;
        private float minHeight = float.MaxValue;
        private int width;
        private int height;

        public override int Width => width;

        public override int Height => height;

        public override float MaxHeight => maxHeight;

        public override float MinHeight => minHeight;
        
        public override IPointOfInterestCollection PointsOfInterest { get; } = new PointOfInterestCollection();


        [SerializeField]
        public Texture2D Heightmap;

        private IStreetGraph graph;

        public void Start() {
            if(Heightmap != null) {
                var pixels = Heightmap.GetPixels();  
                width = Heightmap.width;
                height = Heightmap.height;
                heights = new float[Width, Height];

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        var pixelValue = pixels[x + y * Width].grayscale * 20;
                        if(pixelValue > MaxHeight) maxHeight = pixelValue;
                        if(pixelValue < MinHeight) minHeight = pixelValue;
                        heights[x,y] = pixelValue;
                    }
                }
                Debug.Log($"Max {MaxHeight}");
                Debug.Log($"Min {MinHeight}");
            }

            graph = FindObjectOfType<StreetGraphGameObject>();
            var agent = new LandRoadAgent() {
                StreetGraph = graph,
                World = this
            };

            agent.AStar(new Vector3(10, 200-10,0), new Vector3(140,200-150 ,0));
            agent.AStar(new Vector3(190, 200-55,0), new Vector3(80,200-180 ,0));
        }

        public override float GetHeightAt(float x, float y)
        {
            return heights[(int)x, (int)y];
        }
    }

}