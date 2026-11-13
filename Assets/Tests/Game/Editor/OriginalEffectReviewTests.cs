// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Core.Command.SceCommands;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Data;
    using Core.FileSystem;
    using Core.Primitives;
    using Core.Utilities;
    using Effect;
    using Effect.Original;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using NUnit.Framework;
    using Scene;
    using Settings;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class OriginalEffectReviewTests
    {
        private static OriginalEffectResources Open(string variant)
        {
            return new OriginalEffectResources(OpenFileSystem(variant));
        }

        private static CpkFileSystem OpenFileSystem(string variant)
        {
            string path = Environment.GetEnvironmentVariable(variant + "_TEST_DATA_ROOT") ?? Path.GetFullPath("../Game Data/" + variant);
            if (!Directory.Exists(path))
                Assert.Ignore("Local game data is unavailable: " + variant);
            var crc = new Crc32Hash();
            crc.Init();
            var fs = new CpkFileSystem(path, crc, 936);
            fs.Mount("basedata/basedata.cpk", 936);
            return fs;
        }

        private sealed class EffectScene : IDisposable
        {
            public readonly GameObject Root = new("Effect position regression");
            public readonly GameScene Scene;
            public readonly EffectManager Manager;
            public readonly OriginalEffectResources Resources;
            public readonly Dictionary<int, GameObject> Prefabs;
            private readonly IDisposable _vfx;
            private readonly GameSettings _settings;
            private readonly int _sleepTimeout = Screen.sleepTimeout;

            public EffectScene()
            {
#if PAL3
                var fs = OpenFileSystem("PAL3");
#else
                var fs = OpenFileSystem("PAL3A");
#endif
                _settings = new GameSettings(new InMemoryKeyValueStore());
                Root.SetActive(false);
                Scene = Root.AddComponent<GameScene>();
                Type type = typeof(OriginalEffectPlayer).Assembly.GetType("Pal3.Game.Effect.OptionalVfxPlayer");
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                _vfx = (IDisposable)Activator.CreateInstance(type, flags, null, new object[] { _settings, fs, 936 }, null);
                Resources = (OriginalEffectResources)type.GetField("_original", flags).GetValue(_vfx);
                Prefabs = (Dictionary<int, GameObject>)type.GetField("_prefabs", flags).GetValue(_vfx);
                var scenes = (SceneManager)FormatterServices.GetUninitializedObject(typeof(SceneManager));
                typeof(SceneManager).GetField("_currentGameScene", flags).SetValue(scenes, Scene);
                typeof(SceneManager).GetField("_currentSceneRoot", flags).SetValue(scenes, new GameEntity(Root));
                Manager = (EffectManager)FormatterServices.GetUninitializedObject(typeof(EffectManager));
                typeof(EffectManager).GetField("_sceneManager", flags).SetValue(Manager, scenes);
                typeof(EffectManager).GetField("_optionalVfx", flags).SetValue(Manager, _vfx);
            }

            public void EnablePrivateFallback() => _settings.OptionalVfxEnabled = true;

            public OriginalEffectPlayer PlayOriginal(int group = 15)
            {
                // These tests exercise command dispatch and placement, including
                // PAL3A's shape-change IDs, without requiring their SPar renderer.
                Resources.Definitions.Groups[group] = new EffectGroup();
                Manager.Execute(new EffectPlayCommand(group));
                OriginalEffectPlayer[] players = Root.GetComponentsInChildren<OriginalEffectPlayer>(true);
                Assert.That(players, Has.Length.EqualTo(1), "Missing actors must not suppress native playback");
                Assert.That(players[0].Error, Is.Null);
                return players[0];
            }

            public void Dispose()
            {
                _vfx.Dispose();
                UnityEngine.Object.DestroyImmediate(Root);
                _settings.Dispose();
                Screen.sleepTimeout = _sleepTimeout;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingActorKeepsThePreviousEffectPosition(bool setPosition)
        {
            using var scene = new EffectScene();
            Vector3 expected = Vector3.zero;
            if (setPosition)
            {
                scene.Manager.Execute(new EffectSetPositionCommand(80, 120, -160));
                expected = new GameBoxVector3(80, 120, -160).ToUnityPosition();
            }
            scene.Manager.Execute(new EffectAttachToActorCommand(32767));
            Assert.That(scene.PlayOriginal().transform.localPosition, Is.EqualTo(expected));
        }

        [Test]
        public void RemovedActorDoesNotInvalidateItsCapturedEffectPosition()
        {
            using var scene = new EffectScene();
            scene.Root.transform.position = new Vector3(7, 0, 4);
            scene.Root.transform.rotation = Quaternion.Euler(0, 25, 0);
            var actor = new GameObject("Effect snapshot actor");
            actor.transform.SetParent(scene.Root.transform, false);
            actor.transform.localPosition = new Vector3(2, 3, -4);
            scene.Scene.GetAllActorGameEntities()[7] = new GameEntity(actor);
            scene.Manager.Execute(new EffectAttachToActorCommand(7));
            Vector3 expected = actor.transform.localPosition + Vector3.up * 0.1f;
            UnityEngine.Object.DestroyImmediate(actor);
            Assert.That(Vector3.Distance(scene.PlayOriginal().transform.localPosition, expected), Is.LessThan(0.00001f));
        }

        [TestCase(164)]
        [TestCase(315)]
        public void MissingActorSkipsShapeChangesWithoutSuppressingOriginalEffects(int group)
        {
            using var scene = new EffectScene();
            scene.Manager.Execute(new EffectSetPositionCommand(80, 120, -160));
            scene.Manager.Execute(new EffectAttachToActorCommand(32767));
            Vector3 expected = new GameBoxVector3(80, 120, -160).ToUnityPosition();
            Assert.That(scene.PlayOriginal(group).transform.localPosition, Is.EqualTo(expected));
        }

        [Test]
        public void MissingActorDoesNotReparentPrivateFallbackToTheScene()
        {
            using var scene = new EffectScene();
            scene.EnablePrivateFallback();
            var prefab = new GameObject("Missing actor private fallback");
            try
            {
                scene.Prefabs[-2] = prefab;
                scene.Manager.Execute(new EffectSetPositionCommand(80, 120, -160));
                scene.Manager.Execute(new EffectAttachToActorCommand(32767));
                scene.Manager.Execute(new EffectPlayCommand(-2));
                Assert.That(scene.Root.transform.childCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        private static OriginalEffectObjectOverride Object(OriginalEffectResources resources, int group, string kind)
        {
            for (int index = 0; index < resources.Definitions.Groups[group].Objects.Length; index++)
            {
                EffectGroupObject obj = resources.Definitions.Groups[group].Objects[index];
                resources.Definitions.TryGetType(obj.TypeCrc, out var definition);
                if (EffectClass.Name(definition.ClassNameCrc) == kind)
                    return new OriginalEffectObjectOverride { GroupId = group, ObjectIndex = index, Definition = definition, Tasks = obj.Tasks };
            }
            throw new InvalidOperationException($"Group {group} has no {kind}");
        }

        private static void HideOtherObjects(OriginalEffectResources resources, OriginalEffectOverrides edits, OriginalEffectObjectOverride selected)
        {
            edits.Objects.Add(selected);
            for (int i = 0; i < resources.Definitions.Groups[selected.GroupId].Objects.Length; i++)
                if (i != selected.ObjectIndex)
                    edits.Objects.Add(new OriginalEffectObjectOverride { GroupId = selected.GroupId, ObjectIndex = i, Hidden = true });
        }

        [TestCase(".cvd")]
        [TestCase(".unsupported")]
        public void ModelDiagnosisRejectsMissingAndUnknownFormats(string extension)
        {
            using var resources = Open("PAL3");
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            try
            {
                var edit = Object(resources, 173, "model");
                edit.Definition.TextureName = "effect\\missing-review-model" + extension;
                edits.Objects.Add(edit);
                Assert.That(resources.Diagnose(173, edits).Any(e => e.Contains(edit.Definition.TextureName)), Is.True);
                Assert.That(resources.Diagnose(173), Is.Empty, "Override failures must not poison the original cache");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void CvdDiagnosisIncludesNestedModelTextures()
        {
            using var resources = Open("PAL3");
            var d = Object(resources, 173, "model").Definition;
            var node = Geometries(resources.Model(d.TextureName).RootNodes).First();
            node.Mesh.MeshSections[0].Material.TextureFileNames[0] = "missing-review-nested-texture.tga";
            Assert.That(resources.Diagnose(173).Any(e => e.Contains("missing-review-nested-texture.tga")), Is.True);
        }

        private static IEnumerable<CvdGeometryNode> Geometries(IEnumerable<CvdGeometryNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsGeometryNode)
                    yield return node;
                foreach (var child in Geometries(node.Children))
                    yield return child;
            }
        }

        [Test]
        public void PolDiagnosisAllowsAnEmptyTextureSlot()
        {
            using var resources = Open("PAL3A");
            var d = Object(resources, 157, "model").Definition;
            resources.PolModel(d.TextureName).Meshes[0].Textures[0].Material.TextureFileNames[0] = "";
            Assert.That(resources.Diagnose(157), Is.Empty);
        }

        [Test]
        public void LivePolModelRetainsItsAuxiliaryTextureWhenResourcesAreTrimmed()
        {
            using var resources = Open("PAL3A");
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("POL lightmap lifetime");
            try
            {
                HideOtherObjects(resources, edits, Object(resources, 157, "model"));
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 157, overrides: edits);
                player.Advance(0.5f);
                player.Render(null);
                Assert.That(player.Error, Is.Null);
                var renderers = root.GetComponentsInChildren<Renderer>();
                Assert.That(renderers, Is.Not.Empty);
                Texture main = renderers[0].sharedMaterial.mainTexture;
                Texture lightmap = resources.Texture("effect\\q08qiao.tga");
                foreach (var renderer in renderers)
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Assert.That(material.HasProperty("_ShadowTex"), Is.True);
                        material.SetTexture("_ShadowTex", lightmap);
                        material.SetFloat("_HasShadowTex", 1);
                    }
                resources.TrimUnusedRenderAssets(new[] { root });
                Assert.That(main != null, Is.True, "Live model main texture was released");
                Assert.That(lightmap != null, Is.True, "Live model auxiliary texture was released");
                UnityEngine.Object.DestroyImmediate(root);
                resources.TrimUnusedRenderAssets(Array.Empty<GameObject>());
                Assert.That(main == null && lightmap == null, Is.True, "Unreferenced model textures were retained");
            }
            finally
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void ExplosiveSubdivisionsUseTheOriginalFloatToIntTruncation()
        {
            using var resources = Open("PAL3A");
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Explosive subdivisions");
            try
            {
                var edit = Object(resources, 173, "explosive");
                edit.Definition.RotationAngle = 1.9f;
                edit.Definition.DivisionCount = 2.9f;
                edit.Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 1 } };
                HideOtherObjects(resources, edits, edit);
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 173, overrides: edits);
                player.AdvanceFrame(0.1f);
                player.Render(null);
                Assert.That(player.Error, Is.Null);
                Assert.That(player.QuadCount, Is.EqualTo(2), "C++ truncates 1.9 x 2.9 to 1 x 2");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }
    }
}
