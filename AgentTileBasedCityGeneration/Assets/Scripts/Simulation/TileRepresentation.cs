using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Simulation {
    
    public class TileRepresentation : Representation<Tile> {
        
        [SerializeField] private Tile tile;
        [SerializeField] private LandUsage currentUsageType; // To see it in the inspector

        protected override void OnInitialize(Tile tile) {
            tile.TileUsageChanged += OnTileUsageChanged;
            OnTileUsageChanged((LandUsage.None, tile.UsageType));
            this.tile = tile;
        }

        private void OnTileUsageChanged((LandUsage oldUsageType, LandUsage newUsageType) usageTypes) {
            currentUsageType = usageTypes.newUsageType;
            switch (usageTypes.newUsageType) {
                case LandUsage.None:
                    SpriteRenderer.color = new Color(0.11f, 0.62f, 0.1f);
                    break;
                case LandUsage.Road:
                    SpriteRenderer.color = Color.gray;
                    break;
                case LandUsage.Residential:
                    SpriteRenderer.color = Color.yellow;
                    break;
                case LandUsage.Commercial:
                    SpriteRenderer.color = Color.red;
                    break;
                case LandUsage.Industrial:
                    SpriteRenderer.color = Color.blue;
                    break;
                case LandUsage.Park:
                    SpriteRenderer.color = Color.green;
                    break;
                case LandUsage.Water:
                    SpriteRenderer.color = new Color(0.38f, 0.67f, 0.84f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}