using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Simulation {
    public class World : MonoBehaviour {
        public static World Instance { get; private set; }
    
        [SerializeField] private TileRepresentation tileRepresentationPrefab;
    
        [field: SerializeField] public int Width { get; private set; } = 100;
        [field: SerializeField] public int Height { get; private set; } = 100;
        
        [field: SerializeField, Tooltip("Ticks per Second"), Range(0, 20)] 
        public int Speed { get; private set; } = 1;
        private float TimeSinceLastTick { get; set; }

        private Tile[,] Tiles { get; set; }
        
        public IEnumerable<Tile> AllTiles => Tiles.Cast<Tile>();
        
        private List<Agent> Agents { get; } = new();
        public long Tick { get; private set; }

        private void Awake() {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        
            Tiles = new Tile[Width, Height];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Tiles[x, y] = new Tile(this, x + y < 20 || Math.Abs(x - y) < 2, new Vector2Int(x, y));
                    var tileRepresentation = Instantiate(tileRepresentationPrefab, new Vector3(x, y), Quaternion.identity);
                    tileRepresentation.transform.SetParent(transform);
                    tileRepresentation.name = $"Tile {x}, {y}";
                    tileRepresentation.Initialize(Tiles[x, y]);
                    if (x == 30) {
                        Tiles[x, y].Parcel = new Parcel(this, LandUsage.Road, new List<Tile> {Tiles[x, y]}, 0, 0);
                    }
                }
            }
            
            InitializeAgents();
        }

        private void InitializeAgents() {
            Agents.Add(new PropertyDeveloperAgent(LandUsage.Residential, new RangeInt(1, 4), Tiles[20, 20]));
            Agents.Add(new PropertyDeveloperAgent(LandUsage.Commercial, new RangeInt(1, 6), Tiles[30, 20]));
            Agents.Add(new PropertyDeveloperAgent(LandUsage.Industrial, new RangeInt(1, 6), Tiles[30, 20]));
        }

        private void Update() {
            TimeSinceLastTick += Time.deltaTime;
            if (TimeSinceLastTick >= 1 / Speed) {
                TimeSinceLastTick = 0;
                foreach (var agent in Agents) {
                    agent.UpdateTick();
                }
                Tick++;
            }
        }

        public bool TryGetTile(Vector2Int position, out Tile tile) {
            if (position.x < 0 || position.x >= Width || position.y < 0 || position.y >= Height) {
                tile = null;
                return false;
            }
            tile = Tiles[position.x, position.y];
            return true;
        }
        
    }

    public enum LandUsage {
        Residential,
        Commercial,
        Industrial,
        Park,
        Road,
        Water,
        None
    }
}