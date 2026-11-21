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
    using System.Threading;
    using System.Threading.Tasks;
    using Battle;
    using Battle.Presentation;
    using Engine.Utilities;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed partial class BattleEditorWindow : EditorWindow
    {
        [SerializeField] private BattleScenarioAsset _draft;
        [SerializeField] private BattleScenarioAsset _asset;
        [SerializeField] private string _draftJson = "";
        [SerializeField] private string _savedJson = "";
        [SerializeField] private string _root = "";
        [SerializeField] private string _authoringVariant = "";
        [SerializeField] private List<VariantDraft> _variantDrafts = new();
        [SerializeField] private bool _initializeTestInventory;
        [SerializeField] private int _codepage = 936;
        private BattleEditorSession _session;
        private BattleEditorGamepadInput _gamepadInput;
        private double _lastTime;
        private Task<BattleData> _reconnect;
        private CancellationTokenSource _reconnectCancellation;
        private bool _gamepadPresentationBlocked;
        private bool _currentDraftDirty;
        private readonly DisplayFramePacer _repaintPacer = new();
        private string _error, _selected, _target, _action, _search = "", _previewJson;
        [SerializeField] private bool _autoAllies;
        [SerializeField] private bool _autoEnemies = true;
        private int _addActor;
        private BattleSide _addSide = BattleSide.Enemy;
        private Vector2 _rosterScroll, _inspectorScroll;
        private readonly List<string> _log = new();
        private readonly BattleHudView _hud = new();
        [UnityEngine.Serialization.FormerlySerializedAs("_debugCombat")]
        [SerializeField] private bool _debugBattle;
        [SerializeField] private bool _originalAspect;
        [SerializeField] private bool _expandCamera = true;
        [SerializeField] private bool _touchHud;
        [SerializeField] private Vector4 _hudSafeInsets = new(8, 8, 8, 8);

        private BattleScenario Scenario => _draft.Scenario;

#if PAL3A
        [MenuItem("PAL3A/Battle Editor", priority = 31)]
#else
        [MenuItem("PAL3/Battle Editor", priority = 31)]
#endif
        public static void Open() => GetWindow<BattleEditorWindow>("Battle Editor");

        private void OnEnable()
        {
            titleContent = new GUIContent("Battle Editor");
            wantsMouseMove = true;
            minSize = new Vector2(960, 640);
            saveChangesMessage = "保存战斗配置后再关闭？";
            NormalizeDrafts();
            VariantDraft state = RestoreAuthoringVariant(BattleData.Variant);
            if (_draft == null)
            {
                _draft = CreateInstance<BattleScenarioAsset>();
                _draft.hideFlags = HideFlags.HideAndDontSave;
                _draft.Scenario = new BattleScenario { Variant = BattleData.Variant };
                if (_draftJson.Length == 0 && _asset == null)
                    _initializeTestInventory = true;
            }
            if (_draftJson.Length > 0)
                _draft.Scenario = BattleScenarioJson.RestoreDraft(_draftJson);
            if (_savedJson.Length == 0)
                _savedJson = BattleScenarioJson.Serialize(Scenario);
            _session = new BattleEditorSession();
            _session.PreviewReleased += _hud.Release;
            _gamepadInput?.Dispose();
            _gamepadInput = new BattleEditorGamepadInput(CanReceiveGamepad, ReceiveGamepad);
            _hud.PointerTookControl += ReleaseGamepadInput;
            _hud.KeyboardTookControl += ReleaseGamepadInput;
            _lastTime = EditorApplication.timeSinceStartup;
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.projectChanged += OnAuthoringProjectChanged;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (!string.IsNullOrEmpty(state.ConnectionRoot))
                BeginReconnect(state.ConnectionRoot, state.ConnectionCodepage);
            UpdateDirty();
        }

        public void CreateGUI() => rootVisualElement.Add(new IMGUIContainer(DrawWindow) { style = { flexGrow = 1 } });

        [Serializable]
        private sealed class VariantDraft
        {
            public string Variant;
            public string DraftJson;
            public string SavedJson;
            public string Root;
            public int Codepage;
            public string ConnectionRoot;
            public int ConnectionCodepage;
            public bool InitializeTestInventory;
            public BattleScenarioAsset Asset;
        }

        private static string CanonicalDraftJson(string json) => string.IsNullOrEmpty(json) ? "" : BattleScenarioJson.Serialize(BattleScenarioJson.RestoreDraft(json));

        private void NormalizeDrafts()
        {
            // Repair the old field-only codec before migrating either side of
            // the dirty comparison into a different variant's stored state.
            _draftJson = CanonicalDraftJson(_draftJson);
            _savedJson = CanonicalDraftJson(_savedJson);
            if (_draft != null && _draftJson.Length > 0)
                _draft.Scenario = BattleScenarioJson.RestoreDraft(_draftJson);
            foreach (VariantDraft state in _variantDrafts)
            {
                state.DraftJson = CanonicalDraftJson(state.DraftJson);
                state.SavedJson = CanonicalDraftJson(state.SavedJson);
            }
        }

        private void StoreAuthoringVariant()
        {
            if (_draft != null)
                _draftJson = BattleScenarioJson.Serialize(Scenario);
            if (string.IsNullOrEmpty(_authoringVariant))
                return;
            VariantDraft state = CurrentVariantDraft();
            state.DraftJson = _draftJson;
            state.SavedJson = _savedJson;
            state.Asset = _asset;
            state.Root = _root;
            state.Codepage = _codepage;
            state.InitializeTestInventory = _initializeTestInventory;
        }

        private VariantDraft CurrentVariantDraft()
        {
            VariantDraft state = _variantDrafts.FirstOrDefault(value => value.Variant == _authoringVariant);
            if (state == null)
            {
                state = new VariantDraft { Variant = _authoringVariant };
                _variantDrafts.Add(state);
            }
            return state;
        }

        private VariantDraft RestoreAuthoringVariant(string variant)
        {
            // Migrate windows serialized before variant ownership was recorded.
            if (string.IsNullOrEmpty(_authoringVariant))
                _authoringVariant = _draft != null ? Scenario.Variant : _draftJson.Length > 0 ? BattleScenarioJson.RestoreDraft(_draftJson).Variant : variant;
            if (_authoringVariant != variant)
            {
                CancelReconnect();
                StoreAuthoringVariant();
                VariantDraft state = _variantDrafts.FirstOrDefault(value => value.Variant == variant);
                _draftJson = state?.DraftJson ?? "";
                _savedJson = state?.SavedJson ?? "";
                _asset = state?.Asset;
                _root = state?.Root ?? "";
                _codepage = state?.Codepage ?? 936;
                _initializeTestInventory = state?.InitializeTestInventory ?? true;
                if (_draft != null)
                {
                    Undo.ClearUndo(_draft);
                    _draft.Scenario = _draftJson.Length > 0 ? BattleScenarioJson.RestoreDraft(_draftJson) : new BattleScenario { Variant = variant };
                }
                _selected = _target = _action = _previewJson = _error = null;
            }
            _authoringVariant = variant;
            bool needsDefaults = string.IsNullOrEmpty(_root) && !_variantDrafts.Any(value => value.Variant == variant);
            VariantDraft current = CurrentVariantDraft();
            // Windows serialized before reconnect settings were stored also start
            // from the shared default once. Pending edits stay in Root/Codepage.
            if (current.ConnectionCodepage == 0)
                (current.ConnectionRoot, current.ConnectionCodepage) = DefaultConnection(variant);
            if (needsDefaults)
                (_root, _codepage) = (current.ConnectionRoot, current.ConnectionCodepage);
            return current;
        }

        private static (string Root, int Codepage) DefaultConnection(string variant) =>
            (EditorPrefs.GetString(variant + ".BattleEditor.Path", Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", variant))),
                EditorPrefs.GetInt(variant + ".BattleEditor.Codepage", 936));

        private void OnDisable()
        {
            CancelReconnect();
            _hud.SetInputFocus(false);
            _hud.PointerTookControl -= ReleaseGamepadInput;
            _hud.KeyboardTookControl -= ReleaseGamepadInput;
            _gamepadInput?.Dispose();
            _gamepadInput = null;
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.projectChanged -= OnAuthoringProjectChanged;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            StoreAuthoringVariant();
            BattleEditorSession session = _session;
            _session = null;
            if (session != null)
                session.PreviewReleased -= _hud.Release;
            try
            {
                _hud.Release();
            }
            finally
            {
                session?.Dispose();
            }
        }

        private void OnDestroy()
        {
            if (_draft != null)
                DestroyImmediate(_draft);
        }

        private void OnPlayMode(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode)
                _session?.Stop();
        }

        private void OnLostFocus()
        {
            _heldBattleKeys.Clear();
            _hud.SetInputFocus(false);
            _gamepadInput?.Suspend();
        }

        private void OnFocus() => _hud.SetInputFocus(true);

        private void ReleaseGamepadInput() => _gamepadInput?.Suspend();

        private void OnUndo()
        {
            string previousDraft = _draftJson;
            // Early Undo records predate the first data connection. Preserve
            // the generated baseline when restoring an unbound snapshot.
            if (_draft != null && _asset == null && string.IsNullOrEmpty(Scenario.DataFingerprint) && !string.IsNullOrEmpty(_savedJson))
            {
                BattleScenario baseline = BattleScenarioJson.RestoreDraft(_savedJson);
                if (!string.IsNullOrEmpty(baseline.DataFingerprint))
                    ApplyTestInventoryDefaults(Scenario, baseline);
            }
            UpdateDirty();
            if (_draftJson != previousDraft)
                PauseForDraftChange();
            Repaint();
        }

        private void OnAuthoringProjectChanged()
        {
            UpdateDirty();
            Repaint();
        }

        private void Tick()
        {
            CompleteReconnect();
            double now = EditorApplication.timeSinceStartup;
            if (_session?.IsReplaying == true)
                Try(_session.TickReplay, pauseOnError: false, clearErrorOnSuccess: false);
            else if (_session?.IsPlaying == true)
            {
                Try(() => _session.Tick((float)Math.Clamp(now - _lastTime, 0, 0.1)), clearErrorOnSuccess: false);
            }
            _lastTime = now;
            bool blocksHud = GamepadPresentationBlocksHud;
            if (blocksHud && !_gamepadPresentationBlocked)
                _gamepadInput?.ResetNavigation();
            _gamepadPresentationBlocked = blocksHud;
            if ((_session?.IsReplaying == true || _session?.IsPlaying == true || _session?.Preview != null && focusedWindow == this) &&
                _repaintPacer.TryAdvance(now, DisplayFrameRate.RefreshRate))
                Repaint();
        }

        private void Try(Action action, bool pauseOnError = true, bool clearErrorOnSuccess = true)
        {
            try
            {
                action();
                if (clearErrorOnSuccess)
                    _error = null;
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                if (pauseOnError && _session != null)
                    _session.IsPlaying = false;
                Log(_error);
            }
        }

        private void Log(string message)
        {
            _log.Add(message);
            if (_log.Count > 100)
                _log.RemoveAt(0);
        }

        private void LoadData()
        {
            ConnectData(_root, _codepage);
            EditorPrefs.SetString(BattleData.Variant + ".BattleEditor.Path", _root);
            EditorPrefs.SetInt(BattleData.Variant + ".BattleEditor.Codepage", _codepage);
        }

        private void TryLoadData() => Try(LoadData, pauseOnError: false);

        private void ConnectData(string root, int codepage)
        {
            CancelReconnect();
            _session.Load(root, codepage);
            DataConnected();
        }

        private void BeginReconnect(string root, int codepage)
        {
            CancelReconnect();
            _reconnectCancellation = new CancellationTokenSource();
            CancellationToken cancellation = _reconnectCancellation.Token;
            _reconnect = Task.Run(() => Directory.Exists(root) ? new BattleData(root, codepage, cancellation) : null, cancellation);
        }

        private void CompleteReconnect()
        {
            if (_reconnect is not { IsCompleted: true } reconnect)
                return;
            _reconnect = null;
            _reconnectCancellation.Dispose();
            _reconnectCancellation = null;
            Try(() =>
            {
                BattleData data = reconnect.GetAwaiter().GetResult();
                if (data == null)
                    return;
                if (_session.Data != null)
                {
                    data.Dispose();
                    return;
                }
                _session.UseData(data);
                DataConnected();
            }, pauseOnError: false, clearErrorOnSuccess: false);
            Repaint();
        }

        private void CancelReconnect()
        {
            if (_reconnect == null)
                return;
            Task<BattleData> reconnect = _reconnect;
            CancellationTokenSource cancellation = _reconnectCancellation;
            _reconnect = null;
            _reconnectCancellation = null;
            cancellation.Cancel();
            _ = reconnect.ContinueWith(completed =>
            {
                try
                {
                    if (completed.IsCompletedSuccessfully)
                        completed.Result?.Dispose();
                    _ = completed.Exception;
                }
                finally
                {
                    cancellation.Dispose();
                }
            }, TaskScheduler.Default);
        }

        private void DataConnected()
        {
            if (_initializeTestInventory)
            {
                BattleScenario defaults = BattleTestLoadoutBuilder.CreateScenario(_session.Data);
                BattleScenario baseline = BattleScenarioJson.RestoreDraft(_savedJson);
                ApplyTestInventoryDefaults(baseline, defaults);
                _savedJson = BattleScenarioJson.Serialize(baseline);
                ApplyTestInventoryDefaults(Scenario, defaults);
                _initializeTestInventory = false;
                UpdateDirty();
            }
            _validation = null;
            VariantDraft state = CurrentVariantDraft();
            state.ConnectionRoot = _session.Data.Resources.Root;
            state.ConnectionCodepage = _session.Data.Resources.Codepage;
            Log($"{BattleData.Variant}: {_session.Data.Scenes.Length} 个战场，{_session.Data.Database.CombatActorInfos.Count} 个角色；只读加载 basedata.cpk。");
        }

        private static void ApplyTestInventoryDefaults(BattleScenario scenario, BattleScenario defaults)
        {
            if (scenario.Items.Count == 0)
            {
                scenario.Items = defaults.Items;
                if (scenario.TreasureUses.Count == 0)
                    scenario.TreasureUses = defaults.TreasureUses;
            }
            scenario.DataFingerprint = defaults.DataFingerprint;
        }

        private bool CurrentDraftDirty => _currentDraftDirty;

        private IEnumerable<VariantDraft> DirtyVariants() => _variantDrafts.Where(state => !string.IsNullOrEmpty(state.DraftJson) &&
            state.DraftJson != (state.Asset != null ? BattleScenarioJson.Serialize(state.Asset.Scenario) : state.SavedJson));

        private void UpdateDirty()
        {
            if (_draft == null)
                return;
            _validation = null;
            StoreAuthoringVariant();
            string[] dirty = DirtyVariants().Select(state => state.Variant).ToArray();
            _currentDraftDirty = dirty.Contains(_authoringVariant);
            hasUnsavedChanges = dirty.Length > 0;
            saveChangesMessage = "保存以下作品的战斗配置后再关闭？" + string.Join("、", dirty);
        }

        private void Edit(string label, Action change)
        {
            PauseForDraftChange();
            Undo.RecordObject(_draft, label);
            change();
            EditorUtility.SetDirty(_draft);
            UpdateDirty();
        }

        private bool CanReplace()
        {
            if (!CurrentDraftDirty)
                return true;
            int answer = EditorUtility.DisplayDialogComplex("战斗配置尚未保存", "保存当前配置后再继续？", "保存", "取消", "放弃");
            if (answer == 1)
                return false;
            if (answer == 0)
            {
                Try(() => SaveScenario(false));
                return !CurrentDraftDirty;
            }
            return true;
        }

        private void NewScenario()
        {
            if (!CanReplace())
                return;
            _session.Stop();
            Undo.ClearUndo(_draft);
            _asset = null;
            _draft.Scenario = BattleTestLoadoutBuilder.CreateScenario(_session.Data);
            _initializeTestInventory = _session.Data == null;
            _savedJson = BattleScenarioJson.Serialize(Scenario);
            UpdateDirty();
            _selected = null;
            _drawer = WorkspaceDrawer.Configuration;
            _configurationPage = 1;
        }

        private void LoadAsset(BattleScenarioAsset asset)
        {
            if (asset == null || asset.Scenario == null)
                return;
            CheckAuthoringVersion(asset.Scenario);
            if (asset == _asset || !CanReplace())
                return;
            _session.Stop();
            Undo.ClearUndo(_draft);
            _asset = asset;
            _initializeTestInventory = false;
            _draft.Scenario = asset.Scenario.Copy();
            _savedJson = BattleScenarioJson.Serialize(Scenario);
            UpdateDirty();
            _selected = null;
        }

        // EditorWindow calls this for its close/save flow. Every draft named
        // in the close prompt must be saved before the window can be destroyed.
        public override void SaveChanges() => Try(() => SaveDrafts(SelectScenarioPath));

        private void SaveDrafts(Func<string, string> pickPath)
        {
            StoreAuthoringVariant();
            try
            {
                var planned = new List<(VariantDraft State, BattleScenario Scenario, string Path)>();
                var newPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (VariantDraft state in DirtyVariants())
                {
                    BattleScenario scenario = BattleScenarioJson.RestoreDraft(state.DraftJson);
                    if (scenario.Version != BattleScenario.CurrentVersion || scenario.Variant is not ("PAL3" or "PAL3A") || scenario.Variant != state.Variant)
                        throw new InvalidDataException("Unsupported battle draft: " + state.Variant + ". The draft and preset have been kept unchanged.");
                    string path = null;
                    if (state.Asset == null)
                    {
                        path = pickPath(scenario.Variant);
                        if (path == null)
                            return;
                        if (!newPaths.Add(path))
                            throw new IOException("请为每个作品选择不同的预设路径。");
                    }
                    planned.Add((state, scenario, path));
                }
                foreach (var (state, scenario, path) in planned)
                {
                    BattleScenarioAsset destination = WriteScenario(scenario, state.Asset, path);
                    state.Asset = destination;
                    state.SavedJson = state.DraftJson;
                    state.InitializeTestInventory = false;
                    if (state.Variant == _authoringVariant)
                    {
                        _asset = destination;
                        _savedJson = state.SavedJson;
                        _initializeTestInventory = false;
                    }
                }
                base.SaveChanges();
            }
            finally
            {
                UpdateDirty();
            }
        }

        private void SaveScenario(bool saveAs)
        {
            CheckAuthoringVersion(Scenario);
            BattleScenarioAsset destination = saveAs ? null : _asset;
            string path = destination == null ? SelectScenarioPath(Scenario.Variant) : null;
            if (destination == null && path == null)
                return;
            destination = WriteScenario(Scenario, destination, path);
            _asset = destination;
            _initializeTestInventory = false;
            _savedJson = BattleScenarioJson.Serialize(Scenario);
            UpdateDirty();
        }

        private static string SelectScenarioPath(string variant)
        {
            string path = EditorUtility.SaveFilePanelInProject("保存 " + variant + " 战斗配置", "BattleScenario", "asset", "选择自有预设保存位置");
            if (path.Length == 0)
                return null;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new IOException("该路径已有资产，请选择新路径或直接保存当前预设。");
            return path;
        }

        private BattleScenarioAsset WriteScenario(BattleScenario scenario, BattleScenarioAsset destination, string path)
        {
            if (destination == null)
            {
                destination = CreateInstance<BattleScenarioAsset>();
                try
                {
                    destination.Scenario = scenario.Copy();
                    AssetDatabase.CreateAsset(destination, path);
                }
                catch
                {
                    if (!EditorUtility.IsPersistent(destination))
                        DestroyImmediate(destination);
                    throw;
                }
            }
            else
            {
                destination.Scenario = scenario.Copy();
                EditorUtility.SetDirty(destination);
            }
            AssetDatabase.SaveAssetIfDirty(destination);
            Log("已保存 " + AssetDatabase.GetAssetPath(destination));
            return destination;
        }

        private static void CheckAuthoringVersion(BattleScenario scenario)
        {
            if (scenario.Version != BattleScenario.CurrentVersion || scenario.Variant != BattleData.Variant)
                throw new InvalidDataException($"This editor supports {BattleData.Variant} scenario version {BattleScenario.CurrentVersion}. The draft and existing asset have been kept unchanged.");
        }

        public override void DiscardChanges()
        {
            PauseForDraftChange();
            foreach (VariantDraft state in _variantDrafts.Where(state => state.Variant != _authoringVariant))
            {
                state.DraftJson = BattleScenarioJson.Serialize(state.Asset != null ? state.Asset.Scenario : new BattleScenario { Variant = state.Variant });
                state.SavedJson = state.DraftJson;
                state.InitializeTestInventory = state.Asset == null;
            }
            _draft.Scenario = _asset != null ? _asset.Scenario.Copy() : BattleTestLoadoutBuilder.CreateScenario(_session?.Data);
            _initializeTestInventory = _asset == null && _session?.Data == null;
            _savedJson = BattleScenarioJson.Serialize(Scenario);
            base.DiscardChanges();
            UpdateDirty();
        }

        private void StartPreview()
        {
            _session.Start(Scenario);
            _previewJson = BattleScenarioJson.Serialize(Scenario);
            Log("动作预览已启动；在诊断面板选择角色、动作或攻击目标。此模式只演示动画。");
        }

        private void ExportReport()
        {
            string path = EditorUtility.SaveFilePanel("导出战斗配置与诊断", "", "battle-preview", "json");
            if (path.Length == 0)
                return;
            var report = new PreviewReport
            {
                Scenario = Scenario.Copy(),
                Captured = _session.CapturedScenario,
                Diagnostics = (_session.Data?.Validate(Scenario, forBattle: true) ?? Scenario.Validate()).Concat(_session.Preview?.Diagnostics ?? new List<string>())
                    .Concat(_error == null ? Array.Empty<string>() : new[] { _error }).Distinct().ToArray(),
            };
            report.Inputs = _session.Battle?.Inputs.ToArray();
            report.Events = _session.Battle?.Events.ToArray();
            report.Result = _session.Battle?.Result;
            report.FinalTick = _session.Battle?.Tick ?? 0;
            report.Definitions = _session.Battle?.Definitions;
            report.Actors = _session.Battle?.Actors.ToArray();
            report.Inventory = _session.Battle?.Inventory.ToArray();
            report.Money = _session.Battle?.Money ?? Scenario.Money;
            report.Pal3A = _session.Battle?.Definitions.Variant == "PAL3A" ? _session.Battle.Pal3AState : null;
            ResourceBrowser.ResourceCatalog.WriteTextFile(path,
                Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented), _session.Data?.Resources.Root);
            Log("已导出 " + path);
        }

        // JSON export DTO; Unity does not persist this report.
        private sealed class PreviewReport
        {
            public int FormatVersion = 2;

            public BattleScenario Scenario;
            public BattleScenario Captured;
            public string[] Diagnostics;
            public long FinalTick;
            public BattleInput[] Inputs;
            public BattleEvent[] Events;
            public BattleResult Result;
            public BattleDefinitions Definitions;
            public BattleActorSnapshot[] Actors;
            public BattleItemSnapshot[] Inventory;
            public int Money;
            public BattlePal3AStateSnapshot Pal3A;
        }
    }
}
