// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleHudPolishTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private BattleEditorSession _session;
        private Canvas _canvas;
        private int _magicSkill;

        private sealed class Canvas : IDisposable
        {
            public static readonly Vector2 Size = new(980, 760);
            private readonly OffscreenGui _gui;

            public readonly BattleEditorSession Editor;
            public readonly BattleHudView Hud = new();
            public readonly Rect Area = new(30, 40, 800, 600);
            public bool Enabled = true;
            public bool Touch;
            public int HotControl;
            public Vector2? Pointer;
            public int Repaints;
            public string Tooltip;

            public Canvas(BattleEditorSession editor)
            {
                Editor = editor;
                _gui = new OffscreenGui(Draw);
            }

            public RenderTexture RenderTarget => _gui.RenderTarget;

            public void Paint()
            {
                int before = Repaints;
                _gui.Paint(Size);
                Assert.That(Repaints, Is.GreaterThan(before));
            }

            public void SendEvent(Event input) => _gui.SendEvent(input, Size);

            public void Dispose() => _gui.Dispose();

            private void Draw()
            {
                if (Editor?.Battle == null)
                    return;
                if (Pointer is { } pointer)
                    Event.current.mousePosition = pointer;
                EditorGUI.DrawRect(Area, new Color(.28f, .28f, .28f));
                using (new EditorGUI.DisabledScope(!Enabled))
                    Hud.Draw(Editor.Battle, Editor.Data, Editor.Preview, Area, () => Editor.SyncBattle(), _ => { },
                        interaction: new BattleHudInteractionProfile(Touch));
                HotControl = GUIUtility.hotControl;
                if (Event.current.type == EventType.Repaint)
                {
                    Tooltip = GUI.tooltip;
                    Repaints++;
                }
            }
        }

        [SetUp]
        public void SetUp()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            Assert.That(Directory.Exists(root), Is.True);
            _session = new BattleEditorSession();
            _session.Load(root, 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = _session.Data.Scenes.First(),
                DataFingerprint = _session.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush,
                Music = "NONE"
            };
            BattleParticipant ally = _session.Data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water);
            ally.Vitals[2] = 0;
            _magicSkill = ally.Skills.First(skill =>
            {
                var info = _session.Data.Database.SkillInfos[skill.Id];
                return (int)info.Type is >= 1 and <= 3 && info.ElementAttributes.Count == 1 && info.MpConsumeValue > 0;
            }).Id;
            ally.Skills.Clear();
            ally.Skills.Add(new BattleSkill { Id = _magicSkill, Level = 4, Mastered = true });
            scenario.Participants.Add(ally);
            int enemy = _session.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(_session.Data, id));
            scenario.Participants.Add(_session.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            _session.StartBattle(scenario);
            _session.Preview.Audio.Muted = _session.Preview.Effects.Muted = true;
            _canvas = new Canvas(_session);
            _canvas.Hud.Presenter.Synchronize(_session.Battle);
        }

        [TearDown]
        public void TearDown()
        {
            _canvas?.Dispose();
            _session?.Dispose();
        }

        private T Field<T>(string name) => (T)typeof(BattleHudView).GetField(name, Members).GetValue(_canvas.Hud);

        [Test]
        public void EmptyAndDisabledDescriptionsStayInTheBottomStripWithReadableHoverText()
        {
            BattleHudPresenter presenter = _canvas.Hud.Presenter;
            presenter.Open(BattleHudPage.Magic);
            int category = -1;
            for (int index = 0; index < 6; index++)
            {
                presenter.SetCategory(index);
                if (presenter.Abilities().Any(row => row.Definition.Id == _magicSkill))
                {
                    category = index;
                    break;
                }
            }
            Assert.That(category, Is.GreaterThanOrEqualTo(0));
            presenter.SetCategory((category + 1) % 6);
            _canvas.Paint();
            Assert.That(presenter.Abilities(), Is.Empty);
            Assert.That(Field<string>("_descriptionText"), Is.EqualTo("此分类没有可用条目"));
            Rect strip = Field<Rect>("_descriptionRect");
            Assert.That(strip.yMin, Is.EqualTo(BattleData.Variant == "PAL3A" ? 460 : 454));
            Assert.That(strip.height, Is.EqualTo(BattleData.Variant == "PAL3A" ? 16 : 20));
            Object.DestroyImmediate(Capture("empty-category"));
            presenter.SetCategory(category);
            _canvas.Paint();
            BattleHudAbility row = presenter.Abilities().Single();
            Assert.That(row.Denied, Does.StartWith("Resource 2:"));
            Rect panel = BattleHudText.AbilityPanelBounds(BattleHudPage.Magic, BattleData.Variant == "PAL3A");
            Rect list = BattleHudText.ListBounds(BattleHudPage.Magic, BattleData.Variant == "PAL3A");
            Rect ink = new Rect(_canvas.Area.position + panel.position + list.position + new Vector2(5, 3), new Vector2(80, 13));
            _canvas.Pointer = Vector2.zero;
            _canvas.Paint();
            Texture2D normal = Capture("disabled-normal");
            int normalInk;
            try
            {
                normalInk = DarkPixels(normal, ink);
            }
            finally { Object.DestroyImmediate(normal); }
            Assert.That(normalInk, Is.GreaterThan(20));
            Vector2 point = _canvas.Area.position + panel.position + list.position + new Vector2(30, 9);
            _canvas.Pointer = point;
            _canvas.SendEvent(new Event { type = EventType.MouseMove, mousePosition = point });
            _canvas.Paint();
            Assert.That(Field<string>("_descriptionText"), Does.StartWith("神不足"));
            Assert.That(Field<string>("_descriptionText"), Does.Not.Contain("Resource"));
            Assert.That(Field<string>("_descriptionTooltip"), Does.Contain("需要").And.Contain("当前"));
            Assert.That(_canvas.Tooltip, Does.Contain("需要").And.Contain("当前"), "The full denial must be available while hovering the disabled row, without moving to the description strip.");
            Assert.That(BattleHudText.DescriptionFontSize, Is.EqualTo(14));
            Texture2D capture = Capture("disabled-hover");
            try
            {
                // Exclude the native grid lines and name/cost separator.
                Assert.That(DarkPixels(capture, ink), Is.GreaterThan(normalInk * .7f), "Hover text must remain dark on the pale native list, rather than switching to white.");
            }
            finally { Object.DestroyImmediate(capture); }
            _canvas.Pointer = _canvas.Area.position + panel.position + list.position + new Vector2(115, 9);
            _canvas.Paint();
            Assert.That(_canvas.Tooltip, Does.Contain("需要").And.Contain("当前"), "The denial tooltip must cover the cost column as well as the skill name.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OutsideTapRetiresSecondaryAndDisabledGestureTails()
        {
            var hud = _canvas.Hud;
            Vector2 outside = _canvas.Area.position + new Vector2(620, 170);
            hud.Presenter.Open(BattleHudPage.Magic);
            _canvas.Paint();
            hud.SendPointer(BattleHudIntentKind.PointerDown, 1, outside);
            hud.SendPointer(BattleHudIntentKind.PointerDown, 2, outside);
            Assert.That(hud.SendPointer(BattleHudIntentKind.PointerUp, 1, outside), Is.True);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(hud.SendPointer(BattleHudIntentKind.PointerMove, 2, outside + Vector2.right * 40), Is.True, "A swallowed second touch must not leak a camera drag after the first touch closes the menu.");
            Assert.That(hud.SendPointer(BattleHudIntentKind.PointerUp, 2, outside), Is.True);
            hud.Presenter.Open(BattleHudPage.Magic);
            _canvas.Paint();
            hud.SendPointer(BattleHudIntentKind.PointerDown, 3, outside);
            _canvas.Enabled = false;
            _canvas.Paint();
            _canvas.Enabled = true;
            _canvas.Paint();
            Assert.That(hud.SendPointer(BattleHudIntentKind.PointerUp, 3, outside), Is.True);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic), "A gesture invalidated during disabled input cannot dismiss a resumed menu.");
            hud.SendPointer(BattleHudIntentKind.PointerDown, 3, outside);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 3, outside);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands), "A fresh gesture may reuse an old pointer ID.");
            hud.Presenter.Open(BattleHudPage.Magic);
            _canvas.Paint();
            hud.SendPointer(BattleHudIntentKind.PointerDown, 4, outside);
            hud.SendPointer(BattleHudIntentKind.PointerMove, 4, outside + Vector2.right * 40);
            // The old release happened outside the host. A fresh Down must
            // reset the drag state even when focus and the pointer ID survive.
            hud.SendPointer(BattleHudIntentKind.PointerDown, 4, outside);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 4, outside);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TouchListLeavesTheNativeScrollbarDragAndReleaseWithItsOwner()
        {
            BattleScenario scenario = _session.CapturedScenario;
            scenario.Items = _session.Data.Database.GameItemInfos.Values.Where(item => (int)item.Type == 1)
                .Take(20).Select(item => new BattleItem { Id = checked((int)item.Id), Count = 1 }).ToList();
            Assert.That(scenario.Items.Count, Is.GreaterThan(8));
            _session.StartBattle(scenario);
            _canvas.Touch = true;
            _canvas.Hud.Presenter.Synchronize(_session.Battle);
            _canvas.Hud.Presenter.Open(BattleHudPage.Items);
            _canvas.Paint();
            Assert.That(Field<float>("_touchScrollLimit"), Is.GreaterThan(0));
            Rect list = Field<Rect>("_touchPanel");
            Vector2 point = new(list.xMax - 1, list.center.y);
            _canvas.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            int scrollbar = _canvas.HotControl;
            Assert.That(scrollbar, Is.Not.Zero, "The real GUI scrollbar must capture this press before testing its release.");
            Assert.That(Field<int>("_guiCapturedControl"), Is.Zero, "Scrollbar input belongs to Unity's native control.");
            _canvas.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point + Vector2.down * 24 });
            Assert.That(_canvas.HotControl, Is.EqualTo(scrollbar));
            _canvas.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point + Vector2.down * 24 });
            Assert.That(Field<int?>("_touchCandidatePointer"), Is.Null);
            Assert.That(_canvas.HotControl, Is.Zero, "Touch panning must not swallow the native scrollbar's release.");
            Assert.That(_canvas.Hud.Presenter.CapturedPointerId, Is.Null);
            Assert.That(_canvas.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Items));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RightEdgeOfNativeMagicScrollbarIsInsideTheMenu()
        {
            _canvas.Hud.Presenter.Open(BattleHudPage.Magic);
            _canvas.Paint();
            // PAL3A's real list ends at native X=263, four pixels beyond its
            // panel sprite. This point must not be treated as a background tap.
            float x = BattleData.Variant == "PAL3A" ? 261 : 248;
            float y = BattleData.Variant == "PAL3A" ? 356 : 374;
            Vector2 point = _canvas.Area.position + new Vector2(x, y);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 17, point);
            Assert.That(Field<int?>("_outsidePointer"), Is.Null);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerUp, 17, point);
            Assert.That(_canvas.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic));
            Object.DestroyImmediate(Capture("scrollbar-edge"));
            _canvas.Hud.Presenter.Open(BattleHudPage.Commands);
            Assert.That(_canvas.Hud.Presenter.SetFocus("magic"), Is.True);
            _canvas.Pointer = Vector2.zero;
            _canvas.Paint();
            Texture2D image = Capture("native-focus");
            try
            {
                Vector2 scale = new(image.width / Canvas.Size.x, image.height / Canvas.Size.y);
                int gold = 0;
                for (int edge = 0; edge < 4; edge++)
                    for (int offset = 0; offset < 46; offset++)
                    {
                        Vector2 local = new(12 + (edge < 2 ? offset : edge == 2 ? 0 : 45), 378 + (edge < 2 ? edge * 45 : offset));
                        Vector2 pixel = Vector2.Scale(_canvas.Area.position + local, scale);
                        Color color = image.GetPixel(Mathf.FloorToInt(pixel.x), image.height - 1 - Mathf.FloorToInt(pixel.y));
                        if (color.r > .7f && color.g > .55f && color.b < .47f)
                            gold++;
                    }
                Assert.That(gold, Is.LessThan(48), "Native focus may change the circular sprite, but must not add a gold rectangular outline around it.");
            }
            finally { Object.DestroyImmediate(image); }
            LogAssert.NoUnexpectedReceived();
        }

        private static int DarkPixels(Texture2D image, Rect gui)
        {
            Vector2 scale = new(image.width / Canvas.Size.x, image.height / Canvas.Size.y);
            int count = 0;
            for (int y = Mathf.CeilToInt(gui.yMin * scale.y); y < gui.yMax * scale.y; y++)
                for (int x = Mathf.CeilToInt(gui.xMin * scale.x); x < gui.xMax * scale.x; x++)
                {
                    Color pixel = image.GetPixel(x, image.height - 1 - y);
                    if (pixel.a > .8f && pixel.r < .55f && pixel.g < .55f && pixel.b < .55f)
                        count++;
                }
            return count;
        }

        private Texture2D Capture(string name)
        {
            _canvas.Paint();
            RenderTexture buffer = _canvas.RenderTarget;
            Assert.That(buffer, Is.Not.Null);
            var image = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                image.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                image.Apply();
                string root = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (!string.IsNullOrEmpty(root))
                {
                    string folder = Path.Combine(root, "HudPolish");
                    Directory.CreateDirectory(folder);
                    File.WriteAllBytes(Path.Combine(folder, BattleData.Variant + "-" + name + ".png"), image.EncodeToPNG());
                }
                return image;
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
