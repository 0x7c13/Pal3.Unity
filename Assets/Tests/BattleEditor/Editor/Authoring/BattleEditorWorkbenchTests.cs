// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    public sealed class BattleEditorWorkbenchTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private BattleEditorWindow _window;
        private OffscreenGui _gui;
        private float _pixelsPerPointDuringDraw;
        private bool _hadPath;
        private string _path;
        private bool _hadCodepage;
        private int _codepage;

        private string Preference => BattleData.Variant + ".BattleEditor.Path";
        private BattleEditorSession Session => Get<BattleEditorSession>("_session");
        private BattleScenario Draft => Get<BattleScenarioAsset>("_draft").Scenario;

        private T Get<T>(string field) => (T)typeof(BattleEditorWindow).GetField(field, Members).GetValue(_window);

        private void Set(string field, object value) => typeof(BattleEditorWindow).GetField(field, Members).SetValue(_window, value);

        private object Call(string method, params object[] values) => typeof(BattleEditorWindow).GetMethod(method, Members).Invoke(_window, values);

        [SetUp]
        public void SetUp()
        {
            _hadPath = EditorPrefs.HasKey(Preference);
            _path = EditorPrefs.GetString(Preference);
            _hadCodepage = EditorPrefs.HasKey(BattleData.Variant + ".BattleEditor.Codepage");
            _codepage = EditorPrefs.GetInt(BattleData.Variant + ".BattleEditor.Codepage");
            EditorPrefs.SetString(Preference, Path.Combine(Application.temporaryCachePath, "missing-battle-ux-" + Guid.NewGuid().ToString("N")));
            _window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            _window.position = new Rect(0, 0, 1200, 800);
            var draw = (Action)typeof(BattleEditorWindow).GetMethod("DrawWindow", Members).CreateDelegate(typeof(Action), _window);
            _gui = new OffscreenGui(() =>
            {
                // The host display and detached IMGUI panel can have different pixel scales.
                if (Event.current.type == EventType.Repaint)
                    _pixelsPerPointDuringDraw = EditorGUIUtility.pixelsPerPoint;
                draw();
            });
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _gui?.Dispose();
                if (_window != null)
                {
                    _window.DiscardChanges();
                    Object.DestroyImmediate(_window);
                }
            }
            finally
            {
                if (_hadPath)
                    EditorPrefs.SetString(Preference, _path);
                else
                    EditorPrefs.DeleteKey(Preference);
                if (_hadCodepage)
                    EditorPrefs.SetInt(BattleData.Variant + ".BattleEditor.Codepage", _codepage);
                else
                    EditorPrefs.DeleteKey(BattleData.Variant + ".BattleEditor.Codepage");
            }
        }

        private void Paint()
        {
            _gui.Paint(_window.position.size);
            var controls = Get<Dictionary<string, Rect>>("_workspaceControls");
            Assert.That(controls, Contains.Key("primary"));
            Rect primary = controls["primary"];
            Assert.That(Get<Rect>("_workspaceCanvasRect").yMax, Is.EqualTo(_window.position.height - 8), "The removed status bar must not reserve space below the canvas.");
            foreach (string id in new[] { "stop", "speed", "configuration", "diagnostics" })
                if (controls.TryGetValue(id, out Rect control))
                {
                    Assert.That(control.yMin, Is.EqualTo(primary.yMin), id + " baseline");
                    Assert.That(control.height, Is.EqualTo(primary.height), id + " height");
                }
            foreach (var pair in controls.Where(pair => pair.Key.StartsWith("tabs:", StringComparison.Ordinal)))
                Assert.That(pair.Value.height, Is.EqualTo(primary.height), pair.Key + " height");
            Assert.That(Get<Rect>("_workspaceCanvasRect").yMin - primary.yMax, Is.InRange(3, 5), "Keep only a small gap below the transport buttons.");
            if (!Get<Dictionary<string, bool>>("_sections").GetValueOrDefault("rules") &&
                controls.TryGetValue("section:rules", out Rect rules) && controls.TryGetValue("section:presentation", out Rect presentation))
            {
                Assert.That(presentation.yMin - rules.yMax, Is.InRange(8, 14), "Collapsed groups need visible separation.");
                Assert.That(presentation.xMin, Is.EqualTo(rules.xMin));
                Assert.That(presentation.width, Is.EqualTo(rules.width));
            }
        }

        private void Click(string id) => Click(Get<Dictionary<string, Rect>>("_workspaceControls")[id].center);

        private void Click(Vector2 point)
        {
            SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });
        }

        private void SendEvent(Event input) => _gui.SendEvent(input, _window.position.size);

        private void SetEnum(string field, string name)
        {
            FieldInfo info = typeof(BattleEditorWindow).GetField(field, Members);
            info.SetValue(_window, Enum.Parse(info.FieldType, name));
        }

        private void LoadData()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            Assert.That(Directory.Exists(root), Is.True, "Local game data is required.");
            Set("_root", root);
            Call("LoadData");
        }

        [Test]
        public void FirstUseButtonsStartPauseAndApplyAnEditedBattle()
        {
            Paint();
            Assert.That(Call("PrimaryLabel"), Is.EqualTo("选择数据目录"));
            Assert.That(Session.Data, Is.Null);
            Capture("first-use");
            LoadData();
            Paint();
            Assert.That(Call("PrimaryLabel"), Is.EqualTo("创建随机战斗"));
            Click("welcome-primary");
            Paint();
            Assert.That(Session.Preview, Is.Not.Null);
            Assert.That(Session.Battle, Is.Null);
            Assert.That(Session.IsPlaying, Is.False);
            Assert.That(Call("PrimaryLabel"), Is.EqualTo("开始测试"));
            Call("Edit", "Silent test", (Action)(() =>
            {
                Draft.Music = "NONE";
                Draft.Encounter = BattleEncounter.AllyAmbush;
            }));
            Set("_autoEnemies", false);
            Session.Speed = .4f;
            Click("primary");
            Assert.That(Session.Battle, Is.Not.Null);
            Assert.That(Session.Speed, Is.EqualTo(.4f));
            Assert.That(Session.IsPlaying, Is.True);
            Session.Preview.Audio.Muted = Session.Preview.Effects.Muted = true;
            Paint();
            Capture("running");
            for (int step = 0; step < 3000 && Session.Battle.WaitingActor == null; step++)
                Session.Battle.Step();
            Assert.That(Session.Battle.WaitingActor, Is.Not.Null);
            Session.SyncBattle();
            Call("OnFocus");
            Paint();
            Vector2 point = Get<Rect>("_workspaceCanvasRect").position + new Vector2(32, 32);
            Click(point);
            int inputs = Session.Battle.Inputs.Count;
            SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.F, mousePosition = point });
            Assert.That(Session.Battle.Inputs.Count, Is.GreaterThan(inputs), "The real workbench HUD keyboard route must record a battle command.");
            Capture("hud-command-submitted");
            Click("primary");
            Assert.That(Session.IsPlaying, Is.False);
            BattleSession original = Session.Battle;
            Call("Edit", "Edit seed", (Action)(() => Draft.Seed++));
            Paint();
            Assert.That(Call("PrimaryLabel"), Is.EqualTo("应用并重新开始"));
            Click("configuration");
            Paint();
            Capture("pending-configuration");
            Click("primary");
            Assert.That(Session.Battle, Is.Not.SameAs(original));
            Assert.That(Session.IsPlaying, Is.True);
            Session.Preview.Audio.Muted = Session.Preview.Effects.Muted = true;
            Assert.That(Session.CapturedScenario.Seed, Is.EqualTo(Draft.Seed));
            Session.Battle.Abort();
            Session.SyncBattle();
            Paint();
            Assert.That(Call("PrimaryLabel"), Is.EqualTo("重新测试"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ResultKeyboardInputFollowsFocusWhenThePointerIsOutsideTheCanvas()
        {
            LoadData();
            Call("Edit", "Prepare a completed battle", (Action)(() =>
            {
                BattleData data = Session.Data;
                Draft.Scene = data.Scenes.First();
                Draft.Music = "NONE";
                Draft.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
                int enemyId = data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id));
                BattleParticipant enemy = data.CreateParticipant(enemyId, BattleSide.Enemy, BattleSlot.Water);
                enemy.Vitals[0] = 0;
                Draft.Participants.Add(enemy);
            }));
            Call("StartBattle");
            Session.Preview.Audio.Muted = Session.Preview.Effects.Muted = true;
            Session.StepBattle();
            BattleResultPresentation result = Session.ResultPresentation;
            Assert.That(result, Is.Not.Null);
            for (int step = 0; step < 300 && !result.CanSkip; step++)
                Session.StepBattle();
            Assert.That(result.CanSkip, Is.True);
            int index = result.Index;

            SetEnum("_drawer", "Configuration");
            Paint();
            Click("tab:configuration:0");
            Rect canvas = Get<Rect>("_workspaceCanvasRect");
            Vector2 toolbar = Get<Dictionary<string, Rect>>("_workspaceControls")["primary"].center;
            Assert.That(canvas.Contains(toolbar), Is.False);
            SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return, mousePosition = toolbar });
            Assert.That(result.Index, Is.EqualTo(index), "The sidebar still owns the keyboard.");

            // Release the sidebar's keyboard focus through the canvas input path.
            Click(canvas.center);
            for (int step = 0; step < 300 && !result.CanSkip; step++)
                Session.StepBattle();
            Assert.That(result.CanSkip, Is.True);
            index = result.Index;
            SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return, mousePosition = toolbar });
            Assert.That(result.Index, Is.EqualTo(index + 1), "Moving the pointer away leaves the result keyboard route active.");
        }

        [Test]
        public void EmptyStateIsCenteredWithEveryDrawerAndPagesRenderWithoutErrors()
        {
            foreach (Vector2 size in new[] { new Vector2(960, 640), new Vector2(1600, 800), new Vector2(1000, 1000) })
            {
                _window.position = new Rect(0, 0, size.x, size.y);
                foreach (string drawer in new[] { "None", "Configuration", "Diagnostics", "Connection" })
                {
                    SetEnum("_drawer", drawer);
                    Paint();
                    Rect canvas = Get<Rect>("_workspaceCanvasRect"), welcome = Get<Rect>("_welcomeRect");
                    Assert.That(Vector2.Distance(canvas.center, welcome.center), Is.LessThan(.1f));
                    Assert.That(welcome.xMin, Is.GreaterThanOrEqualTo(canvas.xMin));
                    Assert.That(welcome.xMax, Is.LessThanOrEqualTo(canvas.xMax));
                    Assert.That(canvas.width, Is.GreaterThan(500));
                    Capture($"empty-{(int)size.x}x{(int)size.y}-{drawer}");
                }
            }
            LoadData();
            Call("CreateRandomExample");
            SetEnum("_drawer", "Configuration");
            Paint();
            Click("tab:configuration:0");
            Vector2 tabPoint = Get<Dictionary<string, Rect>>("_workspaceControls")["tab:configuration:0"].center;
            SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow, mousePosition = tabPoint });
            Assert.That(Get<int>("_configurationPage"), Is.EqualTo(1), "Clicking a native tab must also focus its keyboard navigation.");
            Paint();
            SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.LeftArrow, mousePosition = tabPoint });
            Assert.That(Get<int>("_configurationPage"), Is.Zero);
            Paint();
            for (int page = 0; page < 3; page++)
            {
                Click("tab:configuration:" + page);
                Paint();
                Assert.That(Get<int>("_configurationPage"), Is.EqualTo(page));
                Capture("configuration-" + page);
                if (page == 0)
                {
                    var sections = Get<Dictionary<string, bool>>("_sections");
                    sections["rules"] = sections["presentation"] = true;
                    Paint();
                    Capture("configuration-expanded");
                    sections["rules"] = sections["presentation"] = false;
                }
            }
            SetEnum("_drawer", "Diagnostics");
            Paint();
            for (int page = 0; page < 3; page++)
            {
                Click("tab:diagnostics:" + page);
                Paint();
                Assert.That(Get<int>("_diagnosticPage"), Is.EqualTo(page));
                Capture("diagnostics-" + page);
                if (page == 0)
                {
                    Get<Dictionary<string, bool>>("_sections")["display"] = true;
                    Paint();
                    Capture("display-settings");
                    Get<Dictionary<string, bool>>("_sections")["display"] = false;
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RandomNewDraftDetachesSavedAssetAndFailedConnectionKeepsActualData()
        {
            LoadData();
            Call("CreateRandomExample");
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            try
            {
                asset.Scenario = Draft.Copy();
                string saved = BattleScenarioJson.Serialize(asset.Scenario);
                Set("_asset", asset);
                Call("UpdateDirty");
                Assert.That(_window.hasUnsavedChanges, Is.False);
                Call("CreateRandomExample");
                Assert.That(Get<BattleScenarioAsset>("_asset"), Is.Null);
                Assert.That(BattleScenarioJson.Serialize(asset.Scenario), Is.EqualTo(saved));
                Assert.That(_window.hasUnsavedChanges, Is.True);
                BattleData connected = Session.Data;
                Set("_root", Path.Combine(Application.temporaryCachePath, "missing-" + Guid.NewGuid().ToString("N")));
                Assert.Throws<TargetInvocationException>(() => Call("LoadData"));
                Assert.That(Session.Data, Is.SameAs(connected));
                SetEnum("_drawer", "Connection");
                Paint();
                Capture("failed-reconnection");
                Set("_savedJson", BattleScenarioJson.Serialize(Draft));
                Call("UpdateDirty");
                Call("StartBattle");
                Session.IsPlaying = true;
                Session.Preview.Audio.Muted = Session.Preview.Effects.Muted = true;
                Undo.IncrementCurrentGroup();
                Call("Edit", "Seed change", (Action)(() => Draft.Seed++));
                Assert.That(Session.IsPlaying, Is.False);
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(Session.IsPlaying, Is.False);
                Assert.That(Draft.Seed, Is.EqualTo(Session.CapturedScenario.Seed));
                Paint();
                Assert.That((bool)typeof(BattleEditorWindow).GetProperty("DraftPending", Members).GetValue(_window), Is.False);
            }
            finally { Object.DestroyImmediate(asset); }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RenderModesAndRetinaDrawUseTheExpectedLightsAndPixelResolution()
        {
            LoadData();
            Call("CreateRandomExample");
            Call("Edit", "Original lighting fixture", (Action)(() => Draft.ToonRendering = false));
            Call("StartPreview");
            Session.Preview.Trail.Suspended = true;
            Paint();
            Rect canvas = Get<Rect>("_workspaceCanvasRect");
            float height = Math.Min(canvas.height, canvas.width);
            RenderTexture target = (RenderTexture)typeof(ResourceBrowser.ResourceModelPreview).GetField("_target", Members).GetValue(Session.Preview.View);
            Assert.That(_pixelsPerPointDuringDraw, Is.Positive);
            Assert.That(target.width, Is.EqualTo(Mathf.CeilToInt(canvas.width * _pixelsPerPointDuringDraw)));
            Assert.That(target.height, Is.EqualTo(Mathf.CeilToInt(height * _pixelsPerPointDuringDraw)));
            Assert.That(Session.Preview.View.NativeLighting, Is.Not.Null);
            Light[] Lights() => Session.Preview.View.Root.scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Light>()).ToArray();
            Assert.That(Lights(), Is.Empty, "Original rendering must not create a Unity realtime light.");
            Capture("retina-original");
            Call("Edit", "Toon fixture", (Action)(() =>
            {
                Draft.ToonRendering = true;
                Draft.Music = "NONE";
            }));
            Paint();
            Call("StartPreview");
            Session.IsPlaying = false;
            Session.Preview.Audio.Muted = Session.Preview.Effects.Muted = true;
            Session.Preview.Trail.Suspended = true;
            Paint();
            Assert.That(Session.Preview.View.NativeLighting, Is.Null);
            Assert.That(Lights().Length, Is.EqualTo(1));
            Assert.That(Lights()[0].type, Is.EqualTo(LightType.Directional));
            Assert.That(Lights()[0].enabled, Is.True);
            Capture("retina-toon");
            Assert.That(Session.Preview.View.Render(96, 64).width, Is.EqualTo(96));
            Assert.That(Session.Preview.View.Render(96, 64).height, Is.EqualTo(64));
            Session.Preview.Trail.Suspended = false;
            Session.Preview.Trail.Enabled = true;
            Paint();
            target = (RenderTexture)typeof(ResourceBrowser.ResourceModelPreview).GetField("_target", Members).GetValue(Session.Preview.View);
            Assert.That(target.width, Is.EqualTo(256));
            Assert.That(target.height, Is.EqualTo(256));
            Capture("native-trail");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PresetSaveReopenAndStopPreserveTheConfiguredBattle()
        {
            LoadData();
            Call("CreateRandomExample");
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            string path = "Assets/__BattleVisualFlow_" + Guid.NewGuid().ToString("N") + ".asset";
            try
            {
                asset.Scenario = Draft.Copy();
                AssetDatabase.CreateAsset(asset, path);
                Set("_asset", asset);
                Call("Edit", "Configure fixture", (Action)(() =>
                {
                    Draft.Music = "NONE";
                    Draft.Money = 1234;
                }));
                _window.SaveChanges();
                Assert.That(_window.hasUnsavedChanges, Is.False);
                Assert.That(AssetDatabase.LoadAssetAtPath<BattleScenarioAsset>(path).Scenario.Money, Is.EqualTo(1234));
                Call("NewScenario");
                _window.DiscardChanges();
                Call("LoadAsset", asset);
                Assert.That(Draft.Money, Is.EqualTo(1234));
                Paint();
                Click("primary");
                Session.Preview.Audio.Muted = Session.Preview.Effects.Muted = true;
                Assert.That(Session.IsPlaying, Is.True);
                Paint();
                Capture("saved-preset-running");
                Click("stop");
                Assert.That(Session.Preview, Is.Null);
                Assert.That(Session.Battle, Is.Null);
                Assert.That(Draft.Money, Is.EqualTo(1234));
                Assert.That(_window.hasUnsavedChanges, Is.False);
            }
            finally
            {
                _window.DiscardChanges();
                Set("_asset", null);
                AssetDatabase.DeleteAsset(path);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CompletedResultOffersRestartAfterThePresentationFinishes()
        {
            LoadData();
            Call("CreateRandomExample");
            Call("Edit", "Result fixture", (Action)(() =>
            {
                Draft.Music = "NONE";
                foreach (BattleParticipant actor in Draft.Participants)
                    if (actor.Side == BattleSide.Enemy)
                        actor.Vitals[0] = 0;
            }));
            Call("StartBattle");
            Session.Preview.Audio.Muted = Session.Preview.Effects.Muted = true;
            for (int step = 0; step < 100 && Session.Battle.Status == BattleSessionStatus.Running; step++)
                Session.StepBattle();
            Session.SyncBattle();
            Assert.That(Session.Battle.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(Session.ResultPresentation, Is.Not.Null);
            Session.IsPlaying = false;
            Paint();
            Assert.That(Call("PrimaryLabel"), Is.EqualTo("继续结果"));
            Click("primary");
            Assert.That(Session.IsPlaying, Is.True);
            for (int second = 0; second < 120 && !Session.ResultPresentation.Completed; second++)
            {
                Session.SkipResultStep();
                Session.Tick(1f);
            }
            Assert.That(Session.ResultPresentation.Completed, Is.True);
            Paint();
            Assert.That(Call("PrimaryLabel"), Is.EqualTo("重新测试"));
            Capture("completed-result");
            BattleSession previous = Session.Battle;
            Click("primary");
            Assert.That(Session.Battle, Is.Not.SameAs(previous), "The final result action must rebuild a test instead of attempting to resume an already finished presentation.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MenuPickerCallbackIsSafeAndErrorBadgeOpensTheLog()
        {
            Paint();
            // The file menu invokes the picker callback after the original event.
            int? pickerControl = null;
            Set("_showAssetPicker", (Action<BattleScenarioAsset, int>)((asset, id) => pickerControl = id));
            Assert.DoesNotThrow(() => Call("OpenAssetPicker"));
            Assert.That(pickerControl, Is.EqualTo(Get<int>("_assetPickerId")));
            Set("_error", "A deliberately long load error for the diagnostic link.");
            Set("_diagnosticPage", 0);
            Paint();
            Click("diagnostics");
            Assert.That(Get<int>("_diagnosticPage"), Is.EqualTo(2));
            Paint();
            Capture("error-log");
            LogAssert.NoUnexpectedReceived();
        }

        private void AssertRenderedTransport(Texture2D image)
        {
            Vector2 size = _window.position.size;
            Vector2 scale = new(image.width / size.x, image.height / size.y);
            Color32[] pixels = image.GetPixels32();
            Color32 Pixel(int x, int y) => pixels[(image.height - 1 - y) * image.width + x];
            var controls = Get<Dictionary<string, Rect>>("_workspaceControls");
            Rect primary = controls["primary"];
            foreach (string id in new[] { "file", "connection", "primary", "stop", "speed", "configuration", "diagnostics" })
            {
                if (!controls.TryGetValue(id, out Rect rect))
                    continue;
                Assert.That(rect.y, Is.EqualTo(primary.y), id + " baseline");
                Assert.That(rect.height, Is.EqualTo(primary.height), id + " height");
                Color32 background = Pixel(Mathf.RoundToInt((rect.x - 2) * scale.x), Mathf.RoundToInt(rect.center.y * scale.y));
                int visible = 0;
                // Flat native toolbar controls intentionally draw only their
                // content until hovered. Check the entire content area rather
                // than demanding a raised button border in its left margin.
                for (int y = Mathf.CeilToInt(rect.y * scale.y); y < rect.yMax * scale.y; y++)
                    for (int x = Mathf.CeilToInt(rect.x * scale.x); x < rect.xMax * scale.x; x++)
                    {
                        Color32 pixel = Pixel(x, y);
                        if (Math.Max(Math.Abs(pixel.r - background.r), Math.Max(Math.Abs(pixel.g - background.g), Math.Abs(pixel.b - background.b))) > 32)
                            visible++;
                    }
                Assert.That(visible, Is.GreaterThan(12 * scale.x * scale.y), id + " must have visible rendered content.");
            }
        }

        private void Capture(string name)
        {
            string root = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
            if (string.IsNullOrEmpty(root))
                return;
            RenderTexture buffer = _gui.RenderTarget;
            Assert.That(buffer, Is.Not.Null);
            var image = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                image.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                image.Apply();
                if (name == "running" || name == "saved-preset-running")
                    AssertRenderedTransport(image);
                if (name.StartsWith("configuration-", StringComparison.Ordinal) || name.StartsWith("diagnostics-", StringComparison.Ordinal))
                {
                    Rect canvas = Get<Rect>("_workspaceCanvasRect");
                    Vector2 size = _window.position.size;
                    Vector2 scale = new(image.width / size.x, image.height / size.y);
                    int colored = 0;
                    Color32[] pixels = image.GetPixels32();
                    for (int y = Mathf.CeilToInt(canvas.yMin * scale.y); y < canvas.yMax * scale.y; y += 4)
                        for (int x = Mathf.CeilToInt(canvas.xMin * scale.x); x < canvas.xMax * scale.x; x += 4)
                        {
                            int row = image.height - 1 - y;
                            if (x < 0 || x >= image.width || row < 0 || row >= image.height)
                                continue;
                            Color32 color = pixels[row * image.width + x];
                            if (Math.Max(color.r, Math.Max(color.g, color.b)) > 50 &&
                                Math.Max(Math.Abs(color.r - color.g), Math.Abs(color.g - color.b)) > 12)
                                colored++;
                        }
                    Assert.That(colored, Is.GreaterThan(100), "Switching a drawer page must preserve the actual scene pixels in the canvas: " + name);
                }
                string folder = Path.Combine(root, "Workbench");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, BattleData.Variant + "-" + name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
