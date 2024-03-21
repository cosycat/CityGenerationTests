using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

        public IEnumerable<RoadSegment> AllRoadSegments =>
            AllTiles.Select(t => t.MultiTileSite).OfType<RoadSegment>().Distinct();
        
        private List<Agent> Agents { get; } = new();
        public long Tick { get; private set; }

        private void Awake() {
            Random.InitState(1337);

            if (Instance != null) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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

            foreach (var tile in AllTiles) {
                tile.InitializeCurrentTileUsageDistances();
            }
            Debug.Log("Tile distances initialized.");
            
            // TODO remove this, just for testing, simple way to add roads (but initially one road is needed)
            Tiles[Width/2, Height/2].MultiTileSite = new RoadSegment(this, LandUsage.Road, new List<Tile> {Tiles[Width/2, Height/2]}, 0, RoadType.Tertiary);
            // foreach (var tile in Tiles) {
            //     if (tile.Position.x == 15 || tile.Position.y == 30) {
            //         tile.MultiTileSite = new Parcel(this, LandUsage.Road, new List<Tile> {tile}, 0, 0);
            //     }
            // }
            Debug.Log("Debug Road(s) Initialized.");

            Debug.Log("Testing Removing Last Tile of certain type");
            Tiles[Width - 1, Height - 1].MultiTileSite = new Parcel(this, LandUsage.Residential, new List<Tile> {Tiles[Width - 1, Height - 1]}, 0, 0);
            Tiles[Width - 1, Height - 1].MultiTileSite = new Parcel(this, LandUsage.Industrial, new List<Tile> {Tiles[Width - 1, Height - 1]}, 0, 0);
            Tiles[Width - 1, Height - 1].MultiTileSite = null;
            
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
            CreateNewAgent(new TertiaryRoadExtender(LandUsage.Road, AllTiles.First(t => t.UsageType == LandUsage.Road)));
            // CreateNewAgent(new TertiaryRoadExtender(LandUsage.Road, AllTiles.First(t => t.UsageType == LandUsage.Road)));
            
            CreateNewAgent(new TertiaryRoadConnector(LandUsage.Road, AllTiles.First(t => t.UsageType == LandUsage.Road)));
        }
        
        private void CreateNewAgent(Agent agent) {
            Agents.Add(agent);
            var agentRepresentation = Instantiate(agentRepresentationPrefab, new Vector3(agent.CurrTile.Position.x, agent.CurrTile.Position.y), Quaternion.identity);
            agentRepresentation.transform.SetParent(_agentsContainer.transform);
            agentRepresentation.name = $"Agent {agent.AgentUsageType}";
            agentRepresentation.Initialize(agent);
        }

        private void Update() {
            TimeSinceLastTick += Time.deltaTime;
            if (TimeSinceLastTick >= 1f / Speed) {
                OnBeforeTick(Tick);
                TimeSinceLastTick = 0;
                foreach (var agent in Agents) {
                    agent.UpdateTick();
                }
                OnAfterTick(Tick);
                Tick++;
            }
        }

        public bool TryGetTileAt(Vector2Int position, out Tile tile) {
            if (position.x < 0 || position.x >= Width || position.y < 0 || position.y >= Height) {
                tile = null;
                return false;
            }
            tile = Tiles[position.x, position.y];
            return true;
        }


        public List<Tile> GetAllTilesOfType(LandUsage type) {
            return AllTiles.Where(t => t.UsageType == type).ToList();
        }

        /// <summary>
        /// Event that is called before each tick.
        /// </summary>
        public event Action<long> BeforeTick;

        protected virtual void OnBeforeTick(long tick) {
            BeforeTick?.Invoke(tick);
        }

        /// <summary>
        /// Event that is called after each tick.
        /// </summary>
        public event Action<long> AfterTick;

        protected virtual void OnAfterTick(long tick) {
            AfterTick?.Invoke(tick);
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