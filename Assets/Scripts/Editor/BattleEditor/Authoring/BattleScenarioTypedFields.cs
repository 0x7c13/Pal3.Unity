// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using Battle;
    using UnityEditor;
    using UnityEngine;

    // The authoring DTO stays outside Unity's inline serializer. Every write
    // goes through the window's transaction, including boxed struct writeback.
    public static class BattleScenarioTypedFields
    {
        private static readonly Dictionary<string, bool> Expanded = new();
        private static readonly Dictionary<Type, FieldInfo[]> Fields = new();

        public static void Field(object owner, string fieldName, string label, Action<string, Action> edit, GUIStyle buttonStyle = null)
        {
            if (owner == null || edit == null)
                throw new ArgumentNullException(owner == null ? nameof(owner) : nameof(edit));
            FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)
                ?? throw new ArgumentException("Unknown authoring field: " + fieldName, nameof(fieldName));
            if (field.IsInitOnly)
                throw new ArgumentException("The authoring field is read-only.", nameof(fieldName));
            string identity = owner is BattleParticipant actor ? actor.InstanceId : owner.GetType().FullName;
            Draw(field.FieldType, field.GetValue(owner), value => field.SetValue(owner, value), label,
                identity + "." + fieldName, edit, new List<object>(), 0, buttonStyle);
        }

        private static void Draw(Type type, object value, Action<object> assign, string label, string path,
            Action<string, Action> edit, List<object> ancestors, int depth, GUIStyle buttonStyle)
        {
            if (type == typeof(string))
            {
                EditorGUI.BeginChangeCheck();
                string next = EditorGUILayout.TextField(label, (string)value ?? "");
                if (EditorGUI.EndChangeCheck())
                    edit(label, () => assign(next));
                return;
            }
            if (type.IsEnum)
            {
                EditorGUI.BeginChangeCheck();
                Enum next = EditorGUILayout.EnumPopup(label, (Enum)value);
                if (EditorGUI.EndChangeCheck())
                    edit(label, () => assign(next));
                return;
            }
            if (type.IsPrimitive || type == typeof(decimal))
            {
                DrawScalar(type, value, assign, label, edit);
                return;
            }
            if (value == null)
            {
                using var row = new EditorGUILayout.HorizontalScope();
                EditorGUILayout.LabelField(label, "未配置", GUILayout.Height(buttonStyle?.fixedHeight ?? EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("创建", buttonStyle ?? GUI.skin.button, GUILayout.Width(48)))
                    edit(label, () => assign(Create(type)));
                return;
            }
            if (depth >= 12 || !type.IsValueType && ancestors.Exists(parent => ReferenceEquals(parent, value)))
            {
                EditorGUILayout.LabelField(label, type.Name);
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                bool expanded = !Expanded.TryGetValue(path, out bool stored) || stored;
                Rect header = EditorGUILayout.GetControlRect(false, buttonStyle?.fixedHeight ?? EditorGUIUtility.singleLineHeight);
                Expanded[path] = EditorGUI.Foldout(header, expanded, label, true);
                bool optional = type == typeof(BattleMagicSword);
                string action = value is IList ? "清空" : optional ? "移除" : "重置";
                if (!type.IsValueType && GUILayout.Button(action, buttonStyle ?? GUI.skin.button, GUILayout.Width(48)))
                {
                    edit(label, () =>
                    {
                        if (value is Array array)
                        {
                            var cleared = (Array)array.Clone();
                            Array.Clear(cleared, 0, cleared.Length);
                            assign(cleared);
                        }
                        else if (value is IList list)
                        {
                            list.Clear();
                            assign(list);
                        }
                        else
                            assign(optional ? null : Create(type));
                    });
                    return;
                }
                if (!Expanded[path])
                    return;
            }
            ancestors.Add(value);
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            try
            {
                if (value is IList list)
                    DrawList(type, list, assign, label, path, edit, ancestors, depth, buttonStyle);
                else
                {
                    if (!Fields.TryGetValue(type, out FieldInfo[] fields))
                        Fields[type] = fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                    foreach (FieldInfo field in fields)
                    {
                        if (field.IsInitOnly || field.IsLiteral || field.IsNotSerialized)
                            continue;
                        Draw(field.FieldType, field.GetValue(value), replacement =>
                        {
                            field.SetValue(value, replacement);
                            assign(value);
                        }, ObjectNames.NicifyVariableName(field.Name), path + "." + field.Name, edit, ancestors, depth + 1, buttonStyle);
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndent;
                ancestors.RemoveAt(ancestors.Count - 1);
            }
        }

        private static void DrawList(Type type, IList list, Action<object> assign, string label, string path,
            Action<string, Action> edit, List<object> ancestors, int depth, GUIStyle buttonStyle)
        {
            Type element = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            int count = list.Count;
            for (int index = 0; index < count; index++)
            {
                int slot = index;
                using var row = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                Draw(element, list[slot], value =>
                {
                    list[slot] = value;
                    assign(list);
                }, "元素 " + slot, path + "[" + slot + "]", edit, ancestors, depth + 1, buttonStyle);
                if (!type.IsArray && GUILayout.Button("删除元素", buttonStyle ?? GUI.skin.button, GUILayout.Width(80)))
                {
                    edit(label, () =>
                    {
                        list.RemoveAt(slot);
                        assign(list);
                    });
                    return;
                }
            }
            if (!type.IsArray && GUILayout.Button("添加元素", buttonStyle ?? GUI.skin.button))
                edit(label, () =>
                {
                    list.Add(Create(element));
                    assign(list);
                });
        }

        private static object Create(Type type) => type == typeof(string) ? "" :
            type.IsArray ? Array.CreateInstance(type.GetElementType(), 0) : Activator.CreateInstance(type);

        private static void DrawScalar(Type type, object value, Action<object> assign, string label, Action<string, Action> edit)
        {
            EditorGUI.BeginChangeCheck();
            object next;
            if (type == typeof(bool))
                next = EditorGUILayout.Toggle(label, (bool)value);
            else if (type == typeof(float))
                next = EditorGUILayout.FloatField(label, (float)value);
            else if (type == typeof(double))
                next = EditorGUILayout.DoubleField(label, (double)value);
            else if (type == typeof(decimal))
            {
                string text = EditorGUILayout.TextField(label, ((decimal)value).ToString(CultureInfo.InvariantCulture));
                next = decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number) ? number : value;
            }
            else if (type == typeof(ulong))
            {
                string text = EditorGUILayout.TextField(label, value.ToString());
                next = ulong.TryParse(text, out ulong number) ? number : value;
            }
            else if (type == typeof(char))
            {
                string text = EditorGUILayout.TextField(label, value.ToString());
                next = text.Length == 0 ? '\0' : text[0];
            }
            else
            {
                long number = EditorGUILayout.LongField(label, Convert.ToInt64(value));
                next = IntegerValue(type, number);
            }
            if (EditorGUI.EndChangeCheck() && !Equals(next, value))
                edit(label, () => assign(next));
        }

        private static object IntegerValue(Type type, long number)
        {
            (long minimum, long maximum) = type == typeof(byte) ? (byte.MinValue, byte.MaxValue) :
                type == typeof(sbyte) ? (sbyte.MinValue, sbyte.MaxValue) :
                type == typeof(short) ? (short.MinValue, short.MaxValue) :
                type == typeof(ushort) ? (ushort.MinValue, ushort.MaxValue) :
                type == typeof(uint) ? (uint.MinValue, uint.MaxValue) :
                type == typeof(int) ? (int.MinValue, int.MaxValue) : (long.MinValue, long.MaxValue);
            return Convert.ChangeType(Math.Clamp(number, minimum, maximum), type, CultureInfo.InvariantCulture);
        }
    }
}
