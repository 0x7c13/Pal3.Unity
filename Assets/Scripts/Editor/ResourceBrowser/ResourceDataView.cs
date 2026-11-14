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
    using Core.DataReader.Gdb;
    using Core.DataReader.Sce;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class ResourceBrowserWindow
    {
        private bool _inspectData, _dialogueOnly, _showVariables;
        private int _databaseTable, _scriptPage;
        private uint? _pendingScriptBlock;
        private uint _listedBlock = uint.MaxValue;
        private List<ResourceScriptLine> _scriptLines;
        private string _scriptFind = "";

        private void ClearDataView()
        {
            _inspectData = false;
            _databaseTable = 0;
            _scriptPage = 0;
            _scriptLines = null;
            _listedBlock = uint.MaxValue;
            _pendingScriptBlock = null;
            _scriptFind = "";
            _showVariables = false;
        }

        private void DrawScriptView(SceFile file)
        {
            SceScriptBlock[] blocks = file.ScriptBlocks.Values.OrderBy(b => b.Id).ToArray();
            if (_pendingScriptBlock is { } requested)
            {
                int index = Array.FindIndex(blocks, b => b.Id == requested);
                if (index >= 0)
                {
                    _recordIndex = index;
                    _recordQuery = "";
                }
                else
                    _error = $"Script block {requested} is not present in {_selected.Path}.";
                _pendingScriptBlock = null;
            }
            int picked = PickRecord(blocks.Select(b => $"Block {b.Id} · {b.Description}").ToArray());
            if (picked < 0)
                return;
            SceScriptBlock block = blocks[picked];
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _dialogueOnly = GUILayout.Toggle(_dialogueOnly, "Dialogue only", EditorStyles.toolbarButton);
                _showVariables = GUILayout.Toggle(_showVariables, $"Locals ({block.UserVariables.Length})", EditorStyles.toolbarButton);
                if (GUILayout.Button("Export script…", EditorStyles.toolbarButton))
                    ExportScript(file);
            }
            GUILayout.Label("Block-relative byte offsets · mask bits denote variable parameters · inspection listing", EditorStyles.wordWrappedMiniLabel);
            if (_showVariables)
            {
                _objectView.Draw(block.UserVariables, DetailWidth);
                return;
            }
            if (_scriptLines == null || _listedBlock != block.Id)
            {
                _scriptLines = ResourceScriptListing.Decode(block, file.Codepage);
                _listedBlock = block.Id;
                _scriptPage = 0;
            }
            _scriptFind = EditorGUILayout.TextField("Find in block", _scriptFind);
            string[] lines = _scriptLines.Where(line => !_dialogueOnly || line.Dialogue != null || line.IsError)
                .Select(line => _dialogueOnly && !line.IsError ? $"{line.Offset:X6}  {line.Dialogue}" : line.Text)
                .Where(line => line.IndexOf(_scriptFind, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            int pages = Mathf.Max(1, (lines.Length + 99) / 100);
            _scriptPage = Mathf.Clamp(EditorGUILayout.IntField($"Page / {pages} · {lines.Length} lines", _scriptPage + 1) - 1, 0, pages - 1);
            string text = string.Join("\n", lines.Skip(_scriptPage * 100).Take(100));
            if (text.Length > 60000)
                text = text[..60000] + "\n[Display limit reached. Export the script for complete text.]";
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            float height = Mathf.Max(100, style.CalcHeight(new GUIContent(text), Mathf.Max(100, DetailWidth - 30)));
            EditorGUILayout.SelectableLabel(text, style, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();
        }

        private void ExportScript(SceFile file)
        {
            string path = EditorUtility.SaveFilePanel("Export inspection listing", "", _selected.Name + (_dialogueOnly ? ".dialogue" : ""), "txt");
            if (path.Length == 0)
                return;
            try
            {
                _catalog.CheckExportDestination(path);
                ResourceCatalog.WriteNewFile(path, stream =>
                {
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true);
                    ResourceScriptListing.Export(file, writer, _dialogueOnly);
                });
                _status = "Exported " + path;
            }
            catch (Exception ex) { _error = ex.Message; }
        }

        private void DrawDatabaseView(GdbFile database)
        {
            int table = GUILayout.Toolbar(_databaseTable, new[] { "Items", "Actors", "Skills", "Combos" });
            if (table != _databaseTable)
            {
                _databaseTable = table;
                _recordIndex = 0;
                _recordQuery = "";
                _objectView.Reset();
            }
            (string label, object record)[] records = table switch
            {
                0 => database.GameItemInfos.OrderBy(p => p.Key).Select(p => ($"{p.Key} · {p.Value.Name} · {p.Value.Type}", (object)p.Value)).ToArray(),
                1 => database.CombatActorInfos.OrderBy(p => p.Key).Select(p => ($"{p.Key} · {p.Value.Name} · {p.Value.ModelId}", (object)p.Value)).ToArray(),
                2 => database.SkillInfos.OrderBy(p => p.Key).Select(p => ($"{p.Key} · {p.Value.Name}", (object)p.Value)).ToArray(),
                _ => database.ComboSkillInfos.OrderBy(p => p.Key).Select(p => ($"{p.Key} · {p.Value.Name}", (object)p.Value)).ToArray(),
            };
            int index = PickRecord(records.Select(r => r.label).ToArray());
            if (index >= 0)
                _objectView.Draw(records[index].record, DetailWidth);
        }

        private void ExportJson()
        {
            string path = EditorUtility.SaveFilePanel("Export decoded resource JSON", "", _selected.Name, "json");
            if (path.Length == 0)
                return;
            try
            {
                _catalog.CheckExportDestination(path);
                WriteJson(_decoded, path);
                _status = "Exported " + path;
            }
            catch (Exception ex) { _error = ex.Message; }
        }

        public static void WriteJson(object value, string path)
        {
            ResourceCatalog.WriteNewFile(path, stream =>
            {
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true);
                var serializer = new JsonSerializer { Formatting = Formatting.Indented, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                serializer.Converters.Add(new StringEnumConverter());
                serializer.Serialize(writer, value);
            });
        }

        private void OpenSceneScript(string city, uint block)
        {
            ResourceEntry script = _catalog.Find(Core.Contract.Constants.FileConstants.GetSceneSceFileVirtualPath(city));
            using (new EditorGUI.DisabledScope(script == null))
            {
                if (GUILayout.Button("Open script block " + block))
                {
                    Select(script);
                    if (_selected == script)
                        _pendingScriptBlock = block;
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void OpenEffectEditor()
        {
            EditorPrefs.SetString(Variant + ".OriginalEffect.DataPath", _catalog.Root);
            EditorPrefs.SetInt(Variant + ".OriginalEffect.Codepage", _catalog.Codepage);
            EditorApplication.ExecuteMenuItem(Variant + "/Effect Editor");
        }
    }
}
