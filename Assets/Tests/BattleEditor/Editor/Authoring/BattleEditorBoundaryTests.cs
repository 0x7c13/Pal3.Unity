// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // These boundaries require neither a native window nor the active scene.
    public sealed class BattleEditorBoundaryTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Dictionary<string, (bool Exists, string Value)> _paths = new();
        private readonly Dictionary<string, (bool Exists, int Value)> _codepages = new();
        private BattleEditorWindow _window;

        private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Members).GetValue(owner);

        private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Members).SetValue(owner, value);

        private static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Members).Invoke(owner, args);

        private BattleScenario Draft => Get<BattleScenarioAsset>(_window, "_draft").Scenario;

        [SetUp]
        public void SetUp()
        {
            foreach (string variant in new[] { "PAL3", "PAL3A" })
            {
                string path = variant + ".BattleEditor.Path", codepage = variant + ".BattleEditor.Codepage";
                _paths[path] = (EditorPrefs.HasKey(path), EditorPrefs.GetString(path));
                _codepages[codepage] = (EditorPrefs.HasKey(codepage), EditorPrefs.GetInt(codepage));
                EditorPrefs.SetString(path, Path.Combine(Application.temporaryCachePath, "missing-battle-boundary-" + variant + Guid.NewGuid().ToString("N")));
                EditorPrefs.SetInt(codepage, variant == "PAL3" ? 936 : 950);
            }
            _window = ScriptableObject.CreateInstance<BattleEditorWindow>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_window != null)
                {
                    Object.DestroyImmediate(_window);
                }
            }
            finally
            {
                foreach (var pair in _paths)
                    if (pair.Value.Exists)
                        EditorPrefs.SetString(pair.Key, pair.Value.Value);
                    else
                        EditorPrefs.DeleteKey(pair.Key);
                foreach (var pair in _codepages)
                    if (pair.Value.Exists)
                        EditorPrefs.SetInt(pair.Key, pair.Value.Value);
                    else
                        EditorPrefs.DeleteKey(pair.Key);
                _paths.Clear();
                _codepages.Clear();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("LocalGameData")]
        public void ReloadReconnectsWithTheAppliedSettingsAndPreservesPendingConnectionEdits(bool emptyPath)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required to reconnect.");
            Set(_window, "_root", root);
            Set(_window, "_codepage", 936);
            Call(_window, "LoadData");
            string appliedRoot = Get<BattleEditorSession>(_window, "_session").Data.Resources.Root;
            string pendingRoot = emptyPath ? "" : Path.Combine(root, "basedata");
            Set(_window, "_root", pendingRoot);
            Set(_window, "_codepage", 950);

            if (!emptyPath)
            {
                Assert.Throws<TargetInvocationException>(() => Call(_window, "LoadData"));
                Assert.That(Get<BattleEditorSession>(_window, "_session").Data.Resources.Root, Is.EqualTo(appliedRoot));
            }

            string otherWindowRoot = Path.Combine(root, "scene");
            EditorPrefs.SetString(BattleData.Variant + ".BattleEditor.Path", otherWindowRoot);
            EditorPrefs.SetInt(BattleData.Variant + ".BattleEditor.Codepage", 950);
            string otherVariant = BattleData.Variant == "PAL3" ? "PAL3A" : "PAL3";
            Call(_window, "RestoreAuthoringVariant", otherVariant);
            Call(_window, "OnDisable");
            Call(_window, "OnEnable");

            BattleEditorSession session = Get<BattleEditorSession>(_window, "_session");
            Assert.That(session.Data, Is.Null, "Window enable must return before adopting the resource index.");
            Task<BattleData> pending = Get<Task<BattleData>>(_window, "_reconnect");
            Assert.That(pending.Wait(TimeSpan.FromSeconds(15)), Is.True);
            Call(_window, "CompleteReconnect");
            BattleData data = session.Data;
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Resources.Root, Is.EqualTo(appliedRoot));
            Assert.That(data.Resources.Codepage, Is.EqualTo(936));
            Assert.That(Get<string>(_window, "_root"), Is.EqualTo(pendingRoot));
            Assert.That(Get<int>(_window, "_codepage"), Is.EqualTo(950));
            Assert.That(EditorPrefs.GetString(BattleData.Variant + ".BattleEditor.Path"), Is.EqualTo(otherWindowRoot));
            Assert.That(EditorPrefs.GetInt(BattleData.Variant + ".BattleEditor.Codepage"), Is.EqualTo(950));
        }

        [Test]
        [Category("LocalGameData")]
        public void ManualConnectionSupersedesACompletedReloadCandidateThatHasNotBeenAdopted()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required to reconnect.");
            Call(_window, "BeginReconnect", root, 936);
            Task<BattleData> pending = Get<Task<BattleData>>(_window, "_reconnect");
            Assert.That(pending.Wait(TimeSpan.FromSeconds(15)), Is.True);
            Assert.That(Get<BattleEditorSession>(_window, "_session").Data, Is.Null);

            Call(_window, "ConnectData", root, 936);
            BattleData manual = Get<BattleEditorSession>(_window, "_session").Data;
            Call(_window, "CompleteReconnect");
            Assert.That(manual, Is.Not.SameAs(pending.Result));
            Assert.That(Get<BattleEditorSession>(_window, "_session").Data, Is.SameAs(manual));
            Assert.That(Get<Task<BattleData>>(_window, "_reconnect"), Is.Null);
        }

        [Test]
        public void UndoAndRedoOutsideTheBattleDraftPreservePlayback()
        {
            var sceneObject = new GameObject("Battle Editor unrelated Undo");
            BattleEditorSession session = Get<BattleEditorSession>(_window, "_session");
            try
            {
                Undo.IncrementCurrentGroup();
                Undo.RecordObject(sceneObject.transform, "Move scene object");
                sceneObject.transform.localPosition = Vector3.right;
                Undo.FlushUndoRecordObjects();
                session.IsPlaying = true;

                Undo.PerformUndo();
                Assert.That(sceneObject.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(session.IsPlaying, Is.True);
                Undo.PerformRedo();
                Assert.That(sceneObject.transform.localPosition, Is.EqualTo(Vector3.right));
                Assert.That(session.IsPlaying, Is.True);
            }
            finally
            {
                Undo.ClearUndo(sceneObject.transform);
                Object.DestroyImmediate(sceneObject);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedConnectionKeepsPlaybackAndItsDiagnostic(bool playing)
        {
            BattleEditorSession session = Get<BattleEditorSession>(_window, "_session");
            session.IsPlaying = playing;
            string unavailable = Path.Combine(Application.dataPath, "missing-data-" + Guid.NewGuid().ToString("N"));
            Set(_window, "_root", unavailable);

            Call(_window, "TryLoadData");
            string error = Get<string>(_window, "_error");
            Assert.That(error, Does.Contain(unavailable));
            Assert.That(session.IsPlaying, Is.EqualTo(playing));

            Call(_window, "Tick");
            Assert.That(Get<string>(_window, "_error"), Is.EqualTo(error));
        }

        [Test]
        public void UndoAndRedoOfTheBattleDraftPausePlayback()
        {
            BattleEditorSession session = Get<BattleEditorSession>(_window, "_session");
            BattleScenarioAsset draft = Get<BattleScenarioAsset>(_window, "_draft");
            try
            {
                Undo.IncrementCurrentGroup();
                Call(_window, "Edit", "Change battle money", (Action)(() => Draft.Money = 10));
                Undo.FlushUndoRecordObjects();
                session.IsPlaying = true;

                Undo.PerformUndo();
                Assert.That(Draft.Money, Is.Zero);
                Assert.That(session.IsPlaying, Is.False);
                Assert.That(_window.hasUnsavedChanges, Is.False);

                session.IsPlaying = true;
                Undo.PerformRedo();
                Assert.That(Draft.Money, Is.EqualTo(10));
                Assert.That(session.IsPlaying, Is.False);
                Assert.That(_window.hasUnsavedChanges, Is.True);
            }
            finally
            {
                Undo.ClearUndo(draft);
            }
        }

        [Test]
        public void SavingTheCurrentPresetLeavesOtherEditedAssetsUnsaved()
        {
            string prefix = "Assets/__BattleSave_" + Guid.NewGuid().ToString("N");
            string currentPath = prefix + "_current.asset", otherPath = prefix + "_other.asset";
            var current = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            var other = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            try
            {
                AssetDatabase.CreateAsset(current, currentPath);
                AssetDatabase.CreateAsset(other, otherPath);
                byte[] currentBefore = File.ReadAllBytes(currentPath);
                byte[] otherBefore = File.ReadAllBytes(otherPath);
                Call(_window, "LoadAsset", current);
                other.Scenario.Money = 200;
                EditorUtility.SetDirty(other);
                Call(_window, "Edit", "Change current preset", (Action)(() => Draft.Money = 100));
                Call(_window, "SaveScenario", false);

                Assert.That(current.Scenario.Money, Is.EqualTo(100));
                Assert.That(File.ReadAllBytes(currentPath), Is.Not.EqualTo(currentBefore));
                Assert.That(EditorUtility.IsDirty(current), Is.False);
                Assert.That(File.ReadAllBytes(otherPath), Is.EqualTo(otherBefore));
                Assert.That(EditorUtility.IsDirty(other), Is.True);
                Assert.That(other.Scenario.Money, Is.EqualTo(200));
            }
            finally
            {
                AssetDatabase.DeleteAsset(currentPath);
                AssetDatabase.DeleteAsset(otherPath);
            }
        }

        [Test]
        public void SerializedVariantSwitchKeepsBothUnsavedDraftsPresetBindingsAndConnections()
        {
            string first = BattleData.Variant, second = first == "PAL3" ? "PAL3A" : "PAL3";
            var firstAsset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            var secondAsset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            string prefix = "Assets/__BattleReload_" + Guid.NewGuid().ToString("N");
            string firstPath = prefix + first + ".asset", secondPath = prefix + second + ".asset";
            BattleScenarioAsset oldDraft = null;
            try
            {
                firstAsset.Scenario = new BattleScenario { Variant = first, Money = 10 };
                AssetDatabase.CreateAsset(firstAsset, firstPath);
                Set(_window, "_asset", firstAsset);
                Set(_window, "_savedJson", BattleScenarioJson.Serialize(firstAsset.Scenario));
                Draft.Money = 111;
                Set(_window, "_root", "/missing/first-unsaved-connection");
                Set(_window, "_codepage", 951);
                Call(_window, "RestoreAuthoringVariant", second);
                Assert.That(Draft.Variant, Is.EqualTo(second));
                Assert.That(Get<BattleScenarioAsset>(_window, "_asset"), Is.Null);
                Assert.That(Get<string>(_window, "_root"), Is.EqualTo(EditorPrefs.GetString(second + ".BattleEditor.Path")));
                Assert.That(Get<int>(_window, "_codepage"), Is.EqualTo(EditorPrefs.GetInt(second + ".BattleEditor.Codepage")));
                secondAsset.Scenario = new BattleScenario { Variant = second, Money = 20 };
                AssetDatabase.CreateAsset(secondAsset, secondPath);
                Set(_window, "_asset", secondAsset);
                Set(_window, "_savedJson", BattleScenarioJson.Serialize(secondAsset.Scenario));
                Draft.Money = 222;
                Set(_window, "_root", "/missing/second-unsaved-connection");
                Set(_window, "_codepage", 952);
                // Domain reload keeps the serialized authoring state, while its
                // nonpersistent draft object may need to be reconstructed.
                Call(_window, "OnDisable");
                oldDraft = Get<BattleScenarioAsset>(_window, "_draft");
                Set(_window, "_draft", null);
                string serialized = EditorJsonUtility.ToJson(_window);
                string report = Environment.GetEnvironmentVariable("PAL3_BATTLE_REPORT_DIR");
                if (report != null)
                    File.WriteAllText(Path.Combine(report, BattleData.Variant + "-window-state.json"), serialized);
                Object.DestroyImmediate(_window);
                _window = ScriptableObject.CreateInstance<BattleEditorWindow>();
                Call(_window, "OnDisable");
                Object.DestroyImmediate(Get<BattleScenarioAsset>(_window, "_draft"));
                EditorJsonUtility.FromJsonOverwrite(serialized, _window);
                Call(_window, "OnEnable");
                Assert.That(Draft.Variant, Is.EqualTo(first));
                Assert.That(Draft.Money, Is.EqualTo(111));
                Assert.That(_window.hasUnsavedChanges, Is.True);
                Assert.That(Get<BattleScenarioAsset>(_window, "_asset"), Is.SameAs(firstAsset));
                Assert.That(Get<string>(_window, "_root"), Is.EqualTo("/missing/first-unsaved-connection"));
                Assert.That(Get<int>(_window, "_codepage"), Is.EqualTo(951));
                Call(_window, "RestoreAuthoringVariant", second);
                Call(_window, "UpdateDirty");
                Assert.That(Draft.Variant, Is.EqualTo(second));
                Assert.That(Draft.Money, Is.EqualTo(222));
                Assert.That(_window.hasUnsavedChanges, Is.True);
                Assert.That(Get<BattleScenarioAsset>(_window, "_asset"), Is.SameAs(secondAsset));
                Assert.That(Get<string>(_window, "_root"), Is.EqualTo("/missing/second-unsaved-connection"));
                Assert.That(Get<int>(_window, "_codepage"), Is.EqualTo(952));
                Assert.That(firstAsset.Scenario.Money, Is.EqualTo(10));
                Assert.That(secondAsset.Scenario.Money, Is.EqualTo(20));
                Assert.That(typeof(EditorWindow).GetField("m_Parent", Members).GetValue(_window), Is.Null);
            }
            finally
            {
                if (oldDraft != null)
                    Object.DestroyImmediate(oldDraft);
                AssetDatabase.DeleteAsset(firstPath);
                AssetDatabase.DeleteAsset(secondPath);
            }
        }

        [Test]
        public void CancelingOneVariantSaveAsLeavesThePreviouslyBoundAssetUnchanged()
        {
            string first = BattleData.Variant, second = first == "PAL3" ? "PAL3A" : "PAL3";
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            string path = "Assets/__BattleCloseCancel_" + Guid.NewGuid().ToString("N") + ".asset";
            try
            {
                asset.Scenario = new BattleScenario { Variant = first, Money = 10 };
                AssetDatabase.CreateAsset(asset, path);
                Set(_window, "_asset", asset);
                Draft.Money = 100;
                Call(_window, "UpdateDirty");
                Call(_window, "RestoreAuthoringVariant", second);
                Draft.Money = 200;
                Call(_window, "UpdateDirty");
                Call(_window, "SaveDrafts", (Func<string, string>)(_ => null));
                Assert.That(asset.Scenario.Money, Is.EqualTo(10));
                Assert.That(_window.hasUnsavedChanges, Is.True);
                Assert.That(_window.saveChangesMessage, Does.Contain(first).And.Contain(second));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void ClosingAccountsForInactiveDirtyDraftsAndSavesOrDiscardsEveryVariant()
        {
            string first = BattleData.Variant, second = first == "PAL3" ? "PAL3A" : "PAL3";
            var firstAsset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            var secondAsset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            string prefix = "Assets/__BattleClose_" + Guid.NewGuid().ToString("N");
            string firstPath = prefix + first + ".asset", secondPath = prefix + second + ".asset";
            try
            {
                firstAsset.Scenario = new BattleScenario { Variant = first, Money = 10 };
                secondAsset.Scenario = new BattleScenario { Variant = second, Money = 20 };
                AssetDatabase.CreateAsset(firstAsset, firstPath);
                AssetDatabase.CreateAsset(secondAsset, secondPath);
                Set(_window, "_asset", firstAsset);
                Draft.Money = 10;
                Call(_window, "UpdateDirty");
                Call(_window, "RestoreAuthoringVariant", second);
                Set(_window, "_asset", secondAsset);
                Draft.Money = 222;
                Call(_window, "UpdateDirty");
                Call(_window, "RestoreAuthoringVariant", first);
                Call(_window, "UpdateDirty");
                Assert.That(_window.hasUnsavedChanges, Is.True, "An inactive dirty draft must participate in the Editor's close prompt.");
                Assert.That(_window.saveChangesMessage, Does.Contain(second));
                Assert.That((bool)typeof(BattleEditorWindow).GetProperty("CurrentDraftDirty", Members).GetValue(_window), Is.False);
                Assert.That(Call(_window, "CanReplace"), Is.True, "Creating/replacing a clean current draft must not prompt about another variant.");
                Call(_window, "SaveScenario", false);
                Assert.That(secondAsset.Scenario.Money, Is.EqualTo(20), "The File menu's Save action still owns only the current draft.");
                Assert.That(_window.hasUnsavedChanges, Is.True);
                _window.SaveChanges();
                Assert.That(secondAsset.Scenario.Money, Is.EqualTo(222));
                Assert.That(_window.hasUnsavedChanges, Is.False);
                Draft.Money = 333;
                Call(_window, "UpdateDirty");
                Call(_window, "RestoreAuthoringVariant", second);
                Draft.Money = 444;
                Call(_window, "UpdateDirty");
                Assert.That(_window.saveChangesMessage, Does.Contain(first).And.Contain(second));
                _window.SaveChanges();
                Assert.That(firstAsset.Scenario.Money, Is.EqualTo(333));
                Assert.That(secondAsset.Scenario.Money, Is.EqualTo(444));
                Assert.That(_window.hasUnsavedChanges, Is.False);
                Draft.Money = 555;
                Call(_window, "UpdateDirty");
                Call(_window, "RestoreAuthoringVariant", first);
                Draft.Money = 666;
                Call(_window, "UpdateDirty");
                _window.DiscardChanges();
                Assert.That(Draft.Money, Is.EqualTo(333));
                Assert.That(_window.hasUnsavedChanges, Is.False);
                Call(_window, "RestoreAuthoringVariant", second);
                Call(_window, "UpdateDirty");
                Assert.That(Draft.Money, Is.EqualTo(444));
                Assert.That(_window.hasUnsavedChanges, Is.False);
            }
            finally
            {
                AssetDatabase.DeleteAsset(firstPath);
                AssetDatabase.DeleteAsset(secondPath);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LegacyUnownedWindowCanonicalizesBothSidesBeforeVariantMigration(bool keepDraftObject)
        {
            Call(_window, "OnDisable");
            string previous = BattleData.Variant == "PAL3" ? "PAL3A" : "PAL3";
            BattleScenario scenario = BattleScenarioTests.Scenario();
            scenario.Variant = previous;
            scenario.Money = 123;
            string legacy = JsonUtility.ToJson(scenario);
            BattleScenarioAsset draft = Get<BattleScenarioAsset>(_window, "_draft");
            draft.Scenario = BattleScenarioJson.Deserialize(legacy);
            if (!keepDraftObject)
            {
                Object.DestroyImmediate(draft);
                Set(_window, "_draft", null);
            }
            Get<IList>(_window, "_variantDrafts").Clear();
            Set(_window, "_authoringVariant", "");
            Set(_window, "_draftJson", legacy);
            Set(_window, "_savedJson", legacy);
            Set(_window, "_asset", null);
            Set(_window, "_root", "/missing/legacy-owner");
            Set(_window, "_initializeTestInventory", false);
            Call(_window, "OnEnable");
            Assert.That(Draft.Variant, Is.EqualTo(BattleData.Variant));
            Assert.That(_window.hasUnsavedChanges, Is.False, "A clean legacy draft must not acquire a dirty inactive variant during migration.");
            Call(_window, "RestoreAuthoringVariant", previous);
            Call(_window, "UpdateDirty");
            Assert.That(Draft.Money, Is.EqualTo(123));
            Assert.That(Draft.Participants.All(actor => actor.MagicSword == null), Is.True, "Migrate the old inline optional snapshots before storing the old owner.");
            Assert.That(Get<string>(_window, "_savedJson"), Is.EqualTo(Get<string>(_window, "_draftJson")));
            Assert.That(_window.hasUnsavedChanges, Is.False);
            Draft.Money++;
            Call(_window, "UpdateDirty");
            Assert.That(_window.hasUnsavedChanges, Is.True, "Canonicalization must still preserve a real authoring edit.");
            Assert.That(BattleScenarioJson.Deserialize(Get<string>(_window, "_savedJson")).Money, Is.EqualTo(123));
        }

        [Test]
        public void PreviouslyStoredLegacyBaselineDoesNotKeepAnInactiveVariantDirtyAfterReload()
        {
            string current = BattleData.Variant, previous = current == "PAL3" ? "PAL3A" : "PAL3";
            Call(_window, "RestoreAuthoringVariant", previous);
            BattleScenario scenario = BattleScenarioTests.Scenario();
            scenario.Variant = previous;
            scenario.Money = 234;
            string legacy = JsonUtility.ToJson(scenario);
            Get<BattleScenarioAsset>(_window, "_draft").Scenario = BattleScenarioJson.RestoreDraft(legacy);
            Set(_window, "_savedJson", legacy);
            Call(_window, "UpdateDirty");
            Assert.That(_window.hasUnsavedChanges, Is.True, "Recreate the previously stored raw-baseline/canonical-draft mismatch.");
            Call(_window, "RestoreAuthoringVariant", current);
            Call(_window, "OnDisable");
            Call(_window, "OnEnable");
            Assert.That(_window.hasUnsavedChanges, Is.False);
            Call(_window, "RestoreAuthoringVariant", previous);
            Call(_window, "UpdateDirty");
            Assert.That(Draft.Money, Is.EqualTo(234));
            Assert.That(_window.hasUnsavedChanges, Is.False);
        }

        [Test]
        public void DiscardUnboundDraftsRebuildsDefaultsForBothActiveAndInactiveVariants()
        {
            string current = BattleData.Variant, other = current == "PAL3" ? "PAL3A" : "PAL3";
            Draft.Money = 111;
            Draft.Scene = "current-baseline";
            Set(_window, "_savedJson", BattleScenarioJson.Serialize(Draft));
            Draft.Money++;
            Call(_window, "UpdateDirty");
            Call(_window, "RestoreAuthoringVariant", other);
            Draft.Money = 222;
            Draft.Scene = "inactive-baseline";
            Set(_window, "_savedJson", BattleScenarioJson.Serialize(Draft));
            Draft.Money++;
            Call(_window, "UpdateDirty");
            Call(_window, "RestoreAuthoringVariant", current);
            Call(_window, "UpdateDirty");
            Assert.That(_window.hasUnsavedChanges, Is.True);
            _window.DiscardChanges();
            Assert.That(Draft.Money, Is.Zero);
            Assert.That(Draft.Scene, Is.Empty);
            Assert.That(_window.hasUnsavedChanges, Is.False);
            Call(_window, "RestoreAuthoringVariant", other);
            Call(_window, "UpdateDirty");
            Assert.That(Draft.Variant, Is.EqualTo(other));
            Assert.That(Draft.Money, Is.Zero, "Inactive unbound drafts reset to defaults just like the active draft.");
            Assert.That(Draft.Scene, Is.Empty);
            Assert.That(Draft.Items, Is.Empty);
            Assert.That(Draft.DataFingerprint, Is.Empty);
            Assert.That(Get<bool>(_window, "_initializeTestInventory"), Is.True, "The other variant must initialize its own test inventory when its data is next loaded.");
            Assert.That(_window.hasUnsavedChanges, Is.False);
        }

        [TestCase("en-US")]
        [TestCase("fr-FR")]
        [TestCase("de-DE")]
        [Category("LocalGameData")]
        public void CompleteNativeBattleDataLoadKeepsAllCameraValuesAcrossEditorCultures(string culture)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Local game data is required to verify the entire battle loading path.");
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                using var reference = new BattleData(root, 936);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                using var actual = new BattleData(root, 936);
                float[] Values(BattleData data) => data.Cameras.DefaultCamConfigs.SelectMany(camera => new[]
                {
                    camera.GameBoxPositionX, camera.GameBoxPositionY, camera.GameBoxPositionZ,
                    camera.Yaw, camera.Pitch, camera.Roll
                }).ToArray();
                Assert.That(actual.Cameras.DefaultCamConfigs, Is.Not.Empty);
                Assert.That(Values(actual), Is.EqualTo(Values(reference)), "Loading must reject locale-based decimal/group separator reinterpretation, including silent de-DE corruption.");
                float[] Positions(BattleData data) => data.Layout.ActorGameBoxPositions.SelectMany(position => new[] { position.X, position.Y, position.Z }).ToArray();
                Assert.That(Positions(actual), Is.EqualTo(Positions(reference)));
                Assert.That(actual.Fingerprint, Is.EqualTo(reference.Fingerprint));
                string report = Environment.GetEnvironmentVariable("PAL3_BATTLE_REPORT_DIR");
                if (report != null)
                    File.WriteAllText(Path.Combine(report, BattleData.Variant + "-battle-data-" + culture + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        variant = BattleData.Variant,
                        culture,
                        cameras = actual.Cameras.DefaultCamConfigs,
                        actual.Fingerprint
                    }, Newtonsoft.Json.Formatting.Indented));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void LegacyWindowInfersTheOldDraftOwnerBeforeOpeningTheCompiledVariant()
        {
            string previous = BattleData.Variant == "PAL3" ? "PAL3A" : "PAL3";
            Draft.Variant = previous;
            Draft.Money = 333;
            Set(_window, "_authoringVariant", "");
            Set(_window, "_root", "/missing/legacy-connection");
            Call(_window, "RestoreAuthoringVariant", BattleData.Variant);
            Assert.That(Draft.Variant, Is.EqualTo(BattleData.Variant));
            Assert.That(Draft.Money, Is.Zero);
            Call(_window, "RestoreAuthoringVariant", previous);
            Assert.That(Draft.Variant, Is.EqualTo(previous));
            Assert.That(Draft.Money, Is.EqualTo(333));
            Assert.That(Get<string>(_window, "_root"), Is.EqualTo("/missing/legacy-connection"));
        }

        [TestCase(EventModifiers.Command)]
        [TestCase(EventModifiers.Control)]
        [TestCase(EventModifiers.Alt)]
        public void ModifiedEditorShortcutsReachTheEditorWithoutNavigatingOrSubmitting(EventModifiers modifiers)
        {
            BattleSession battle = BattleHudTests.Session();
            var view = new BattleHudView();
            view.Presenter.Synchronize(battle);
            Set(view, "_battle", battle);
            Set(view, "_inputEnabled", true);
            int inputs = battle.Inputs.Count;
            string focus = view.Presenter.FocusedId;
            bool editing = EditorGUIUtility.editingTextField;
            int hot = GUIUtility.hotControl;
            try
            {
                EditorGUIUtility.editingTextField = false;
                GUIUtility.hotControl = 0;
                foreach (KeyCode key in new[] { KeyCode.S, KeyCode.Z, KeyCode.R })
                {
                    var input = new Event { type = EventType.KeyDown, keyCode = key, modifiers = modifiers, mousePosition = new Vector2(5, 5) };
                    Call(view, "HandleGuiInput", input, new Rect(0, 0, 800, 600));
                    Assert.That(input.type, Is.EqualTo(EventType.KeyDown), key + " must remain available to Editor shortcuts.");
                    Assert.That(view.Presenter.FocusedId, Is.EqualTo(focus));
                    Assert.That(battle.Inputs.Count, Is.EqualTo(inputs));
                }
                var bare = new Event { type = EventType.KeyDown, keyCode = KeyCode.S, mousePosition = new Vector2(5, 5) };
                Call(view, "HandleGuiInput", bare, new Rect(0, 0, 800, 600));
                Assert.That(bare.type, Is.EqualTo(EventType.Used));
                Assert.That(view.Presenter.FocusedId, Is.EqualTo("items"));
                Assert.That(battle.Inputs.Count, Is.EqualTo(inputs));
            }
            finally
            {
                GUIUtility.hotControl = hot;
                EditorGUIUtility.editingTextField = editing;
                view.Release();
            }
        }

        [TestCase("Stop")]
        [TestCase("Dispose")]
        [TestCase("Preview")]
        [TestCase("Battle")]
        [TestCase("Replay")]
        [Category("LocalGameData")]
        public void FailingReleaseNotificationCannotRetainOldOrCandidatePreviewScenes(string operation)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Local game data is required for preview ownership verification.");
            BattleEditorSession session = Get<BattleEditorSession>(_window, "_session");
            session.Load(root, 936);
            BattleData data = session.Data;
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint, Music = "NONE" };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id));
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            int scenes = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            if (operation is "Replay" or "Battle" or "Preview")
                session.StartBattle(scenario);
            else
                session.Start(scenario);
            BattlePreview previous = session.Preview;
            Assert.That(UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount, Is.EqualTo(scenes + 1));
            Action notification = Get<Action>(session, "PreviewReleased");
            Set(session, "PreviewReleased", notification + (Action)(() => throw new InvalidOperationException("Injected release failure")));
            try
            {
                Action change = operation switch
                {
                    "Stop" => session.Stop,
                    "Dispose" => session.Dispose,
                    "Preview" => () => session.Start(scenario),
                    "Battle" => () => session.StartBattle(scenario),
                    _ => session.ReplayBattle
                };
                Assert.Throws<InvalidOperationException>(() => change());
                Assert.That(session.Preview, Is.Null);
                Assert.That(session.Battle, Is.Null);
                Assert.That(session.CapturedScenario, Is.Null);
                Assert.That(session.ResultPresentation, Is.Null);
                Assert.That(session.IsPlaying, Is.False);
                Assert.That(previous.View.Root == null, Is.True);
                Assert.That(UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount, Is.EqualTo(scenes),
                    "Both the old preview and a fully constructed replacement must be disposed when a release notification fails.");
                if (operation == "Dispose")
                    Assert.That(session.Data, Is.Null);
                else
                    Assert.That(session.Data, Is.SameAs(data), "Stopping/replacing a preview keeps its read-only data connection.");
            }
            finally
            {
                Set(session, "PreviewReleased", notification);
            }
        }

        [Test]
        [Category("LocalGameData")]
        public void SessionStopReleasesHudBattlePreviewPointersAndDecodedPixelCaches()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Local game data is required to verify the complete preview ownership chain.");
            BattleEditorSession session = Get<BattleEditorSession>(_window, "_session");
            session.Load(root, 936);
            BattleScenario scenario = BattleTestLoadoutBuilder.CreateScenario(session.Data);
            scenario.Scene = session.Data.Scenes.First();
            scenario.Music = "NONE";
            scenario.Encounter = BattleEncounter.AllyAmbush;
            scenario.Participants.Add(session.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = session.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(session.Data, id));
            scenario.Participants.Add(session.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            session.StartBattle(scenario);
            for (int step = 0; step < 3000 && session.Battle.WaitingActor == null; step++)
                session.Battle.Step();
            Assert.That(session.Battle.WaitingActor, Is.Not.Null);
            BattleHudView view = Get<BattleHudView>(_window, "_hud");
            view.TakeNavigationInput(session.Battle);
            Set(view, "_data", session.Data);
            Set(view, "_preview", session.Preview);
            Set(view, "_changed", (Action)(() => _window.Repaint()));
            view.PreparePointers(session.Battle, session.Data, session.Preview);
            var sprite = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                IDictionary pixels = Get<IDictionary>(view, "_spritePixels");
                Type pixelType = typeof(BattleHudView).GetNestedType("SpritePixels", BindingFlags.NonPublic);
                pixels.Add(sprite, Activator.CreateInstance(pixelType, new object[] { sprite }));
                Assert.That(Get<IDictionary>(view, "_pointers").Count, Is.GreaterThan(0));
                session.Stop();
                foreach (string field in new[] { "_battle", "_data", "_preview", "_selectionOwner", "_pointerOwner", "_pointerModel", "_changed", "_repaint", "_selectActor" })
                    Assert.That(Get<object>(view, field), Is.Null, field + " retained a stopped preview.");
                Assert.That(pixels.Count, Is.Zero);
                Assert.That(Get<IDictionary>(view, "_pointers").Count, Is.Zero);
                Assert.That(view.Presenter.Controller, Is.Null);
                Assert.That(view.Presenter.FocusItems(), Is.Empty);
                Assert.That(view.SendIntent(new BattleHudIntent(BattleHudIntentKind.Repeat)), Is.False);
                session.Stop();
            }
            finally
            {
                Object.DestroyImmediate(sprite);
            }
        }
    }
}
