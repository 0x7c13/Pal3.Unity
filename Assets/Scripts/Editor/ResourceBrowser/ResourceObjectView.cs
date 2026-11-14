// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    // A bounded, lazy field inspector for protocol records, not a serializer or editing framework.
    public sealed class ResourceObjectView
    {
        private readonly HashSet<string> _expanded = new() { "root" };
        private readonly Dictionary<string, int> _pages = new();
        private Vector2 _scroll;
        private float _width;

        public void Reset()
        {
            _expanded.Clear();
            _expanded.Add("root");
            _pages.Clear();
            _scroll = Vector2.zero;
        }

        public void Draw(object value, float width)
        {
            _width = width;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawValue("Record", value, "root", 0);
            EditorGUILayout.EndScrollView();
        }

        private void DrawValue(string label, object value, string path, int depth)
        {
            if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(label, GUILayout.Width(180));
                    string text = value?.ToString() ?? "null";
                    float height = Mathf.Max(18, EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(text), Mathf.Max(60, _width - 220)));
                    EditorGUILayout.SelectableLabel(text, EditorStyles.wordWrappedLabel, GUILayout.Height(height));
                }
                return;
            }
            if (depth > 12)
            {
                GUILayout.Label(label + " (maximum display depth)");
                return;
            }
            int count = value is ICollection collection ? collection.Count : -1;
            bool expanded = _expanded.Contains(path);
            bool next = EditorGUILayout.Foldout(expanded, count >= 0 ? $"{label} [{count}]" : label + " · " + value.GetType().Name, true);
            if (next)
                _expanded.Add(path);
            else
                _expanded.Remove(path);
            if (!next)
                return;
            EditorGUI.indentLevel++;
            try
            {
                if (value is IEnumerable sequence)
                {
                    int page = _pages.TryGetValue(path, out int selected) ? selected : 0;
                    if (count > 50)
                    {
                        page = Mathf.Clamp(EditorGUILayout.IntField("Page (50 per page)", page + 1) - 1, 0, (count - 1) / 50);
                        _pages[path] = page;
                    }
                    int index = 0;
                    if (value is IDictionary dictionary)
                    {
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            if (index >= page * 50 + 50)
                                break;
                            if (index++ >= page * 50)
                                DrawValue(entry.Key.ToString(), entry.Value, path + "." + entry.Key, depth + 1);
                        }
                    }
                    else
                    {
                        foreach (object item in sequence)
                        {
                            if (index >= page * 50 + 50)
                                break;
                            if (index++ >= page * 50)
                                DrawValue((index - 1).ToString(), item, path + "." + (index - 1), depth + 1);
                        }
                    }
                }
                else
                {
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
                    foreach (FieldInfo field in value.GetType().GetFields(flags))
                        DrawValue(field.Name, field.GetValue(value), path + "." + field.Name, depth + 1);
                    foreach (PropertyInfo property in value.GetType().GetProperties(flags).Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
                    {
                        object child;
                        try
                        {
                            child = property.GetValue(value);
                        }
                        catch (Exception ex) { child = ex.GetBaseException().Message; }
                        DrawValue(property.Name, child, path + "." + property.Name, depth + 1);
                    }
                }
            }
            finally { EditorGUI.indentLevel--; }
        }
    }
}
