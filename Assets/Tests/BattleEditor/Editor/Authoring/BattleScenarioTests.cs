// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.TestTools;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class BattleScenarioTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string ReloadWindow = "BattleScenarioReloadTest";

        private const string ReloadEnvironment = ReloadWindow + ".Environment";

        private static void IsolateReloadSource()
        {
            string preference = BattleData.Variant + ".BattleEditor.Path";
            SessionState.SetBool(ReloadEnvironment + ".HadPath", EditorPrefs.HasKey(preference));
            SessionState.SetString(ReloadEnvironment + ".Path", EditorPrefs.GetString(preference));
            SessionState.SetBool(ReloadEnvironment, true);
            EditorPrefs.SetString(preference, System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                "../../workspace/ResourceBrowserQA/battle-missing-source", Guid.NewGuid().ToString("N"))));
        }

        [TearDown]
        public void CleanupReloadState()
        {
            string draftId = SessionState.GetString(ReloadWindow + ".DraftId", "");
            try
            {
                foreach (BattleEditorWindow window in Resources.FindObjectsOfTypeAll<BattleEditorWindow>().Where(value => value.name == ReloadWindow))
                {
                    window.DiscardChanges();
                    Object.DestroyImmediate(window);
                }
            }
            finally
            {
                try
                {
                    foreach (BattleScenarioAsset draft in Resources.FindObjectsOfTypeAll<BattleScenarioAsset>().Where(value => value.GetEntityId().ToString() == draftId))
                    {
                        Undo.ClearUndo(draft);
                        Object.DestroyImmediate(draft);
                    }
                }
                finally
                {
                    try
                    {
                        if (SessionState.GetBool(ReloadEnvironment, false))
                        {
                            string preference = BattleData.Variant + ".BattleEditor.Path";
                            if (SessionState.GetBool(ReloadEnvironment + ".HadPath", false))
                                EditorPrefs.SetString(preference, SessionState.GetString(ReloadEnvironment + ".Path", ""));
                            else
                                EditorPrefs.DeleteKey(preference);
                        }
                    }
                    finally
                    {
                        SessionState.EraseBool(ReloadWindow);
                        SessionState.EraseString(ReloadWindow + ".WindowId");
                        SessionState.EraseString(ReloadWindow + ".DraftId");
                        SessionState.EraseBool(ReloadEnvironment);
                        SessionState.EraseBool(ReloadEnvironment + ".HadPath");
                        SessionState.EraseString(ReloadEnvironment + ".Path");
                    }
                }
            }
        }

        private sealed class FailingTextureSource : ITextureDataSource
        {
            public int Width => 4;

            public int Height => 4;

            public int MipCount => 1;

            public TextureDataFormat Format => TextureDataFormat.Rgba32;

            public bool HasTransparency => false;

            public ArraySegment<byte> GetMipData(int mip) => throw new System.IO.IOException("Injected texture read failure");

            public void CopyMipToRgba32(int mip, byte[] destination) => throw new NotSupportedException();

            public ITexturePixelReader CreatePixelReader() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }

        [Test]
        public void TextureReadFailureReleasesNativeTextureImmediately()
        {
            int before = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            using var source = new FailingTextureSource();
            Assert.Throws<System.IO.IOException>(() => new UnityTextureFactory().CreateTexture(source, TextureCreateOptions.Readable));
            Assert.That(Resources.FindObjectsOfTypeAll<Texture2D>().Length, Is.EqualTo(before));
        }

        internal static BattleScenario Scenario()
        {
            return new BattleScenario
            {
                Scene = "synthetic",
                Participants =
                {
                    new BattleParticipant { ActorId = 101, Side = BattleSide.Ally, Attributes = new[] { 100, 50, 60, 20, 10, 5, 2, 0, 0, 0, 0, 0 }, Vitals = new[] { 80, 40, 30 } },
                    new BattleParticipant { ActorId = 701, Side = BattleSide.Enemy },
                },
                Items = { new BattleItem { Id = 8001, Count = 2 } },
            };
        }

        [Test]
        public void CapturingAndRestartingDoNotShareWritableState()
        {
            BattleScenario draft = Scenario();
            draft.Participants[0].Skills.Add(new BattleSkill { Id = 2001, Level = 2 });
            draft.Participants[0].Statuses.Add(new BattleStatus { Id = 0, Rounds = 3 });
            var setup = new BattleSetup(draft);
            draft.Participants[0].Attributes[0] = 999;
            draft.Items[0].Count = 999;
            BattleScenario first = setup.CreateInstance();
            first.Participants[0].Attributes[0] = 1;
            first.Participants[0].Vitals[0] = 1;
            first.Participants[0].Equipment[0] = 42;
            first.Participants[0].Skills[0].Level = 9;
            first.Participants[0].Statuses[0].Rounds = 1;
            first.Items[0].Count = 1;
            first.Camera.FieldOfView = 90;
            BattleScenario second = setup.CreateInstance();
            Assert.That(second.Participants[0].Attributes[0], Is.EqualTo(100));
            Assert.That(second.Participants[0].Vitals[0], Is.EqualTo(80));
            Assert.That(second.Participants[0].Equipment[0], Is.Zero);
            Assert.That(second.Participants[0].Skills[0].Level, Is.EqualTo(2));
            Assert.That(second.Participants[0].Statuses[0].Rounds, Is.EqualTo(3));
            Assert.That(second.Items[0].Count, Is.EqualTo(2));
            Assert.That(second.Camera.FieldOfView, Is.EqualTo(39));
        }

        [Test]
        public void SameSpeciesNeedsDifferentInstanceIdsAndSlots()
        {
            BattleScenario scenario = Scenario();
            BattleParticipant enemy = scenario.Participants[1].Copy();
            scenario.Participants.Add(enemy);
            Assert.That(scenario.Validate().Any(error => error.Contains("instance ID")), Is.True);
            Assert.That(scenario.Validate().Any(error => error.Contains("occupies")), Is.True);
            enemy.InstanceId = Guid.NewGuid().ToString("N");
            enemy.Slot = BattleSlot.Fire;
            Assert.That(scenario.Validate(), Is.Empty);
            enemy.Attributes[0] = 100;
            Assert.That(scenario.Participants[1].Attributes[0], Is.Zero);
        }

        [Test]
        public void InvalidInitialValuesAreRejectedBeforeStarting()
        {
            BattleScenario scenario = Scenario();
            scenario.Version++;
            scenario.Participants[0].Vitals[0] = 101;
            scenario.Participants[1].Position.X = float.NaN;
            scenario.Camera.FieldOfView = float.PositiveInfinity;
            scenario.Items.Add(new BattleItem { Id = 8001, Count = 3 });
            Assert.That(scenario.Validate().Count, Is.GreaterThanOrEqualTo(5));
            Assert.Throws<ArgumentException>(() => new BattleSetup(scenario));
            scenario.Participants = null;
            Assert.That(scenario.Validate(), Is.Not.Empty);
        }

        [Test]
        public void CoincidentCameraOverrideIsRejected()
        {
            BattleScenario scenario = Scenario();
            scenario.Camera.Override = true;
            Assert.That(scenario.Validate().Any(error => error.Contains("must differ")), Is.True);
        }

        [Test]
        public void LoadingFutureSchemaKeepsDraftAndSavingCannotOverwriteIt()
        {
            var window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            var future = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            try
            {
                var draft = (BattleScenarioAsset)typeof(BattleEditorWindow).GetField("_draft", Private).GetValue(window);
                string before = JsonUtility.ToJson(draft.Scenario);
                future.Scenario.Version = BattleScenario.CurrentVersion + 1;
                future.Scenario.Variant = BattleData.Variant;
                var error = Assert.Throws<TargetInvocationException>(() => typeof(BattleEditorWindow).GetMethod("LoadAsset", Private).Invoke(window, new object[] { future }));
                Assert.That(error.InnerException, Is.TypeOf<System.IO.InvalidDataException>());
                Assert.That(JsonUtility.ToJson(draft.Scenario), Is.EqualTo(before));
                draft.Scenario.Version++;
                error = Assert.Throws<TargetInvocationException>(() => typeof(BattleEditorWindow).GetMethod("SaveScenario", Private).Invoke(window, new object[] { false }));
                Assert.That(error.InnerException, Is.TypeOf<System.IO.InvalidDataException>());
            }
            finally
            {
                window.DiscardChanges();
                Object.DestroyImmediate(window);
                Object.DestroyImmediate(future);
            }
        }

        [Test]
        public void UndoAfterSaveMarksDraftDirtyWithoutUndoingSavedAsset()
        {
            string path = "Assets/__BattleSaveTest_" + Guid.NewGuid().ToString("N") + ".asset";
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            asset.Scenario.Variant = BattleData.Variant;
            AssetDatabase.CreateAsset(asset, path);
            var window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            try
            {
                typeof(BattleEditorWindow).GetMethod("LoadAsset", Private).Invoke(window, new object[] { asset });
                var draft = (BattleScenarioAsset)typeof(BattleEditorWindow).GetField("_draft", Private).GetValue(window);
                Undo.RecordObject(draft, "Change local money");
                draft.Scenario.Money = 321;
                Undo.FlushUndoRecordObjects();
                window.SaveChanges();
                Assert.That(asset.Scenario.Money, Is.EqualTo(321));
                Assert.That(window.hasUnsavedChanges, Is.False);
                Undo.PerformUndo();
                Assert.That(asset.Scenario.Money, Is.EqualTo(321));
                Assert.That(draft.Scenario.Money, Is.Zero);
                Assert.That(window.hasUnsavedChanges, Is.True);
            }
            finally
            {
                window.DiscardChanges();
                Object.DestroyImmediate(window);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void PresetRoundTripPreservesNestedInputsAndDoesNotAliasDraft()
        {
            string path = "Assets/__BattleScenarioTest_" + Guid.NewGuid().ToString("N") + ".asset";
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            try
            {
                BattleScenario draft = Scenario();
                draft.Participants[0].Skills.Add(new BattleSkill { Id = 2, Level = 3, Uses = 8, Mastered = true });
                draft.Participants[0].Statuses.Add(new BattleStatus { Id = 3, Rounds = 4 });
                asset.Scenario = draft.Copy();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<BattleScenarioAsset>(path);
                Assert.That(JsonUtility.ToJson(loaded.Scenario), Is.EqualTo(JsonUtility.ToJson(draft)));
                Assert.That(loaded.Scenario.Participants.All(actor => actor.MagicSword == null), Is.True);
                draft.Participants[0].Skills[0].Uses = 99;
                Assert.That(loaded.Scenario.Participants[0].Skills[0].Uses, Is.EqualTo(8));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void OptionalSwordSnapshotsSurviveJsonAndActualAssetReload()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Equipment[0] = BattleMagicSwordRules.WeaponId;
            scenario.Participants[0].MagicSword = new BattleMagicSword { AttackBonus = 77, Energy = 132.5f };
            BattleScenario copy = BattleScenarioJson.Deserialize(BattleScenarioJson.Serialize(scenario));
            Assert.That(copy.Participants[1].MagicSword, Is.Null);
            Assert.That(copy.Participants[0].MagicSword.AttackBonus, Is.EqualTo(77));
            Assert.That(copy.Participants[0].MagicSword.Energy, Is.EqualTo(132.5f));
            string path = "Assets/__BattleScenarioTest_" + Guid.NewGuid().ToString("N") + ".asset";
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            try
            {
                asset.Scenario = scenario;
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                asset = AssetDatabase.LoadAssetAtPath<BattleScenarioAsset>(path);
                Assert.That(asset.Scenario.Participants[1].MagicSword, Is.Null);
                Assert.That(asset.Scenario.Participants[0].MagicSword.AttackBonus, Is.EqualTo(77));
                Assert.That(asset.Scenario.Participants[0].MagicSword.Energy, Is.EqualTo(132.5f));
                Undo.RecordObject(asset, "Remove sword snapshot");
                asset.Scenario.Participants[0].MagicSword = null;
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(asset.Scenario.Participants[0].MagicSword.Energy, Is.EqualTo(132.5f));
                Undo.PerformRedo();
                Assert.That(asset.Scenario.Participants[0].MagicSword, Is.Null);
            }
            finally
            {
                Undo.ClearUndo(asset);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [UnityTest]
        public IEnumerator UndoAndRealAssemblyReloadPreserveUnsavedDraft()
        {
            if (!SessionState.GetBool(ReloadWindow, false))
            {
                IsolateReloadSource();
                var window = ScriptableObject.CreateInstance<BattleEditorWindow>();
                window.name = ReloadWindow;
                var draft = (BattleScenarioAsset)typeof(BattleEditorWindow).GetField("_draft", Private).GetValue(window);
                Undo.RecordObject(draft, "Test battle draft");
                draft.Scenario.Money = 123;
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(draft.Scenario.Money, Is.Zero);
                Undo.PerformRedo();
                Assert.That(draft.Scenario.Money, Is.EqualTo(123));
                SessionState.SetString(ReloadWindow + ".WindowId", window.GetEntityId().ToString());
                SessionState.SetString(ReloadWindow + ".DraftId", draft.GetEntityId().ToString());
                SessionState.SetBool(ReloadWindow, true);
                EditorUtility.RequestScriptReload();
            }
            yield return new WaitForDomainReload();
            var restored = Resources.FindObjectsOfTypeAll<BattleEditorWindow>().Single(window => window.name == ReloadWindow);
            var restoredDraft = (BattleScenarioAsset)typeof(BattleEditorWindow).GetField("_draft", Private).GetValue(restored);
            Assert.That(restored.GetEntityId().ToString(), Is.EqualTo(SessionState.GetString(ReloadWindow + ".WindowId", "")));
            Assert.That(restoredDraft.GetEntityId().ToString(), Is.EqualTo(SessionState.GetString(ReloadWindow + ".DraftId", "")));
            Assert.That(typeof(EditorWindow).GetField("m_Parent", Private).GetValue(restored), Is.Null);
            var session = (BattleEditorSession)typeof(BattleEditorWindow).GetField("_session", Private).GetValue(restored);
            Assert.That(session.Data, Is.Null, "Draft persistence must not load user game data.");
            Assert.That(restoredDraft.Scenario.Money, Is.EqualTo(123));
            Assert.That(restored.hasUnsavedChanges, Is.True);
            restored.DiscardChanges();
            Assert.That(restored.hasUnsavedChanges, Is.False);
            Object.DestroyImmediate(restored);
            Assert.That(restored == null, Is.True);
            Assert.That(restoredDraft == null, Is.True);
        }
    }
}
