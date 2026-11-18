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
    using System.Threading.Tasks;
    using Battle;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    // Exercise authoring lifecycle without creating or showing a native window.
    [Category("LocalGameData")]
    public sealed class BattleTestInventoryLifecycleTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
        private BattleEditorWindow _window;
        private bool _hadPath;
        private string _path;

        private string Preference => BattleData.Variant + ".BattleEditor.Path";
        private BattleScenario Draft => Get<BattleScenarioAsset>("_draft").Scenario;

        private T Get<T>(string field) => (T)typeof(BattleEditorWindow).GetField(field, Members).GetValue(_window);

        private void Set(string field, object value) => typeof(BattleEditorWindow).GetField(field, Members).SetValue(_window, value);

        private void Call(string method, params object[] arguments) => typeof(BattleEditorWindow).GetMethod(method, Members).Invoke(_window, arguments);

        [SetUp]
        public void SetUp()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            _hadPath = EditorPrefs.HasKey(Preference);
            _path = EditorPrefs.GetString(Preference);
            EditorPrefs.SetString(Preference, Path.Combine(Application.temporaryCachePath, "missing-inventory-" + Guid.NewGuid().ToString("N")));
            _window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            Set("_root", root);
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
                UnityEngine.Object.DestroyImmediate(_window);
            if (_hadPath)
                EditorPrefs.SetString(Preference, _path);
            else
                EditorPrefs.DeleteKey(Preference);
        }

        [Test]
        public void FirstConnectionInitializesCleanTestDefaultsButReconnectionPreservesExplicitEmptyInventory()
        {
            Call("LoadData");
            Assert.That(Draft.Items.Count, Is.GreaterThan(50));
            Assert.That(Draft.DataFingerprint, Is.EqualTo(Get<BattleEditorSession>("_session").Data.Fingerprint));
            Assert.That(_window.hasUnsavedChanges, Is.False);
            Assert.That(Draft.TreasureUses.Select(item => item.Id), Is.EqualTo(Enumerable.Range(6119, BattleData.Variant == "PAL3" ? 7 : 6)));
            Assert.That(Draft.TreasureUses.Select(item => item.Count), Is.All.EqualTo(3));
            Draft.Items.Clear();
            Draft.TreasureUses.Clear();
            Call("UpdateDirty");
            Call("LoadData");
            Assert.That(Draft.Items, Is.Empty);
            Assert.That(Draft.TreasureUses, Is.Empty);
            Assert.That(_window.hasUnsavedChanges, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SavedTreasureUseEditsSurvivePresetReplacementAndDataReload(bool clearCounters)
        {
            Call("LoadData");
            string path = "Assets/__BattleTreasure_" + Guid.NewGuid().ToString("N") + ".asset";
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            try
            {
                asset.Scenario = Draft.Copy();
                AssetDatabase.CreateAsset(asset, path);
                Call("LoadAsset", asset);
                Call("Edit", "Edit treasure uses", (Action)(() =>
                {
                    if (clearCounters)
                        Draft.TreasureUses.Clear();
                    else
                        Draft.TreasureUses.First(item => item.Id == 6119).Count = 0;
                }));
                Assert.That(_window.hasUnsavedChanges, Is.True);
                Call("SaveScenario", false);
                Assert.That(_window.hasUnsavedChanges, Is.False);

                Call("NewScenario");
                Assert.That(Draft.TreasureUses.First(item => item.Id == 6119).Count, Is.EqualTo(3));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Call("LoadAsset", AssetDatabase.LoadAssetAtPath<BattleScenarioAsset>(path));
                Call("LoadData");
                if (clearCounters)
                    Assert.That(Draft.TreasureUses, Is.Empty);
                else
                    Assert.That(Draft.TreasureUses.First(item => item.Id == 6119).Count, Is.Zero);
                Assert.That(Draft.Items.First(item => item.Id == 6119).Count, Is.EqualTo(99));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void SavingBeforeFirstConnectionPreservesTheSavedInventory()
        {
            string path = "Assets/__BattleEarlySave_" + Guid.NewGuid().ToString("N") + ".asset";
            try
            {
                Draft.Money = 50;
                Call("UpdateDirty");
                Call("SaveDrafts", (Func<string, string>)(_ => path));
                BattleScenarioAsset asset = AssetDatabase.LoadAssetAtPath<BattleScenarioAsset>(path);
                Assert.That(asset.Scenario.Items, Is.Empty);
                Call("LoadData");
                Assert.That(Draft.Items, Is.Empty);
                Assert.That(Draft.DataFingerprint, Is.EqualTo(asset.Scenario.DataFingerprint));
                Assert.That(_window.hasUnsavedChanges, Is.False);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void UndoOfAnEditBeforeConnectionPreservesGeneratedDefaults()
        {
            Call("Edit", "Edit initial money", (Action)(() => Draft.Money = 50));
            Undo.FlushUndoRecordObjects();
            Call("LoadData");
            string fingerprint = Draft.DataFingerprint;
            int items = Draft.Items.Count;
            Assert.That(items, Is.GreaterThan(50));
            Assert.That(_window.hasUnsavedChanges, Is.True);

            Undo.PerformUndo();
            Assert.That(Draft.Money, Is.Zero);
            Assert.That(Draft.Items.Count, Is.EqualTo(items));
            Assert.That(Draft.DataFingerprint, Is.EqualTo(fingerprint));
            Assert.That(_window.hasUnsavedChanges, Is.False);

            Undo.PerformRedo();
            Assert.That(Draft.Money, Is.EqualTo(50));
            Assert.That(Draft.Items.Count, Is.EqualTo(items));
            Assert.That(_window.hasUnsavedChanges, Is.True);
        }

        [Test]
        public void RestoredEmptyDraftStaysEmptyAndExplicitNewDraftGetsTestDefaults()
        {
            Call("LoadData");
            Draft.Items.Clear();
            Call("UpdateDirty");
            Call("OnDisable");
            UnityEngine.Object.DestroyImmediate(Get<BattleScenarioAsset>("_draft"));
            Set("_draft", null);
            Call("OnEnable");
            Assert.That(Get<Task<BattleData>>("_reconnect").Wait(TimeSpan.FromSeconds(15)), Is.True);
            Call("CompleteReconnect");
            Assert.That(Draft.Items, Is.Empty, "Reconnect must preserve an intentionally empty inventory.");
            Set("_savedJson", BattleScenarioJson.Serialize(Draft));
            Call("UpdateDirty");
            Call("NewScenario");
            Assert.That(Draft.Items.Count, Is.GreaterThan(50));
            Assert.That(_window.hasUnsavedChanges, Is.False, "An untouched new draft must use its own defaults as the saved baseline.");
            Draft.Money++;
            Call("UpdateDirty");
            Assert.That(_window.hasUnsavedChanges, Is.True, "Editing the new draft still requires saving.");
        }
    }
}
