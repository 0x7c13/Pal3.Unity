// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Linq;
    using Core.Contract.Constants;
    using Core.DataReader.Lgt;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Engine.Extensions;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class ResourceBrowserWindow
    {
        private readonly ResourceObjectView _objectView = new();
        private string _recordQuery = "";
        private int _recordIndex, _sceneRecordType;
        private NavFile _sceneNav;
        private bool _sceneLayout;
        private Texture2D _navTexture;
        private int _navLayer, _drawnNavLayer = -1;
        private float _navZoom = 1;
        private Vector2 _navPan;
        private Vector2Int _navTile;

        private void ClearSceneView()
        {
            _objectView.Reset();
            _recordQuery = "";
            _recordIndex = 0;
            _sceneRecordType = 0;
            _sceneNav = null;
            _sceneLayout = false;
            _navLayer = 0;
            _drawnNavLayer = -1;
            _navZoom = 1;
            _navPan = Vector2.zero;
            _navTile = Vector2Int.zero;
            if (_navTexture != null)
                DestroyImmediate(_navTexture);
            _navTexture = null;
        }

        private void DrawResourceLink(string label, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            ResourceEntry entry = _catalog.Find(path);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(entry == null))
                {
                    if (GUILayout.Button("Open " + label, GUILayout.Width(130)))
                    {
                        Select(entry);
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.SelectableLabel((entry == null ? "Missing: " : "") + path, GUILayout.Height(18));
            }
        }

        private int PickRecord(string[] labels)
        {
            string query = EditorGUILayout.TextField("Find record", _recordQuery);
            if (query != _recordQuery)
            {
                _recordQuery = query;
                _recordIndex = 0;
                _objectView.Reset();
            }
            int[] indices = Enumerable.Range(0, labels.Length).Where(i => labels[i].IndexOf(_recordQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (indices.Length == 0)
            {
                EditorGUILayout.HelpBox("No matching records.", MessageType.Info);
                return -1;
            }
            int current = Mathf.Max(0, Array.IndexOf(indices, _recordIndex));
            int next = EditorGUILayout.Popup($"Record ({indices.Length:N0})", current, indices.Select(i => labels[i]).ToArray());
            if (_recordIndex != indices[next])
                _objectView.Reset();
            _recordIndex = indices[next];
            return _recordIndex;
        }

        private void DrawSceneView(ScnFile scene)
        {
            GUILayout.Label(scene.SceneInfo.ToString(), EditorStyles.wordWrappedLabel);
            GUILayout.Label($"{scene.NpcInfos.Length} NPCs · {scene.ObjectInfos.Length} objects", EditorStyles.miniLabel);
            DrawResourceLink("background", ResourceSceneData.BackgroundPath(scene.SceneInfo));
            DrawResourceLink("navigation", FileConstants.GetNavFileVirtualPath(scene.SceneInfo.CityName, scene.SceneInfo.Model));
            DrawResourceLink("script", FileConstants.GetSceneSceFileVirtualPath(scene.SceneInfo.CityName));
            bool layout = GUILayout.Toggle(_sceneLayout, "Preview static layout", EditorStyles.miniButton);
            if (layout != _sceneLayout)
            {
                _sceneLayout = layout;
                if (layout)
                    CreateSceneLayout(scene);
                else
                {
                    _model?.Dispose();
                    _model = null;
                }
            }
            if (_sceneLayout && _model != null)
            {
                if (GUILayout.Button("Frame layout"))
                    _model.Frame();
                Rect rect = GUILayoutUtility.GetRect(100, 10000, 180, 260);
                _model.Draw(rect);
                DrawSceneMarkers(scene, rect);
                if (Event.current.type is EventType.Used or EventType.MouseDrag or EventType.ScrollWheel)
                    Repaint();
            }
            int type = GUILayout.Toolbar(_sceneRecordType, new[] { "Scene", "NPCs", "Objects" });
            if (type != _sceneRecordType)
            {
                _sceneRecordType = type;
                _recordQuery = "";
                _recordIndex = 0;
                _objectView.Reset();
            }
            if (type == 0)
                _objectView.Draw(scene.SceneInfo, DetailWidth);
            else if (type == 1)
            {
                int index = PickRecord(scene.NpcInfos.Select(n => $"{n.Id} · {n.Name} · Script {n.ScriptId}").ToArray());
                if (index >= 0)
                {
                    OpenSceneScript(scene.SceneInfo.CityName, scene.NpcInfos[index].ScriptId);
                    _objectView.Draw(scene.NpcInfos[index], DetailWidth);
                }
            }
            else
            {
                int index = PickRecord(scene.ObjectInfos.Select(o => $"{o.Id} · {o.Name} · {o.Type} · Script {o.ScriptId}").ToArray());
                if (index >= 0)
                {
                    OpenSceneScript(scene.SceneInfo.CityName, scene.ObjectInfos[index].ScriptId);
                    DrawResourceLink("object model", ResourceSceneData.ObjectPath(scene.SceneInfo, scene.ObjectInfos[index]));
                    _objectView.Draw(scene.ObjectInfos[index], DetailWidth);
                }
            }
        }

        private void CreateSceneLayout(ScnFile scene)
        {
            _model?.Dispose();
            _model = new ResourceModelPreview(_catalog, _toon);
            _sceneNav = null;
            var errors = new System.Collections.Generic.List<string>();
            try
            {
                ResourceEntry nav = _catalog.Find(FileConstants.GetNavFileVirtualPath(scene.SceneInfo.CityName, scene.SceneInfo.Model));
                if (nav != null)
                    _sceneNav = (NavFile)ResourceFormats.Read(nav, _catalog.Read(nav), _catalog.Codepage);
            }
            catch (Exception ex) { errors.Add("Navigation: " + ex.Message); }
            bool loadedBackground = false;
            try
            {
                ResourceEntry background = _catalog.Find(ResourceSceneData.BackgroundPath(scene.SceneInfo));
                if (background != null)
                {
                    _model.Load(background, ResourceFormats.Read(background, _catalog.Read(background), _catalog.Codepage));
                    loadedBackground = true;
                }
            }
            catch (Exception ex)
            {
                errors.Add("Background: " + ex.Message);
                _model.Dispose();
                _model = new ResourceModelPreview(_catalog, _toon);
            }
            if (!loadedBackground)
                _model.FramePoints(scene.NpcInfos.Select(n => ResourceSceneData.NpcPosition(n, _sceneNav))
                    .Concat(scene.ObjectInfos.Select(o => o.GameBoxPosition.ToUnityPosition())));
            if (errors.Count > 0)
                _error = string.Join("\n", errors);
        }

        private void DrawSceneMarkers(ScnFile scene, Rect rect)
        {
            for (int i = 0; i < scene.NpcInfos.Length; i++)
                DrawMarker(rect, ResourceSceneData.NpcPosition(scene.NpcInfos[i], _sceneNav), "N" + scene.NpcInfos[i].Id, 1, i, Color.cyan);
            for (int i = 0; i < scene.ObjectInfos.Length; i++)
                DrawMarker(rect, scene.ObjectInfos[i].GameBoxPosition.ToUnityPosition(), "O" + scene.ObjectInfos[i].Id, 2, i, Color.yellow);
        }

        private void DrawMarker(Rect rect, Vector3 position, string label, int type, int index, Color color)
        {
            if (!_model.Project(position, rect, out Vector2 point) || !rect.Contains(point))
                return;
            Color previous = GUI.color;
            GUI.color = color;
            if (GUI.Button(new Rect(point.x - 12, point.y - 8, 34, 18), label, EditorStyles.miniButton))
            {
                _sceneRecordType = type;
                _recordIndex = index;
                _recordQuery = "";
                _objectView.Reset();
            }
            GUI.color = previous;
        }

        private void DrawNavigationView(NavFile nav)
        {
            if (nav.Layers.Length == 0)
            {
                EditorGUILayout.HelpBox("No navigation layers.", MessageType.Info);
                return;
            }
            _navLayer = EditorGUILayout.Popup("Layer", _navLayer, Enumerable.Range(0, nav.Layers.Length).Select(i => "Layer " + i).ToArray());
            NavLayer layer = nav.Layers[_navLayer];
            if (layer.Width > SystemInfo.maxTextureSize || layer.Height > SystemInfo.maxTextureSize)
            {
                EditorGUILayout.HelpBox("Navigation dimensions exceed the GPU texture limit. Export the data for inspection.", MessageType.Warning);
                return;
            }
            if (_navTexture == null || _drawnNavLayer != _navLayer)
            {
                if (_navTexture != null)
                    DestroyImmediate(_navTexture);
                _navTexture = new Texture2D(layer.Width, layer.Height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point
                };
                _navTexture.SetPixels32(layer.Tiles.Select(ResourceSceneData.TileColor).ToArray());
                _navTexture.Apply();
                _drawnNavLayer = _navLayer;
                _navTile = Vector2Int.zero;
                _navPan = Vector2.zero;
            }
            GUILayout.Label($"{layer.Width} × {layer.Height} · Green: walkable · Red: obstacle · Gold: other blocked · Blue: portals", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label("GameBox tile axes: X →, Z ↑ · Drag to pan · Wheel to zoom", EditorStyles.miniLabel);
            Rect view = GUILayoutUtility.GetRect(100, 10000, 150, 10000, GUILayout.ExpandHeight(true));
            GUI.BeginGroup(view);
            float scale = Mathf.Min(view.width / layer.Width, view.height / layer.Height) * _navZoom;
            Rect image = new((view.width - layer.Width * scale) / 2 + _navPan.x, (view.height - layer.Height * scale) / 2 + _navPan.y,
                layer.Width * scale, layer.Height * scale);
            GUI.DrawTexture(image, _navTexture, ScaleMode.StretchToFill, false);
            foreach (var portal in layer.Portals.Where(p => !p.IsEmpty))
            {
                Rect area = new(image.x + portal.Left * scale, image.y + (layer.Height - 1 - portal.Bottom) * scale,
                    (portal.Right - portal.Left + 1) * scale, (portal.Bottom - portal.Top + 1) * scale);
                EditorGUI.DrawRect(new Rect(area.x, area.y, area.width, 2), Color.cyan);
                EditorGUI.DrawRect(new Rect(area.x, area.yMax - 2, area.width, 2), Color.cyan);
                EditorGUI.DrawRect(new Rect(area.x, area.y, 2, area.height), Color.cyan);
                EditorGUI.DrawRect(new Rect(area.xMax - 2, area.y, 2, area.height), Color.cyan);
            }
            Event e = Event.current;
            if (new Rect(0, 0, view.width, view.height).Contains(e.mousePosition))
            {
                if (image.Contains(e.mousePosition) && e.type is EventType.MouseMove or EventType.MouseDown)
                {
                    _navTile = ResourceSceneData.TileAt(e.mousePosition, image, layer);
                    Repaint();
                }
                if (e.type == EventType.MouseDrag)
                {
                    _navPan += e.delta;
                    e.Use();
                    Repaint();
                }
                if (e.type == EventType.ScrollWheel)
                {
                    _navZoom = Mathf.Clamp(_navZoom * Mathf.Exp(-e.delta.y * 0.06f), 0.5f, 24);
                    e.Use();
                    Repaint();
                }
            }
            GUI.EndGroup();
            _navTile.x = Mathf.Clamp(EditorGUILayout.IntField("Tile X", _navTile.x), 0, layer.Width - 1);
            _navTile.y = Mathf.Clamp(EditorGUILayout.IntField("Tile Z", _navTile.y), 0, layer.Height - 1);
            NavTile tile = layer.Tiles[_navTile.x + _navTile.y * layer.Width];
            GUILayout.Label($"Height: {tile.GameBoxYPosition} GameBox units · Floor: {tile.FloorType} ({(int)tile.FloorType})", EditorStyles.wordWrappedLabel);
            GUILayout.Label($"Walkable: {tile.IsWalkable()} · Obstacle: {tile.IsObstacle} · Obstacle distance: {tile.DistanceToNearestObstacle}");
            var portalIndices = layer.Portals.Select((p, i) => (p, i)).Where(pair => !pair.p.IsEmpty && pair.p.IsPointInsideRect(_navTile.x, _navTile.y)).Select(pair => pair.i);
            GUILayout.Label("Portals: " + string.Join(", ", portalIndices));
        }

        private void DrawLightView(LgtFile lights)
        {
            int index = PickRecord(lights.LightNodes.Select((light, i) => $"{i} · {light.LightType} · Radius {light.DecayRadius}").ToArray());
            if (index >= 0)
                _objectView.Draw(lights.LightNodes[index], DetailWidth);
        }
    }
}
