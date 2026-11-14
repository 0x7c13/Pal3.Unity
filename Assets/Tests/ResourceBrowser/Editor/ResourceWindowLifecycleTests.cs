// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class ResourceWindowLifecycleTests
    {
        private const string WindowName = "ResourceBrowserReloadTest";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private sealed class KeyboardFocusProbe : EditorWindow
        {
            public ResourceBrowserWindow Browser;
            public bool FocusList, InputFocused, ListFocused;
            public bool DrawList, ExitedGui, ScrollClosedAtExit, ListFocusedAtExit;
            public Vector2 MousePosition;
            private string _text = "Input";

            private void OnGUI()
            {
                if (Event.current.type == EventType.MouseMove)
                    MousePosition = Event.current.mousePosition;
                int listControl = GUIUtility.GetControlID(FocusType.Keyboard);
                if (FocusList)
                {
                    GUIUtility.keyboardControl = listControl;
                    FocusList = false;
                }
                Vector2 origin = GUIUtility.GUIToScreenPoint(Vector2.zero);
                try
                {
                    typeof(ResourceBrowserWindow).GetMethod("HandleListKeys", Private).Invoke(Browser, new object[] { listControl });
                    if (DrawList)
                        typeof(ResourceBrowserWindow).GetMethod("DrawList", Private).Invoke(Browser, new object[] { new Rect(0, 60, 240, 180), listControl });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ExitGUIException)
                {
                    ExitedGui = true;
                    ScrollClosedAtExit = GUIUtility.GUIToScreenPoint(Vector2.zero) == origin;
                    ListFocusedAtExit = GUIUtility.keyboardControl == listControl;
                    throw ex.InnerException;
                }
                GUI.SetNextControlName("TestInput");
                _text = EditorGUILayout.TextField(_text);
                InputFocused = GUI.GetNameOfFocusedControl() == "TestInput";
                ListFocused = GUIUtility.keyboardControl == listControl;
            }
        }

        [UnityTest]
        public IEnumerator MouseAndKeyboardSelectionExitTheCurrentGuiPass()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserQA/test-output", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(output);
            var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
            var probe = ScriptableObject.CreateInstance<KeyboardFocusProbe>();
            try
            {
                typeof(ResourceBrowserWindow).GetMethod("CancelScan", Private).Invoke(window, null);
                var catalog = (ResourceCatalog)Activator.CreateInstance(typeof(ResourceCatalog), Private, null, new object[] { output, 936 }, null);
                typeof(ResourceBrowserWindow).GetField("_catalog", Private).SetValue(window, catalog);
                var entries = (List<ResourceEntry>)typeof(ResourceBrowserWindow).GetField("_visible", Private).GetValue(window);
                var binary = new ResourceEntry("first.bin", Path.Combine(output, "first.bin"));
                var text = new ResourceEntry("second.txt", Path.Combine(output, "second.txt"));
                File.WriteAllBytes(binary.PhysicalPath, new byte[] { 1, 2, 3 });
                File.WriteAllText(text.PhysicalPath, "Selected text");
                entries.Add(binary);
                entries.Add(text);
                probe.Browser = window;
                probe.DrawList = true;
                probe.wantsMouseMove = true;
                probe.Show();
                probe.Focus();
                yield return null;
                foreach (bool mouse in new[] { true, false })
                {
                    typeof(ResourceBrowserWindow).GetMethod("Select", Private).Invoke(window, new object[] { binary });
                    probe.FocusList = true;
                    probe.SendEvent(new Event { type = EventType.Repaint });
                    probe.ExitedGui = false;
                    var click = new Vector2(20, 108);
                    probe.SendEvent(new Event { type = EventType.MouseMove, mousePosition = click });
                    // SendEvent uses host-view coordinates; account for the window's tab offset.
                    click += click - probe.MousePosition;
                    probe.SendEvent(mouse
                        ? new Event { type = EventType.MouseDown, button = 0, mousePosition = click }
                        : new Event { type = EventType.KeyDown, keyCode = KeyCode.DownArrow });
                    Assert.That(probe.ExitedGui, Is.True, mouse ? "Mouse selection" : "Keyboard selection");
                    Assert.That(probe.ScrollClosedAtExit, Is.True, "The scroll view must close before selection exits OnGUI.");
                    Assert.That(probe.ListFocusedAtExit, Is.True, "Selection must preserve list keyboard focus.");
                    Assert.That(typeof(ResourceBrowserWindow).GetField("_selected", Private).GetValue(window), Is.SameAs(text), mouse ? "Mouse selection" : "Keyboard selection");
                    var draft = (ResourceTextDraft)typeof(ResourceBrowserWindow).GetField("_textDraft", Private).GetValue(window);
                    Assert.That(draft.Text, Is.EqualTo("Selected text"));
                    probe.SendEvent(new Event { type = EventType.Repaint });
                }
            }
            finally
            {
                probe.Close();
                window.DiscardChanges();
                Object.DestroyImmediate(window);
                Directory.Delete(output, true);
            }
        }

        [UnityTest]
        public IEnumerator ListNavigationDoesNotConsumeArrowsAfterKeyboardFocusMovesToAnInput()
        {
            var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
            var probe = ScriptableObject.CreateInstance<KeyboardFocusProbe>();
            try
            {
                typeof(ResourceBrowserWindow).GetMethod("CancelScan", Private).Invoke(window, null);
                var entries = (List<ResourceEntry>)typeof(ResourceBrowserWindow).GetField("_visible", Private).GetValue(window);
                entries.Add(new ResourceEntry("first.txt"));
                entries.Add(new ResourceEntry("second.txt"));
                FieldInfo selection = typeof(ResourceBrowserWindow).GetField("_selected", Private);
                selection.SetValue(window, entries[0]);
                probe.Browser = window;
                probe.Show();
                probe.Focus();
                yield return null;
                probe.FocusList = true;
                probe.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(probe.ListFocused, Is.True);
                probe.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab, character = '\t' });
                probe.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(probe.InputFocused, Is.True, "Tab must transfer the list's keyboard focus to the input.");
                probe.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.DownArrow });
                Assert.That(selection.GetValue(window), Is.SameAs(entries[0]));
            }
            finally
            {
                probe.Close();
                Object.DestroyImmediate(window);
            }
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void CompletedIndexRebindsDraftSelectionOnlyToItsOriginalCatalog(bool sameRoot, bool sourceExists)
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserQA/test-output", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(output);
            string pathPreference = ResourceBrowserWindow.Variant + ".ResourceBrowser.Path";
            string codepagePreference = ResourceBrowserWindow.Variant + ".ResourceBrowser.Codepage";
            bool hadPath = EditorPrefs.HasKey(pathPreference), hadCodepage = EditorPrefs.HasKey(codepagePreference);
            string previousPath = EditorPrefs.GetString(pathPreference);
            int previousCodepage = EditorPrefs.GetInt(codepagePreference);
            var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
            try
            {
                typeof(ResourceBrowserWindow).GetMethod("CancelScan", Private).Invoke(window, null);
                using var catalog = (ResourceCatalog)Activator.CreateInstance(typeof(ResourceCatalog), Private, null, new object[] { output, 936 }, null);
                var entry = new ResourceEntry("draft.txt", Path.Combine(output, "draft.txt"));
                catalog.Entries.Add(new ResourceEntry("before.txt"));
                if (sourceExists)
                {
                    catalog.Entries.Add(entry);
                    var lookup = (Dictionary<string, ResourceEntry>)typeof(ResourceCatalog).GetField("_lookup", Private).GetValue(catalog);
                    lookup.Add(entry.Path, entry);
                }
                catalog.Entries.Add(new ResourceEntry("later.txt"));
                var draft = ScriptableObject.CreateInstance<ResourceTextDraft>();
                draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                draft.SourceRoot = sameRoot ? output : Path.Combine(output, "other");
                draft.SourcePath = entry.Path;
                draft.PhysicalPath = Path.Combine(draft.SourceRoot, "draft.txt");
                draft.Load(System.Text.Encoding.UTF8.GetBytes("original"), 65001);
                draft.Text = "unsaved text after reload";
                typeof(ResourceBrowserWindow).GetField("_textDraft", Private).SetValue(window, draft);
                typeof(ResourceBrowserWindow).GetMethod("RestoreTextDraft", Private).Invoke(window, null);
                FieldInfo selection = typeof(ResourceBrowserWindow).GetField("_selected", Private);
                Assert.That(selection.GetValue(window), Is.Not.SameAs(entry));
                typeof(ResourceBrowserWindow).GetField("_scan", Private).SetValue(window, Task.FromResult(catalog));
                typeof(ResourceBrowserWindow).GetMethod("Tick", Private).Invoke(window, null);

                var selected = (ResourceEntry)selection.GetValue(window);
                var visible = (List<ResourceEntry>)typeof(ResourceBrowserWindow).GetField("_visible", Private).GetValue(window);
                Assert.That(selected.Path, Is.EqualTo(draft.SourcePath));
                Assert.That(selected.PhysicalPath, Is.EqualTo(draft.PhysicalPath));
                Assert.That(visible.IndexOf(selected), Is.EqualTo(sameRoot && sourceExists ? 1 : -1));
                if (sameRoot && sourceExists)
                    Assert.That(selected, Is.SameAs(entry), "Highlight and arrow navigation must use the newly indexed entry.");
                else
                    Assert.That(selected, Is.Not.SameAs(entry), "A foreign or missing source must keep a detached draft.");
                Assert.That(draft.Text, Is.EqualTo("unsaved text after reload"));
                Assert.That(window.hasUnsavedChanges, Is.True);
            }
            finally
            {
                window.DiscardChanges();
                Object.DestroyImmediate(window);
                if (hadPath)
                    EditorPrefs.SetString(pathPreference, previousPath);
                else
                    EditorPrefs.DeleteKey(pathPreference);
                if (hadCodepage)
                    EditorPrefs.SetInt(codepagePreference, previousCodepage);
                else
                    EditorPrefs.DeleteKey(codepagePreference);
                Directory.Delete(output, true);
            }
        }

        [UnityTest]
        public IEnumerator UnsavedTextSurvivesRealAssemblyReloadAndClosesCleanlyAfterDiscard()
        {
            if (!SessionState.GetBool(WindowName, false))
            {
                var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
                window.name = WindowName;
                window.Show();
                var draft = ScriptableObject.CreateInstance<ResourceTextDraft>();
                draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                draft.SourceRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Game Data", ResourceBrowserWindow.Variant));
                draft.SourcePath = "reload-test.txt";
                draft.Load(System.Text.Encoding.UTF8.GetBytes("original"), 65001);
                draft.Text = "unsaved text after reload";
                using var serialized = new SerializedObject(window);
                serialized.FindProperty("_textDraft").objectReferenceValue = draft;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                typeof(ResourceBrowserWindow).GetMethod("UpdateTextDirty", Private).Invoke(window, null);
                SessionState.SetBool(WindowName, true);
                EditorUtility.RequestScriptReload();
            }
            yield return new WaitForDomainReload();
            ResourceBrowserWindow restored = Resources.FindObjectsOfTypeAll<ResourceBrowserWindow>().Single(w => w.name == WindowName);
            try
            {
                using var serialized = new SerializedObject(restored);
                var draft = (ResourceTextDraft)serialized.FindProperty("_textDraft").objectReferenceValue;
                Assert.That(draft, Is.Not.Null);
                Assert.That(draft.Text, Is.EqualTo("unsaved text after reload"));
                Assert.That(draft.IsDirty, Is.True);
                Assert.That(restored.hasUnsavedChanges, Is.True);
                var selected = (ResourceEntry)typeof(ResourceBrowserWindow).GetField("_selected", Private).GetValue(restored);
                Assert.That(selected.Path, Is.EqualTo(draft.SourcePath));
                restored.DiscardChanges();
                Assert.That(restored.hasUnsavedChanges, Is.False);
                restored.Close();
                Assert.That(draft == null, Is.True);
            }
            finally
            {
                SessionState.EraseBool(WindowName);
                if (restored != null)
                {
                    restored.DiscardChanges();
                    restored.Close();
                }
            }
        }
    }
}
