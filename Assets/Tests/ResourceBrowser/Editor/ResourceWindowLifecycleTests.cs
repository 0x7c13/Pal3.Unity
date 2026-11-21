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
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class ResourceWindowLifecycleTests
    {
        private const string WindowName = "ResourceBrowserReloadTest";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private const string EnvironmentKey = WindowName + ".Environment";

        [SetUp]
        public void IsolateLifecyclePreferences()
        {
            // SessionState survives the real reload; instance/static fixture fields do not.
            if (SessionState.GetBool(EnvironmentKey, false))
                return;
            string pathKey = ResourceBrowserWindow.Variant + ".ResourceBrowser.Path";
            string codepageKey = ResourceBrowserWindow.Variant + ".ResourceBrowser.Codepage";
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserQA/lifecycle", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory);
            SessionState.SetBool(EnvironmentKey + ".HadPath", EditorPrefs.HasKey(pathKey));
            SessionState.SetString(EnvironmentKey + ".Path", EditorPrefs.GetString(pathKey));
            SessionState.SetBool(EnvironmentKey + ".HadCodepage", EditorPrefs.HasKey(codepageKey));
            SessionState.SetInt(EnvironmentKey + ".Codepage", EditorPrefs.GetInt(codepageKey));
            SessionState.SetString(EnvironmentKey + ".Directory", directory);
            SessionState.SetBool(EnvironmentKey, true);
            // A missing source is deliberate: draft persistence does not require
            // indexing the user's real data or starting a background scan.
            EditorPrefs.SetString(pathKey, Path.Combine(directory, "unavailable-source"));
            EditorPrefs.SetInt(codepageKey, 936);
        }

        [TearDown]
        public void RestoreLifecycleEnvironment()
        {
            string draftId = SessionState.GetString(WindowName + ".DraftId", "");
            try
            {
                foreach (ResourceBrowserWindow window in Resources.FindObjectsOfTypeAll<ResourceBrowserWindow>().Where(value => value.name == WindowName))
                {
                    window.DiscardChanges();
                    Object.DestroyImmediate(window);
                }
            }
            finally
            {
                try
                {
                    // Recover an orphan too if the reference itself was lost by a regression.
                    foreach (ResourceTextDraft draft in Resources.FindObjectsOfTypeAll<ResourceTextDraft>().Where(value => value.GetEntityId().ToString() == draftId))
                    {
                        Undo.ClearUndo(draft);
                        Object.DestroyImmediate(draft);
                    }
                }
                finally
                {
                    try
                    {
                        RestorePreferencesAndDirectory();
                    }
                    finally
                    {
                        ClearReloadState();
                    }
                }
            }
        }

        private static void RestorePreferencesAndDirectory()
        {
            if (!SessionState.GetBool(EnvironmentKey, false))
                return;
            string pathKey = ResourceBrowserWindow.Variant + ".ResourceBrowser.Path";
            string codepageKey = ResourceBrowserWindow.Variant + ".ResourceBrowser.Codepage";
            if (SessionState.GetBool(EnvironmentKey + ".HadPath", false))
                EditorPrefs.SetString(pathKey, SessionState.GetString(EnvironmentKey + ".Path", ""));
            else
                EditorPrefs.DeleteKey(pathKey);
            if (SessionState.GetBool(EnvironmentKey + ".HadCodepage", false))
                EditorPrefs.SetInt(codepageKey, SessionState.GetInt(EnvironmentKey + ".Codepage", 936));
            else
                EditorPrefs.DeleteKey(codepageKey);
            string directory = SessionState.GetString(EnvironmentKey + ".Directory", "");
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        private static void ClearReloadState()
        {
            SessionState.EraseBool(WindowName);
            SessionState.EraseString(WindowName + ".WindowId");
            SessionState.EraseString(WindowName + ".DraftId");
            SessionState.EraseBool(EnvironmentKey);
            SessionState.EraseBool(EnvironmentKey + ".HadPath");
            SessionState.EraseString(EnvironmentKey + ".Path");
            SessionState.EraseBool(EnvironmentKey + ".HadCodepage");
            SessionState.EraseInt(EnvironmentKey + ".Codepage");
            SessionState.EraseString(EnvironmentKey + ".Directory");
        }

        [Test]
        public void DetachedTextEditingStateIsPrivateToEachPanel()
        {
            bool original = EditorGUIUtility.editingTextField;
            bool editingOnReturn = false, otherEditing = true;
            var size = new Vector2(200, 100);
            using var first = new OffscreenGui(() =>
            {
                if (Event.current.type == EventType.Repaint)
                {
                    editingOnReturn = EditorGUIUtility.editingTextField;
                    EditorGUIUtility.editingTextField = true;
                }
            });
            using var second = new OffscreenGui(() => otherEditing = EditorGUIUtility.editingTextField);
            first.Paint(size);
            Assert.That(EditorGUIUtility.editingTextField, Is.EqualTo(original));
            second.Paint(size);
            Assert.That(otherEditing, Is.False);
            first.Paint(size);
            Assert.That(editingOnReturn, Is.True);
            Assert.That(EditorGUIUtility.editingTextField, Is.EqualTo(original));
        }

        [Test]
        public void DetachedPointerCaptureStaysWithItsPanelAndReleasesOnMouseUp()
        {
            int original = GUIUtility.hotControl;
            int firstControl = 0, secondControl = -1;
            var size = new Vector2(200, 100);
            using var first = new OffscreenGui(() =>
            {
                int control = GUIUtility.GetControlID(FocusType.Passive);
                if (Event.current.type == EventType.MouseDown)
                    GUIUtility.hotControl = control;
                firstControl = GUIUtility.hotControl;
            });
            using var second = new OffscreenGui(() => secondControl = GUIUtility.hotControl);
            first.SendEvent(new Event { type = EventType.MouseDown }, size);
            Assert.That(firstControl, Is.Not.Zero);
            Assert.That(GUIUtility.hotControl, Is.EqualTo(original));
            second.Paint(size);
            Assert.That(secondControl, Is.Zero);
            first.Paint(size);
            Assert.That(firstControl, Is.Not.Zero);
            first.SendEvent(new Event { type = EventType.MouseUp }, size);
            first.Paint(size);
            Assert.That(firstControl, Is.Zero);
            Assert.That(GUIUtility.hotControl, Is.EqualTo(original));
        }

        private sealed class KeyboardFocusProbe
        {
            private readonly ResourceBrowserWindow _window;
            private readonly bool _drawList;
            private string _text = "Input";
            public bool FocusList, InputFocused, ListFocused;
            public bool ExitedGui, ScrollClosedAtExit, ListFocusedAtExit;

            public KeyboardFocusProbe(ResourceBrowserWindow window, bool drawList = false)
            {
                _window = window;
                _drawList = drawList;
            }

            public void Draw()
            {
                int listControl = GUIUtility.GetControlID(FocusType.Keyboard);
                if (FocusList)
                {
                    GUIUtility.keyboardControl = listControl;
                    FocusList = false;
                }
                Vector2 origin = GUIUtility.GUIToScreenPoint(Vector2.zero);
                try
                {
                    typeof(ResourceBrowserWindow).GetMethod("HandleListKeys", Private).Invoke(_window, new object[] { listControl });
                    if (_drawList)
                        typeof(ResourceBrowserWindow).GetMethod("DrawList", Private).Invoke(_window, new object[] { new Rect(0, 60, 240, 180), listControl });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ExitGUIException)
                {
                    ExitedGui = true;
                    ScrollClosedAtExit = GUIUtility.GUIToScreenPoint(Vector2.zero) == origin;
                    ListFocusedAtExit = GUIUtility.keyboardControl == listControl;
                    throw ex.InnerException;
                }
                if (!_drawList)
                {
                    GUI.SetNextControlName("TestInput");
                    _text = EditorGUILayout.TextField(_text);
                    InputFocused = GUI.GetNameOfFocusedControl() == "TestInput";
                }
                ListFocused = GUIUtility.keyboardControl == listControl;
            }
        }

        [Test]
        [Category("OffscreenGraphics")]
        public void MouseAndKeyboardSelectionExitTheCurrentGuiPass()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserQA/test-output", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(output);
            var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
            try
            {
                var probe = new KeyboardFocusProbe(window, drawList: true);
                using var gui = new OffscreenGui(probe.Draw);
                Vector2 canvasSize = new(320, 280);
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
                foreach (bool mouse in new[] { true, false })
                {
                    typeof(ResourceBrowserWindow).GetMethod("Select", Private).Invoke(window, new object[] { binary });
                    probe.FocusList = true;
                    gui.Paint(canvasSize);
                    probe.ExitedGui = false;
                    var click = new Vector2(20, 108);
                    try
                    {
                        gui.SendEvent(mouse
                            ? new Event { type = EventType.MouseDown, button = 0, mousePosition = click }
                            : new Event { type = EventType.KeyDown, keyCode = KeyCode.DownArrow }, canvasSize);
                    }
                    finally
                    {
                        if (mouse)
                            gui.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = click }, canvasSize);
                    }
                    Assert.That(probe.ExitedGui, Is.True, mouse ? "Mouse selection" : "Keyboard selection");
                    Assert.That(probe.ScrollClosedAtExit, Is.True, "The scroll view must close before selection exits OnGUI.");
                    Assert.That(probe.ListFocusedAtExit, Is.True, "Selection must preserve list keyboard focus.");
                    Assert.That(typeof(ResourceBrowserWindow).GetField("_selected", Private).GetValue(window), Is.SameAs(text), mouse ? "Mouse selection" : "Keyboard selection");
                    var draft = (ResourceTextDraft)typeof(ResourceBrowserWindow).GetField("_textDraft", Private).GetValue(window);
                    Assert.That(draft.Text, Is.EqualTo("Selected text"));
                    gui.Paint(canvasSize);
                }
            }
            finally
            {
                window.DiscardChanges();
                Object.DestroyImmediate(window);
                Directory.Delete(output, true);
            }
        }

        [Test]
        [Category("OffscreenGraphics")]
        public void ListNavigationDoesNotConsumeArrowsAfterKeyboardFocusMovesToAnInput()
        {
            var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
            try
            {
                var probe = new KeyboardFocusProbe(window);
                using var gui = new OffscreenGui(probe.Draw);
                Vector2 canvasSize = new(320, 280);
                typeof(ResourceBrowserWindow).GetMethod("CancelScan", Private).Invoke(window, null);
                var entries = (List<ResourceEntry>)typeof(ResourceBrowserWindow).GetField("_visible", Private).GetValue(window);
                entries.Add(new ResourceEntry("first.txt"));
                entries.Add(new ResourceEntry("second.txt"));
                FieldInfo selection = typeof(ResourceBrowserWindow).GetField("_selected", Private);
                selection.SetValue(window, entries[0]);
                probe.FocusList = true;
                gui.Paint(canvasSize);
                Assert.That(probe.ListFocused, Is.True);
                gui.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab, character = '\t' }, canvasSize);
                gui.Paint(canvasSize);
                Assert.That(probe.InputFocused, Is.True, "Tab must transfer the list's keyboard focus to the input.");
                gui.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.DownArrow }, canvasSize);
                Assert.That(selection.GetValue(window), Is.SameAs(entries[0]));
            }
            finally
            {
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

        [Test]
        public void DestroyingTheDiscardedWindowCancelsExportsAndReleasesItsDraft()
        {
            var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
            window.name = WindowName;
            try
            {
                var export = new CancellationTokenSource();
                CancellationToken exportToken = export.Token;
                typeof(ResourceBrowserWindow).GetField("_exportCancellation", Private).SetValue(window, export);
                var draft = ScriptableObject.CreateInstance<ResourceTextDraft>();
                draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                draft.SourceRoot = EditorPrefs.GetString(ResourceBrowserWindow.Variant + ".ResourceBrowser.Path");
                draft.SourcePath = "close-test.txt";
                draft.Load(System.Text.Encoding.UTF8.GetBytes("original"), 65001);
                draft.Text = "edited text";
                typeof(ResourceBrowserWindow).GetField("_textDraft", Private).SetValue(window, draft);
                typeof(ResourceBrowserWindow).GetMethod("UpdateTextDirty", Private).Invoke(window, null);
                Assert.That(window.hasUnsavedChanges, Is.True);
                window.DiscardChanges();
                Assert.That(window.hasUnsavedChanges, Is.False);
                Assert.That(draft.Text, Is.EqualTo("original"));
                Object.DestroyImmediate(window);
                Assert.That(window == null, Is.True);
                Assert.That(exportToken.IsCancellationRequested, Is.True, "OnDisable must cancel pending exports.");
                Assert.That(draft == null, Is.True);
            }
            finally
            {
                if (window != null)
                {
                    window.DiscardChanges();
                    Object.DestroyImmediate(window);
                }
            }
        }

        [UnityTest]
        public IEnumerator UnsavedTextSurvivesRealAssemblyReloadAndDiscardsCleanly()
        {
            if (!SessionState.GetBool(WindowName, false))
            {
                var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
                window.name = WindowName;
                // Keep this state-persistence test detached from desktop input.
                var draft = ScriptableObject.CreateInstance<ResourceTextDraft>();
                draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                draft.SourceRoot = EditorPrefs.GetString(ResourceBrowserWindow.Variant + ".ResourceBrowser.Path");
                draft.SourcePath = "reload-test.txt";
                draft.Load(System.Text.Encoding.UTF8.GetBytes("original"), 65001);
                draft.Text = "unsaved text after reload";
                using var serialized = new SerializedObject(window);
                serialized.FindProperty("_textDraft").objectReferenceValue = draft;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                typeof(ResourceBrowserWindow).GetMethod("UpdateTextDirty", Private).Invoke(window, null);
                SessionState.SetString(WindowName + ".WindowId", window.GetEntityId().ToString());
                SessionState.SetString(WindowName + ".DraftId", draft.GetEntityId().ToString());
                SessionState.SetBool(WindowName, true);
                EditorUtility.RequestScriptReload();
            }
            yield return new WaitForDomainReload();
            ResourceBrowserWindow restored = Resources.FindObjectsOfTypeAll<ResourceBrowserWindow>().Single(w => w.name == WindowName);
            using var restoredObject = new SerializedObject(restored);
            var restoredDraft = (ResourceTextDraft)restoredObject.FindProperty("_textDraft").objectReferenceValue;
            Assert.That(restoredDraft, Is.Not.Null);
            Assert.That(restored.GetEntityId().ToString(), Is.EqualTo(SessionState.GetString(WindowName + ".WindowId", "")), "The original native window must survive, not be recreated.");
            Assert.That(restoredDraft.GetEntityId().ToString(), Is.EqualTo(SessionState.GetString(WindowName + ".DraftId", "")), "The original native draft must survive, not be reconstructed from the source file.");
            Assert.That(typeof(EditorWindow).GetField("m_Parent", Private).GetValue(restored), Is.Null, "Persistence must not attach a desktop GUI host.");
            Assert.That(typeof(ResourceBrowserWindow).GetField("_scan", Private).GetValue(restored), Is.Null, "No background index of user data should be started.");
            Assert.That(restoredDraft.Text, Is.EqualTo("unsaved text after reload"));
            Assert.That(restoredDraft.IsDirty, Is.True);
            Assert.That(restored.hasUnsavedChanges, Is.True);
            var selected = (ResourceEntry)typeof(ResourceBrowserWindow).GetField("_selected", Private).GetValue(restored);
            Assert.That(selected.Path, Is.EqualTo(restoredDraft.SourcePath));
            restored.DiscardChanges();
            Assert.That(restored.hasUnsavedChanges, Is.False);
            // EditorWindow.Close requires a GUI parent in this Unity version.
            Object.DestroyImmediate(restored);
            Assert.That(restored == null, Is.True);
            Assert.That(restoredDraft == null, Is.True);
            // TearDown owns recovery even if the framework aborts at the yield.
        }
    }
}
