using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Simulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace DebugInfo {
    public class DebugInfoIMGUI : MonoBehaviour {
        [SerializeField] private GameObject tileHighlight;
        [SerializeField] private GameObject multiTileHighlightPrefab;
        private List<GameObject> _multiTileHighlights = new();

        private bool _showDebugInfo = true;
        private string _currText = "";
        private Camera _camera;

        private void Start() {
            _camera = Camera.main;

            useGUILayout = true;
        }

        private void Update() {
            var mouseOverTile = GetMouseOverTile();
            _currText = GetDebugText(mouseOverTile);
            tileHighlight.SetActive(mouseOverTile != null);
            if (mouseOverTile != null) {
                tileHighlight.transform.position = new Vector3(mouseOverTile.Position.x, mouseOverTile.Position.y, 0);

                var multiTileSiteCount = mouseOverTile.MultiTileSite?.Tiles.Count ?? 0;
                // Create new highlights if the site has more tiles than the current highlights
                while (_multiTileHighlights.Count < multiTileSiteCount) {
                    var newHighlight = Instantiate(multiTileHighlightPrefab, new Vector3(), Quaternion.identity);
                    _multiTileHighlights.Add(newHighlight);
                }
                Debug.Assert(_multiTileHighlights.Count >= multiTileSiteCount, "_multiTileHighlights.Count >= multiTileSiteCount");
                // Hide highlights if the site has less tiles than the current highlights
                for (int i = 0; i < multiTileSiteCount; i++) {
                    _multiTileHighlights[i].SetActive(true);
                    _multiTileHighlights[i].transform.position = new Vector3(mouseOverTile.MultiTileSite!.Tiles[i].Position.x, mouseOverTile.MultiTileSite.Tiles[i].Position.y, 0);
                }
                for (int i = multiTileSiteCount; i < _multiTileHighlights.Count; i++) {
                    _multiTileHighlights[i].SetActive(false);
                }
                
            }
            
        }

        private void OnGUI() {
            var guiStyle = new GUIStyle(GUI.skin.box) {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
            };
            GUILayout.BeginVertical(guiStyle);
            // GUILayout.BeginArea(guiStyle);
            if (GUILayout.Button("Toggle Debug Info", guiStyle, GUILayout.Width(200), GUILayout.Height(20))) {
                _showDebugInfo = !_showDebugInfo;
            }

            if (_showDebugInfo) {
                GUILayout.Label(_currText, guiStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            // GUILayout.EndArea();
        }

        private Tile GetMouseOverTile() {
            Debug.Assert(_camera != null, "_camera == null");
            var mouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            var mouseTilePos = new Vector2Int(Mathf.RoundToInt(mouseWorldPos.x), Mathf.RoundToInt(mouseWorldPos.y));
            return World.Instance.TryGetTileAt(mouseTilePos, out var tile) ? tile : null;
        }


        private string GetDebugText([CanBeNull] Tile tile) {
            if (tile == null) return "No tile under mouse";
            var str = new StringBuilder()
                .AppendLine($"{tile.Position}")
                .AppendLine($"LandUse: {tile.UsageType}")
                .AppendLine($"Elevation: {tile.Elevation}");

            str.AppendLine($"Distances:");
            foreach (var landUsage in (LandUsage[])Enum.GetValues(typeof(LandUsage))) {
                str.AppendLine($" - {landUsage}: {tile.GetDistanceTo(landUsage)}");
            }

            str.AppendLine();
            if (tile.MultiTileSite != null) {
                var site = tile.MultiTileSite;
                str.AppendLine($"MultiTileSite: {site}");
                str.AppendLine($" - Area: {site.Area}");
                str.AppendLine($" - Age: {site.Age}");
                str.AppendLine($" - Value: {site.CalcValue()}");
                str.AppendLine($" - UsageType: {site.UsageType}");
                if (site is Parcel parcel) {
                    str.AppendLine($" - Population: {parcel.Population}");
                }
            } else {
                str.AppendLine("No MultiTileSite");
            }

            return str.ToString();
        }
    }
}