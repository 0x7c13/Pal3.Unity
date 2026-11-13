// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Pal3.Core.DataReader.Cpk;
    using Pal3.Core.DataReader.Data;
    using Pal3.Core.DataReader.Par;
    using Pal3.Core.FileSystem;
    using Pal3.Game.Effect.Original;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;

    public sealed class OriginalEffectEditor : EditorWindow
    {
        private OriginalEffectResources _resources;
        private OriginalEffectPlayer _player;
        private OriginalPortalEffect _portal;
        private uint _portalType;
        [SerializeField] private OriginalEffectOverrides _edits;
        [SerializeField] private string _assetPath = "", _savedJson;
        private GameObject _root, _private;
        private Scene _scene;
        private Camera _camera;
        private RenderTexture _target, _privateTarget;
        private string _dataPath, _search = "", _error;
        [SerializeField] private bool _pal3a;

        private string Variant => _pal3a ? "PAL3A" : "PAL3";

        private int _codepage = 936;
        [SerializeField] private int _id = 98, _objectIndex;
        private Vector2 _listScroll, _inspectorScroll, _orbit = new(135, 15);
        private float _distance = 14, _speed = 1, _duration = 15;
        [SerializeField] private float _time;
        private bool _playing, _compare, _loop;
        private uint _seed = 1;
        private bool _emitterCurve;
        private int _particleIndex, _curveIndex;
        private double _lastUpdate;
        private string _previewJson;
        private float? _pendingSeek;
        private double _seekAfter;

        [MenuItem("PAL3/Original Effect Editor")]
        public static void Open()
        {
            var window = GetWindow<OriginalEffectEditor>("Original Effects");
            window.ChangeVariant(false);
        }

        [MenuItem("PAL3A/Original Effect Editor")]
        public static void OpenPal3A()
        {
            var window = GetWindow<OriginalEffectEditor>("Original Effects");
            window.ChangeVariant(true);
        }

        private void ChangeVariant(bool pal3a)
        {
            if (_pal3a == pal3a || !CanReplaceEdits())
                return;
            _pal3a = pal3a;
            _id = pal3a ? 160 : 98;
            _time = 0;
            LoadOverrides(Resources.Load<OriginalEffectOverrides>($"Effects/{Variant}Overrides"));
            LoadPreferences();
            LoadData();
        }

        private void LoadPreferences()
        {
            _dataPath = EditorPrefs.GetString($"{Variant}.OriginalEffect.DataPath", Path.GetFullPath(Path.Combine(Application.dataPath, $"../../Game Data/{Variant}")));
            _codepage = EditorPrefs.GetInt($"{Variant}.OriginalEffect.Codepage", 936) == 950 ? 950 : 936;
        }

        public void CreateGUI() => rootVisualElement.Add(new IMGUIContainer(DrawWindow) { style = { flexGrow = 1 } });

        private void OnEnable()
        {
            minSize = new Vector2(1050, 640);
            LoadPreferences();
            if (_edits == null)
                LoadOverrides(Resources.Load<OriginalEffectOverrides>($"Effects/{Variant}Overrides"));
            _edits.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            saveChangesMessage = "Save the original effect overrides before closing?";
            _scene = EditorSceneManager.NewPreviewScene();
            var cameraObject = new GameObject("Effect preview camera") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(cameraObject, _scene);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
            _camera.fieldOfView = 40;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 500;
            _camera.scene = _scene;
            _lastUpdate = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += RefreshAfterUndo;
            if (Directory.Exists(_dataPath))
                LoadData();
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= RefreshAfterUndo;
            ClearPreview();
            _resources?.Dispose();
            _resources = null;
            if (_target != null)
            {
                _target.Release();
                DestroyImmediate(_target);
            }
            if (_privateTarget != null)
            {
                _privateTarget.Release();
                DestroyImmediate(_privateTarget);
            }
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
        }

        private void OnDestroy()
        {
            if (_edits != null)
            {
                Undo.ClearUndo(_edits);
                DestroyImmediate(_edits);
            }
        }

        private void LoadOverrides(OriginalEffectOverrides asset)
        {
            if (_edits != null)
            {
                Undo.ClearUndo(_edits);
                DestroyImmediate(_edits);
            }
            _edits = asset != null ? Instantiate(asset) : CreateInstance<OriginalEffectOverrides>();
            _edits.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _assetPath = asset != null ? AssetDatabase.GetAssetPath(asset) : "";
            _savedJson = JsonUtility.ToJson(_edits);
            hasUnsavedChanges = false;
        }

        private void MarkChanged() => hasUnsavedChanges = JsonUtility.ToJson(_edits) != _savedJson;

        public override void SaveChanges()
        {
            if (SaveOverrides())
                base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            LoadOverrides(_assetPath.Length > 0 ? AssetDatabase.LoadAssetAtPath<OriginalEffectOverrides>(_assetPath) : null);
            base.DiscardChanges();
            RefreshAfterUndo();
        }

        private bool CanReplaceEdits()
        {
            if (!hasUnsavedChanges)
                return true;
            int choice = EditorUtility.DisplayDialogComplex("Original effect overrides", "Save your current edits before loading another asset?", "Save", "Cancel", "Discard");
            return choice == 2 || choice == 0 && SaveOverrides();
        }

        private bool SaveOverrides()
        {
            string path = EditorUtility.SaveFilePanelInProject($"Save {Variant} effect overrides",
                _assetPath.Length > 0 ? Path.GetFileNameWithoutExtension(_assetPath) : $"{Variant}Overrides", "asset",
                $"Use Assets/Resources/Effects/{Variant}Overrides.asset for runtime loading.");
            if (path.Length == 0)
                return false;
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && existing is not OriginalEffectOverrides)
            {
                _error = "Choose an OriginalEffectOverrides asset or a new path.";
                return false;
            }
            var saved = existing as OriginalEffectOverrides;
            if (saved != null)
            {
                Undo.RecordObject(saved, $"Save {Variant} effect overrides");
                EditorUtility.CopySerialized(_edits, saved);
                saved.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(saved);
            }
            else
            {
                saved = Instantiate(_edits);
                saved.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(saved, path);
            }
            saved.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(saved);
            AssetDatabase.SaveAssets();
            _assetPath = path;
            _savedJson = JsonUtility.ToJson(_edits);
            hasUnsavedChanges = false;
            return true;
        }

        private void LoadData()
        {
            float time = _time;
            ClearPreview();
            _resources?.Dispose();
            _resources = null;
            _error = null;
            try
            {
                var hash = new Crc32Hash();
                hash.Init();
                var fs = new CpkFileSystem(_dataPath, hash, _codepage);
                fs.Mount("basedata/basedata.cpk", _codepage);
                _resources = new OriginalEffectResources(fs, _codepage);
                if (_resources.GameVariant != Variant)
                    throw new InvalidDataException($"The selected directory contains {_resources.GameVariant} data; select the matching variant first.");
                EditorPrefs.SetString($"{Variant}.OriginalEffect.DataPath", _dataPath);
                Restart();
                Seek(time);
            }
            catch (Exception ex)
            {
                _resources?.Dispose();
                _resources = null;
                _error = ex.ToString();
            }
        }

        private void ClearPreview()
        {
            if (_root != null)
                DestroyImmediate(_root);
            if (_private != null)
                DestroyImmediate(_private);
            _player = null;
            _portal = null;
        }

        private void Restart()
        {
            ClearPreview();
            _time = 0;
            _pendingSeek = null;
            _error = null;
            _previewJson = JsonUtility.ToJson(_edits);
            if (_resources == null)
                return;
            try
            {
                _root = new GameObject("Original effect preview") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(_root, _scene);
                if (_id < 0)
                {
                    _portal = _root.AddComponent<OriginalPortalEffect>();
                    _portal.AutoAdvance = false;
                    _portal.Initialize(_resources, _portalType);
                }
                else
                {
                    string[] issues = _resources.Diagnose(_id, _edits);
                    if (issues.Length > 0)
                        _error = string.Join("\n", issues);
                    else
                    {
                        _player = _root.AddComponent<OriginalEffectPlayer>();
                        _player.AutoAdvance = false;
                        _player.Initialize(_resources, _id, _seed, _edits);
                        _player.Loop = false;
                        if (_player.Error != null)
                            _error = _player.Error;
                    }
                }
                if (_compare)
                {
                    GameObject prefab = Resources.Load<GameObject>($"Prefabs/VFX/{Variant}/{(_id < 0 ? 0 : _id)}");
                    if (prefab != null)
                    {
                        _private = Instantiate(prefab);
                        SceneManager.MoveGameObjectToScene(_private, _scene);
                        foreach (Transform child in _private.GetComponentsInChildren<Transform>(true))
                            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
                        foreach (ParticleSystem system in _private.GetComponentsInChildren<ParticleSystem>(true))
                        {
                            system.useAutoRandomSeed = false;
                            system.randomSeed = _seed;
                            system.Simulate(0, false, true, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ClearPreview();
                _error = ex.ToString();
            }
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Min(0.1f, (float)(now - _lastUpdate));
            _lastUpdate = now;
            if (_pendingSeek.HasValue && now >= _seekAfter)
            {
                Seek(_pendingSeek.Value);
                Repaint();
            }
            if (_playing)
            {
                Advance(dt * _speed);
                if (_time >= _duration)
                {
                    if (_loop)
                        Restart();
                    else
                        _playing = false;
                }
                Repaint();
            }
        }

        private void Advance(float delta)
        {
            _time += delta;
            _player?.Advance(delta);
            _portal?.Sample(_time);
            if (_private != null)
                foreach (ParticleSystem ps in _private.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Simulate(delta, false, false, true);
        }

        private void Seek(float time)
        {
            _pendingSeek = null;
            _time = time;
            _player?.Seek(time);
            _portal?.Sample(time);
            if (_private != null)
                foreach (ParticleSystem ps in _private.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Simulate(time, false, true, true);
        }

        private void QueueSeek(float time)
        {
            _playing = false;
            _pendingSeek = time;
            _seekAfter = EditorApplication.timeSinceStartup + 0.05;
            Repaint();
        }

        private void RefreshAfterUndo()
        {
            MarkChanged();
            if (_previewJson == JsonUtility.ToJson(_edits))
                return;
            float time = _time;
            Restart();
            Seek(time);
            Repaint();
        }

        private void DrawWindow()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int variant = EditorGUILayout.Popup(_pal3a ? 1 : 0, new[] { "PAL3", "PAL3A" }, GUILayout.Width(80));
                if ((variant == 1) != _pal3a)
                    ChangeVariant(variant == 1);
                _dataPath = EditorGUILayout.TextField(_dataPath);
                int codepage = EditorGUILayout.Popup(_codepage == 950 ? 1 : 0, new[] { "GBK (936)", "Big5 (950)" }, GUILayout.Width(105)) == 0 ? 936 : 950;
                if (codepage != _codepage)
                {
                    _codepage = codepage;
                    EditorPrefs.SetInt($"{Variant}.OriginalEffect.Codepage", _codepage);
                    LoadData();
                }
                if (GUILayout.Button("Browse", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel($"{Variant} data (contains basedata)", _dataPath, "");
                    if (path.Length > 0)
                    {
                        _dataPath = path;
                        LoadData();
                    }
                }
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(55)))
                    LoadData();
            }
            var asset = _assetPath.Length > 0 ? AssetDatabase.LoadAssetAtPath<OriginalEffectOverrides>(_assetPath) : null;
            var selected = (OriginalEffectOverrides)EditorGUILayout.ObjectField("Overrides", asset, typeof(OriginalEffectOverrides), false);
            if (selected != asset && CanReplaceEdits())
            {
                LoadOverrides(selected);
                Restart();
            }
            if (_resources == null)
            {
                EditorGUILayout.HelpBox(_error ?? "Choose the PAL3 game data directory.", MessageType.Info);
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(185)))
                {
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                    _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                    if (GUILayout.Button("Scene: Transport point"))
                        Select(-1);
                    for (int id = 0; id < _resources.Definitions.Groups.Length; id++)
                    {
                        EffectGroup g = _resources.Definitions.Groups[id];
                        if (g.Objects.Length == 0)
                            continue;
                        string label = $"{id}  ({g.Objects.Length})";
                        if (_search.Length > 0 && !label.Contains(_search) && !g.Objects.Any(o => _resources.Definitions.TryGetType(o.TypeCrc, out var d) && d.TypeName.Contains(_search)))
                            continue;
                        if (GUILayout.Toggle(_id == id, label, "Button") && _id != id)
                            Select(id);
                    }
                    EditorGUILayout.EndScrollView();
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    if (_id < 0 && _pal3a)
                    {
                        int style = EditorGUILayout.Popup("Transport style", (int)_portalType, new[] { "Default", "1 (trans_s)", "2 (trans_m)", "3 (trans_l)", "4 (trans_f)", "5 (trans_t)", "6 (trans_p)" });
                        if ((uint)style != _portalType)
                        {
                            _portalType = (uint)style;
                            Restart();
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(_playing ? "Pause" : "Play", GUILayout.Width(60)))
                            _playing = !_playing;
                        if (GUILayout.Button("Reset", GUILayout.Width(55)))
                            Restart();
                        if (GUILayout.Button("Step", GUILayout.Width(50)))
                        {
                            _playing = false;
                            Advance(OriginalEffectPlayer.FixedStep);
                            Repaint();
                        }
                        GUILayout.Label("Speed", GUILayout.Width(40));
                        _speed = EditorGUILayout.FloatField(_speed, GUILayout.Width(45));
                        _speed = float.IsFinite(_speed) ? Mathf.Clamp(_speed, 0, 16) : 1;
                        _loop = GUILayout.Toggle(_loop, "Loop");
                        bool compare = GUILayout.Toggle(_compare, "Private comparison");
                        if (compare != _compare)
                        {
                            _compare = compare;
                            Restart();
                        }
                    }
                    Rect preview = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawPreview(preview);
                    float displayedTime = _pendingSeek ?? _time;
                    float time = EditorGUILayout.Slider("Time", displayedTime, 0, _duration);
                    if (Mathf.Abs(time - displayedTime) > 0.00001f)
                        QueueSeek(time);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _duration = EditorGUILayout.FloatField("Preview duration", _duration);
                        _duration = float.IsFinite(_duration) ? Mathf.Clamp(_duration, 0.1f, 600) : 15;
                        uint seed = (uint)Mathf.Max(1, EditorGUILayout.IntField("Seed", (int)_seed));
                        if (seed != _seed)
                        {
                            _seed = seed;
                            Restart();
                        }
                    }
                    EditorGUILayout.LabelField($"Original nodes: {_player?.NodeCount ?? 0}   Quads/particles: {_player?.QuadCount ?? 0}   {_player?.Error}");
                    EditorGUILayout.LabelField("Drag to orbit · Scroll to zoom", EditorStyles.miniLabel);
                    if (_error != null)
                        EditorGUILayout.HelpBox(_error, MessageType.Warning);
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(310)))
                    DrawInspector();
            }
        }

        private void Select(int id)
        {
            _id = id;
            _objectIndex = 0;
            Restart();
        }

        private void DrawPreview(Rect rect)
        {
            Event e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    _orbit += e.delta * new Vector2(0.5f, -0.5f);
                    e.Use();
                    Repaint();
                }
                if (e.type == EventType.ScrollWheel)
                {
                    _distance = Mathf.Clamp(_distance * Mathf.Exp(e.delta.y * 0.08f), 0.3f, 200);
                    e.Use();
                    Repaint();
                }
            }
            if (e.type != EventType.Repaint || rect.width < 1 || rect.height < 1)
                return;
            int w = Mathf.Max(1, (int)(rect.width / (_compare ? 2 : 1))), h = Mathf.Max(1, (int)rect.height);
            if (_target == null || _target.width != w || _target.height != h)
            {
                if (_target != null)
                {
                    _target.Release();
                    DestroyImmediate(_target);
                }
                if (_privateTarget != null)
                {
                    _privateTarget.Release();
                    DestroyImmediate(_privateTarget);
                }
                _target = new RenderTexture(w, h, 24) { hideFlags = HideFlags.HideAndDontSave };
                _target.Create();
                _privateTarget = new RenderTexture(w, h, 24) { hideFlags = HideFlags.HideAndDontSave };
                _privateTarget.Create();
            }
            _camera.transform.position = Quaternion.Euler(_orbit.y, _orbit.x, 0) * new Vector3(0, 0, -_distance) + Vector3.up * 1.5f;
            _camera.transform.LookAt(Vector3.up * 1.5f);
            _camera.aspect = (float)w / h;
            _player?.Render(_camera);
            void Draw(RenderTexture destination, GameObject hidden, Rect area, string label)
            {
                Renderer[] rs = hidden == null ? Array.Empty<Renderer>() : hidden.GetComponentsInChildren<Renderer>();
                bool[] state = rs.Select(r => r.enabled).ToArray();
                for (int i = 0; i < rs.Length; i++)
                    rs[i].enabled = false;
                try
                {
                    RenderPipeline.SubmitRenderRequest(_camera, new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
                    GUI.DrawTexture(area, destination, ScaleMode.StretchToFill, false);
                    GUI.Label(new Rect(area.x + 8, area.y + 8, area.width - 16, 25), label, EditorStyles.whiteBoldLabel);
                }
                finally
                {
                    for (int i = 0; i < rs.Length; i++)
                        if (rs[i] != null)
                            rs[i].enabled = state[i];
                }
            }
            Draw(_target, _private, new Rect(rect.x, rect.y, w, h), $"Original {Variant} data");
            if (_compare)
                Draw(_privateTarget, _root, new Rect(rect.x + w, rect.y, w, h), _private != null ? "Private prefab (comparison only)" : "No private prefab");
        }

        private void DrawInspector()
        {
            EditorGUILayout.LabelField("Original object / task parameters", EditorStyles.boldLabel);
            if (_id < 0)
            {
                EditorGUILayout.HelpBox("MXTransport: original two cones and rotating plane. This is separate from flexeff group 0.", MessageType.Info);
                return;
            }
            EffectGroup group = _resources.Definitions.Groups[_id];
            if (group.Objects.Length == 0)
                return;
            _objectIndex = EditorGUILayout.IntSlider("Object", _objectIndex, 0, group.Objects.Length - 1);
            EffectGroupObject source = group.Objects[_objectIndex];
            _resources.Definitions.TryGetType(source.TypeCrc, out EffectDefinition d);
            EditorGUILayout.LabelField($"{d.TypeName} / {EffectClass.Name(d.ClassNameCrc)}");
            EditorGUILayout.SelectableLabel(d.TextureName ?? "", GUILayout.Height(35));
            OriginalEffectObjectOverride edit = _edits.Find(_id, _objectIndex);
            if (edit == null && GUILayout.Button("Edit this object"))
            {
                Undo.RecordObject(_edits, "Edit original effect");
                edit = new OriginalEffectObjectOverride
                {
                    GroupId = _id,
                    ObjectIndex = _objectIndex,
                    Definition = JsonUtility.FromJson<EffectDefinition>(JsonUtility.ToJson(d)),
                    Tasks = source.Tasks.Select(t => JsonUtility.FromJson<EffectTask>(JsonUtility.ToJson(t))).ToArray()
                };
                _edits.Objects.Add(edit);
                MarkChanged();
            }
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            if (edit != null)
            {
                var so = new SerializedObject(_edits);
                so.Update();
                SerializedProperty entry = so.FindProperty("Objects").GetArrayElementAtIndex(_edits.Objects.IndexOf(edit));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Hidden"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Definition"), true);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Tasks"), true);
                if (so.ApplyModifiedProperties())
                {
                    MarkChanged();
                    float time = _time;
                    Restart();
                    Seek(time);
                }
            }
            else
                foreach (EffectTask t in source.Tasks)
                    EditorGUILayout.LabelField($"{t.StartTime:0.###}s + {t.Duration:0.###}s   loops={t.RequireLoops}");
            EffectDefinition effective = edit?.Definition ?? d;
            if (EffectClass.Name(effective.ClassNameCrc) == "S-Particle")
            {
                try
                {
                    SparFile spar = _resources.Spar(effective.ParticleFileName);
                    EditorGUILayout.LabelField($"SPar: {spar.Groups.Length} groups / shader {spar.Shader}");
                    foreach (SparGroup sg in spar.Groups)
                    {
                        EditorGUILayout.LabelField($"{sg.Name}: capacity {sg.Capacity}, {sg.Keyframes.Length} keys", EditorStyles.wordWrappedLabel);
                        EditorGUILayout.SelectableLabel(sg.Texture, GUILayout.Height(30));
                    }
                    EditorGUILayout.HelpBox("Native SPar playback: keyframes, emitter paths, particle controllers and original shader blending. SPar fields are read-only here.", MessageType.Info);
                }
                catch (Exception ex)
                {
                    EditorGUILayout.HelpBox(ex.Message, MessageType.Warning);
                }
            }
            if (EffectClass.Name(effective.ClassNameCrc) == "particle")
                DrawParticleCurves(effective.ParticleFileName);
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Save overrides for game…"))
                SaveOverrides();
            if (GUILayout.Button("Discard edits for this object") && edit != null)
            {
                Undo.RecordObject(_edits, "Discard original effect override");
                _edits.Objects.Remove(edit);
                MarkChanged();
                Restart();
            }
        }

        private void DrawParticleCurves(string path)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Particle resource curves", EditorStyles.boldLabel);
            ParticleFile file;
            try
            {
                file = _resources.Particle(path);
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox(ex.Message, MessageType.Warning);
                return;
            }
            _emitterCurve = EditorGUILayout.Toggle("Emitter curves", _emitterCurve);
            int count = _emitterCurve ? file.Emitters.Length : file.Particles.Length;
            if (count == 0)
                return;
            _particleIndex = EditorGUILayout.IntSlider("Resource index", _particleIndex, 0, count - 1);
            string[] names = _emitterCurve ? new[] { "Direction", "RandomVelocity", "Emission", "Life" } : new[] { "Size", "Rotation", "Alpha", "Color" };
            _curveIndex = EditorGUILayout.Popup("Curve", Mathf.Clamp(_curveIndex, 0, names.Length - 1), names);
            string name = names[_curveIndex];
            ParticleCurve source = OriginalEffectOverrides.FindCurve(file, _emitterCurve, _particleIndex, name);
            var entry = _edits.ParticleCurves.Find(c => string.Equals(c.File, path, StringComparison.OrdinalIgnoreCase)
                && c.Emitter == _emitterCurve && c.Index == _particleIndex && c.Parameter == name);
            bool validEntry = entry == null || entry.Channels != null && entry.Channels.Length == source.Channels && entry.Channels.All(c => c != null);
            if (!validEntry)
                EditorGUILayout.HelpBox("This curve override has invalid channels. Restore the original curve to repair it.", MessageType.Warning);
            for (int channel = 0; channel < source.Channels; channel++)
            {
                if (!validEntry)
                    break;
                AnimationCurve curve = entry != null ? CopyCurve(entry.Channels[channel]) : SampleCurve(source, channel);
                EditorGUI.BeginChangeCheck();
                AnimationCurve changed = EditorGUILayout.CurveField($"Channel {channel}", curve);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_edits, "Edit particle curve");
                    if (entry == null)
                    {
                        entry = new OriginalParticleCurveOverride
                        {
                            File = path,
                            Parameter = name,
                            Index = _particleIndex,
                            Emitter = _emitterCurve,
                            Channels = Enumerable.Range(0, source.Channels).Select(c => SampleCurve(source, c)).ToArray()
                        };
                        _edits.ParticleCurves.Add(entry);
                    }
                    entry.Channels[channel] = CopyCurve(changed);
                    MarkChanged();
                    float time = _time;
                    Restart();
                    Seek(time);
                }
            }
            EditorGUILayout.LabelField(entry == null ? "Original sampled curve (unchanged)" : "Unity curve override for this particle resource", EditorStyles.wordWrappedMiniLabel);
            if (entry != null && GUILayout.Button("Restore original particle curve"))
            {
                Undo.RecordObject(_edits, "Restore particle curve");
                _edits.ParticleCurves.Remove(entry);
                MarkChanged();
                float time = _time;
                Restart();
                Seek(time);
            }
        }

        private static AnimationCurve CopyCurve(AnimationCurve curve) =>
            new(curve.keys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode
            };

        private static AnimationCurve SampleCurve(ParticleCurve source, int channel)
        {
            var curve = new AnimationCurve(Enumerable.Range(0, 101).Select(i => new Keyframe(i / 100f, source.Evaluate(i / 100f, channel))).ToArray());
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }
    }
}
