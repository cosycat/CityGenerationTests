using System;
using UnityEngine;

public class World : MonoBehaviour {
    public static World Instance { get; private set; }
    
    [SerializeField] private TileRepresentation tileRepresentationPrefab;
    
    [field: SerializeField] public int Width { get; private set; } = 100;
    [field: SerializeField] public int Height { get; private set; } = 100;

    private Tile[,] Tiles { get; set; }

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
                
            }
        }
    }
}

public enum LandUsage {
    None,
    Road,
    Residential,
    Commercial,
    Industrial,
    Park,
    Water
}