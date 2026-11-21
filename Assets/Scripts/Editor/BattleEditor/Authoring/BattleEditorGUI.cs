// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using Core.Contract.Enums;
    using ResourceBrowser;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleEditorWindow
    {
        private static readonly string[] AttributeLabels = { "精上限", "气上限", "神上限", "攻击", "防御", "速度", "幸运", "水", "火", "风", "雷", "土" };
        private readonly ResourceObjectView _dataView = new();
        private int _dataTable, _dataIndex, _equipmentSlot;
        private string _dataSearch = "";

        private void CreateRandomExample()
        {
            if (!CanReplace())
                return;
            BattleScenario example = _session.CreateRandomExample();
            // Construct the replacement before changing the draft or its asset ownership.
            _session.Start(example);
            Undo.ClearUndo(_draft);
            _asset = null;
            _initializeTestInventory = false;
            _savedJson = BattleScenarioJson.Serialize(new BattleScenario { Variant = BattleData.Variant });
            Edit("Create random battle example", () => _draft.Scenario = example);
            _previewJson = _draftJson;
            _selected = example.Participants.FirstOrDefault()?.InstanceId;
            _drawer = WorkspaceDrawer.None;
        }

        private void SyncHudBattle()
        {
            _session.SyncBattle();
            Repaint();
        }

        private void SelectHudActor(string id)
        {
            _selected = id;
            _action = null;
            Repaint();
        }

        private void DrawRoster()
        {
            GUILayout.Label("参战者", EditorStyles.boldLabel);
            _rosterScroll = EditorGUILayout.BeginScrollView(_rosterScroll, GUILayout.Height(Mathf.Min(230, 62 + Scenario.Participants.Count * 34)));
            try
            {
                foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
                {
                    GUILayout.Label(side == BattleSide.Ally ? "我方" : "敌方", EditorStyles.boldLabel);
                    foreach (BattleParticipant actor in Scenario.Participants.ToArray())
                    {
                        if (actor.Side != side)
                            continue;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool selected = actor.InstanceId == _selected;
                            if (GUILayout.Toggle(selected, ActorLabel(actor), Theme.Roster) != selected)
                            {
                                _selected = actor.InstanceId;
                                _configurationPage = 1;
                                _action = null;
                            }
                            GUILayout.Space(4);
                            if (DrawerButton("×", GUILayout.Width(24)))
                                Edit("Remove participant", () => Scenario.Participants.Remove(actor));
                        }
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
            _addParticipant = Section("add-participant", "添加角色／怪物");
            if (!_addParticipant)
                return;
            _addSide = (BattleSide)EditorGUILayout.EnumPopup("加入阵营", _addSide);
            _search = EditorGUILayout.TextField("搜索 ID／名称", _search);
            if (_session.Data == null)
            {
                GUILayout.Label("请先加载原版数据。");
                return;
            }
            int[] candidates = RosterCandidates(_session.Data, _addSide, _search, Scenario.Participants);
            _addActor = Mathf.Clamp(_addActor, 0, Math.Max(0, candidates.Length - 1));
            _addActor = EditorGUILayout.Popup(_addActor, candidates.Select(id => $"{id} · {_session.Data.Database.CombatActorInfos[id].Name}").ToArray());
            using (new EditorGUI.DisabledScope(candidates.Length == 0))
                if (DrawerButton("添加到" + (_addSide == BattleSide.Ally ? "我方" : "敌方")))
                    Try(() =>
                    {
                        if (Scenario.Participants.Count(actor => actor.Side == _addSide) >= (_addSide == BattleSide.Ally ? 4 : 6))
                            throw new InvalidOperationException("当前阵营已达原版参战人数上限。");
                        BattleSlot slot = Enum.GetValues(typeof(BattleSlot)).Cast<BattleSlot>().First(value => !Scenario.Participants.Any(actor => actor.Side == _addSide && actor.Slot == value));
                        BattleParticipant actor = _session.Data.CreateTestParticipant(candidates[_addActor], _addSide, slot);
                        Edit("Add participant", () => Scenario.Participants.Add(actor));
                        _selected = actor.InstanceId;
                        _configurationPage = 1;
                    });
        }

        private static int[] RosterCandidates(BattleData data, BattleSide side, string search, IEnumerable<BattleParticipant> roster)
        {
            var allyIdentities = new HashSet<int>(roster.Where(actor => actor?.Side == BattleSide.Ally)
                .Select(actor => BattleData.PlayerIdentity(actor.ActorId)));
            return data.Database.CombatActorInfos.OrderBy(pair => pair.Key)
                .Where(pair => pair.Key > 0 && data.CombatRosterDenial(pair.Key, side) == null &&
                    (side == BattleSide.Enemy || !allyIdentities.Contains(BattleData.PlayerIdentity(pair.Key))) &&
                    ($"{pair.Key} {pair.Value.Name} {pair.Value.ModelId}").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(pair => pair.Key).ToArray();
        }

        private string ActorLabel(BattleParticipant actor)
        {
            string name = _session.Data?.Database.CombatActorInfos.TryGetValue(actor.ActorId, out var info) == true ? info.Name : actor.ActorId.ToString();
            return $"{name} ({actor.ActorId}) · {actor.Slot}";
        }

        private void DrawDataCatalog()
        {
            if (_session.Data == null)
            {
                GUILayout.Label("请先加载原版数据。");
                return;
            }
            int table = DrawerTabs("tabs:catalog", _dataTable, new[] { "技能", "物品", "合击", "状态" });
            if (table != _dataTable)
            {
                _dataTable = table;
                _dataIndex = 0;
                _dataSearch = "";
                _dataView.Reset();
            }
            var database = _session.Data.Database;
            (int id, string name, object record)[] records = _dataTable switch
            {
                0 => database.SkillInfos.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value.Name, (object)pair.Value)).ToArray(),
                1 => database.GameItemInfos.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value.Name, (object)pair.Value)).ToArray(),
                2 => database.ComboSkillInfos.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value.Name, (object)pair.Value)).ToArray(),
                _ => Enum.GetValues(typeof(ActorCombatStateType)).Cast<ActorCombatStateType>().Select(state => ((int)state, state.ToString(), (object)state)).ToArray(),
            };
            _dataSearch = EditorGUILayout.TextField("搜索 ID／名称", _dataSearch);
            records = records.Where(record => $"{record.id} {record.name}".IndexOf(_dataSearch, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (records.Length == 0)
                return;
            int previous = _dataIndex;
            _dataIndex = EditorGUILayout.Popup(Mathf.Clamp(_dataIndex, 0, records.Length - 1), records.Select(record => $"{record.id} · {record.name}").ToArray());
            if (previous != _dataIndex)
                _dataView.Reset();
            var selected = records[_dataIndex];
            BattleParticipant actor = Scenario.Participants.FirstOrDefault(participant => participant.InstanceId == _selected);
            using (new EditorGUI.DisabledScope((_dataTable == 0 || _dataTable == 3) && actor == null))
            {
                if (_dataTable == 0 && selected.id > 0 && DrawerButton("加入选中角色的初始技能"))
                    Edit("Add initial skill", () =>
                    {
                        if (actor.Skills.All(skill => skill.Id != selected.id))
                            actor.Skills.Add(new BattleSkill { Id = selected.id, Mastered = true });
                    });
                if (_dataTable == 3 && BattleStatusRules.SupportsInitialState((BattleState)selected.id) && DrawerButton("加入选中角色的初始状态"))
                    Edit("Add initial status", () =>
                    {
                        if (actor.Statuses.All(state => state.Id != selected.id))
                            actor.Statuses.Add(new BattleStatus { Id = selected.id });
                    });
            }
            if (_dataTable == 1 && selected.id > 0)
            {
                if (DrawerButton("放入本局物品（+1）"))
                    Edit("Add battle item", () =>
                    {
                        BattleItem item = Scenario.Items.FirstOrDefault(entry => entry.Id == selected.id);
                        if (item == null)
                            Scenario.Items.Add(new BattleItem { Id = selected.id });
                        else if (item.Count < int.MaxValue)
                            item.Count++;
                    });
                _equipmentSlot = EditorGUILayout.Popup("装备槽位", _equipmentSlot, new[] { "武器", "帽子", "衣服", "鞋子", "饰品一", "饰品二" });
                using (new EditorGUI.DisabledScope(actor == null))
                    if (DrawerButton("装备到选中角色（开局校验类型与适用者）"))
                        Edit("Set battle equipment", () =>
                        {
                            if (actor.Equipment.Length != 6)
                                actor.Equipment = new int[6];
                            actor.Equipment[_equipmentSlot] = selected.id;
                            if (_equipmentSlot == 0)
                                actor.MagicSword = selected.id == BattleMagicSwordRules.WeaponId ? actor.MagicSword ?? new BattleMagicSword() : null;
                        });
            }
            _dataView.Draw(selected.record, DrawerWidth - 30);
        }

        private void DrawScenario(BattleScenario scenario)
        {
            GUILayout.Label("配置作品：" + Scenario.Variant, EditorStyles.boldLabel);
            if (_session.Data != null)
            {
                int index = Array.FindIndex(_session.Data.Scenes, name => name.Equals(Scenario.Scene, StringComparison.OrdinalIgnoreCase));
                int selected = EditorGUILayout.Popup("战斗场景", index, _session.Data.Scenes);
                if (selected >= 0 && selected != index)
                {
                    Edit("Set battle scene", () =>
                    {
                        scenario.Scene = _session.Data.Scenes[selected];
                        scenario.SceneElement = (int)_session.Data.SceneMap.CombatSceneElementTypeInfo.GetValueOrDefault(scenario.Scene);
                    });
                }
            }
            else
                Field(scenario, "Scene", "战斗场景");
            ElementType element = (ElementType)EditorGUILayout.EnumPopup("场景五灵（阵位关系）", (ElementType)scenario.SceneElement);
            if ((int)element != scenario.SceneElement)
                Edit("Set scene element", () => scenario.SceneElement = (int)element);
#if PAL3A
            ElementType fieldElement = (ElementType)EditorGUILayout.EnumPopup("初始五灵法阵", (ElementType)scenario.FiveSpiritElement);
            if ((int)fieldElement != scenario.FiveSpiritElement)
                Edit("Set five spirit element", () => scenario.FiveSpiritElement = (int)fieldElement);
            int force = EditorGUILayout.Popup("法阵所属", scenario.FiveSpiritForce, new[] { "无", "我方", "敌方" });
            if (force != scenario.FiveSpiritForce)
                Edit("Set five spirit force", () => scenario.FiveSpiritForce = force);
#endif

            BattleParticipant[] allies = scenario.Participants.Where(actor => actor.Side == BattleSide.Ally).ToArray();
            string[] ignoredChoices = new[] { "无" }.Concat(allies.Select(ActorLabel)).ToArray();
            int ignoredBefore = Array.FindIndex(allies, actor => actor.InstanceId == scenario.EnemyAiIgnoredActor) + 1;
            if (ignoredBefore == 0 && !string.IsNullOrEmpty(scenario.EnemyAiIgnoredActor))
                ignoredBefore = -1;
            int ignoredNext = EditorGUILayout.Popup("敌方 AI 忽略目标", ignoredBefore, ignoredChoices);
            if (ignoredNext >= 0 && ignoredNext != ignoredBefore)
                Edit("Set ignored AI actor", () => scenario.EnemyAiIgnoredActor = ignoredNext == 0 ? "" : allies[ignoredNext - 1].InstanceId);
            Field(scenario, "Encounter", "遭遇类型");
            if (Section("rules", "高级战斗规则"))
            {
                Field(scenario, "Seed", "随机种子");
                Field(scenario, "RoundLimit", "Boss 回合限制");
                GUILayout.Label("-1 不限；0 立即判定。", EditorStyles.wordWrappedMiniLabel);
                Field(scenario, "ScriptBattle", "剧情战斗");
                Field(scenario, "MustFail", "必败战斗");
                Field(scenario, "GameOverOnDefeat", "败北结束游戏");

#if PAL3A
                BattlePal3AState pal3A = scenario.Pal3A;
                if (pal3A?.SpiritWheel?.Charges is { Length: 5 } charges)
                {
                    string[] spiritNames = { "水", "火", "风", "雷", "土" };
                    for (int elementIndex = 0; elementIndex < 5; elementIndex++)
                    {
                        int index = elementIndex;
                        int charge = EditorGUILayout.IntSlider("五灵轮 · " + spiritNames[index], charges[index], 0, 9);
                        if (charge != charges[index])
                            Edit("Set spirit wheel charge", () => charges[index] = charge);
                    }
                    Field(pal3A, "SpiritVisualSeed", "摄灵表现随机种子");
                    Field(pal3A, "NangongStartsTransformed", "南宫煌开局炎兽形态");
                    Field(pal3A, "PengxuStartsTransformed", "王蓬絮开局仙兽形态");
                    Field(pal3A, "PreserveLiaoRiLosses", "继承燎日累计损失");
                    if (pal3A.PreserveLiaoRiLosses)
                    {
                        Field(pal3A, "HpLoss", "累计精请求损失");
                        Field(pal3A, "MpLoss", "累计神请求损失");
                    }
                }
                else
                    Field(scenario, "Pal3A", "问情篇初始状态");
#endif
            }
            if (!Section("presentation", "音乐、渲染与相机"))
                return;
            GUILayout.Label("音乐留空按场景自动选择；NONE 为静音，也可填写原始曲号。", EditorStyles.wordWrappedMiniLabel);
            Field(scenario, "Music", "战斗音乐");
            int renderMode = EditorGUILayout.Popup("光影模式", scenario.ToonRendering ? 1 : 0, new[] { "原版光影", "实时光照 + Toon" });
            if ((renderMode == 1) != scenario.ToonRendering)
                Edit("Set battle render mode", () => scenario.ToonRendering = renderMode == 1);
            GUILayout.Label(scenario.ToonRendering ? "Unity 实时光照与 Toon 材质。" : "原始烘焙光影、顶点色与角色光照；关闭 Unity 实时灯。", EditorStyles.wordWrappedMiniLabel);
            Field(scenario, "MotionBlur", "原版镜头拖影");
            Field(scenario, "Camera", "相机覆盖");

            if (_session.Preview != null && DrawerButton("将当前视角保存到配置"))
            {
                _session.Preview.View.GetCamera(out Vector3 position, out Vector3 target, out float fov);
                Edit("Capture preview camera", () => Scenario.Camera = new BattleCamera { Override = true, Position = Point(position), Target = Point(target), FieldOfView = fov });
            }
        }

        private void DrawParticipant(BattleParticipant actor)
        {
            GUILayout.Label(ActorLabel(actor), EditorStyles.boldLabel);
            GUILayout.Label("实例：" + actor.InstanceId, EditorStyles.wordWrappedMiniLabel);
            BattleSlot slot = (BattleSlot)EditorGUILayout.EnumPopup("阵位（占用时交换）", actor.Slot);
            if (slot != actor.Slot)
                Edit("Swap battle slots", () =>
                {
                    BattleParticipant other = Scenario.Participants.FirstOrDefault(candidate => candidate != actor && candidate.Side == actor.Side && candidate.Slot == slot);
                    if (other != null)
                        other.Slot = actor.Slot;
                    actor.Slot = slot;
                });

            Field(actor, "Level", "等级");
            if (Section("actor-details", "成长与显示位置"))
            {
                Field(actor, "Model", "模型覆盖");
                Field(actor, "Experience", "当前经验");
                Field(actor, "Love", "好感度");
                GUILayout.Label(actor.AttributeSource switch
                {
                    BattleAttributeSource.ExplicitEffective => "显式有效属性：下面的数值直接作为本局最终属性，装备不重复加成。",
                    BattleAttributeSource.BaseWithEquipment => "基础属性加装备：下面提供裸装基础数值，开局按原版计算装备加成。",
                    _ => "原版等级属性：按角色和等级从已核实的原版成长数据生成属性；无法核实的角色会明确拒绝。"
                }, EditorStyles.wordWrappedMiniLabel);
                Field(actor, "OverridePosition", "覆盖显示位置");
                if (actor.OverridePosition)
                    Field(actor, "Position", "位置（Unity 单位）");
                else if (_session.Data != null)
                    EditorGUILayout.LabelField("原始位置", _session.Data.Position(actor, Scenario.SceneElement).ToString("F2"));
            }
            if (Section("attributes", "初始属性与精气神"))
            {
                Field(actor, "AttributeSource", "属性来源");
                using (new EditorGUI.DisabledScope(actor.AttributeSource == BattleAttributeSource.LevelGrowth))
                    for (int i = 0; i < (actor.Attributes?.Length ?? 0) && i < AttributeLabels.Length; i++)
                    {
                        int index = i;
                        int value = EditorGUILayout.IntField(AttributeLabels[index], actor.Attributes[index]);
                        if (value != actor.Attributes[index])
                            Edit("Set initial attribute", () => actor.Attributes[index] = value);
                    }
                for (int i = 0; i < (actor.Vitals?.Length ?? 0) && i < 3; i++)
                {
                    int index = i;
                    int value = EditorGUILayout.IntField(new[] { "当前精", "当前气", "当前神" }[index], actor.Vitals[index]);
                    if (value != actor.Vitals[index])
                        Edit("Set initial vitals", () => actor.Vitals[index] = value);
                }
            }
            if (Section("equipment", "装备与魔剑"))
            {
                Field(actor, "Equipment", "装备 ID");
                if (actor.Equipment is { Length: > 0 } && actor.Equipment[0] == BattleMagicSwordRules.WeaponId)
                {
                    if (actor.MagicSword == null)
                    {
                        if (DrawerButton("建立魔剑培养快照（发布默认值）"))
                            Edit("Capture magic sword defaults", () => actor.MagicSword = new BattleMagicSword());
                    }
                    else
                        Field(actor, "MagicSword", "魔剑培养与初始能量");
                }
                else if (actor.MagicSword != null && DrawerButton("移除未装备的魔剑快照"))
                    Edit("Remove unequipped magic sword snapshot", () => actor.MagicSword = null);
            }
            if (Section("skills", $"初始技能（{actor.Skills.Count}）"))
                Field(actor, "Skills", "技能列表");
            if (Section("statuses", $"初始状态（{actor.Statuses.Count}）"))
                Field(actor, "Statuses", "状态列表");
            GUILayout.Label("掌握必须显式设置；技能、状态与装备按本作原始数据及已验证的规则校验，不可用原因显示在实战指令和诊断中。", EditorStyles.wordWrappedMiniLabel);
        }

        private void Field(object owner, string name, string label)
        {
            BattleScenarioTypedFields.Field(owner, name, label, Edit, Theme.Button);
            GUILayout.Space(2);
        }

        private static BattlePoint Point(Vector3 value) => new() { X = value.x, Y = value.y, Z = value.z };

        private void DrawCanvas(Rect available, bool newBattleKey)
        {
            BattlePreview preview = _session.Preview;
            EditorGUI.DrawRect(available, new Color(0.12f, 0.12f, 0.12f));
            if (preview == null)
            {
                DrawWelcome(available);
                return;
            }
            preview.View.ExpandReferenceFraming = _expandCamera;
            preview.DamageNumberColorMode = _damageNumberColor;
            Rect rect = _originalAspect ? BattleHudLayout.ContentRect(available) : available;
            // The game supports landscape and square canvases. A tall tool
            // window or an open drawer must not create a portrait battle HUD.
            if (rect.height > rect.width)
                rect = new Rect(rect.x, rect.center.y - rect.width / 2, rect.width, rect.width);
            preview.View.SetViewportContext(rect);
            EditorGUI.DrawRect(available, Color.black);
            if (Event.current.type == EventType.MouseMove && rect.Contains(Event.current.mousePosition))
                Repaint();
            Event inputEvent = Event.current;
            BattleSession battle = _session.Battle;
            bool coAttackBlocksHud = battle?.IsCoAttackPresentationBlockingInput == true;
            bool ownsKeyboard = GUIUtility.keyboardControl == 0 && !EditorGUIUtility.editingTextField;
            // Unity sends a separate character event for Tab. Let the key-code event
            // drive the HUD and consume the character before IMGUI assigns focus.
            bool battleOwnsTab = !DraftPending && ownsKeyboard && (battle?.Status == BattleSessionStatus.Running ||
                _session.ResultPresentation is { Completed: false });
            if (battleOwnsTab && inputEvent.type == EventType.KeyDown &&
                inputEvent.keyCode == KeyCode.None && inputEvent.character is '\t' or '\x19')
                inputEvent.Use();
            bool canReadKey = BattleHudView.CanReadBattleKey(inputEvent);
            bool clickedCanvas = inputEvent.type == EventType.MouseDown && rect.Contains(inputEvent.mousePosition);
            if (!DraftPending && _session.ResultPresentation is { Completed: false } resultInput && (canReadKey || clickedCanvas))
            {
                if (clickedCanvas)
                {
                    BattleHudView.TakeGuiKeyboardFocus();
                    if (inputEvent.button == 1 && resultInput.StatisticsVisible)
                        resultInput.CloseStatistics();
                }
                _session.SkipResultStep();
                inputEvent.Use();
            }
            else if (!DraftPending && battle != null && (canReadKey || coAttackBlocksHud && clickedCanvas))
            {
                if (coAttackBlocksHud && clickedCanvas)
                    BattleHudView.TakeGuiKeyboardFocus();
                bool skipped = battle.IsModalPresentation
                    ? canReadKey && inputEvent.keyCode == KeyCode.Escape && battle.SkipModalPresentation()
                    : coAttackBlocksHud && (clickedCanvas || newBattleKey) && battle.SkipCoAttackPresentation();
                if (skipped)
                {
                    _session.SyncBattle();
                    Repaint();
                }
                if (battle.IsModalPresentation || skipped || coAttackBlocksHud)
                    inputEvent.Use();
            }
            if (!DraftPending && battle?.Status == BattleSessionStatus.Running && canReadKey && !newBattleKey && BattleHudView.KeyboardIntent(inputEvent.keyCode) is { } repeated &&
                repeated.Kind is BattleHudIntentKind.Confirm or BattleHudIntentKind.Back && inputEvent.type == EventType.KeyDown)
                inputEvent.Use();
            void DrawHud()
            {
                using var input = new EditorGUI.DisabledScope(DraftPending || coAttackBlocksHud);
                _hud.Draw(_session.Battle, _session.Data, preview, rect,
                SyncHudBattle, SelectHudActor, _hudSafeInsets, Repaint, new BattleHudInteractionProfile(_touchHud));
            }
            if (_session.Battle != null && Event.current.type != EventType.Repaint)
                DrawHud();
            if (battleOwnsTab &&
                inputEvent.keyCode == KeyCode.Tab && BattleHudView.CanReadBattleKey(inputEvent))
                inputEvent.Use();
            if (_session.Battle == null && Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                string picked = preview.InstanceAt(Event.current.mousePosition, rect);
                if (picked != null)
                    SelectHudActor(picked);
            }
            if (_session.Battle != null)
                _hud.PreparePointers(_session.Battle, _session.Data, preview);
            if (_session.Battle != null && BattleHudView.CanReadBattleKey(Event.current) &&
                Event.current.keyCode is KeyCode.F or KeyCode.R && rect.Contains(Event.current.mousePosition))
                Event.current.Use();
            // Native list/scrollbar controls get first refusal before the
            // battle canvas accepts any remaining pointer gesture.
            if (_session.Battle != null)
                _hud.CompleteGuiInput(Event.current, rect);
            preview.View.Draw(rect, allowCameraInput: _session.Battle == null);
            if (_session.Battle?.IsPreparingPresentation == true)
            {
                GUI.Label(rect, "正在准备战斗影片…", Theme.WelcomeText);
                return;
            }
            if (battle == null || _debugBattle && !battle.IsModalPresentation)
                preview.DrawLabels(rect, _selected);
            if (_session.Battle != null)
                preview.DrawBattleOverlay(_session.Battle, rect, _hudSafeInsets, BattleHudView.ElementWheelVisible(_session.Battle, _hud.Presenter), _hud.NavigationFocusedControl);
            if (_session.Battle != null)
                preview.DrawBattleNumbers(_session.Battle, rect, _hudSafeInsets);
            if (_session.Battle != null && Event.current.type == EventType.Repaint)
                DrawHud();
            if (_session.Battle != null)
                preview.DrawBattleMessages(_session.Battle, rect, _hudSafeInsets);
            if (_session.Battle != null)
                preview.DrawSwordSparks(rect, _hudSafeInsets);
            if (_session.Battle?.CoAttackScreenOpacity > 0)
            {
                uint value = _session.Battle.CoAttackScreenColor;
                Color color = new((value >> 16 & 255) / 255f, (value >> 8 & 255) / 255f,
                    (value & 255) / 255f, _session.Battle.CoAttackScreenOpacity);
                EditorGUI.DrawRect(rect, color);
            }
            if (_session.ResultPresentation != null)
                preview.DrawResultOverlay(_session.ResultPresentation, rect, _hudSafeInsets);
        }

        private void DrawActions(BattlePreview preview)
        {
            BattleScenario captured = _session.CapturedScenario;
            BattleParticipant actor = captured.Participants.FirstOrDefault(candidate => candidate.InstanceId == _selected);
            if (actor == null)
            {
                GUILayout.Label("选择参战者以预览动作。", EditorStyles.miniLabel);
                return;
            }
            string[] actions = _session.Data.Actions(actor).Select(action => action.ActionName).ToArray();
            int index = Array.IndexOf(actions, _action);
            if (index < 0)
                index = Math.Max(0, Array.IndexOf(actions, _session.Data.IdleAction(actor)));
            using (new EditorGUILayout.HorizontalScope())
            {
                index = EditorGUILayout.Popup("动作", index, actions, GUILayout.Height(BattleEditorTheme.ControlHeight));
                _action = actions[index];
                GUILayout.Space(6);
                if (DrawerButton("演示动作", GUILayout.Width(80)))
                    Try(() =>
                    {
                        preview.PlayAction(actor.InstanceId, _action);
                        _session.IsPlaying = true;
                    });
            }
            BattleParticipant[] targets = captured.Participants.Where(candidate => candidate.Side != actor.Side).ToArray();
            int target = Math.Max(0, Array.FindIndex(targets, candidate => candidate.InstanceId == _target));
            using (new EditorGUILayout.HorizontalScope())
            {
                target = EditorGUILayout.Popup("攻击目标", target, targets.Select(ActorLabel).ToArray(), GUILayout.Height(BattleEditorTheme.ControlHeight));
                if (targets.Length > 0)
                    _target = targets[target].InstanceId;
                GUILayout.Space(6);
                using (new EditorGUI.DisabledScope(targets.Length == 0))
                    if (DrawerButton("普攻演示", GUILayout.Width(80)))
                        Try(() =>
                        {
                            preview.PlayAttack(actor.InstanceId, _target);
                            _session.IsPlaying = true;
                        });
            }
            GUILayout.Label("动画标记（仅显示，不触发结算）：" + string.Join(" · ", preview.AnimationMarkers(actor.InstanceId)), EditorStyles.wordWrappedMiniLabel);
        }
    }
}
