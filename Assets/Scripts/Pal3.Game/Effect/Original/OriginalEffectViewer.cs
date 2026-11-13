// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using UnityEngine;

    // Runtime controls for ResourceViewer; editor tooling uses the same player.
    public sealed class OriginalEffectViewer : MonoBehaviour
    {
        private static int _nextWindowId = 25000;
        private readonly int _windowId = _nextWindowId++;
        private OriginalEffectResources _resources;
        private Transform _parent;
        private Action _beforeLoad;
        private OriginalEffectPlayer _player;
        private OriginalPortalEffect _portal;

        public uint PortalType
        {
            get; set;
        }

        private GameObject _root;
        private string _id = "98", _diagnostics = "";
        private Rect _window = new(15, 65, 330, 250);
        private bool _visible;

        public void Initialize(OriginalEffectResources resources, Transform parent, Action beforeLoad)
        {
            _resources = resources;
            _parent = parent;
            _beforeLoad = beforeLoad;
        }

        public void Load(int groupId)
        {
            Stop();
            _beforeLoad?.Invoke();
            _id = groupId.ToString();
            _visible = true;
            _diagnostics = "";
            try
            {
                if (groupId < -1)
                    throw new ArgumentOutOfRangeException(nameof(groupId));
                OriginalEffectOverrides edits = Resources.Load<OriginalEffectOverrides>(_resources.OverridesPath);
                if (groupId >= 0)
                {
                    _diagnostics = string.Join("\n", _resources.Diagnose(groupId, edits));
                    if (_diagnostics.Length > 0)
                        return;
                }
                _root = new GameObject(groupId < 0 ? "Original Transport" : $"Original {_resources.GameVariant} group {groupId}");
                _root.transform.SetParent(_parent, false);
                if (groupId < 0)
                {
                    _portal = _root.AddComponent<OriginalPortalEffect>();
                    _portal.Initialize(_resources, PortalType);
                }
                else
                {
                    _player = _root.AddComponent<OriginalEffectPlayer>();
                    _player.Initialize(_resources, groupId, overrides: edits);
                    _diagnostics += _player.Error;
                }
            }
            catch (Exception ex)
            {
                _diagnostics = ex.Message;
                Stop();
            }
        }

        public void Stop()
        {
            if (_root != null)
            {
                _root.SetActive(false);
                OriginalEffectResources.Destroy(_root);
            }
            _root = null;
            _player = null;
            _portal = null;
        }

        public void Seek(float time)
        {
            time = Mathf.Clamp(float.IsFinite(time) ? time : 0, 0, 600);
            if (_player != null)
                _player.Seek(time);
            if (_portal != null)
                _portal.Sample(time);
        }

        public void Pause(bool value)
        {
            if (_player != null)
                _player.Paused = value;
            if (_portal != null)
                _portal.AutoAdvance = !value;
        }

        private void OnDisable() => Stop();

        private void OnGUI()
        {
            if (_resources == null)
                return;
            if (GUI.Button(new Rect(Screen.width - 145, 15, 130, 32), $"Original {_resources.GameVariant} effects"))
                _visible = !_visible;
            if (_visible)
                _window = GUI.Window(_windowId, _window, Draw, $"Original {_resources.GameVariant} effect viewer");
        }

        private void Draw(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Group", GUILayout.Width(45));
            _id = GUILayout.TextField(_id);
            if (GUILayout.Button("Load") && int.TryParse(_id, out int group))
                Load(group);
            if (GUILayout.Button("Transport"))
                Load(-1);
            GUILayout.EndHorizontal();
            if (_portal != null && _resources.GameVariant == "PAL3A")
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Transport style {PortalType}");
                if (GUILayout.Button("Next style"))
                {
                    PortalType = (PortalType + 1) % 7;
                    Load(-1);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
                Pause(false);
            if (GUILayout.Button("Pause"))
                Pause(true);
            if (GUILayout.Button("Reset"))
                Seek(0);
            if (GUILayout.Button("Stop"))
                Stop();
            GUILayout.EndHorizontal();
            if (_player != null)
            {
                _player.Speed = GUILayout.HorizontalSlider(_player.Speed, 0, 4);
                GUILayout.Label($"Speed {_player.Speed:0.00}x");
                float time = GUILayout.HorizontalSlider(_player.Time, 0, Mathf.Max(30, _player.Time));
                if (Mathf.Abs(time - _player.Time) > 0.001f)
                {
                    Pause(true);
                    Seek(time);
                }
                GUILayout.Label($"{_player.Time:0.00}s  objects {_player.NodeCount}  quads/particles {_player.QuadCount}");
                if (_player.Error != null)
                    GUILayout.Label(_player.Error);
            }
            GUILayout.Label(_diagnostics.Length == 0 ? "Original data / shared runtime" : _diagnostics);
            GUI.DragWindow();
        }
    }
}
