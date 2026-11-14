// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Linq;
    using Core.DataReader.Mtl;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class ResourceBrowserWindow
    {
        private ResourceModelPreview _model;
        private object _decoded;
        [SerializeField] private bool _toon, _loop = true;
        private bool _playing, _showDependencies;
        private float _time, _speed = 1;
        private double _lastTick;
        private string[] _animationPaths = Array.Empty<string>();
        private string[] _animationLabels = Array.Empty<string>();

        private void LoadModelView(ResourceEntry entry)
        {
            _decoded = ResourceFormats.Read(entry, _bytes, _catalog.Codepage);
            if (_decoded is MtlFile)
                return;
            _model = new ResourceModelPreview(_catalog, _toon);
            try
            {
                _model.Load(entry, _decoded);
            }
            catch
            {
                _model.Dispose();
                _model = null;
                throw;
            }
            string directory = ResourceModelPreview.DirectoryOf(entry.Path);
            if (entry.Extension is ".msh" or ".mov")
            {
                _animationPaths = _catalog.Entries.Where(e => e.Extension == ".mov" &&
                    ResourceModelPreview.DirectoryOf(e.Path).Equals(directory, StringComparison.OrdinalIgnoreCase)).Select(e => e.Path).ToArray();
                _animationLabels = new[] { "Select animation…" }.Concat(_animationPaths.Select(ResourceModelPreview.FileName)).ToArray();
            }
            _lastTick = EditorApplication.timeSinceStartup;
        }

        private void TickModel()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)Math.Min(0.1, now - _lastTick);
            _lastTick = now;
            if (_model == null || !_playing || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            _time += delta * _speed;
            if (_time >= _model.Duration)
            {
                if (_loop && _model.Duration > 0)
                    _time %= _model.Duration;
                else
                {
                    _time = _model.Duration;
                    _playing = false;
                }
            }
            try
            {
                _model.Sample(_time);
            }
            catch (Exception ex)
            {
                _playing = false;
                _error = ex.Message;
            }
            Repaint();
        }

        private void ClearModelView()
        {
            _model?.Dispose();
            _model = null;
            _decoded = null;
            _playing = false;
            _time = 0;
            _animationPaths = Array.Empty<string>();
            _animationLabels = Array.Empty<string>();
        }

        private void ToggleModelPlayback()
        {
            _playing = !_playing;
            if (_playing && _time >= _model.Duration)
                _time = 0;
        }

        private void DrawModelView()
        {
            if (_decoded is MtlFile materialFile)
            {
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                foreach (var material in materialFile.Materials)
                {
                    EditorGUILayout.LabelField("Material textures", EditorStyles.boldLabel);
                    foreach (string name in material.TextureFileNames)
                    {
                        string path = ResourceModelPreview.CombinePath(ResourceModelPreview.DirectoryOf(_selected.Path), name);
                        ResourceEntry texture = _catalog.Find(System.IO.Path.ChangeExtension(path, ".dds")) ?? _catalog.Find(path);
                        using (new EditorGUI.DisabledScope(texture == null))
                        {
                            if (GUILayout.Button(name))
                            {
                                Select(texture);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                    string text = Newtonsoft.Json.JsonConvert.SerializeObject(material, Newtonsoft.Json.Formatting.Indented);
                    var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                    EditorGUILayout.SelectableLabel(text, style, GUILayout.Height(style.CalcHeight(new GUIContent(text), Mathf.Max(100, DetailWidth - 30))));
                }
                EditorGUILayout.EndScrollView();
                return;
            }
            if (_model == null)
            {
                DrawHex();
                return;
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool toon = GUILayout.Toggle(_toon, "Toon", EditorStyles.toolbarButton, GUILayout.Width(55));
                if (toon != _toon)
                {
                    _toon = toon;
                    Select(_selected);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Frame", EditorStyles.toolbarButton, GUILayout.Width(55)))
                    _model.Frame();
                _showDependencies = GUILayout.Toggle(_showDependencies, "Dependencies", EditorStyles.toolbarButton);
            }
            if (_animationPaths.Length > 0)
            {
                int current = Array.FindIndex(_animationPaths, p => p.Equals(_selected.Path, StringComparison.OrdinalIgnoreCase)) + 1;
                int selected = EditorGUILayout.Popup("Animation", current, _animationLabels);
                if (selected != current && selected > 0)
                {
                    Select(_catalog.Find(_animationPaths[selected - 1]));
                    GUIUtility.ExitGUI();
                }
            }
            if (_model.Duration > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(_playing ? "Pause" : "Play", GUILayout.Width(55)))
                        ToggleModelPlayback();
                    _loop = GUILayout.Toggle(_loop, "Loop", GUILayout.Width(55));
                    _speed = EditorGUILayout.Slider("Speed", _speed, 0.1f, 3);
                }
                EditorGUI.BeginChangeCheck();
                _time = EditorGUILayout.Slider("Time (seconds)", _time, 0, _model.Duration);
                if (EditorGUI.EndChangeCheck())
                {
                    _playing = false;
                    try
                    {
                        _model.Sample(_time);
                    }
                    catch (Exception ex) { _error = ex.Message; }
                }
            }
            GUILayout.Label(_model.Statistics, EditorStyles.miniLabel);
            GUILayout.Label("Drag: orbit · Shift / middle drag: pan · Wheel: zoom · F: frame", EditorStyles.miniLabel);
            if (_model.Dependencies.Any(pair => !pair.Value))
                EditorGUILayout.HelpBox("Some textures are missing (shown white). See Dependencies for exact paths.", MessageType.Warning);
            if (_showDependencies)
            {
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.MaxHeight(150));
                foreach (var dependency in _model.Dependencies)
                {
                    if (GUILayout.Button((dependency.Value ? "Open  " : "Missing  ") + dependency.Key, EditorStyles.linkLabel) && dependency.Value)
                    {
                        Select(_catalog.Find(dependency.Key));
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            _model.Draw(GUILayoutUtility.GetRect(100, 10000, 100, 10000, GUILayout.ExpandHeight(true)));
            if (Event.current.type is EventType.MouseDrag or EventType.ScrollWheel or EventType.Used)
                Repaint();
        }
    }
}
