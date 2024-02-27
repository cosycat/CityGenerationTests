using System;
using UnityEngine;

public class Tile {
    private World World { get; }
    public Vector2Int Position { get; }
    public LandUsage LandUsage { get; private set; }
    
    public bool IsWater => LandUsage == LandUsage.Water;

    public Tile(World world, bool isWater, Vector2Int position) {
        World = world;
        LandUsage = isWater ? LandUsage.Water : LandUsage.None;
        Position = position;
    }
    
    public event Action<Tile> TileChanged;

    protected virtual void OnTileChanged(Tile obj) {
        TileChanged?.Invoke(obj);
    }
}