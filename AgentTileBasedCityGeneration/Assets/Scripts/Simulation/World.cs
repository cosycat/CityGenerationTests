using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation {
    public class World : MonoBehaviour {
        public static World Instance { get; private set; }
    
        [SerializeField] private TileRepresentation tileRepresentationPrefab;
        private GameObject _tilesContainer;
        [SerializeField] private AgentRepresentation agentRepresentationPrefab;
        private GameObject _agentsContainer;
    
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

            Debug.Log("Creating Tiles...");
            _tilesContainer = new GameObject("Tiles");
            _tilesContainer.transform.SetParent(transform);
            Tiles = new Tile[Width, Height];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Tiles[x, y] = new Tile(this, x + y < 6 || Math.Abs(x - y) < 2, new Vector2Int(x, y), Random.Range(0f, 5f));
                    // Tiles[x, y] = new Tile(this, x + y == 0, new Vector2Int(x, y));
                    var tileRepresentation = Instantiate(tileRepresentationPrefab, new Vector3(x, y), Quaternion.identity);
                    tileRepresentation.transform.SetParent(_tilesContainer.transform);
                    tileRepresentation.name = $"Tile {x}, {y}";
                    tileRepresentation.Initialize(Tiles[x, y]);
                }
            }
            Debug.Log($"{Width * Height} Tiles Created.");
            Debug.Log("Initializing Tile distances...");
            foreach (var tile in AllTiles) {
                tile.InitializeCurrentTileUsageDistances();
            }
            Debug.Log("Tile distances initialized.");
            Debug.Log("Initializing Debug Roads...");
            // TODO remove this, just for testing, simple way to add roads
            foreach (var tile in Tiles) {
                if (tile.Position.x == 5 || tile.Position.y == 30) {
                    tile.MultiTileSite = new Parcel(this, LandUsage.Road, new List<Tile> {tile}, 0, 0);
                }
            }
            Debug.Log("Debug Roads Initialized.");
            Debug.Log("Initializing Agents...");
            InitializeAgents();
            Debug.Log("Agents Initialized.");
        }

        private void InitializeAgents() {
            _agentsContainer = new GameObject("Agents");
            _agentsContainer.transform.SetParent(transform);
            CreateNewAgent(new PropertyDeveloperAgent(LandUsage.Residential, new RangeInt(1, 4), Tiles[Width / 2, Height / 2]));
            CreateNewAgent(new PropertyDeveloperAgent(LandUsage.Commercial, new RangeInt(1, 6), Tiles[Width / 2, Height / 2]));
            CreateNewAgent(new PropertyDeveloperAgent(LandUsage.Industrial, new RangeInt(1, 6), Tiles[Width / 2, Height / 2]));
            
            CreateNewAgent(new TertiaryRoadExtender(LandUsage.Road, AllTiles.First(t => t.UsageType == LandUsage.Road)));
        }
        
        private void CreateNewAgent(Agent agent) {
            Agents.Add(agent);
            var agentRepresentation = Instantiate(agentRepresentationPrefab, new Vector3(agent.CurrSite.Position.x, agent.CurrSite.Position.y), Quaternion.identity);
            agentRepresentation.transform.SetParent(_agentsContainer.transform);
            agentRepresentation.name = $"Agent {agent.UsageType}";
            agentRepresentation.Initialize(agent);
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