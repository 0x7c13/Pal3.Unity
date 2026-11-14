// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class ResourceBrowserWindow : EditorWindow
    {
#if PAL3A
        public const string Variant = "PAL3A";
#else
        public const string Variant = "PAL3";
#endif
        [SerializeField] private string _dataPath, _query = "";
        [SerializeField] private int _codepage = 936, _categoryIndex, _sourceIndex;
        private ResourceCatalog _catalog;

        private float DetailWidth => position.width - Mathf.Clamp(position.width * 0.34f, 240, 460) - 12;

        private bool SelectedCatalogMatches => _catalog != null && (_textDraft == null || _textDraft.CanReadFrom(_catalog.Root));

        private Task<ResourceCatalog> _scan;
        private CancellationTokenSource _scanCancellation;
        private volatile string _scanStatus;
        private readonly List<ResourceEntry> _visible = new();
        private string[] _sources = { "All" };
        private ResourceEntry _selected;
        private byte[] _bytes;
        private ResourceTexturePreview _texture;
        private ResourceMediaPreview _media;
        private ResourceExportMode _exportMode;
        private string _selectedMetadata;
        private string _error, _status;
        private Vector2 _listScroll, _detailScroll;
        private int _hexPage;
        private string _hex;
        private Task<string> _export;
        private CancellationTokenSource _exportCancellation;
        private volatile string _exportStatus;

        [MenuItem(Variant + "/Resource Browser", priority = 30)]
        public static void Open() => GetWindow<ResourceBrowserWindow>("Resources");

        private void OnEnable()
        {
            minSize = new Vector2(900, 580);
            wantsMouseMove = true;
            _dataPath = EditorPrefs.GetString(Variant + ".ResourceBrowser.Path",
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", Variant)));
            _codepage = EditorPrefs.GetInt(Variant + ".ResourceBrowser.Codepage", 936);
            saveChangesMessage = "Save the edited resource text as a new file before closing?";
            ResourceMediaPreview.CleanupStaleTemporaryFiles();
            Undo.undoRedoPerformed += UpdateTextDirty;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (Directory.Exists(_dataPath))
                StartScan(restoreDraft: true);
            RestoreTextDraft();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UpdateTextDirty;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            CancelScan();
            _exportCancellation?.Cancel();
            _exportCancellation?.Dispose();
            _exportCancellation = null;
            if (_export != null)
                _ = _export.ContinueWith(task => { _ = task.Exception; }, TaskScheduler.Default);
            _export = null;
            ClearSelection();
            _catalog?.Dispose();
            _catalog = null;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode)
                ClearSelection();
            if (state == PlayModeStateChange.EnteredEditMode)
                RestoreTextDraft();
            Repaint();
        }

        private void CancelScan()
        {
            _scanCancellation?.Cancel();
            _scanCancellation?.Dispose();
            _scanCancellation = null;
            if (_scan != null)
            {
                _ = _scan.ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                        task.Result.Dispose();
                    _ = task.Exception;
                }, TaskScheduler.Default);
                _scan = null;
            }
        }

        private void StartScan(bool restoreDraft = false)
        {
            if (!restoreDraft)
            {
                if (!CanReplaceSelection())
                    return;
                DestroyTextDraft();
            }
            CancelScan();
            ClearSelection();
            _catalog?.Dispose();
            _catalog = null;
            _visible.Clear();
            _error = null;
            _status = null;
            string path = _dataPath;
            int codepage = _codepage;
            _scanCancellation = new CancellationTokenSource();
            CancellationToken token = _scanCancellation.Token;
            _scan = Task.Run(() => ResourceCatalog.Load(path, codepage, Variant, token, message => _scanStatus = message), token);
        }

        private void Tick()
        {
            TickModel();
            if (_media != null && _media.Tick())
                Repaint();
            if (_scan is { IsCompleted: true })
            {
                try
                {
                    _catalog = _scan.GetAwaiter().GetResult();
                    _sources = new[] { "All" }.Concat(_catalog.Entries.Select(e => e.Source).Distinct().OrderBy(s => s)).ToArray();
                    _sourceIndex = 0;
                    EditorPrefs.SetString(Variant + ".ResourceBrowser.Path", _catalog.Root);
                    EditorPrefs.SetInt(Variant + ".ResourceBrowser.Codepage", _codepage);
                    Filter();
                    RestoreTextDraft();
                    _status = $"{_catalog.Entries.Count:N0} resources indexed";
                    if (_catalog.Diagnostics.Count > 0)
                        _error = string.Join("\n", _catalog.Diagnostics);
                }
                catch (OperationCanceledException) { _status = "Indexing cancelled."; }
                catch (Exception ex) { _error = ex.Message; }
                _scan = null;
                _scanCancellation?.Dispose();
                _scanCancellation = null;
                Repaint();
            }
            if (_export is { IsCompleted: true })
            {
                try
                {
                    _status = _export.GetAwaiter().GetResult();
                }
                catch (Exception ex) { _error = ex.Message; }
                _export = null;
                _exportCancellation?.Dispose();
                _exportCancellation = null;
                Repaint();
            }
            if (_scan != null || _export != null)
                Repaint();
        }

        private void Filter()
        {
            _visible.Clear();
            _categoryIndex = Mathf.Clamp(_categoryIndex, 0, ResourceFormats.Categories.Length - 1);
            _sourceIndex = Mathf.Clamp(_sourceIndex, 0, _sources.Length - 1);
            string[] terms = ResourceCatalog.SplitQuery(_query);
            if (_catalog != null)
                _visible.AddRange(_catalog.Entries.Where(e => ResourceCatalog.Matches(e, terms,
                    ResourceFormats.Categories[_categoryIndex], _sources[_sourceIndex])));
            _listScroll = Vector2.zero;
        }

        private void OnGUI()
        {
            int listControl = GUIUtility.GetControlID(FocusType.Keyboard);
            HandleListKeys(listControl);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(Variant, EditorStyles.boldLabel, GUILayout.Width(50));
                using (new EditorGUI.DisabledScope(_scan != null || _export != null))
                {
                    _dataPath = EditorGUILayout.TextField(_dataPath, EditorStyles.toolbarTextField);
                    if (GUILayout.Button("Folder…", EditorStyles.toolbarButton, GUILayout.Width(65)))
                    {
                        string path = EditorUtility.OpenFolderPanel("Select " + Variant + " game data", _dataPath, "");
                        if (path.Length > 0)
                            _dataPath = path;
                    }
                    _codepage = EditorGUILayout.Popup(_codepage == 950 ? 1 : 0, new[] { "简体 936", "繁體 950" }, GUILayout.Width(90)) == 0 ? 936 : 950;
                    if (GUILayout.Button("Index / Reload", EditorStyles.toolbarButton, GUILayout.Width(100)))
                        StartScan();
                }
                if (_scan != null && GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    CancelScan();
                    _status = "Indexing cancelled.";
                }
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                GUI.SetNextControlName("ResourceSearch");
                _query = EditorGUILayout.TextField(_query, EditorStyles.toolbarSearchField);
                _categoryIndex = EditorGUILayout.Popup(_categoryIndex, ResourceFormats.Categories, GUILayout.Width(110));
                _sourceIndex = EditorGUILayout.Popup(_sourceIndex, _sources, GUILayout.Width(150));
                if (EditorGUI.EndChangeCheck())
                    Filter();
                _exportMode = (ResourceExportMode)EditorGUILayout.EnumPopup(_exportMode, EditorStyles.toolbarPopup, GUILayout.Width(75));
                using (new EditorGUI.DisabledScope(_visible.Count == 0 || _export != null || _scan != null))
                {
                    if (GUILayout.Button("Export filtered…", EditorStyles.toolbarButton, GUILayout.Width(115)))
                        ExportFiltered();
                }
            }
            GUILayout.Label($"{_visible.Count:N0} matches · Search: words, -exclude, ext:dds · {_status}", EditorStyles.miniLabel);
            if (_scan != null)
                EditorGUILayout.HelpBox("Indexing: " + _scanStatus, MessageType.Info);
            if (_export != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(_exportStatus);
                    if (GUILayout.Button("Cancel export", GUILayout.Width(110)))
                        _exportCancellation?.Cancel();
                }
            }
            if (!string.IsNullOrEmpty(_error))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(_error, MessageType.Warning);
                    if (GUILayout.Button("×", GUILayout.Width(22)))
                        _error = null;
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                float width = Mathf.Clamp(position.width * 0.34f, 240, 460);
                Rect listRect = GUILayoutUtility.GetRect(width, width, 100, 100000, GUILayout.ExpandHeight(true));
                DrawList(listRect, listControl);
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    DrawDetail();
            }
        }

        private void DrawList(Rect rect, int listControl)
        {
            const float rowHeight = 38;
            ResourceEntry clicked = null;
            _listScroll = GUI.BeginScrollView(rect, _listScroll, new Rect(0, 0, rect.width - 18, _visible.Count * rowHeight));
            int first = Mathf.Max(0, (int)(_listScroll.y / rowHeight));
            int last = Mathf.Min(_visible.Count, first + Mathf.CeilToInt(rect.height / rowHeight) + 1);
            for (int i = first; i < last; i++)
            {
                ResourceEntry entry = _visible[i];
                Rect row = new(0, i * rowHeight, rect.width - 18, rowHeight);
                if (entry == _selected)
                    EditorGUI.DrawRect(row, new Color(0.2f, 0.4f, 0.6f, 0.5f));
                GUI.Label(new Rect(row.x + 6, row.y, row.width - 6, 19), entry.Name, EditorStyles.boldLabel);
                GUI.Label(new Rect(row.x + 6, row.y + 18, row.width - 6, 18), new GUIContent(entry.Path, entry.Path), EditorStyles.miniLabel);
                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
                {
                    clicked = entry;
                    GUIUtility.keyboardControl = listControl;
                    Event.current.Use();
                    break;
                }
            }
            GUI.EndScrollView();
            if (clicked != null)
            {
                Select(clicked);
                GUIUtility.ExitGUI();
            }
        }

        private void HandleListKeys(int listControl)
        {
            if (Event.current.type != EventType.KeyDown || Event.current.keyCode is not (KeyCode.UpArrow or KeyCode.DownArrow) ||
                GUIUtility.keyboardControl != listControl && GUI.GetNameOfFocusedControl() != "ResourceSearch" || _visible.Count == 0)
                return;
            const float rowHeight = 38;
            int index = Mathf.Clamp(_visible.IndexOf(_selected) + (Event.current.keyCode == KeyCode.DownArrow ? 1 : -1), 0, _visible.Count - 1);
            Select(_visible[index]);
            float bottom = _listScroll.y + Mathf.Max(100, position.height - 180);
            if (index * rowHeight < _listScroll.y)
                _listScroll.y = index * rowHeight;
            else if ((index + 1) * rowHeight > bottom)
                _listScroll.y += (index + 1) * rowHeight - bottom;
            Event.current.Use();
            GUIUtility.ExitGUI();
        }

        private void ClearSelection()
        {
            _media?.Dispose();
            _media = null;
            _selectedMetadata = null;
            ClearModelView();
            ClearSceneView();
            ClearDataView();
            _texture?.Dispose();
            _texture = null;
            _selected = null;
            _bytes = null;
            _hex = null;
            _hexPage = 0;
            _detailScroll = Vector2.zero;
        }

        private void Select(ResourceEntry entry)
        {
            if (entry == null || !CanReplaceSelection())
                return;
            DestroyTextDraft();
            ClearSelection();
            _selected = entry;
            _error = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            try
            {
                var sizes = _catalog.GetSizes(entry);
                _selectedMetadata = $"{entry.Source} · {sizes.length:N0} bytes" + (entry.IsArchived ? $" · packed {sizes.packedLength:N0} bytes" : "");
                if (entry.Category is "Audio" or "Video")
                {
                    _media = new ResourceMediaPreview(_catalog, entry);
                    return;
                }
                _bytes = _catalog.Read(entry, entry.Category == "Text" ? 4 * 1024 * 1024 : ResourceCatalog.PreviewByteLimit);
                if (entry.Category == "Textures")
                    _texture = new ResourceTexturePreview(entry, _bytes);
                else if (entry.Category == "Models")
                    LoadModelView(entry);
                else if (entry.Category == "Text")
                    LoadText(entry);
                else
                    _decoded = ResourceFormats.Read(entry, _bytes, _catalog.Codepage);
            }
            catch (Exception ex)
            {
                if (_textDraft != null && _textDraft.Text == null)
                    DestroyTextDraft();
                _error = entry.Path + "\n" + ex.Message;
            }
            Repaint();
        }

        private void DrawDetail()
        {
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Choose a resource. Models, textures, scripts, scenes and data use dedicated views. No Play Mode is required.", MessageType.Info);
                return;
            }
            GUILayout.Label(_selected.Name, EditorStyles.boldLabel);
            if (_selectedMetadata != null)
                GUILayout.Label(_selectedMetadata, EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(_selected.Path, GUILayout.Height(32));
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Copy path", EditorStyles.toolbarButton))
                    EditorGUIUtility.systemCopyBuffer = _selected.Path;
                using (new EditorGUI.DisabledScope(!SelectedCatalogMatches))
                {
                    if (GUILayout.Button("Export original…", EditorStyles.toolbarButton))
                        ExportSelected();
                    if (_decoded != null)
                    {
                        _inspectData = GUILayout.Toggle(_inspectData, "Fields", EditorStyles.toolbarButton);
                        if (GUILayout.Button("Export JSON…", EditorStyles.toolbarButton))
                            ExportJson();
                    }
                }
                if (_texture != null && GUILayout.Button("Export PNG…", EditorStyles.toolbarButton))
                    ExportPng();
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Resource previews are available in Edit Mode.", MessageType.Info);
                return;
            }
            if (_inspectData && _decoded != null)
            {
                _objectView.Draw(_decoded, DetailWidth);
                return;
            }
            if (_selected.Category == "Effects")
            {
                if (GUILayout.Button("Open Effect Editor"))
                    OpenEffectEditor();
            }
            if (_media != null)
                _media.Draw();
            else if (_texture != null)
            {
                GUILayout.Label($"{_texture.Texture.width} × {_texture.Texture.height} · {_texture.Texture.format}", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                    _texture.DrawSettings();
                _texture.Draw(GUILayoutUtility.GetRect(100, 10000, 100, 10000, GUILayout.ExpandHeight(true)));
            }
            else if (_selected.Category == "Models")
                DrawModelView();
            else if (_decoded is Core.DataReader.Scn.ScnFile scene)
                DrawSceneView(scene);
            else if (_decoded is Core.DataReader.Nav.NavFile nav)
                DrawNavigationView(nav);
            else if (_decoded is Core.DataReader.Lgt.LgtFile lights)
                DrawLightView(lights);
            else if (_decoded is Core.DataReader.Sce.SceFile script)
                DrawScriptView(script);
            else if (_decoded is Core.DataReader.Gdb.GdbFile database)
                DrawDatabaseView(database);
            else if (_selected.Category == "Text")
                DrawTextView();
            else if (_decoded != null)
                _objectView.Draw(_decoded, DetailWidth);
            else
                DrawHex();
        }

        private void DrawHex()
        {
            if (_bytes == null)
                return;
            int pages = Mathf.Max(1, (_bytes.Length + 4095) / 4096);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"{_bytes.Length:N0} bytes · Hex page", GUILayout.Width(170));
                int page = Mathf.Clamp(EditorGUILayout.IntField(_hexPage + 1, GUILayout.Width(60)) - 1, 0, pages - 1);
                if (page != _hexPage)
                {
                    _hexPage = page;
                    _hex = null;
                    _detailScroll = Vector2.zero;
                }
                GUILayout.Label($"/ {pages}");
            }
            if (_hex == null)
            {
                var text = new StringBuilder();
                for (int offset = _hexPage * 4096; offset < Mathf.Min(_bytes.Length, (_hexPage + 1) * 4096); offset += 16)
                {
                    text.Append(offset.ToString("X8")).Append("  ");
                    for (int i = offset; i < Mathf.Min(offset + 16, _bytes.Length); i++)
                        text.Append(_bytes[i].ToString("X2")).Append(' ');
                    text.AppendLine();
                }
                _hex = text.ToString();
            }
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            EditorGUILayout.TextArea(_hex, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();
        }

        private void ExportSelected()
        {
            if (!SelectedCatalogMatches)
                return;
            string path = EditorUtility.SaveFilePanel("Export original (existing files are kept)", "", _selected.Name, _selected.Extension.TrimStart('.'));
            if (path.Length == 0)
                return;
            try
            {
                var sizes = _catalog.GetSizes(_selected);
                if (_selected.IsArchived && sizes.length + sizes.packedLength > ResourceCatalog.PreviewByteLimit &&
                    !EditorUtility.DisplayDialog("Large archive resource", $"Extraction needs about {(sizes.length + sizes.packedLength) / 1024 / 1024:N0} MiB of byte buffers. Continue?", "Export", "Cancel"))
                    return;
                _catalog.ExportOriginal(_selected, path);
                _status = "Exported " + path;
            }
            catch (Exception ex) { _error = ex.Message; }
        }

        private void ExportPng()
        {
            string path = EditorUtility.SaveFilePanel("Export PNG (existing files are kept)", "", Path.GetFileNameWithoutExtension(_selected.Name), "png");
            if (path.Length == 0)
                return;
            try
            {
                _catalog.CheckExportDestination(path);
                path = ResourceCatalog.ExportPath(Path.GetDirectoryName(path), Path.GetFileName(path));
                _texture.SavePng(path);
                _status = "Exported " + path;
            }
            catch (Exception ex) { _error = ex.Message; }
        }

        private void ExportFiltered()
        {
            string folder = EditorUtility.OpenFolderPanel("Export filtered resources (existing files are skipped)", "", "");
            if (folder.Length == 0)
                return;
            try
            {
                _catalog.CheckExportDestination(folder);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                return;
            }
            ResourceExportMode mode = _exportMode;
            ResourceEntry[] entries = _visible.Where(entry => ResourceExport.Supports(entry, mode)).ToArray();
            ResourceCatalog catalog = _catalog;
            _exportCancellation = new CancellationTokenSource();
            CancellationToken token = _exportCancellation.Token;
            _export = Task.Run(() =>
            {
                int saved = 0, skipped = 0, failed = 0;
                var failures = new List<string>();
                foreach (ResourceEntry entry in entries)
                {
                    if (token.IsCancellationRequested)
                        break;
                    _exportStatus = $"{saved + skipped + failed + 1:N0}/{entries.Length:N0} · {entry.Path}";
                    try
                    {
                        string destination = ResourceCatalog.ExportPath(folder, entry.Path + ResourceExport.Suffix(mode));
                        if (File.Exists(destination))
                        {
                            skipped++;
                            continue;
                        }
                        var sizes = catalog.GetSizes(entry);
                        if ((entry.IsArchived || mode != ResourceExportMode.Original) && sizes.length + sizes.packedLength > ResourceCatalog.PreviewByteLimit)
                        {
                            skipped++;
                            continue;
                        }
                        ResourceExport.Write(catalog, entry, mode, destination);
                        saved++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        if (failures.Count < 10)
                            failures.Add(entry.Path + ": " + ex.Message);
                    }
                }
                return $"{(token.IsCancellationRequested ? "Cancelled. " : "")}Exported {saved:N0}, skipped {skipped:N0}, failed {failed:N0}. Existing files and entries requiring more than 64 MiB to decode are skipped; export large originals individually. " + string.Join(" · ", failures);
            });
        }
    }
}
