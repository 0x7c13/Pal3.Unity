// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class ResourceBrowserWindow
    {
        [SerializeField] private ResourceTextDraft _textDraft;
        private string _textFind = "";
        private int _textPage, _textMatch = -1;
        private bool _saveUtf8;

        private void LoadText(ResourceEntry entry)
        {
            _textDraft = CreateInstance<ResourceTextDraft>();
            _textDraft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _textDraft.SourcePath = entry.Path;
            _textDraft.PhysicalPath = entry.PhysicalPath;
            _textDraft.SourceRoot = _catalog.Root;
            _textDraft.Load(_bytes, _catalog.Codepage);
            _textPage = 0;
            _textMatch = -1;
            _textFind = "";
            _saveUtf8 = false;
            UpdateTextDirty();
        }

        private void RestoreTextDraft()
        {
            if (_textDraft == null)
                return;
            ResourceEntry entry = _catalog != null && _textDraft.CanReadFrom(_catalog.Root)
                ? _catalog.Find(_textDraft.SourcePath) : null;
            _selected = entry ?? new ResourceEntry(_textDraft.SourcePath, _textDraft.PhysicalPath);
            UpdateTextDirty();
        }

        private void UpdateTextDirty()
        {
            _textMatch = -1;
            hasUnsavedChanges = _textDraft != null && _textDraft.IsDirty;
            Repaint();
        }

        private void DestroyTextDraft()
        {
            if (_textDraft != null)
            {
                Undo.ClearUndo(_textDraft);
                DestroyImmediate(_textDraft);
            }
            _textDraft = null;
            hasUnsavedChanges = false;
        }

        private bool CanReplaceSelection()
        {
            if (_textDraft == null || !_textDraft.IsDirty)
                return true;
            int choice = EditorUtility.DisplayDialogComplex("Unsaved text copy", "Save your edited text as a new file before leaving this resource?", "Save copy", "Cancel", "Discard");
            return choice == 2 || choice == 0 && SaveTextCopy();
        }

        public override void SaveChanges()
        {
            if (SaveTextCopy())
                base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            if (_textDraft != null)
                _textDraft.Text = _textDraft.Original;
            base.DiscardChanges();
        }

        private void OnDestroy() => DestroyTextDraft();

        private bool SaveTextCopy()
        {
            if (_textDraft == null)
                return true;
            string name = ResourceModelPreview.FileName(_textDraft.SourcePath);
            string path = EditorUtility.SaveFilePanel("Save text copy (choose a new file)", "", Path.GetFileNameWithoutExtension(name) + ".edited", Path.GetExtension(name).TrimStart('.'));
            if (path.Length == 0)
                return false;
            try
            {
                ResourceCatalog.CheckOutsideRoot(_textDraft.SourceRoot, path);
                _catalog?.CheckExportDestination(path);
                byte[] bytes = _textDraft.Encode(_saveUtf8);
                ResourceCatalog.WriteNewFile(path, stream => stream.Write(bytes, 0, bytes.Length));
                _textDraft.Original = _textDraft.Text;
                UpdateTextDirty();
                _status = "Saved text copy: " + path;
                return true;
            }
            catch (Exception ex)
            {
                _error = ex.Message + " Choose UTF-8 if the source encoding cannot represent the edited characters.";
                return false;
            }
        }

        private void DrawTextView()
        {
            if (_textDraft == null || _textDraft.Text == null)
            {
                DrawHex();
                return;
            }
            EditorGUILayout.SelectableLabel("Source: " + _textDraft.SourceRoot, EditorStyles.miniLabel, GUILayout.Height(20));
            if (_catalog != null && !_textDraft.CanReadFrom(_catalog.Root))
                EditorGUILayout.HelpBox("This draft belongs to another data folder. Save a text copy or choose a resource from the current catalog. Original export is disabled for this draft.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"Encoding: {_textDraft.Codepage}{(_textDraft.HasBom ? " + BOM" : "")} · {_textDraft.Text.Length:N0} characters");
                _saveUtf8 = GUILayout.Toggle(_saveUtf8, "Save as UTF-8", GUILayout.Width(110));
                if (GUILayout.Button("Save copy…", GUILayout.Width(95)))
                    SaveTextCopy();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                string find = EditorGUILayout.TextField("Find", _textFind);
                if (find != _textFind)
                {
                    _textFind = find;
                    _textMatch = -1;
                }
                if (GUILayout.Button("Next", GUILayout.Width(50)) && _textFind.Length > 0)
                {
                    _textMatch = _textDraft.Text.IndexOf(_textFind, Mathf.Clamp(_textMatch + 1, 0, _textDraft.Text.Length), StringComparison.OrdinalIgnoreCase);
                    if (_textMatch < 0)
                        _textMatch = _textDraft.Text.IndexOf(_textFind, StringComparison.OrdinalIgnoreCase);
                    if (_textMatch >= 0)
                        _textPage = _textMatch / 16000;
                }
            }
            if (_textMatch >= 0 && _textFind.Length > 0)
                EditorGUILayout.HelpBox($"Match at character {_textMatch}: " + ResourceTextDraft.ContextAt(_textDraft.Text, _textMatch), MessageType.Info);
            if (!_textDraft.Editable)
            {
                EditorGUILayout.HelpBox("Large text is read-only and paged. Export the original to edit it externally.", MessageType.Info);
                int pages = Mathf.Max(1, (_textDraft.Text.Length + 15999) / 16000);
                _textPage = Mathf.Clamp(EditorGUILayout.IntField($"Page / {pages}", _textPage + 1) - 1, 0, pages - 1);
            }
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            if (_textDraft.Editable)
            {
                float height = Mathf.Max(150, style.CalcHeight(new GUIContent(_textDraft.Text), Mathf.Max(100, DetailWidth - 30)));
                string edited = EditorGUILayout.TextArea(_textDraft.Text, style, GUILayout.Height(height), GUILayout.ExpandWidth(true));
                if (edited != _textDraft.Text)
                {
                    if (edited.Length > 60000)
                        _error = "In-window text editing is limited to 60,000 characters. Export a copy for larger edits.";
                    else
                    {
                        Undo.RecordObject(_textDraft, "Edit resource text copy");
                        _textDraft.Text = edited;
                        UpdateTextDirty();
                    }
                }
            }
            else
            {
                int start = _textPage * 16000;
                string page = _textDraft.Text.Substring(start, Mathf.Min(16000, _textDraft.Text.Length - start));
                float height = Mathf.Max(150, style.CalcHeight(new GUIContent(page), Mathf.Max(100, DetailWidth - 30)));
                EditorGUILayout.SelectableLabel(page, style, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
