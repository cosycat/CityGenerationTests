using System;
using UnityEngine;

namespace Simulation {
    
    public class TileRepresentation : Representation<Tile> {

        protected override void OnInitialize(Tile tile) {
            tile.TileUsageChanged += OnTileUsageChanged;
            OnTileUsageChanged((LandUsage.None, tile.UsageType));
        }

        private void OnTileUsageChanged((LandUsage oldUsageType, LandUsage newUsageType) usageTypes) {
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