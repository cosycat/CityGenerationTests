using System;
using UnityEngine;
using FreeFormGraph.Agent;
using FreeFormGraph.LineBased;

namespace FreeFormGraph.World {
    public class HeightmapWorld: WorldGameObject, IWorld {
        private float[,] heights;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public float MaxHeight {get; private set;} = float.MinValue;
        public float MinHeight {get; private set;} = float.MaxValue;

        [SerializeField]
        public Texture2D Heightmap;

        private IStreetGraph graph;

        public void Start() {
            if(Heightmap != null) {
                var pixels = Heightmap.GetPixels();  
                Width = Heightmap.width;
                Height = Heightmap.height;
                heights = new float[Width, Height];

                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        var pixelValue = pixels[x + y * Width].grayscale * 20;
                        if(pixelValue > MaxHeight) MaxHeight = pixelValue;
                        if(pixelValue < MinHeight) MinHeight = pixelValue;
                        heights[x,y] = pixelValue;
                    }
                }
                Debug.Log($"Max {MaxHeight}");
                Debug.Log($"Min {MinHeight}");
            }

            graph = FindObjectOfType<StreetGraphGameObject>().graph;
            var agent = new LandRoadAgent() {
                StreetGraph = graph,
                World = this
            };

            agent.AStar(new Vector3(10, 200-10,0), new Vector3(140,200-150 ,0));
            agent.AStar(new Vector3(190, 200-55,0), new Vector3(80,200-180 ,0));
        }

        public float GetHeightAt(float x, float y)
        {
            return heights[(int)x, (int)y];
        }
    }

}