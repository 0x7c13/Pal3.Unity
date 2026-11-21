// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleEditorWindow
    {
        private const float DrawerWidth = 360;

        private enum WorkspaceDrawer
        {
            None, Configuration, Diagnostics, Connection
        }

        private enum WorkspaceAction
        {
            Connect, Random, Configure, Start, Pause, Resume, Restart, Apply
        }

        [SerializeField] private WorkspaceDrawer _drawer;
        [SerializeField] private int _configurationPage;
        [SerializeField] private int _diagnosticPage;
        [SerializeField] private BattleDamageNumberColorMode _damageNumberColor;
        private bool _addParticipant;
        private string[] _validation;
        private readonly Dictionary<string, bool> _sections = new();
        private readonly Dictionary<string, Rect> _workspaceControls = new();
        private Rect _workspaceCanvasRect;
        private Rect _welcomeRect;
        private Vector2 _workspaceGuiScreenOrigin;
        private int _assetPickerId;
        private Action<BattleScenarioAsset, int> _showAssetPicker = (asset, id) =>
            EditorGUIUtility.ShowObjectPicker<BattleScenarioAsset>(asset, false, "", id);

        private BattleEditorTheme _theme;

        private BattleEditorTheme Theme => _theme != null && _theme.ProSkin == EditorGUIUtility.isProSkin ? _theme : _theme = new BattleEditorTheme();

        private void RememberControl(string id, Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                _workspaceControls[id] = new Rect(GUIUtility.GUIToScreenPoint(rect.position) - _workspaceGuiScreenOrigin, rect.size);
        }

        private bool DraftPending => _session?.Preview != null && _previewJson != _draftJson;

        private void PauseForDraftChange()
        {
            if (_session != null)
            {
                _session.CancelReplay();
                _session.IsPlaying = false;
            }
        }

        private string[] Validation()
        {
            if (_validation == null)
            {
                if (BattleScenarioEditing.NeedsRepair(Scenario))
                    _validation = new[] { "配置缺少必需内容，请先修复。" };
                else
                    _validation = (_session.Data?.Validate(Scenario, forBattle: true) ?? Scenario.Validate()).ToArray();
            }
            return _validation;
        }

        private WorkspaceAction PrimaryAction()
        {
            if (_session.Data == null)
                return WorkspaceAction.Connect;
            if (BattleScenarioEditing.NeedsRepair(Scenario))
                return WorkspaceAction.Configure;
            if (Scenario.Participants.Count == 0 && string.IsNullOrEmpty(Scenario.Scene))
                return WorkspaceAction.Random;
            if (Validation().Length > 0)
                return WorkspaceAction.Configure;
            if (DraftPending)
                return WorkspaceAction.Apply;
            if (_session.Battle == null)
                return WorkspaceAction.Start;
            if (_session.ResultPresentation?.Completed == true ||
                _session.Battle.Status is BattleSessionStatus.Aborted or BattleSessionStatus.Faulted ||
                _session.Battle.Status == BattleSessionStatus.Completed && _session.ResultPresentation == null)
                return WorkspaceAction.Restart;
            return _session.IsPlaying ? WorkspaceAction.Pause : WorkspaceAction.Resume;
        }

        private string PrimaryLabel() => PrimaryAction() switch
        {
            WorkspaceAction.Connect => "选择数据目录",
            WorkspaceAction.Random => "创建随机战斗",
            WorkspaceAction.Configure => "检查配置",
            WorkspaceAction.Start => "开始测试",
            WorkspaceAction.Pause => "暂停",
            WorkspaceAction.Resume => _session.Battle.Status == BattleSessionStatus.Completed ? "继续结果" : "继续测试",
            WorkspaceAction.Restart => "重新测试",
            _ => "应用并重新开始",
        };

        private void RunPrimaryAction()
        {
            switch (PrimaryAction())
            {
                case WorkspaceAction.Connect:
                    ChooseDataFolder(true);
                    break;
                case WorkspaceAction.Random:
                    Try(CreateRandomExample);
                    break;
                case WorkspaceAction.Configure:
                    _drawer = WorkspaceDrawer.Configuration;
                    _inspectorScroll = Vector2.zero;
                    break;
                case WorkspaceAction.Pause:
                    _session.IsPlaying = false;
                    break;
                case WorkspaceAction.Resume:
                    _session.IsPlaying = true;
                    break;
                default:
                    Try(() =>
                    {
                        StartBattle();
                        _session.IsPlaying = true;
                    });
                    break;
            }
            Repaint();
        }

        private readonly HashSet<KeyCode> _heldBattleKeys = new();

        private void DrawWindow()
        {
            Event input = Event.current;
            if (input.rawType == EventType.KeyUp)
                _heldBattleKeys.Remove(input.keyCode);
            bool newBattleKey = BattleHudView.IsBattleKey(input) && _heldBattleKeys.Add(input.keyCode);
            if (Event.current.type == EventType.MouseDown)
                ReleaseGamepadInput();
            if (_draft == null || _session == null)
                return;
            if (Event.current.type == EventType.Repaint)
            {
                _workspaceControls.Clear();
                _workspaceGuiScreenOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
            }
            _assetPickerId = GUIUtility.GetControlID(0x42617474, FocusType.Passive);
            HandleAssetPicker();
            bool replaying = _session.IsReplaying;
            if (replaying && input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape)
            {
                _session.CancelReplay();
                input.Use();
            }
            using var disabled = new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || replaying);
            DrawWorkspaceToolbar();
            bool drawerVisible = _drawer != WorkspaceDrawer.None;
            float drawerLeft = position.width - DrawerWidth - 8;
            _workspaceCanvasRect = new Rect(8, BattleEditorTheme.CanvasTop, drawerVisible ? drawerLeft - 16 : position.width - 16, Mathf.Max(100, position.height - BattleEditorTheme.CanvasTop - 8));
            DrawCanvas(_workspaceCanvasRect, newBattleKey);
            if (DraftPending || replaying)
            {
                var banner = new Rect(_workspaceCanvasRect.x + 12, _workspaceCanvasRect.y + 8, _workspaceCanvasRect.width - 24, 30);
                EditorGUI.DrawRect(banner, new Color(0.12f, 0.12f, 0.12f, .96f));
                GUI.Label(banner, replaying ? "正在重放战斗记录…" : "配置已修改 · 画布保留上次测试，应用配置后继续操作",
                    BattleHudCanvas.LabelStyle(11, Color.white, alignment: TextAnchor.MiddleCenter));
                if (replaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    GUI.enabled = true;
                    try
                    {
                        if (GUI.Button(new Rect(banner.xMax - 72, banner.y + 3, 66, 24), "取消"))
                            _session.CancelReplay();
                    }
                    finally
                    {
                        GUI.enabled = false;
                    }
                }
            }
            if (drawerVisible)
                DrawDrawer(new Rect(drawerLeft, BattleEditorTheme.CanvasTop, DrawerWidth, _workspaceCanvasRect.height));
        }

        private bool WorkspaceButton(string id, Rect rect, string label, bool selected = false, bool primary = false, bool toolbar = false)
        {
            RememberControl(id, rect);
            Color previous = GUI.backgroundColor;
            try
            {
                if (primary)
                    GUI.backgroundColor = Theme.Accent;
                return GUI.Toggle(rect, selected, label, primary ? Theme.PrimaryButton : toolbar ? Theme.ToolbarButton : Theme.Button) != selected;
            }
            finally
            {
                GUI.backgroundColor = previous;
            }
        }

        private bool DrawerButton(string label, params GUILayoutOption[] options) => GUILayout.Button(label, Theme.Button, options);

        private int DrawerTabs(string id, int selected, string[] labels)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, Theme.Tabs,
                GUILayout.Height(BattleEditorTheme.ControlHeight), GUILayout.ExpandWidth(true));
            RememberControl(id, rect);
            int navigationId = GUIUtility.GetControlID(id.GetHashCode(), FocusType.Keyboard, rect);
            Event input = Event.current;
            if (GUI.enabled && input.type == EventType.MouseDown && input.button == 0 && rect.Contains(input.mousePosition))
                GUIUtility.keyboardControl = navigationId;
            // GUILayout.Toolbar resolves built-in Left/Mid/Right styles by
            // name, discarding the local height. Use those native textures
            // through our own style copies so their actual pixels stay 24pt.
            float[] widths = labels.Select(label => Theme.Tabs.CalcSize(new GUIContent(label)).x).ToArray();
            float total = widths.Sum();
            float extra = Math.Max(0, rect.width - total) / labels.Length;
            float x = rect.x;
            int next = selected;
            for (int index = 0; index < labels.Length; index++)
            {
                float width = total > rect.width ? widths[index] * rect.width / total : widths[index] + extra;
                GUIStyle style = index == 0 ? Theme.TabLeft : index == labels.Length - 1 ? Theme.TabRight : Theme.TabMiddle;
                float pixels = EditorGUIUtility.pixelsPerPoint;
                float left = Mathf.Round(x * pixels) / pixels;
                float right = Mathf.Round((x + width) * pixels) / pixels;
                var tabRect = new Rect(left, rect.y, right - left, rect.height);
                RememberControl(id.Replace("tabs:", "tab:") + ":" + index, tabRect);
                if (GUI.Toggle(tabRect, selected == index, labels[index], style) && selected != index)
                    next = index;
                x += width;
            }
            if (GUI.enabled && input.type == EventType.KeyDown && input.keyCode is KeyCode.LeftArrow or KeyCode.RightArrow &&
                GUIUtility.keyboardControl == navigationId && !EditorGUIUtility.editingTextField)
            {
                next = (selected + (input.keyCode == KeyCode.LeftArrow ? labels.Length - 1 : 1)) % labels.Length;
                GUI.changed = true;
                input.Use();
            }
            return next;
        }

        private void DrawWorkspaceToolbar()
        {
            const float y = 4, gap = 6, height = BattleEditorTheme.ControlHeight;
            EditorGUI.DrawRect(new Rect(0, 0, position.width, BattleEditorTheme.CanvasTop), Theme.Header);
            EditorGUI.DrawRect(new Rect(0, BattleEditorTheme.CanvasTop - 1, position.width, 1), Theme.Border);
            if (WorkspaceButton("file", new Rect(8, y, 50, height), "文件 ▾", toolbar: true))
                ShowFileMenu();
            string connection = _reconnect != null ? "正在连接… ▾" : _session.Data == null ? "连接数据 ▾" : "数据已连接 ▾";
            if (WorkspaceButton("connection", new Rect(64, y, 92, height), connection,
                _drawer == WorkspaceDrawer.Connection, toolbar: true))
                ToggleDrawer(WorkspaceDrawer.Connection);
            ToolbarSeparator(162, y, height);
            GUI.Label(new Rect(174, y, 42, height), BattleData.Variant, Theme.Title);

            // Keep the primary action at the right edge in every test state.
            // Only the document title absorbs changes in available width.
            float primaryX = position.width - 8 - 120;
            float runX = primaryX - (_session.Preview != null ? 44 + gap : 0) - (_session.Battle != null ? 52 + gap : 0);
            float toolsX = runX - 12 - 72 * 2 - gap;
            string name = (_asset != null ? _asset.name : "未命名战斗") + (CurrentDraftDirty ? " *" : "");
            GUI.Label(new Rect(222, y, Mathf.Max(0, toolsX - 234), height),
                new GUIContent(name, _asset != null ? AssetDatabase.GetAssetPath(_asset) : "使用文件菜单保存为战斗预设"), Theme.Metadata);
            if (WorkspaceButton("configuration", new Rect(toolsX, y, 72, height), "编辑配置",
                _drawer == WorkspaceDrawer.Configuration, toolbar: true))
                ToggleDrawer(WorkspaceDrawer.Configuration);
            if (WorkspaceButton("diagnostics", new Rect(toolsX + 72 + gap, y, 72, height),
                _error == null ? "诊断工具" : "诊断 · !", _drawer == WorkspaceDrawer.Diagnostics, toolbar: true))
            {
                ToggleDrawer(WorkspaceDrawer.Diagnostics);
                if (_error != null)
                {
                    _drawer = WorkspaceDrawer.Diagnostics;
                    _diagnosticPage = 2;
                    _inspectorScroll = Vector2.zero;
                }
            }
            ToolbarSeparator(runX - 7, y, height);
            float x = runX;
            if (_session.Preview != null)
            {
                if (WorkspaceButton("stop", new Rect(x, y, 44, height), "停止", toolbar: true))
                    _session.Stop();
                x += 44 + gap;
            }
            if (_session.Battle != null)
            {
                float[] speeds = { 1f, 2f, 4f };
                int old = Array.IndexOf(speeds, _session.Speed);
                if (old < 0)
                {
                    old = speeds.Length;
                    speeds = speeds.Append(_session.Speed).ToArray();
                }
                var speedRect = new Rect(x, y, 52, height);
                RememberControl("speed", speedRect);
                int next = EditorGUI.Popup(speedRect, old, speeds.Select(speed => $"{speed:0.##}×").ToArray(), Theme.Popup);
                if (old != next)
                    _session.Speed = speeds[next];
            }
            if (WorkspaceButton("primary", new Rect(primaryX, y, 120, height), PrimaryLabel(), primary: true))
                RunPrimaryAction();
        }

        private void ToolbarSeparator(float x, float y, float height) =>
            EditorGUI.DrawRect(new Rect(x, y + 5, 1, height - 10), Theme.Border);

        private void ToggleDrawer(WorkspaceDrawer drawer)
        {
            _drawer = _drawer == drawer ? WorkspaceDrawer.None : drawer;
            Repaint();
        }

        private void DrawWelcome(Rect canvas)
        {
            float width = Mathf.Min(400, canvas.width - 40);
            _welcomeRect = new Rect(canvas.center.x - width / 2, canvas.center.y - 92, width, 184);
            WorkspaceAction action = PrimaryAction();
            string text = action switch
            {
                WorkspaceAction.Connect => $"连接 {BattleData.Variant} 原版数据，配置并测试一场战斗。",
                WorkspaceAction.Random => "随机生成场景与双方队伍，或从预设开始。",
                WorkspaceAction.Configure => Validation().FirstOrDefault() ?? "在右侧配置场景与双方队伍。",
                _ => $"场景 {Scenario.Scene} · 我方 {Scenario.Participants.Count(p => p.Side == BattleSide.Ally)} 人 · 敌方 {Scenario.Participants.Count(p => p.Side == BattleSide.Enemy)} 人",
            };
            GUI.Label(new Rect(_welcomeRect.x, _welcomeRect.y + 8, width, 34), "战斗编辑器", Theme.WelcomeTitle);
            GUI.Label(new Rect(_welcomeRect.x, _welcomeRect.y + 50, width, 36), text, Theme.WelcomeText);
            const float buttonWidth = 224;
            float x = _welcomeRect.center.x - buttonWidth / 2;
            if (WorkspaceButton("welcome-primary", new Rect(x, _welcomeRect.y + 102, buttonWidth, BattleEditorTheme.ControlHeight), PrimaryLabel(), primary: true))
                RunPrimaryAction();
            bool needsRoster = (Scenario.Participants?.Count ?? 0) == 0;
            using (new EditorGUI.DisabledScope(_session.Data == null))
                if (WorkspaceButton("welcome-manual", new Rect(x, _welcomeRect.y + 136, 108, BattleEditorTheme.ControlHeight),
                    needsRoster ? "手动配置" : "编辑配置"))
                {
                    _drawer = WorkspaceDrawer.Configuration;
                    _configurationPage = needsRoster ? 1 : 0;
                }
            if (WorkspaceButton("welcome-open", new Rect(x + 116, _welcomeRect.y + 136, 108, BattleEditorTheme.ControlHeight), "打开预设…"))
                OpenAssetPicker();
        }

        private void DrawDrawer(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 8, rect.width - 20, rect.height - 16));
            float previous = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 142;
            try
            {
                Rect header = GUILayoutUtility.GetRect(0, BattleEditorTheme.ControlHeight, GUILayout.ExpandWidth(true));
                GUI.Label(new Rect(header.x, header.y, header.width - 32, header.height), _drawer switch
                {
                    WorkspaceDrawer.Configuration => "战斗配置",
                    WorkspaceDrawer.Diagnostics => "诊断工具",
                    _ => "连接原版数据"
                }, Theme.Title);
                if (WorkspaceButton("drawer-close", new Rect(header.xMax - 24, header.y, 24, header.height), "×"))
                    ToggleDrawer(_drawer);
                GUILayout.Space(6);
                if (_drawer == WorkspaceDrawer.Configuration)
                    _configurationPage = DrawerTabs("tabs:configuration", _configurationPage, new[] { "场景", "队伍", "物品" });
                if (_drawer == WorkspaceDrawer.Diagnostics)
                    _diagnosticPage = DrawerTabs("tabs:diagnostics", _diagnosticPage, new[] { "观察", "资源表", "日志" });
                GUILayout.Space(6);
                using var scroll = new EditorGUILayout.ScrollViewScope(_inspectorScroll);
                _inspectorScroll = scroll.scrollPosition;
                GUILayout.Space(2);
                if (_drawer == WorkspaceDrawer.Connection)
                    DrawConnection();
                else if (_drawer == WorkspaceDrawer.Diagnostics)
                    DrawDiagnostics();
                else
                    DrawConfiguration();
            }
            finally
            {
                EditorGUIUtility.labelWidth = previous;
                GUILayout.EndArea();
            }
        }

        private void DrawConfiguration()
        {
            if (_session.Data == null)
            {
                EditorGUILayout.HelpBox("请先连接原版数据，再选择战斗场景和添加参战者。", MessageType.Info);
                if (DrawerButton("连接原版数据"))
                    _drawer = WorkspaceDrawer.Connection;
            }
            if (BattleScenarioEditing.NeedsRepair(Scenario))
            {
                EditorGUILayout.HelpBox("配置缺少必需内容。修复会保留已有字段并移除空元素，可撤销。", MessageType.Warning);
                if (DrawerButton("修复缺失配置"))
                    Edit("Repair battle authoring fields", () => _draft.Scenario = BattleScenarioEditing.Repair(Scenario));
                return;
            }
            foreach (string message in Validation().Take(3))
                EditorGUILayout.HelpBox(message, MessageType.Warning);
            if (_session.Data != null && Scenario.DataFingerprint != _session.Data.Fingerprint)
            {
                if (DrawerButton("将配置绑定到当前数据"))
                    Edit("Bind battle data", () => Scenario.DataFingerprint = _session.Data.Fingerprint);
            }
            if (_configurationPage == 0)
                DrawScenario(Scenario);
            else if (_configurationPage == 1)
            {
                DrawRoster();
                GUILayout.Space(10);
                BattleParticipant actor = Scenario.Participants.FirstOrDefault(p => p.InstanceId == _selected);
                if (actor != null)
                    DrawParticipant(actor);
                else
                    GUILayout.Label("选择上方参战者编辑等级、阵位与初始状态。", EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                Field(Scenario, "Money", "本局金钱");
                Field(Scenario, "TreasureUses", "仙宝 ID／剩余次数（0–255）");
                Field(Scenario, "Items", "物品 ID／数量");
                EditorGUILayout.HelpBox("只影响本次测试，不写入正式背包或存档。可在诊断的资源表中查找物品并加入配置。", MessageType.Info);
            }
        }

        private bool Section(string id, string title)
        {
            GUILayout.Space(BattleEditorTheme.GroupSpacing);
            Rect rect = EditorGUILayout.GetControlRect(false, BattleEditorTheme.ControlHeight);
            RememberControl("section:" + id, rect);
            float labelWidth = EditorGUIUtility.labelWidth;
            bool value;
            try
            {
                EditorGUIUtility.labelWidth = rect.width;
                value = EditorGUI.Foldout(rect, _sections.GetValueOrDefault(id), title, true, Theme.Section);
            }
            finally
            {
                EditorGUIUtility.labelWidth = labelWidth;
            }
            _sections[id] = value;
            if (value)
                GUILayout.Space(4);
            return value;
        }

        private void DrawConnection()
        {
            if (_session.Data != null)
            {
                GUILayout.Label("当前连接", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(_session.Data.Resources.Root, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(44));
                GUILayout.Label(_session.Data.Resources.Codepage == 950 ? "繁體中文 · 950" : "简体中文 · 936", EditorStyles.miniLabel);
            }
            GUILayout.Space(8);
            GUILayout.Label("数据目录", EditorStyles.boldLabel);
            _root = EditorGUILayout.TextField(_root);
            if (DrawerButton("浏览目录…"))
                ChooseDataFolder(false);
            _codepage = EditorGUILayout.Popup("文字编码", _codepage == 950 ? 1 : 0, new[] { "简体 936", "繁體 950" }) == 0 ? 936 : 950;
            EditorGUILayout.HelpBox($"选择 {BattleData.Variant} 中直接包含 basedata 的目录。加载成功后会停止当前测试，保留战斗配置。", MessageType.Info);
            if (DrawerButton("加载数据"))
                TryLoadData();
            if (_error != null)
                EditorGUILayout.HelpBox(_error, MessageType.Error);
        }

        private void ChooseDataFolder(bool load)
        {
            _drawer = WorkspaceDrawer.Connection;
            string path = EditorUtility.OpenFolderPanel("选择 " + BattleData.Variant + " 数据目录", _root, "");
            if (path.Length == 0)
                return;
            _root = path;
            if (load)
                TryLoadData();
        }

        private void DrawDiagnostics()
        {
            if (_diagnosticPage == 1)
            {
                if (BattleScenarioEditing.NeedsRepair(Scenario))
                    EditorGUILayout.HelpBox("先修复配置，再从资源表加入字段。", MessageType.Warning);
                else
                    DrawDataCatalog();
                return;
            }
            if (_diagnosticPage == 2)
            {
                if (_error != null)
                    EditorGUILayout.HelpBox(_error, MessageType.Error);
                foreach (string message in Validation())
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                foreach (string message in _session.Preview?.Diagnostics ?? Array.Empty<string>())
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                GUILayout.Label(string.Join("\n", _log), EditorStyles.wordWrappedMiniLabel);
                if (_session.Battle != null)
                    GUILayout.Label(string.Join("\n", _session.Battle.Events.Skip(Math.Max(0, _session.Battle.Events.Count - 50))), EditorStyles.wordWrappedMiniLabel);
                if (DrawerButton("导出配置与诊断…"))
                    Try(ExportReport);
                return;
            }
            DrawDisplaySettings();
            if (_session.Data == null || BattleScenarioEditing.NeedsRepair(Scenario))
                return;
            if (DraftPending)
                EditorGUILayout.HelpBox("画布使用修改前的配置。应用并重新开始后，调试命令恢复可用。", MessageType.Info);
            using (new EditorGUI.DisabledScope(DraftPending))
            {
                if (_session.Battle != null)
                {
                    if (DrawerButton("单步（一个逻辑步）"))
                        Try(_session.StepBattle);
                    if (Section("live-inspection", "当前测试状态与调试命令"))
                        DrawBattleBody();
                }
                if (_session.Preview != null && _session.Battle == null)
                {
                    GUILayout.Label("动作预览", EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (DrawerButton(_session.IsPlaying ? "暂停动作" : "播放动作"))
                        {
                            if (!_session.IsPlaying && _session.Preview.Time >= _session.Preview.Duration)
                                _session.Preview.Sample(0);
                            _session.IsPlaying = !_session.IsPlaying;
                        }
                        GUILayout.Space(6);
                        if (DrawerButton("单步动作"))
                        {
                            _session.IsPlaying = false;
                            Try(() => _session.Preview.Sample(_session.Preview.Time + 0.02f));
                        }
                    }
                    float speed = EditorGUILayout.Slider("预览倍速", _session.Speed, 0.1f, 4);
                    if (speed != _session.Speed)
                        _session.Speed = speed;
                    float time = EditorGUILayout.Slider("时间", _session.Preview.Time, 0, _session.Preview.Duration);
                    if (time != _session.Preview.Time)
                    {
                        _session.IsPlaying = false;
                        Try(() => _session.Preview.Sample(time));
                    }
                    DrawActions(_session.Preview);
                }
            }
            if (Section("preview-tools", "动作与测试工具"))
            {
                GUILayout.Label("动作预览会替换当前测试，只播放动画。", EditorStyles.wordWrappedMiniLabel);
                if (DrawerButton("建立动作预览"))
                    Try(StartPreview);
                _autoAllies = EditorGUILayout.Toggle("下次开局我方自动", _autoAllies);
                _autoEnemies = EditorGUILayout.Toggle("下次开局敌方自动", _autoEnemies);
                if (_session.Preview != null && !DraftPending && DrawerButton("重开当前测试快照"))
                    Try(_session.Restart);
            }
        }

        private void DrawDisplaySettings()
        {
            if (!Section("display", "画布与操作设置"))
                return;
            _originalAspect = EditorGUILayout.Toggle("原版 4:3 画布", _originalAspect);
            _expandCamera = EditorGUILayout.Toggle("扩展原版视野", _expandCamera);
            _touchHud = EditorGUILayout.Toggle("触屏操作", _touchHud);
            _hudSafeInsets = EditorGUILayout.Vector4Field("安全区 左／上／右／下", _hudSafeInsets);
            int color = EditorGUILayout.Popup("受击数字颜色", (int)_damageNumberColor, new[] { "原版蓝色", "黄色", "白色" });
            _damageNumberColor = (BattleDamageNumberColorMode)color;
            _debugBattle = EditorGUILayout.Toggle("显示角色调试标签", _debugBattle);
            if (_session.Preview != null && DrawerButton("镜头回到全景"))
                _session.Preview.View.Frame();
        }

        private void ShowFileMenu()
        {
            var menu = new GenericMenu();
            bool editable = !EditorApplication.isPlayingOrWillChangePlaymode;
            AddFileAction(menu, "新建战斗", () => Try(NewScenario), editable);
            AddFileAction(menu, "打开预设…", OpenAssetPicker, editable);
            if (_session.Data != null && editable)
                menu.AddItem(new GUIContent("创建随机战斗"), false, () => Try(CreateRandomExample));
            else
                menu.AddDisabledItem(new GUIContent("创建随机战斗"));
            menu.AddSeparator("");
            AddFileAction(menu, "保存", () => Try(() => SaveScenario(false)), editable);
            AddFileAction(menu, "另存为…", () => Try(() => SaveScenario(true)), editable);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("导出配置与诊断…"), false, () => Try(ExportReport));
            menu.ShowAsContext();
        }

        private static void AddFileAction(GenericMenu menu, string label, GenericMenu.MenuFunction action, bool enabled)
        {
            if (enabled)
                menu.AddItem(new GUIContent(label), false, action);
            else
                menu.AddDisabledItem(new GUIContent(label));
        }

        private void OpenAssetPicker()
        {
            _showAssetPicker(_asset, _assetPickerId);
        }

        private void HandleAssetPicker()
        {
            if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == _assetPickerId)
            {
                var asset = EditorGUIUtility.GetObjectPickerObject() as BattleScenarioAsset;
                if (asset != null)
                    Try(() => LoadAsset(asset));
                Event.current.Use();
            }
        }
    }
}
